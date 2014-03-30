using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;
using Microsoft.WindowsAzure.MediaServices.Client;
using TED.Samples.WAMSBulter.BackEndService.Notifications;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;

using System.Xml.Serialization;

namespace TED.Samples.WAMSBulter.BackEndService.Notifications
{
   
    public interface INotificator
    {
        void sendNotification(List<JobFinishInfo> Info);
        
    }

 
    public class QueueNotificator:INotificator
    {
        string queueName = "wamsbutlerallencodefinish";
        string strconn;

        public QueueNotificator(string queueConn)
        {
            strconn = queueConn;
        }
        public void sendNotification(List<JobFinishInfo> Info)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(strconn);

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            try
            {
                // Retrieve a reference to a queue.
                CloudQueue queue = queueClient.GetQueueReference(queueName);

                // Create the queue if it doesn't already exist.
                queue.CreateIfNotExists();

                // Create a message and add it to the queue.
                //
                //queue.AddMessage(message);
                List<string> msg = new System.Collections.Generic.List<string>();
                msg.Add(Info.FirstOrDefault().OriginalMp4);
                foreach (JobFinishInfo item in Info)
                {
                    msg.Add(string.Format("{0}: {1}", item.OriginalMp4, item.AssetUri));
                }
                var serializer = new XmlSerializer(typeof(List<string>));
                System.IO.StringWriter textWriter = new System.IO.StringWriter();

                serializer.Serialize(textWriter, msg);
                CloudQueueMessage message = new CloudQueueMessage(textWriter.ToString());
                queue.AddMessage(message);
         
            }
            catch (Exception X)
            {

                Trace.TraceError("[sendNotification] " + X.Message);
            }
           
        }
    }

  
}
