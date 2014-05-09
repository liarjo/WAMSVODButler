using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections;

namespace TED.Samples.WAMSBulter.BackEndService.Config
{
    public class ConfigHelper
    {
        private string myPartitionKey;
        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        private Hashtable htConfig;
        public ConfigHelper(string strConn, string AppId)
        {
            storageAccount = CloudStorageAccount.Parse(strConn);
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("wamsbutlerconfig");
            myPartitionKey = AppId;
            htConfig = new Hashtable();
            RefreshConfig();

        }
        public void RefreshConfig()
        {

            TableQuery<ConfigEntity> query =
                new TableQuery<ConfigEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, myPartitionKey));
            foreach (ConfigEntity item in table.ExecuteQuery(query))
            {
                htConfig.Add(item.RowKey, item.value);
            }
        }
        public string GetConfig(string key)
        {
            return htConfig[key].ToString();
        }
        
    }
}
