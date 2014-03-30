using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace TED.Samples.WAMSBulter.BackEndService.Notifications
{
    [Serializable]
    public class EncodeJobNotification
    {

        private ILocator[] myLocators;
        private IJob[] myJobList;
        private string myAssetName;
        private string myUrlForClientStreaming;
        public IJob[] JobList { get { return myJobList; } }
        public string AssetName { get { return myAssetName; } }
        public ILocator[] Locators { get { return myLocators; } }
        public string UrlForClientStreaming { get { return myUrlForClientStreaming; } }
        public EncodeJobNotification(IJob[] jobsReady, string OriginalAssetName, ILocator[] Locators, string UrlForClientStreaming)
        {
            myJobList = jobsReady;
            myAssetName = OriginalAssetName;
            myLocators = Locators;
            myUrlForClientStreaming = UrlForClientStreaming;
        }
    }

    public class JobFinishInfo 
    {
        private IJob myFinishJob;
        private string myOriginalMp4;
        private string myProcessId;
        private string myAssetUri;
        private MediaContentType myAssetType;
        public IJob FinishJob { get { return myFinishJob; } }
        public string OriginalMp4 { get { return myOriginalMp4; } }
        public string ProcessId { get { return myProcessId; } }
        public string AssetUri { get { return myAssetUri; } }
        public MediaContentType AssetType { get { return myAssetType; } }
        public JobFinishInfo(string ProcessId, string OriginalMp4, IJob FinishJob, MediaContentType AssetType, string AssetUri)
        {
            myAssetType = AssetType;
            myAssetUri = AssetUri;
            myFinishJob = FinishJob;
            myOriginalMp4 = OriginalMp4;
            myProcessId = ProcessId;
        }

    }
}
