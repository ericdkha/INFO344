using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.IO;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WebRole1
{

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private static Trie titles = new Trie();
        private static string filePath;
        private PerformanceCounter memory = new PerformanceCounter("Memory", "Available MBytes");

        public float getMemory()
        {
            float availableMem = memory.NextValue();
            return availableMem;
        }

        // Gets the wiki file from Azure Cloud Storage, downloads it, and stores the file path to it in a variable
        [WebMethod]
        public string downloadWiki()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("ericdkha_AzureStorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("info344test");
            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        // Add code to download blob!! Google it!
                        filePath = System.IO.Path.GetTempFileName();
                        using (var fileStream = System.IO.File.OpenWrite(filePath))
                        {
                            blob.DownloadToStream(fileStream);
                        }
                    }
                }
            }
            return "Downloading Wiki was sucessful";
        }

        // Method to build the trie using the wiki data downloaded
        [WebMethod]
        public string buildTrie()
        {
            int count = 0;
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    titles.AddTitle(line);
                    count++;
                    // Checks to see if the programming has not exceeded 50 MB
                    if (count % 10000 == 0 && getMemory() < 50)
                    {
                        break;
                    }
                }
            }
            return "done:" + count.ToString() + " titles added";
        }

        // Method to search for a given string 
        [WebMethod]
        public List<string> searchFn(string input)
        {
            if (titles == null)
            {
                buildTrie();
            }
            // calls Trie's search method
            titles.SearchForPrefix(input);
            return titles.getTitles();
        }
    }
}
