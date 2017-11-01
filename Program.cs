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
            var currentdir = System.IO.Directory.GetCurrentDirectory();
            ThreadPool.SetMinThreads(100, 4);
            ServicePointManager.DefaultConnectionLimit = 100; //(Or More)
            string argument = args.ToString();
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
                        Console.WriteLine("test");
                        DownloadFilesAsync(args).Wait();
                        break;
                    }
            }
          
            //    CreateFiles();
     
            Console.WriteLine("Application complete. After you press any key the container and blobs will be deleted.");
            Console.Read();
            Util.DeleteExistingContainersAsync().Wait();
        }

        private static async Task UploadFilesAsync(string[] args)
        {
            CloudBlobContainer[] containers = Util.GetRandomContainers();

            // path to the directory to upload
            string uploadPath = "d:\\perffiles";


            Stopwatch time = Stopwatch.StartNew();
            try
            {

                Console.WriteLine("Iterating in directiory: {0}", uploadPath);

                // Seed the Random value using the Ticks representing current time and date
                // Since int is used as seen we cast (loss of long data)


                int count = 0;
                int max_outstanding = 100;
                int completed_count = 0;
                Semaphore sem = new Semaphore(max_outstanding, max_outstanding);
                List<Task> Tasks = new List<Task>();
                Console.WriteLine("Found {0} file(s)", Directory.GetFiles(uploadPath).Count());
                foreach (string fileName in Directory.GetFiles(uploadPath))
                {

                    // Console.WriteLine("Starting {0}", fileName);
                    var container = containers[count % 5];
                    Random r = new Random((int)DateTime.Now.Ticks);
                    String s = (r.Next() % 10000).ToString("X5");
                    Console.WriteLine("Starting upload of {0} as {1} to container {2}.", fileName, s, container.Name);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(s);
                    blockBlob.StreamWriteSizeInBytes = 100 * 1024 * 1024;
                    sem.WaitOne();
                    Tasks.Add(blockBlob.UploadFromFileAsync(fileName, null, new BlobRequestOptions() { ParallelOperationThreadCount = 8, DisableContentMD5Validation = true, StoreBlobContentMD5 = false }, null).ContinueWith((t) =>
                    {
                        sem.Release();
                        Interlocked.Increment(ref completed_count);
                    }));
                    count++;
                }

                await Task.WhenAll(Tasks);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {

            }
            time.Stop();

            Console.WriteLine("Upload has been completed in {0} seconds. Press any key to continue", time.Elapsed.TotalSeconds.ToString());

            Console.ReadLine();
        }

        private static async Task DownloadFilesAsync(string[] args)
        {
            List<CloudBlobContainer> containers = await Util.ListContainers();
            var directory = Directory.CreateDirectory("download");
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            Stopwatch time = Stopwatch.StartNew();
            // download thee blob
            try
            {
                List<Task> Tasks = new List<Task>();
                foreach (CloudBlobContainer container in containers)
                {
                    do
                    {
                        resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                        {
                            foreach (var blobItem in resultSegment.Results)
                            {
                                CloudBlockBlob blockBlob = container.GetBlockBlobReference(((CloudBlockBlob)blobItem).Name);
                                Console.WriteLine("Starting download of {0} from container {1}", blockBlob.Name, container.Name);
                                Tasks.Add(blockBlob.DownloadToFileAsync(blockBlob.Name, FileMode.Create, null, new BlobRequestOptions() { DisableContentMD5Validation = true, StoreBlobContentMD5 = false }, null));
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
            Console.WriteLine("Download has been completed in {0} seconds.", time.Elapsed.TotalSeconds.ToString());
        }
    }
}
