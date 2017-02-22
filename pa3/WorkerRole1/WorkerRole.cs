using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Xml;
using HtmlAgilityPack;
using WebRole1;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private static CloudQueue commandQueue;
        private static CloudQueue urlQueue;
        private static CloudTable statsTable;
        private static CloudTable urlTable;

        private int indexedCount = 0;
        private int queueCount = 0;
        private int crawledCount = 0;
        private bool stop = true;
        private bool readRobots = false;

        private HashSet<string> disallowed = new HashSet<string>();
        private List<string> errorUrl = new List<string>();
        private HashSet<string> visited = new HashSet<string>();
        private Queue<string> lastTen = new Queue<string>();

        private static string htmlRegex = @"^(http|https):\/\/[a-zA-Z0-9\-\.]+\.cnn\.com\/[a-zA-Z\d\/\.\-]+\/[a-zA-Z\d\-]+(\.cnn\.html|\.html|\.wtvr\.html|[a-zA-Z\d]+|\?[a-zA-Z\=a-zA-Z\&+\=a-zA-z0-9]+)$";

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            cloudInitialization();

            while (true)
            {
                readCommandQ();
                urlQueue.FetchAttributes();
                queueCount = (int)urlQueue.ApproximateMessageCount;
                if (!stop)
                {
                    crawlUrl();
                }
                updateDash();
                Thread.Sleep(500);
                Trace.TraceInformation("Working");
            }
        }

        // Helper Function that connects to all Azure's Cloud services
        private void cloudInitialization()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
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
                Thread.Sleep(5000);
                cloudInitialization();
            }

            updateDash();
        }

        // Helper Function that reads a queue for commands such as stop, clear, and start for the crawler and then does actions for the correspoding commands
        private void readCommandQ()
        {
            CloudQueueMessage command = commandQueue.PeekMessage();
            if (command != null)
            {
                if (command.AsString == "stop")
                {
                    stop = true;
                }
                else if (command.AsString == "clear")
                {
                    stop = true;
                    readRobots = false;
                    disallowed = new HashSet<string>();
                    visited = new HashSet<string>();
                    lastTen = new Queue<string>();
                    errorUrl = new List<string>();
                    crawledCount = 0;
                    queueCount = 0;
                    indexedCount = 0;
                    urlQueue.Clear();
                    urlTable.Delete();
                    cloudInitialization();
                    commandQueue.Clear();
                    CloudQueueMessage restart = new CloudQueueMessage("stop");
                    commandQueue.AddMessage(restart);
                    updateDash();
                }
                else
                {
                    stop = false;
                    // Checks to see if the robots.txt file has already been read to reduce redundancy in function
                    if (readRobots == false)
                    {
                        readRobot();
                        readRobots = true;
                    }
                }
            }
           
        }

        // Helper Function that reads from the Queue of urls and crawls through each url
        private void crawlUrl()
        {
            CloudQueueMessage url = urlQueue.GetMessage();
            if (url != null)
            {
                urlQueue.DeleteMessage(url);
                // Attempt to crawl through url
                try
                {
                    WebClient wClient = new WebClient();
                    string htmlString = wClient.DownloadString(url.AsString);
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlString);
                    crawlSubUrls(htmlDoc, url.AsString);
                    getUrlInfo(htmlDoc, url.AsString);
                    crawledCount++;
                }
                catch (Exception error) // If an error occurs while crawling, the url gets added to list of erros
                {
                    errorUrl.Add(error + ": " + url.AsString);
                }
            }
        }

        // Helper Function that reads through the robots.txt files for cnn and bleacher report
        private void readRobot()
        {
            WebClient website = new WebClient();
            string robot = website.DownloadString("http://www.cnn.com/robots.txt");
            string robot2 = website.DownloadString("http://www.bleacherreport.com/robots.txt");
            
            crawlCnn(robot);
            crawlBr(robot2);
        }

        // Helper Function that crawls specifically through CNN's robot.txt
        private void crawlCnn(string robot)
        {
            string line = "";
            using (StringReader reader = new StringReader(robot))
            {
                while((line = reader.ReadLine()) != null)
                {
                    string[] lineContent = line.Split(' ');

                    // Checks to see if the link is a sitemap
                    if (lineContent[0].Equals("Sitemap:")) 
                    {
                        XmlNodeList siteXml = getNodes(lineContent[1], "sitemap");
                        // Checks to see what type of sitemap the xml link went to
                        if (siteXml.Count != 0)
                        {
                            // Reads xml for sitemap
                            readSitemap(siteXml); 
                        }
                        else
                        {
                            // Read xml of urlsets
                            siteXml = getNodes(lineContent[1], "url");
                            readUrlSet(siteXml);
                        }
                    }
                    
                    else if (lineContent[0].Equals("Disallow:"))
                    {
                        // Adds to list of disallowed paths
                        disallowed.Add(lineContent[1]);
                    }
                }
            }
        }

        // Helper Function that crawls specifically through BleacherReport's robot.txt
        private void crawlBr(string robot)
        {
            string line = "";
            using (StringReader reader = new StringReader(robot))
            {
                while((line = reader.ReadLine()) != null)
                {
                    string[] lineContent = line.Split(' ');
                    // Checks to make sure the sitemap is for nba only
                    if (lineContent[1].Equals("http://bleacherreport.com/sitemap/nba.xml"))
                    {
                        XmlNodeList siteXml = getNodes(lineContent[1], "url");
                        // Reads the xml of the page
                        readUrlSet(siteXml);
                    }
                    // Checks to see if the link is a disallowed paths
                    else if (lineContent[0].Equals("Disallow:"))
                    {
                        // Adds to list of disallowed paths
                        disallowed.Add(lineContent[1]);
                    }
                }
            }
        }

        // Helper function to read xml nodes in a sitemap and add specific ones to the queue to be read
        private void readSitemap(XmlNodeList siteXml)
        {
            foreach (XmlNode sitemap in siteXml)
            {
                string date = sitemap["lastmod"].InnerText;
                // Checks to see if the url is from a valid date
                if (validDate(date))
                {
                    // Gets the sitemap links from the current sitemap
                    string xml2 = sitemap["loc"].InnerText;
                    XmlNodeList urls = getNodes(xml2, "url");
                    foreach (XmlNode url in urls)
                    {
                        if (url["lastmod"] != null)
                        {
                            date = url["lastmod"].InnerText;
                        }
                        if (validDate(date))
                        {
                            string loc = url["loc"].InnerText;
                            // Checks to see if the link is already been read and adds to queue & list if not
                            if (!visited.Contains(loc))
                            {
                                visited.Add(loc);
                                CloudQueueMessage newUrl = new CloudQueueMessage(loc);
                                urlQueue.AddMessage(newUrl);
                            }
                        }
                    }
                }
                // Update number of urls in the queue
                urlQueue.FetchAttributes();
                queueCount = (int)urlQueue.ApproximateMessageCount;
                updateDash();
            }
        }
        // Helper function to read xml nodes in a urlset and adds specific ones to the queue to be read
        private void readUrlSet(XmlNodeList siteXml)
        {
            foreach (XmlNode sitemap in siteXml)
            {
                string date = DateTime.Now.ToString();
                if (sitemap["lastmod"] != null)
                {
                    date = sitemap["lastmod"].InnerText;
                }
                // Checks to see if the url is from a valid date
                if (validDate(date))
                {
                    string url = sitemap["loc"].InnerText;
                    // Checks to see if the link is already been read and adds to queue & list if not
                    if (!visited.Contains(url))
                    {
                        visited.Add(url);
                        CloudQueueMessage newUrl = new CloudQueueMessage(url);
                        urlQueue.AddMessage(newUrl);
                    }
                }
            }
        }

        // Helper function the help gather information in the html of the passed url and htmldocument
        private void getUrlInfo(HtmlDocument htmlDoc, string url)
        {
            var htmlMeta = htmlDoc.DocumentNode.Descendants("meta");
            string date = DateTime.Now.ToString();

            // Looking for the last modified information
            foreach (var tag in htmlMeta)
            {
                if (tag.Attributes.Contains("http-equiv") && tag.Attributes["http-equiv"].Value == "last-modified")
                {
                    date = tag.Attributes["content"].Value;
                    break;
                }
            }
            insertUrlTable(htmlDoc, url, date);
        }

        // Helper function to help look for addition links in the html of the passed htmldocument and its url
        private void crawlSubUrls(HtmlDocument htmlDoc, string url)
        {
            var hrefs =  htmlDoc.DocumentNode.SelectNodes("//a[@href]");
            string link = "";
            if (hrefs != null)
            {
                foreach (HtmlNode aTag in hrefs)
                {
                    link = aTag.Attributes["href"].Value;
                    if (Regex.Match(link, htmlRegex).Success)
                    {
                        string[] tokens = url.Split(new String[] { "/" }, StringSplitOptions.None);
                        // Checks to see if the link is disallowed for crawlers and if the link had been visited already, 
                        // adds to queue & list if not
                        if (!disallowed.Contains(tokens[3]) && !visited.Contains(link))
                        {
                            visited.Add(link);
                            CloudQueueMessage msg = new CloudQueueMessage(link);
                            urlQueue.AddMessage(msg);
                        }
                    }
                }
            }
        }

        // Helper function to checkt he validitiy of a passed date
        // Returns true if the passed date is newer than 12/2016
        private bool validDate(string date)
        {
            try
            {  
                DateTime lastMod = DateTime.Parse(date);
                if (lastMod.Year == 2017 || (lastMod.Year == 2016 && lastMod.Month == 12))
                {
                    return true;
                } else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // Helper function that gets all of the nodes in an xml document with a specific tag that's passed into the function and the url to the xml doc
        // returns a xmlnodelist of all the nodes that match the tag
        private XmlNodeList getNodes(string xml, string tag)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xml);
            XmlNodeList xmlNodes = xmlDoc.GetElementsByTagName(tag);
            return xmlNodes;
        }

        // Helper function that takes in an htmldocument, a url and a date and creates an object to be inserted into the Url Table
        private void insertUrlTable(HtmlDocument htmlDoc, string url, string date)
        {
            if (date != "" & validDate(date))
            {
                // Finds the title of the htmldocument
                var title = htmlDoc.DocumentNode.SelectSingleNode("//title").InnerText;
                var encodedUrl = WebUtility.UrlEncode(url);
                TableOperation check = TableOperation.Retrieve<UrlInfo>("url", encodedUrl);
                TableResult retrievedResult = urlTable.Execute(check);
                // Checks to see if the title and url have been added to the table already, adds if not
                if (retrievedResult.Result == null)
                {
                    UrlInfo urlInfo = new UrlInfo(encodedUrl, url, title, date);
                    TableOperation insert = TableOperation.Insert(urlInfo);
                    urlTable.Execute(insert);
                    indexedCount++;
                    updateLastTenUrl(url);
                    updateDash();
                }
            }
        }

        // Helper function that helps update the DashInfo object with new stats of the cralwer and inserts it into the stats table
        private void updateDash()
        {
            DashInfo dash = new DashInfo("worker1", crawledCount, queueCount, (indexedCount - errorUrl.Count), JsonConvert.SerializeObject(lastTen), JsonConvert.SerializeObject(errorUrl), readRobots);

             TableOperation insertOperation = TableOperation.InsertOrReplace(dash);

            statsTable.Execute(insertOperation);
        }

        // Helper function the help update the last ten urls indexed into the table; LIMIT 10
        private void updateLastTenUrl(string url)
        {
            if (lastTen.Count > 9)
            {
                lastTen.Dequeue();
            }
            lastTen.Enqueue(url);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            //this.cancellationTokenSource.Cancel();
            //this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
