using Microsoft.WindowsAzure.Storage.Table;

namespace WebRole1
{
    /// <summary>
    // This class is for holding information about a Url and its contents such as the url's encoded form, the title of the page, and the date of the
    // page. To be stored in the Url Table
    /// </summary>
    public class UrlInfo : TableEntity
    {
        public UrlInfo(string urlEncode, string url, string title, string date)
        {
            this.PartitionKey = "url";
            this.RowKey = urlEncode;
            this.UrlEncode = urlEncode;
            this.Url = url;
            this.Title = title;
            this.Date = date;
        }

        public UrlInfo() { }

        public string UrlEncode { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Date { get; set; }
    }
}