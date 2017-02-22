using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using WorkerRole1;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace WebRole1
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class admin : WebService
    {
        private static CloudQueue commandQueue;
        private static CloudQueue urlQueue;
        private static CloudTable statsTable;
        private static CloudTable urlTable;

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

        // Function that makes a query to the stats table to retrieve the last ten urls indexed and returns them in a queue object
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public Queue<string> lastTen()
        {
            Queue<string> result = new Queue<string>();
            cloudInitialization();

            TableQuery<DashInfo> query = new TableQuery<DashInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "worker1"));
            var queryResult = statsTable.ExecuteQuery(query);
            var dash = queryResult.First();
            var lastTen = JsonConvert.DeserializeObject<Queue<string>>(dash.lastTen);
            if (lastTen == null)
            {
                Queue<string> empty = new Queue<string>();
                result = empty;
            }
            else
            {
                result = lastTen;
            }
            return result;
        }

        // Function that makes a query to the stats table to retrieve the current insertion errors and returns them in a list of strings
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
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
        public string GetPageTitle(string url)
        {
            try
            {
                string title = "";
                string encodedUrl = WebUtility.UrlEncode(url);
                TableQuery<UrlInfo> query = new TableQuery<UrlInfo>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, encodedUrl));
                var queryResult = urlTable.ExecuteQuery(query);
                var results = queryResult.First();
                title = results.Title;
                // Checks to see if the title was found from the passed url
                if (title == null)
                {
                    title = "Website not found :(";
                }
                return new JavaScriptSerializer().Serialize(title); ;
            }
            catch (Exception error) // If an error occurs, return a message to the user
            {
                return "website not found: " + error;
            }
        }

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
            } catch // If an error occurs, wait, and attempt to read the queue again
            {
                Thread.Sleep(5000);
                getCommand();
                return "idle";
            }
        }
    }
}
