using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;


namespace WebRole1
{

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class wiki : WebService
    {
        private static Trie titles = new Trie();
        private static string filePath;
        private static CloudQueue commandQueue;
        private static CloudQueue urlQueue;
        private static CloudTable statsTable;
        private static CloudTable urlTable;
        private static CloudTable trieTable;
        private static Dictionary<string, List<UrlInfo>> cache = new Dictionary<string, List<UrlInfo>>();


        public float getMemory()
        {
            PerformanceCounter memory = new PerformanceCounter("Memory", "Available MBytes");
            float availableMem = memory.NextValue();
            return availableMem;
        }

        // Gets the wiki file from Azure Cloud Storage, downloads it, and stores the file path to it in a variable
        [WebMethod]
        public string downloadWiki()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
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
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
    ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            trieTable = tableClient.GetTableReference("triestattable");
            trieTable.CreateIfNotExists();

            int count = 0;
            string lastLine = "";
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    titles.AddTitle(line);
                    count++;
                    lastLine = line;
                    // Checks to see if the programming has not exceeded 50 MB
                    if (count % 10000 == 0 && getMemory() < 50)
                    {
                        break;
                    }
                }
            }
            TableOperation insertTrieStat = TableOperation.InsertOrReplace(new TrieStat("trie", lastLine, count.ToString()));
            trieTable.Execute(insertTrieStat);
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

        // Helper Function that connects to all Azure's Cloud services
        private void cloudInitialization()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            try
            {
                commandQueue = queueClient.GetQueueReference("crawlercommandqueue");
                commandQueue.CreateIfNotExists();

                urlQueue = queueClient.GetQueueReference("crawlerurlqueue");
                urlQueue.CreateIfNotExists();

                urlTable = tableClient.GetTableReference("crawlerurltable");
                urlTable.CreateIfNotExists();

                statsTable = tableClient.GetTableReference("crawlerstatstable");
                statsTable.CreateIfNotExists();
            }
            catch // If an error occurs, wait 5 seconds and attemt to connect to the cloud services again
            {
                System.Threading.Thread.Sleep(5000);
                cloudInitialization();
            }

        }

        // Function that adds a start message to the command queue for the crawler to read
        [WebMethod]
        public void StartCrawling()
        {
            cloudInitialization();
            commandQueue.Clear();

            CloudQueueMessage start = new CloudQueueMessage("start");
            commandQueue.AddMessage(start);
        }

        // Function that adds a stop message to the command queue for the crawler to read
        [WebMethod]
        public void StopCrawling()
        {
            cloudInitialization();
            commandQueue.Clear();

            CloudQueueMessage stop = new CloudQueueMessage("stop");
            commandQueue.AddMessage(stop);
        }

        // Function that stops the cralwer adds a clear message to the command queue for the crawler to read
        [WebMethod]
        public void ClearIndex()
        {
            StopCrawling();
            commandQueue.Clear();
            CloudQueueMessage clear = new CloudQueueMessage("clear");
            commandQueue.AddMessage(clear);
        }

        // Function that clears the current cache of keywords to urls
        [WebMethod]
        public void ClearCache()
        {
            if (cache.Count() > 1)
            {
                cache.Clear();
            }
        }

        // Function that makes a query to the stats table to retrieve stat information about the crawler and returns a list of those stats
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetDashboard()
        {
            List<string> result = new List<string>();
            cloudInitialization();

            TableQuery<DashInfo> query = new TableQuery<DashInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));

            foreach (DashInfo entity in statsTable.ExecuteQuery(query))
            {
                //result.Add(entity.cpu.ToString());
                //result.Add(entity.ram.ToString());
                result.Add(entity.totalCrawled.ToString());
                result.Add(entity.queueCount.ToString());
                result.Add(entity.indexedCount.ToString());
            }
            return result;
        }

        // Function that makes a query to the stats table to retrieve stat information about the crawler and returns a list of those stats
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> GetTrieStats()
        {
            List<string> result = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            trieTable = tableClient.GetTableReference("triestattable");
            trieTable.CreateIfNotExists();
            TableQuery<TrieStat> query = new TableQuery<TrieStat>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "trie"));

            foreach (TrieStat entity in trieTable.ExecuteQuery(query))
            {
                result.Add(entity.size.ToString());
                result.Add(entity.last);
            }
            return result;
        }

        // Function that makes a query to the stats table to retrieve the last ten urls indexed and returns them in a queue object
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> lastTen()
        {
            List<string> result = new List<string>();
            cloudInitialization();

            TableQuery<DashInfo> query = new TableQuery<DashInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
            var queryResult = statsTable.ExecuteQuery(query);
            var dash = queryResult.First();
            var lastTen = JsonConvert.DeserializeObject<List<string>>(dash.lastTen);

            if (lastTen == null)
            {
                List<string> empty = new List<string>();
                result = empty;
            }
            else
            {
                foreach (string url in lastTen)
                {
                    result.Add(url);
                }
            }
            return result;
        }

        // Function that makes a query to the stats table to retrieve the current insertion errors and returns them in a list of strings
        [WebMethod]
        public List<string> errors()
        {
            List<string> result = new List<string>();
            cloudInitialization();

            TableQuery<DashInfo> query = new TableQuery<DashInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
            var queryResult = statsTable.ExecuteQuery(query);

            var dash = queryResult.First();
            var errors = JsonConvert.DeserializeObject<List<string>>(dash.errorUrl);
            if (errors == null)
            {
                List<string> empty = new List<string>();
                result = empty;
            }
            else
            {
                result = errors;
            }
            return result;
        }

        // Function that gets the current available mb from a performance counter and returns it in a string format
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetRam()
        {
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            int ram = Convert.ToInt32(ramCounter.NextValue());

            return new JavaScriptSerializer().Serialize(ram);
        }

        // Function that gets the current cpu utilization from a performance counter and returns it in a string format
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetCpu()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            float cpuCounterValue = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            cpuCounterValue = cpuCounter.NextValue();
            //int cpu = Convert.ToInt32((cpuCounter.NextValue()));
            return new JavaScriptSerializer().Serialize(cpuCounterValue + "%");
        }

        // Function that makes a query from the url table to find the passed url, and returns the title of the page from the url
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string[]> GetPageTitle(string input)
        {
            try
            {
                // To keep track of all of the matching urls given the keyword
                List<UrlInfo> data = new List<UrlInfo>();

                // A list of ordered results to be returned to the user
                List<string[]> results = new List<string[]>();

                if (input[0] != '\0' && input[0] != ' ')
                {
                    var keywords = input.Split(' ');

                    if (cache.Count > 100)
                    {
                        cache.Clear();
                    }

                    foreach (string keyword in keywords)
                    {

                        if (cache.ContainsKey(keyword))
                        {
                            foreach (UrlInfo info in cache[keyword])
                            {
                                data.Add(info);
                            }
                        }
                        else // Current keyword is not found in the cache
                        {
                            // To keep track of Url matches to be saved into the cache
                            List<UrlInfo> forCache = new List<UrlInfo>();

                            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                                ConfigurationManager.AppSettings["StorageConnectionString"]);
                            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                            urlTable = tableClient.GetTableReference("crawlerurltable");
                            urlTable.CreateIfNotExists();

                            TableQuery<UrlInfo> query = new TableQuery<UrlInfo>()
                                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, keyword));
                            var searchResults = urlTable.ExecuteQuery(query);
                            foreach (UrlInfo matches in searchResults)
                            {
                                data.Add(matches);
                                forCache.Add(matches);
                            }

                            if (data.Count != 0 && !cache.ContainsKey(keyword))
                            {
                                // Save Url matches for current keyword into the cache
                                cache.Add(keyword, forCache);
                            }
                        }
                    }

                    //sort the results
                    var rankedResults = data
                        .GroupBy(x => x.RowKey)
                        .Select(x => new Tuple<string, int, DateTime, UrlInfo>(x.ElementAt(0).Title, x.ToList().Count, Convert.ToDateTime(x.ElementAt(0).Date), x.ElementAt(0)))
                        .OrderByDescending(x => x.Item3)
                        .OrderByDescending(x => x.Item2);

                    foreach (var result in rankedResults)
                    {
                        string[] resultInfo = new string[] { result.Item4.Title, WebUtility.UrlDecode(result.Item4.Url), (result.Item4.Date) };
                        results.Add(resultInfo);
                    }

                    return results;
                }
                else
                {
                    return results;
                }
            }
            catch (Exception error)
            {
                return null;
            }
        }
        //        if (!cache.ContainsKey(input))
        //        {
        //            try
        //            {
        //                //string encodedUrl = WebUtility.UrlEncode(url);
        //                cache.Add(input, results);
        //                return results;
        //                //var results = queryResult.First();
        //                //title = results.Title;
        //                //// Checks to see if the title was found from the passed url
        //                //if (title == null)
        //                //{
        //                //    title = "Website not found :(";
        //                //}
        //                //return new JavaScriptSerializer().Serialize(title); ;
        //            }
        //            catch (Exception error) // If an error occurs, return a message to the user
        //            {
        //                return null;
        //            }
        //        }
        //        return cache[input];
        //    }
        //    return results;
        // }


        // Function to read the command queue and returns the string of the crawler's current state
        // for the current command 
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getCommand()
        {
            cloudInitialization();
            commandQueue.FetchAttributes();
            // Retrieve the cached approximate message count.
            int? cachedMessageCount = commandQueue.ApproximateMessageCount;
            try
            {
                if (cachedMessageCount != null || cachedMessageCount != 0)
                {
                    if (commandQueue.PeekMessage().AsString == "start")
                    {
                        TableQuery<DashInfo> query = new TableQuery<DashInfo>()
                            .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
                        var queryResult = statsTable.ExecuteQuery(query);
                        var dash = queryResult.First();
                        bool doneLoading = dash.loading;
                        // Checks to see if the cralwer is done loading or still crawling
                        if (doneLoading)
                        {
                            return new JavaScriptSerializer().Serialize("crawling");
                        }
                        else
                        {
                            return new JavaScriptSerializer().Serialize("loading");
                        }
                    }
                    // The crawler is currently in an idle state
                    else
                    {
                        return new JavaScriptSerializer().Serialize("idle");
                    }
                }
                // if the command queue is empty
                else
                {
                    commandQueue.AddMessage(new CloudQueueMessage("stop"));
                    return new JavaScriptSerializer().Serialize("idle");
                }
            }
            catch // If an error occurs, wait, and attempt to read the queue again
            {
                Thread.Sleep(5000);
                getCommand();
                return "idle";
            }
        }
    }

}