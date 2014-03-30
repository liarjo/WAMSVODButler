using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace TED.Samples.WAMSBulter.BackEndService
{
    public class OutPutFormat : TableEntity
    {
        public int OutTypesId { get; set; }
        public string JobEncodeDescription { get; set; }
        public string TaskEncodeDescription { get; set; }
        public string MediaProcessorByName { get; set; }
        public string EncodeDescription { get; set; }
        public string NameTail { get; set; }

        public OutPutFormat(string ExternalStorageContainer, string Order, int OutPutType)
        {
            this.PartitionKey = ExternalStorageContainer;
            this.RowKey = Order;
            OutTypesId = (int)OutPutType;

        }
        public OutPutFormat()
        { }


    }
}
