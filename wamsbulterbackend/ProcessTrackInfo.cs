using Microsoft.WindowsAzure.Storage.Table;


namespace TED.Samples.WAMSBulter.BackEndService
{
    class ProcessTrackInfo : TableEntity
    {
        public string Step { get; set; }
        
        public ProcessTrackInfo(string AppId, string ProcessId, string BlobName)
        {
            this.PartitionKey = string.Format("pti_{0}_{1}", AppId, BlobName);
            this.RowKey = ProcessId;
        }
        public ProcessTrackInfo()
        { }
    }
}
