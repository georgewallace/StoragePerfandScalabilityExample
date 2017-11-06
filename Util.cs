using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StoragePerfandScalabilityExample
{
    public class Util
    {
        private static CloudStorageAccount storageAccount;
        private static CloudBlobClient blobClient;
        public const string LowerCaseAlphabet = "abcdefghijklmnopqrstuvwyxz";

        // Retrieve the CloudBlobClient
        private static CloudBlobClient GetCloudBlobClient()
        {
            if (Util.blobClient == null)
            {
                Util.blobClient = GetStorageAccount().CreateCloudBlobClient();
            }

            return Util.blobClient;
        }

        // Load the connection string from the Config.json file.
        private static string LoadConnectionStringFromConfigration()
        {
            // How to create a storage connection string: http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
#if DOTNET5_4
            //For .Net Core,  will get Storage Connection string from Config.json file
            return System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"];
#else
            //For .net, will get Storage Connection string from App.Config file
            return JObject.Parse(File.ReadAllText("Config.json"))["StorageConnectionString"].ToString();
#endif
        }

        // Retrieves a storage account
        private static CloudStorageAccount GetStorageAccount()
        {
            if (Util.storageAccount == null)
            {
                string connectionString = LoadConnectionStringFromConfigration();
                Util.storageAccount = CloudStorageAccount.Parse(connectionString);
            }

            return Util.storageAccount;
        }

        // Creates containers with random names
        public static CloudBlobContainer[] GetRandomContainers()
        {
            CloudBlobClient blobClient = GetCloudBlobClient();
            IRetryPolicy exponentialRetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 10);
            blobClient.DefaultRequestOptions.RetryPolicy = exponentialRetryPolicy;
            CloudBlobContainer[] blobContainers = new CloudBlobContainer[5];
            for (int i = 0; i < blobContainers.Length; i++)
            {
                blobContainers[i] = blobClient.GetContainerReference(GenerateString(5, new Random((int)DateTime.Now.Ticks), LowerCaseAlphabet));
                Console.WriteLine("Created container {0}", blobContainers[i].Uri);
                try
                {
                    blobContainers[i].CreateIfNotExistsAsync().Wait();
                }
                catch (StorageException)
                {
                    Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                    Console.ReadLine();
                    throw;
                }
            }
            return blobContainers;
        }

        // Generate a random string of characters.
        private static string GenerateString(int size, Random rng, string alphabet)
        {
            char[] chars = new char[size];
            for (int i = 0; i < size; i++)
            {
                chars[i] = alphabet[rng.Next(alphabet.Length)];
            }
            return new string(chars);
        }

        // Lists the containers in a storage account
        public static async Task<List<CloudBlobContainer>> ListContainers()
        {
            CloudBlobClient blobClient = GetCloudBlobClient();
            BlobContinuationToken continuationToken = null;
            List<CloudBlobContainer> containers = new List<CloudBlobContainer>();
            do
            {
                var listingResult = await blobClient.ListContainersSegmentedAsync(continuationToken);
                continuationToken = listingResult.ContinuationToken;
                containers.AddRange(listingResult.Results);
            }
            while (continuationToken != null);
            return containers;
        }

        // Deletes the containers in a storage account.
        public static async Task DeleteExistingContainersAsync()
        {
            CloudBlobClient client = Util.GetCloudBlobClient();
            List<CloudBlobContainer> containers = await Util.ListContainers();
            foreach(CloudBlobContainer container in containers)
            { 
            await container.DeleteIfExistsAsync();
            }
        }
    }
}
