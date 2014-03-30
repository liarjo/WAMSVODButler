using Microsoft.WindowsAzure.Storage.Table;

namespace TED.Samples.WAMSBulter.BackEndService.Config
{
    class ConfigEntity : TableEntity
    {
        public string value { get; set; }
        public ConfigEntity()
        { }
    }
}
