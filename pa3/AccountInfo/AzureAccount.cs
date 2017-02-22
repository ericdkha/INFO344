using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountInfo
{
    public class AzureAccount
    {
        public CloudStorageAccount storageAccount;
        public CloudQueueClient queueClient;
        public CloudTableClient tableClient;

        //private static CloudQueue commandQueue = queueClient.GetQueueReference("crawlercommandqueue");
        //private static CloudQueue urlQueue = queueClient.GetQueueReference("crawlerurlqueue");
        //private static CloudTable statsTable = tableClient.GetTableReference("crawlerstatstable");
        //private static CloudQueue urlTable = tableClient.GetQueueReference("crawlerurltable");

        public AzureAccount()
        { 
            storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            queueClient = storageAccount.CreateCloudQueueClient();
            tableClient = storageAccount.CreateCloudTableClient();
        }
    }


}
