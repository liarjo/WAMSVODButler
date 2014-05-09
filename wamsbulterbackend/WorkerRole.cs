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
       
        private void ProcessAllSingleFileVideo()
        {
            //Read congifuration from Table in aech iteration
            Config();
            if (Active)
            {
                ButlerEncoderPublish myHelp = new ButlerEncoderPublish(MediaAccountName, MediaAccountKey, wamsBulterConn, AppId);
                myHelp.ProfileFileDirectory = Path.GetFullPath(@".\configFile\");
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
                ProcessAllSingleFileVideo();
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
