using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    /// <summary>
    // This class is to store information about the current state of the crawler such as the # of urls crawled, index, the count of urls still
    // in the pipline (queue) to be read, the last ten urls indexed, the errors encounted, and whether or not the cralwer is loading or crawling.
    // To be stored in the stats table
    /// </summary>
    class DashInfo : TableEntity
    {
        public DashInfo(string worker, int totalCrawled,
            int queueCount, int indexedCount, string lastTen, string errorUrl, bool loading)
        {
            this.PartitionKey = "dash";
            this.RowKey = worker;
            this.totalCrawled = totalCrawled;
            this.queueCount = queueCount;
            this.indexedCount = indexedCount;
            this.lastTen = lastTen;
            this.errorUrl = errorUrl;
            this.loading = loading;
        }

        public DashInfo() { }

        public int totalCrawled { get; set; }

        public int queueCount { get; set; }

        public int indexedCount { get; set; }

        public string lastTen { get; set; }

        public string errorUrl { get; set; }

        public bool loading { get; set; }
    }
}