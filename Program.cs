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

    class Program
    {

        static void Main(string[] args)
        {
            string argument = "";
            var currentdir = System.IO.Directory.GetCurrentDirectory();

            // Set threading and default connection limit to 100 to ensure multiple threads and connections can be opened.  This is in addition to parallelism with the storage client library.
            ThreadPool.SetMinThreads(100, 4);
            ServicePointManager.DefaultConnectionLimit = 100; //(Or More)

            // Allow the user to pass arguments for upload, download, or both to allow for flexibility in the application.
            if (args.Length > 0)
            {
                argument = args[0].ToString();
            }
            switch (argument)
            {
                case "upload":
                    {
                        UploadFilesAsync(args).Wait();
                        break;
                    }
                case "download":
                    {
                        DownloadFilesAsync(args).Wait();
                        break;
                    }
                default:
                    {
                        UploadFilesAsync(args).Wait();
                        
                        DownloadFilesAsync(args).Wait();
                        break;
                    }
            }
            Console.WriteLine("Application complete. After you press any key the container and blobs will be deleted.");
            Console.ReadKey();
            Util.DeleteExistingContainersAsync().Wait();
        }

        private static async Task UploadFilesAsync(string[] args)
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
                // Wait for all the asynchronous uploads are complete.
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

        private static async Task DownloadFilesAsync(string[] args)
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
