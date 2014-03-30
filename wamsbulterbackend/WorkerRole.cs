using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TED.Samples.WAMSBulter.BackEndService.Config;
using TED.Samples.WAMSBulter.BackEndService.Notifications;

namespace TED.Samples.WAMSBulter.BackEndService
{
    public class WorkerRole : RoleEntryPoint
    {
        string MediaAccountName;
        string MediaAccountKey;
        string ExternalStorageConn;
        string AssetStorageConn;
        string ExternalStorageContainer;
        string wamsBulterConn; 
        string AppId;
        bool Active = false;

        private void Config()
        {

            wamsBulterConn = RoleEnvironment.GetConfigurationSettingValue("strConnConfig");
            AppId = RoleEnvironment.GetConfigurationSettingValue("appId");
            ConfigHelper xConfig = new ConfigHelper(wamsBulterConn, AppId);

            MediaAccountName = xConfig.GetConfig("MediaAccountName");
            MediaAccountKey = xConfig.GetConfig("MediaAccountKey");
            ExternalStorageConn = xConfig.GetConfig("ExternalStorageConn");
            AssetStorageConn = xConfig.GetConfig("AssetStorageConn");
            ExternalStorageContainer = xConfig.GetConfig("ExternalStorageContainer");
            Active = ("1" == xConfig.GetConfig("Active"));
        }
        private void OnAssetAllEncodeFinish(object sender, EncodeJobNotification Info)
        {
            //TODO>Notification 
        }
        private void OnJobEncodeFinish(object sender, JobFinishInfo Info)
        {
            string msg = string.Format("Video {0} are publish at {1}", Info.OriginalMp4, Info.AssetUri);
            Trace.TraceWarning(msg);
        }

        private void ProcessAll()
        {
            //Read congifuration from Table in aech iteration
            Config();
            if (Active)
            {
                ButlerEncoderPublish myHelp = new ButlerEncoderPublish(MediaAccountName, MediaAccountKey, wamsBulterConn, AppId);
                myHelp.ProfileFileDirectory = Path.GetFullPath(@".\configFile\");
                myHelp.OnJobEncodeFinish += OnJobEncodeFinish;
                myHelp.OnAssetAllEncodeFinish += OnAssetAllEncodeFinish;
                //Process the New MP4 blob files from the Stagin Storage
                myHelp.ProcessNewVideos(ExternalStorageConn, ExternalStorageContainer, AssetStorageConn);
            }
            else
            {
                Trace.TraceWarning("Worker Role Butler is not Active");
            }
           

        }
        public override void Run()
        {

            while (true)
            {
                ProcessAll();
                Thread.Sleep(int.Parse(CloudConfigurationManager.GetSetting("TimeSleep")));
                
            }
          
        }

        
        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            return base.OnStart();
        }
    }
}
