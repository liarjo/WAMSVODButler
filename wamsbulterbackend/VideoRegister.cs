using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;


namespace TED.Samples.WAMSBulter.BackEndService
{
    public class VideoRegister : TableEntity
    {
        public string BlobName { get; set; }
        public string BlobUri { get; set; }
        public VideoRegister(string pk, string rk)
        {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }
        public VideoRegister()
        { }
    }
}
