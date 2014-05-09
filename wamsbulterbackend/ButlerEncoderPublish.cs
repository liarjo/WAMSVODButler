using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
//using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TED.Samples.WAMSBulter.BackEndService.Notifications;
using TED.Samples.WAMSBulter.BackEndService.TraceUtil;


namespace TED.Samples.WAMSBulter.BackEndService
{
    class ButlerEncoderPublish
    {
        private Object thisLock = new Object();
       // public EventHandler<JobFinishInfo> OnJobEncodeFinish;
       // public EventHandler<EncodeJobNotification> OnAssetAllEncodeFinish;
        private CloudMediaContext _MediaServiceContext;
        private string _accountMediaName;
        private string _accountMediaKey;
        private System.Collections.Hashtable VideoProcessHistoric;
        private string myWamsButlerConn;
        private string myAppId;
        private Hashtable GetReadyVideoProcess(string WamsButlerConn, string VideoProcessContainer)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(WamsButlerConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(VideoProcessContainer);
            table.CreateIfNotExists();
            TableQuery<VideoRegister> query =
                new TableQuery<VideoRegister>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,myAppId));

            Hashtable VideoProcessList = new Hashtable();
            foreach (VideoRegister entity in table.ExecuteQuery(query))
            {
                VideoProcessList.Add(entity.BlobName, entity.BlobUri);
            }
            return VideoProcessList;
        }
        public string ProfileFileDirectory
        {
            get;
            set;
        }
        private CloudMediaContext ObtainContext(string _accountName, string _accountKey)
        {
            return new CloudMediaContext(_accountName, _accountKey);
        }
        public ButlerEncoderPublish(string MediaAccountName, string MediaAccountKey, string WamsButlerConn, string AppId)
        {
            _accountMediaName = MediaAccountName;
            _accountMediaKey = MediaAccountKey;
            _MediaServiceContext = ObtainContext(MediaAccountName, MediaAccountKey);
            ProfileFileDirectory = null;
            myWamsButlerConn=WamsButlerConn;
            myAppId = AppId;
        }
        //TODO: move to workerEncoder
        private IAsset CreateAssetFromBlob(CloudBlobContainer externalMediaBlobContainer, string ExternalBlobName, CloudBlobClient assetBlobClient, string MediaServicesBlobName, string myProcessId)
        {
            // Create a new asset.
            //myProcessId = Guid.NewGuid().ToString();

            CloudMediaContext MediaContext = ObtainContext(_accountMediaName, _accountMediaKey);
            string assetName = string.Format("{0}_{1}_Butler_{2}", externalMediaBlobContainer.Name, ExternalBlobName, myProcessId);
            IAsset asset = MediaContext.Assets.Create(assetName, AssetCreationOptions.None);
            IAccessPolicy writePolicy=MediaContext.AccessPolicies.Create("writePolicy_" + assetName, TimeSpan.FromMinutes(120), AccessPermissions.Write);
            ILocator destinationLocator=MediaContext.Locators.CreateLocator(LocatorType.Sas, asset, writePolicy);

            string assetContainerName = (new Uri(destinationLocator.Path)).Segments[1];
            CloudBlobContainer assetContainer = assetBlobClient.GetContainerReference(assetContainerName);
            CloudBlockBlob ExternalBlob = externalMediaBlobContainer.GetBlockBlobReference(ExternalBlobName);
            CloudBlockBlob assetBlob = assetContainer.GetBlockBlobReference(MediaServicesBlobName);

            var sas = externalMediaBlobContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7),
                Permissions = SharedAccessBlobPermissions.Read,
            });
            var srcBlockBlobSasUri = string.Format("{0}{1}", ExternalBlob.Uri, sas);
            assetBlob.StartCopyFromBlob(new Uri(srcBlockBlobSasUri));

            CloudBlockBlob blobStatusCheck;
            blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(MediaServicesBlobName);
            while (blobStatusCheck.CopyState.Status == CopyStatus.Pending)
            {
                Task.Delay(TimeSpan.FromSeconds(10d)).Wait();
                Trace.TraceInformation("Waiting copy of  " + blobStatusCheck.Name);
                blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(MediaServicesBlobName);
            }
            assetBlob.FetchAttributes();

            var assetFile = asset.AssetFiles.Create(MediaServicesBlobName);
            destinationLocator.Delete();
            writePolicy.Delete();
            //// Refresh the asset.
            asset = MediaContext.Assets.Where(a => a.Id == asset.Id).FirstOrDefault();
            return asset;
        }
        private void OnJobError(object sender, IJob X)
        {
            string ErrorMsg = "Job " + X.Id;
            foreach (ITask task in X.Tasks)
            {
                foreach (ErrorDetail detail in task.ErrorDetails)
                {
                    ErrorMsg += string.Format("[{0}] code {1} : {2}", X.Id, task.Id, detail.Code, detail.Message);
                }
            }
            Trace.TraceError(ErrorMsg);
        }
        private void OnJobFinish(object sender, IJob X)
        {
            //TODO: message Service bus
            Trace.TraceInformation("Job {0} Finished!", X.Id);
        }
        private System.Collections.Generic.IEnumerable<OutPutFormat> GetMediaContentType(string wamsButlerConn, string OutputFormatTableName, string AppId)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(wamsButlerConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(OutputFormatTableName);
            table.CreateIfNotExists();
            TableQuery<OutPutFormat> query =
                new TableQuery<OutPutFormat>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, AppId));
            return table.ExecuteQuery(query);

        }
        private ILocator MasterPublish( OutPutFormat currentOutPutFormat , IAsset currentAsset, EncodeJob currentWorkerEncoder)
        {
            ILocator currentLocator = null;
            //b. Publish
            switch ((MediaContentType)currentOutPutFormat.OutTypesId)
            {
                case MediaContentType.SmoothStreaming:
                    //b. Publish H264
                    currentLocator = currentWorkerEncoder.GetDynamicStreamingUrl(currentAsset.Id, LocatorType.OnDemandOrigin, MediaContentType.SmoothStreaming);
                    break;
                case MediaContentType.HLS:
                    currentLocator = currentWorkerEncoder.GetDynamicStreamingUrl(currentAsset.Id, LocatorType.OnDemandOrigin, MediaContentType.HLS);
                    break;
                case MediaContentType.H264Broadband720p:
                    //b. Publish H264
                    currentLocator = currentWorkerEncoder.GetDynamicStreamingUrl(currentAsset.Id, LocatorType.Sas, MediaContentType.H264Broadband720p);
                    break;
                case MediaContentType.HDS:
                    currentLocator = currentWorkerEncoder.GetDynamicStreamingUrl(currentAsset.Id, LocatorType.OnDemandOrigin, MediaContentType.HDS);
                    break;
                default:
                    //b. otherssss
                     currentLocator = currentWorkerEncoder.GetDynamicStreamingUrl(currentAsset.Id, LocatorType.Sas, MediaContentType.OtherSingleFile);
                    break;
            }
            return currentLocator;
        }
        public void PublishFromBlob(string ExternalStorageConn, string ExternalStorageContainer, string ExternalBlobName, string AssetStorageConn, string AssestBlobName, string myEncodeProcessId)
        {
            //1. Create Asset //
            CloudStorageAccount externalStorageAccount = CloudStorageAccount.Parse(ExternalStorageConn);
            CloudBlobClient externalCloudBlobClient = externalStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer externalMediaBlobContainer = externalCloudBlobClient.GetContainerReference(ExternalStorageContainer);
            CloudStorageAccount assetStorageAccount = CloudStorageAccount.Parse(AssetStorageConn);
            CloudBlobClient AssetBlobStoragclient = assetStorageAccount.CreateCloudBlobClient();
            System.Collections.Generic.List<JobFinishInfo> NotificationList = new System.Collections.Generic.List<JobFinishInfo>();
            IAsset OriginalAsset;
            try
            {
                OriginalAsset = this.CreateAssetFromBlob(externalMediaBlobContainer, ExternalBlobName, AssetBlobStoragclient, AssestBlobName, myEncodeProcessId);
                if (OriginalAsset == null)
                {
                    throw new Exception("Add Asset from blob Fail!");
                }
            }
            catch (Exception X)
            {
                throw new Exception("[CreateAssetFromBlob] " + X.Message);
            }

            //2. Setup the enviroment//
            string assetName = OriginalAsset.Name;
            IAsset CurrentAsset;
            //0.setup
            EncodeJob myWorkerEncoder = new EncodeJob(ProfileFileDirectory);
            myWorkerEncoder.MediaServiceContext = ObtainContext(_accountMediaName, _accountMediaKey);
            myWorkerEncoder.ConnConfigFiles = this.myWamsButlerConn;
            //TODO: improve
            myWorkerEncoder.OnJobError += OnJobError;
            myWorkerEncoder.OnJobFinish += OnJobFinish;
            IAsset lastEncodedAsset = null;
            IJob currentJob;
            ILocator currentLocator;
            //1. check Types
            System.Collections.Generic.IEnumerable<OutPutFormat> formatList= GetMediaContentType(myWamsButlerConn, "wamsbutleroutputformat", myAppId);
            if (formatList.Count() == 0)
            {
                throw new Exception("Output Format missing, review Table wamsbutleroutputformat");
            }
            //3. Loop for each Output Format//
            foreach (OutPutFormat encodeX in formatList)
            {
                currentJob = null;
                currentLocator = null;
                if ((MediaContentType)encodeX.OutTypesId == MediaContentType.HLS)
                {
                    //This is package JOB, not real encoding for this reason uses the last Encode asset
                    //Only use when you are making Static HLS Package
                    CurrentAsset = lastEncodedAsset;
                }
                else
                {
                    CurrentAsset = OriginalAsset;
                }
                //3.1. Encode & output Assets
                try
                {
                    //Here we does the encoding and return the iJOB executed
                    currentJob = myWorkerEncoder.ExecuteJob(encodeX, CurrentAsset, assetName + encodeX.NameTail);
                }
                catch (Exception X)
                {
                    
                    throw new Exception("[ExecuteJob] "+ X.Message);
                }
                
                if (currentJob != null)
                {
                    lastEncodedAsset = currentJob.Tasks[0].OutputAssets[0];
                }

                if (lastEncodedAsset == null)
                {
                    throw new Exception("PublishFromBlob Error: first encode could not be only publish");
                }
                

                ////3.2. Publish the Asset //
                try
                {
                    currentLocator = this.MasterPublish(encodeX, lastEncodedAsset, myWorkerEncoder);
                }
                catch (Exception X)
                {
                    
                    throw new Exception("[MasterPublish] "+ X.Message);
                }
                
                //Notificaction JOB Complete
                JobFinishInfo jobCompleteMessage = new JobFinishInfo(myEncodeProcessId, ExternalBlobName, currentJob, (MediaContentType)encodeX.OutTypesId, myWorkerEncoder.UrlForClientStreaming);
                NotificationList.Add(jobCompleteMessage);
            }
            //4.Delete Original Asset
            DeleteAssest(OriginalAsset);

            //5. Notification: all encodig jobs are ready for this asset
            //notication File Complete
            INotificator notRobot = new QueueNotificator(myWamsButlerConn);
            try
            {
                notRobot.sendNotification(NotificationList);
            }
            catch (Exception X)
            {

                throw new Exception("[sendNotification] " + X.Message) ;
            }
        }
        private IAsset GetAsset(string assetId)
        {
            // Use a LINQ Select query to get an asset.
            var assetInstance =
                from a in _MediaServiceContext.Assets
                where a.Id == assetId
                select a;
            // Reference the asset as an IAsset.
            IAsset asset = assetInstance.FirstOrDefault();

            return asset;
        }
        private void DeleteAssest(IAsset asset)
        {
            // delete the asset
            asset.Delete();
            // Verify asset deletion
            if (GetAsset(asset.Id) == null)
            {
                Trace.TraceInformation("Deleted the Asset: " + asset.Name);
            }
        }
        private bool IsNewVideo(string BlobName)
        {

            return (!VideoProcessHistoric.Contains(BlobName));

        }
        private void NewVideoProcessed(string BlobName, string blobUri, string WamsButlerConn, string VideoProcessContainer)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(WamsButlerConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(VideoProcessContainer);
            table.CreateIfNotExists();
            VideoRegister xVideo = new VideoRegister(myAppId, BlobName);
            xVideo.BlobUri = blobUri;
            xVideo.BlobName = BlobName;
            TableOperation insertOperation = TableOperation.Insert(xVideo);
            table.Execute(insertOperation);
            //TODO: lock
            VideoProcessHistoric.Add(BlobName, 1);
        }
        private void ProcessNewVideo(CloudBlockBlob myExternalBlobVideo, string myExternalStorageConn, string myExternalStorageContainer, string myAssetStorageConn)
        {
            string myEncodeProcessId = Guid.NewGuid().ToString();
            try
            {
                Trace.TraceInformation("strat Processing " + myExternalBlobVideo.Name);
                PublishFromBlob(myExternalStorageConn, myExternalStorageContainer, myExternalBlobVideo.Name, myAssetStorageConn, myExternalBlobVideo.Name,myEncodeProcessId);
                Trace.TraceInformation("Finish Processing " + myExternalBlobVideo.Name);
                NewVideoProcessed(myExternalBlobVideo.Name, myExternalBlobVideo.Uri.AbsoluteUri, myWamsButlerConn, "wamsbutlervideohistory");
            }
            catch (Exception X)
            {
                string msgDetail = string.Format("{2} Error in prosessing blob {0}, Error :{1}", myExternalBlobVideo.Name, X.Message, myEncodeProcessId);
                Trace.TraceError(msgDetail);
                //TODO:send notificacion

                //RollBack
                RollBack(myEncodeProcessId);
                msgDetail = string.Format("RollBack process: {0} blob {1}", myEncodeProcessId, myExternalBlobVideo.Name);
                Trace.TraceWarning(msgDetail);
                Trace.Flush();
            }
        }
        public void ProcessNewVideos(string ExternalStorageConn, string ExternalStorageContainer, string AssetStorageConn)
        {
            CloudStorageAccount externalStorageAccount = CloudStorageAccount.Parse(ExternalStorageConn);
            CloudBlobClient externalCloudBlobClient = externalStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer externalContainer = externalCloudBlobClient.GetContainerReference(ExternalStorageContainer);
            //TODO: check if another process runs incomplete before

            //TODO: Multi Staging Storage
            this.VideoProcessHistoric = this.GetReadyVideoProcess(myWamsButlerConn, "wamsbutlervideohistory");
            Hashtable myEncodeList = new Hashtable();
            int myEncodeListkey = 0;
            foreach (IListBlobItem item in externalContainer.ListBlobs(null, false, BlobListingDetails.None, null, null))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob ExternalBlobVideo = (CloudBlockBlob)item;
                    if (IsNewVideo(ExternalBlobVideo.Name) && (System.IO.Path.GetExtension(ExternalBlobVideo.Name).ToLower() == ".mp4"))
                    {
                        myEncodeList.Add(myEncodeListkey, ExternalBlobVideo);
                        myEncodeListkey += 1;
                    }
                    
                }
            }

            Task[] myEncodeTaks = new Task[myEncodeList.Count+1];
            for (int i = 0; i < myEncodeList.Count; i++)
            {
                CloudBlockBlob ExternalBlobVideo = (CloudBlockBlob)myEncodeList[i];
                myEncodeTaks[i] = Task.Factory.StartNew(() => ProcessNewVideo(ExternalBlobVideo, ExternalStorageConn, ExternalStorageContainer, AssetStorageConn));
            }
            myEncodeTaks[myEncodeList.Count] = Task.Factory.StartNew(() => DeleteOldWADLOGData(this.myWamsButlerConn, 2));
            
            Task.WaitAll(myEncodeTaks);     
        }
        private void DeleteOldWADLOGData(string srtConn, int DaysAgo)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(srtConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            try
            {
                CloudTable table = tableClient.GetTableReference("WADLogsTable");
                string pkfileter = "0" + DateTime.UtcNow.AddHours(-24*DaysAgo).Ticks.ToString();
                TableQuery<WadLogEntity> query =
                    new TableQuery<WadLogEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, pkfileter));
                System.Collections.Generic.IEnumerable<WadLogEntity> oldLogs = table.ExecuteQuery(query);
                foreach (WadLogEntity logE in oldLogs)
                {
                    TableOperation deleteOperation = TableOperation.Delete(logE);
                    table.Execute(deleteOperation);
                }
            }
            catch (Exception X)
            {
                Trace.TraceError("[DeleteOldWADLOGData] " + X.Message);
            }
        }
        private void RollBack(string PartialBlobName)
        {
            foreach (IAsset asset in _MediaServiceContext.Assets)
            {
                if (asset.Name.Contains(PartialBlobName))
                {
                    Trace.TraceInformation("Deleting Assest: " + asset.Name);
                    DeleteAssest(asset);
                }
            }
        }
    }
}
