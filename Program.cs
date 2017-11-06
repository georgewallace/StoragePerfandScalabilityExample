namespace AzPerf
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System.Threading.Tasks;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using StoragePerfandScalabilityExample;

    /// <summary>
    /// Azure Storage Performance and Scalability Sample - Demonstrate how to use use parallelism with. 
    /// Azure blob storage in conjunction with large block sizes to transfer larges amount of data 
    /// effectiviely and efficiently.
    ///
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - https://docs.microsoft.com/azure/storage/common/storage-create-storage-account
    /// - Getting Started with Blobs - https://docs.microsoft.com/azure/storage/blobs/storage-dotnet-how-to-use-blobs
    /// - Blob Service Concepts - https://docs.microsoft.com/rest/api/storageservices/Blob-Service-Concepts
    /// - Blob Service REST API - https://docs.microsoft.com/rest/api/storageservices/Blob-Service-REST-API
    /// - Blob Service C# API - https://docs.microsoft.com/dotnet/api/overview/azure/storage?view=azure-dotnet
    /// - Scalability and performance targets - https://docs.microsoft.com/azure/storage/common/storage-scalability-targets
    ///   Azure Storage Performance and Scalability checklist https://docs.microsoft.com/azure/storage/common/storage-performance-checklist
    /// - Storage Emulator - https://docs.microsoft.com/azure/storage/common/storage-use-emulator
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/library/hh191443.aspx
    /// </summary>

    class Program
    {

        static void Main(string[] args)
        {
            // Set threading and default connection limit to 100 to ensure multiple threads and connections can be opened.
            // This is in addition to parallelism with the storage client library that is defined in the functions below.
            ThreadPool.SetMinThreads(100, 4);
            ServicePointManager.DefaultConnectionLimit = 100; //(Or More)

            // Call the UploadFilesAsync function.
            UploadFilesAsync().Wait();
            // Uncomment the following line to enable downloading of files from the storage account.  This is commented out
            // initially to support the tutorial at http://inserturlhere.
            //DownloadFilesAsync().Wait();
            Console.WriteLine("Application complete. After you press any key the container and blobs will be deleted.");
            Console.ReadKey();
            // The following function will delete the container and all files contained in them.  This is commented out initialy
            // As the tutorial at http://inserturlhere has you upload only for one tutorial and download for the other. 
            //Util.DeleteExistingContainersAsync().Wait();
        }

        private static async Task UploadFilesAsync()
        {
            // Create random 5 characters containers to upload files to.
            CloudBlobContainer[] containers = Util.GetRandomContainers();
            var currentdir = System.IO.Directory.GetCurrentDirectory();
            // path to the directory to upload
            string uploadPath = currentdir + "\\upload";
            Stopwatch time = Stopwatch.StartNew();
            try
            {
                Console.WriteLine("Iterating in directiory: {0}", uploadPath);
                int count = 0;
                List<Task> Tasks = new List<Task>();
                Console.WriteLine("Found {0} file(s)", Directory.GetFiles(uploadPath).Count());

                // Iterate through the files
                foreach (string fileName in Directory.GetFiles(uploadPath))
                {
                    // Create random file names and set the block size that is used for the upload.
                    var container = containers[count % 5];
                    Random r = new Random((int)DateTime.Now.Ticks);
                    String s = (r.Next() % 10000).ToString("X5");
                    Console.WriteLine("Starting upload of {0} as {1} to container {2}.", fileName, s, container.Name);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(s);
                    blockBlob.StreamWriteSizeInBytes = 100 * 1024 * 1024;

                    // Create tasks for each file that is uploaded. This is added to a collection that executes them all asyncronously.  Defined the BlobRequestionOptions on the upload.
                    // This includes defining an exponential retry policy to ensure that failed connections are retried with a backoff policy. As multiple large files are being uploaded
                    // large block sizes this can cause an issue if an exponential retry policy is not defined.  Additionally parallel operations are enabled with a thread count of 8
                    // This could be should be multiple of the number of cores that the machine has. Lastly MD5 hash validation is disabled, this imroves the upload speed.
                    Tasks.Add(blockBlob.UploadFromFileAsync(fileName, null, new BlobRequestOptions() {
                            RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 10),
                            ParallelOperationThreadCount = 8,
                            DisableContentMD5Validation = true,
                            StoreBlobContentMD5 = false }, null));
                    count++;
                }
                // Creates an asynchonous task that completes when all the uploads complete.
                await Task.WhenAll(Tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            time.Stop();

            Console.WriteLine("Upload has been completed in {0} seconds. Press any key to continue", time.Elapsed.TotalSeconds.ToString());

            Console.ReadLine();
        }

        private static async Task DownloadFilesAsync()
        {
            // Retrieve the list of containers in the storage account.  Create a directory and configure variables for use later.
            List<CloudBlobContainer> containers = await Util.ListContainers();
            var directory = Directory.CreateDirectory("download");
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            Stopwatch time = Stopwatch.StartNew();
            // download thee blob
            try
            {
                List<Task> Tasks = new List<Task>();
                // Iterate throung the containers
                foreach (CloudBlobContainer container in containers)
                {
                    do
                    {
                        // Return the blobs from the container lazily 10 at a time.
                        resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                        {
                            foreach (var blobItem in resultSegment.Results)
                            {
                                // Get the blob and add a task to download the blob asynchronously from the storage account.
                                CloudBlockBlob blockBlob = container.GetBlockBlobReference(((CloudBlockBlob)blobItem).Name);
                                Console.WriteLine("Starting download of {0} from container {1}", blockBlob.Name, container.Name);
                                Tasks.Add(blockBlob.DownloadToFileAsync(directory.FullName + "\\" + blockBlob.Name, FileMode.Create, null, new BlobRequestOptions() { DisableContentMD5Validation = true, StoreBlobContentMD5 = false }, null));
                            }
                        }
                    }
                    while (continuationToken != null);
                }
                // Creates an asynchonous task that completes when all the downloads complete.
                await Task.WhenAll(Tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);
            }
            time.Stop();
            Console.WriteLine("Download has been completed in {0} seconds. Press any key to continue", time.Elapsed.TotalSeconds.ToString());
            Console.ReadLine();
        }
    }
}
