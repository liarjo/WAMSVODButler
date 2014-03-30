using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace TED.Samples.WAMSBulter.BackEndService.TraceUtil
{
    public class WadLogEntity:TableEntity

    {
        public WadLogEntity()
        {
            PartitionKey = "a";
            RowKey = string.Format("{0:10}_{1}", DateTime.MaxValue.Ticks - DateTime.Now.Ticks, Guid.NewGuid());
        }

        public string Role { get; set; }
        public string RoleInstance { get; set; }
        public int Level { get; set; }
        public string Message { get; set; }
        public int Pid { get; set; }
        public int Tid { get; set; }
        public int EventId { get; set; }
        public Int64 EventTickCount { get; set; }
        public DateTime EventDateTime
        {
            get
            {
                return new DateTime(long.Parse(this.PartitionKey.Substring(1)));
            }
        }
    }
}
