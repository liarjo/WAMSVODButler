using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace TED.Samples.WAMSBulter.BackEndService
{
    public class EncodeJob//:IEncodeJob
    {
        private string PreviousJobState;
        private string myConnConfigFiles;
        private CloudMediaContext myMediaServiceContext;
        private string myProfileFileDirectory;
        public EventHandler<IJob> OnJobError;
        public EventHandler<IJob> OnJobFinish;
        public EventHandler<IJob> OnJobCancel;
        public string UrlForClientStreaming { get; set; }
        public CloudMediaContext MediaServiceContext
        {
            get
            {
                return myMediaServiceContext;
            }
            set
            {
                myMediaServiceContext = value;
            }
        }
        public EncodeJob(string ProfileFileDirectory)
        {
            myProfileFileDirectory = ProfileFileDirectory;
        }
        public string ConnConfigFiles
        {
            get { return myConnConfigFiles; }
            set { myConnConfigFiles = value; }
        }
        private IAsset GetAsset(string assetId)
        {
            // Use a LINQ Select query to get an asset.
            var assetInstance =
                from a in myMediaServiceContext.Assets
                where a.Id == assetId
                select a;
            // Reference the asset as an IAsset.
            IAsset asset = assetInstance.FirstOrDefault();

            return asset;
        }
        public ILocator GetDynamicStreamingUrl(string targetAssetID, LocatorType type, MediaContentType contentType)
        {
            IAssetFile assetFile = null;
            ILocator locator = null;
            Uri smoothUri = null;

            var daysForWhichStreamingUrlIsActive = 365;
            var outputAsset = myMediaServiceContext.Assets.Where(a => a.Id == targetAssetID).FirstOrDefault();
            var accessPolicy = myMediaServiceContext.AccessPolicies.Create(outputAsset.Name,
                                                             TimeSpan.FromDays(daysForWhichStreamingUrlIsActive),
                                                             AccessPermissions.Read | AccessPermissions.List);
            var assetFiles = outputAsset.AssetFiles.ToList();
            switch (type)
            {
                case LocatorType.None:
                    break;
                case LocatorType.OnDemandOrigin:
                    assetFile = assetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();
                    locator = myMediaServiceContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, outputAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));
                    switch (contentType)
	                {
                        case MediaContentType.SmoothStreaming:
                             smoothUri = new Uri(locator.Path + assetFile.Name + "/manifest");
                            break;
                        case MediaContentType.HLS:
                            smoothUri = new Uri(locator.Path + assetFile.Name + "/manifest(format=m3u8-aapl)");
                            break;
                        case MediaContentType.HDS:
                            smoothUri = new Uri(locator.Path + assetFile.Name + "/manifest(format=f4m-f4f)");
                            break;
                        case MediaContentType.DASH:
                            smoothUri = new Uri(locator.Path + assetFile.Name + "/manifest(format=mpd-time-csf)");
                            break;
                        default:
                            throw new Exception("GetDynamicStreamingUrl Error: you must chose HLS, Smooth or HDS");
                            break;
	                }
                    this.UrlForClientStreaming = smoothUri.ToString();
                 break;
                case LocatorType.Sas:
                     var mp4Files = assetFiles.Where(f => f.Name.ToLower().EndsWith(".mp4")).ToList();
                     assetFile = mp4Files.OrderBy(f => f.ContentFileSize).LastOrDefault(); //Get Largest File
                    if (assetFile != null)
                    {
                        locator = myMediaServiceContext.Locators.CreateLocator(LocatorType.Sas, outputAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));
                        var mp4Uri = new UriBuilder(locator.Path);
                        mp4Uri.Path += "/" + assetFile.Name;
                        this.UrlForClientStreaming = mp4Uri.ToString();
                    }
                 break;
                default:
                 break;
            }
            return locator;
        }
        private IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = myMediaServiceContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
               ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }
        private IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in myMediaServiceContext.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();
            return job;
        }
        private void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;

            if (PreviousJobState != e.CurrentState.ToString())
            {
                PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("Job {0} state Changed from {1} to {2}", job.Id, e.PreviousState, e.CurrentState);

            }
            switch (e.CurrentState)
            {
                case JobState.Finished:
                    if (OnJobFinish != null)
                    {
                        OnJobFinish(this, job);
                    }
                    break;
                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Trace.TraceInformation("Please wait Job {0} Finish", job.Id);
                    break;
                case JobState.Canceled:
                    if (OnJobCancel != null)
                    {
                        OnJobCancel(this, job);
                    }
                    break;
                case JobState.Error:
                    if (OnJobError != null)
                    {
                        OnJobError(this, job);
                    }
                    break;
                default:
                    break;
            }
        }
        private void WaitJobFinish(string jobId)
        {
            IJob myJob = GetJob(jobId);
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            double avance = 0;
            //TODO: imporve wating method
            while ((myJob.State != JobState.Finished) && (myJob.State != JobState.Canceled) && (myJob.State != JobState.Error))
            {
                if (myJob.State == JobState.Processing)
                {
                    if (avance != (myJob.Tasks[0].Progress / 100))
                    {
                        avance = myJob.Tasks[0].Progress / 100;
                        Trace.TraceInformation("job " + myJob.Id + " Percent complete:" + avance.ToString("#0.##%"));
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
                myJob.Refresh();
            }
            //TODO: test this kind of error
            if (myJob.State == JobState.Error)
            {
                throw new Exception(string.Format("Error JOB {0}", myJob.Id));
            }
        }
        public IJob ExecuteJob(OutPutFormat format, IAsset myAsset, string OutPutAssetName)
        {
            IJob jobX = null;
            if (format.EncodeDescription == "N/A")
            {
                //only Publish
                Trace.TraceInformation("ExecuteJob: Not encoding only publish " + myAsset.Name);
            }
            else
            {
                if (format.EncodeDescription.Contains(".xml"))
                {
                    format.EncodeDescription = myProfileFileDirectory + format.EncodeDescription;
                }
                Trace.TraceInformation("Create " + format.JobEncodeDescription);
               
                jobX = ExecuteMasterJob(format.JobEncodeDescription, format.TaskEncodeDescription +"_"+ myAsset.Name, myAsset, OutPutAssetName, format.EncodeDescription, (MediaContentType)format.OutTypesId, format.MediaProcessorByName);

            }
            return jobX;
        }
        private IJob ExecuteMasterJob(string JobEncodeDescription, string TaskEncodeDescription, IAsset myAsset, string OutPutAssetName, string EncodeDescription, MediaContentType TypeEncode, string MediaProcessorByName)
        {
            // 1. Configuration or encode label
            string ConfOrLabelEncode;
            if (EncodeDescription.Contains(".xml"))
            {
                if (File.Exists(EncodeDescription))
                {
                    ConfOrLabelEncode = File.ReadAllText(Path.GetFullPath(EncodeDescription));
                }
                else
                {
                    string xmlblobname = Path.GetFileName(EncodeDescription);

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.myConnConfigFiles);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference("wamsvodbuterencodeconfig");
                    CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(xmlblobname);

                    string encodeConfigXml;
                    using (var memoryStream = new MemoryStream())
                    {
                        blockBlob2.DownloadToStream(memoryStream);
                        memoryStream.Position = 0;
                        StreamReader sr= new StreamReader(memoryStream);
                        encodeConfigXml = sr.ReadToEnd();
                    }
                    ConfOrLabelEncode = encodeConfigXml;
                }
            }
            else
            {
                //Encoding by name
                ConfOrLabelEncode = EncodeDescription;
            }


            //2. Onlu form HLS Valida que el Asset contenga un solo archivo ISM
            if (TypeEncode == MediaContentType.HLS)
            {
                var ismAssetFiles = myAsset.AssetFiles.ToList().
                            Where(f => f.Name.EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (ismAssetFiles.Count() != 1)
                    throw new ArgumentException("The asset should have only one, .ism file");
                ismAssetFiles.First().IsPrimary = true;
                ismAssetFiles.First().Update();
            }
            // 3. Creal el JOB
            IJob job = myMediaServiceContext.Jobs.Create(JobEncodeDescription);
            IMediaProcessor processor = GetLatestMediaProcessorByName(MediaProcessorByName);

            // 4. Crea la tarea con los detalles de codificación del archivo XML
            ITask task = job.Tasks.AddNew(
                TaskEncodeDescription,
                processor,
                ConfOrLabelEncode,
                TaskOptions.ProtectedConfiguration);

            // 5. Define el Asset de entrada
            task.InputAssets.Add(myAsset);

            // 6. Crea el Asset de Salida con el sufijo HLS.
            string nombreContenido = OutPutAssetName;
            task.OutputAssets.AddNew(nombreContenido, AssetCreationOptions.None);

            //7. define el manejador del evento 
            job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);

            //8. Lanza el JOB
            job.Submit();

            //9. Revisa el estado de ejecución del JOB 
            Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);

            //10. en vez de utilizar  progressJobTask.Wait(); que solo muestra cuando el JOB termina
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            this.WaitJobFinish(job.Id);
            //11. regresa la referencia al JOB terminado
            return job;
        }
        
    }
}
