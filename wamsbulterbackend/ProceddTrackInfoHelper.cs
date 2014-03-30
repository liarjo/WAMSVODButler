using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TED.Samples.WAMSBulter.BackEndService
{
    class ProcessTrackInfoHelper
    {
        private CloudStorageAccount storageAccount;
        private CloudTable table;
        private string GetPartitionKey(string AppId, String OriginalBlobName)
        {
            return string.Format("pti_{0}_{1}", AppId, OriginalBlobName);
        }
        public ProcessTrackInfoHelper(string strConn)
        {
            storageAccount = CloudStorageAccount.Parse(strConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Create the table if it doesn't exist.
            table = tableClient.GetTableReference("wamsbutlerprocesstrack");
            table.CreateIfNotExists();
        }
        
        public void CreateProcess(string AppId, string ProcessId, String OriginalBlobName)
        {
            ProcessTrackInfo info = new ProcessTrackInfo(AppId, ProcessId, OriginalBlobName);
            info.Step = "0";
            TableOperation insertOperation = TableOperation.Insert(info);
            table.Execute(insertOperation);
        }
        public void StepAdvance(string AppId, string ProcessId, String OriginalBlobName, string codecIdStep)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<ProcessTrackInfo>(GetPartitionKey(AppId, OriginalBlobName), ProcessId);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            ProcessTrackInfo updateEntity = (ProcessTrackInfo)retrievedResult.Result;
            updateEntity.Step = codecIdStep;
            TableOperation updateOperation = TableOperation.Replace(updateEntity);
            table.Execute(updateOperation);

        }
        public void CloseProcess(string AppId, String OriginalBlobName)
        {
            TableQuery<ProcessTrackInfo> query = 
                new TableQuery<ProcessTrackInfo>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, GetPartitionKey(AppId, OriginalBlobName))
                );

            // Print the fields for each customer.
            foreach (ProcessTrackInfo entity in table.ExecuteQuery(query))
            {
                TableOperation deleteOperation = TableOperation.Delete(entity);
                table.Execute(deleteOperation);

            }
        }
    }
}
