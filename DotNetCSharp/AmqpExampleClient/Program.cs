using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace DotNetExamples
{
    class Program
    {/// <summary>
        /// This program performs a POST request to the zoneTransitions
        /// configure message queue endpoint of the ItemSense instance
        /// specified in the SolutionConstants class in the SolutionItems 
        /// project. Upon successful completion of the POST, the resulting
        /// message queue details are then used to create and configure an
        /// AMQP message queue client for receiving events from the queue.
        /// </summary>
        /// <param name="args">
        /// Command line parameters ignored.
        /// </param>
        static void Main(string[] args)
        {
            // Define an object that will contain the AMQP Message Queue details
            ItemSense.AmqpMessageQueueDetails MsgQueueDetails = null;

            try
            {
                // Create a JSON object for configuring a zoneTransition
                // Message Queue
                ItemSense.ZoneTransitionMessageQueueConfig msgQConfig =
                    new ItemSense.ZoneTransitionMessageQueueConfig()
                    {
                        FromZone = "ABSENT",
                        ToZone = "FACILITY"
                    };

                // Create a string-based JSON object of the object
                string objectAsJson = JsonConvert.SerializeObject(msgQConfig);
                // Now translate the JSON into bytes
                byte[] objectAsBytes = Encoding.UTF8.GetBytes(objectAsJson);

                // Create the full path to the configure zoneTransitions
                // Message Queu endpoint from default ItemSense URI
                string ZoneTransitionMessageQueueConfigApiEndpoint =
                    SolutionConstants.ItemSenseUri +
                    "/data/v1/messageQueues/zoneTransition/configure";

                // Create a WebRequest, identifying it as a POST request
                // with a JSON payload, and assign it the specified
                // credentials.
                WebRequest ItemSensePostRequest = 
                    WebRequest.Create(ZoneTransitionMessageQueueConfigApiEndpoint);
                ItemSensePostRequest.Credentials = 
                    new System.Net.NetworkCredential(
                        SolutionConstants.ItemSenseUsername,
                        SolutionConstants.ItemSensePassword
                        );
                ItemSensePostRequest.Proxy = null;
                ItemSensePostRequest.Method = "POST";
                ItemSensePostRequest.ContentType = "application/json";
                ItemSensePostRequest.ContentLength = objectAsBytes.Length;

                // Create an output data stream representation of the
                // POST WebRequest to output the data
                Stream dataStream = ItemSensePostRequest.GetRequestStream();
                dataStream.Write(objectAsBytes, 0, objectAsBytes.Length);
                dataStream.Close();

                // Execute the POST request and retain the response.
                using (HttpWebResponse ItemSenseResponse = (HttpWebResponse)ItemSensePostRequest.GetResponse())
                {
                    // The response StatusCode is a .NET Enum, so convert it to
                    // integer so that we can verify it against the status
                    // codes that ItemSense returns
                    ItemSense.ResponseCode ResponseCode = 
                        (ItemSense.ResponseCode)ItemSenseResponse.StatusCode;

                    // In this instance, we are only interested in whether
                    // the ItemSense response to the POST request was a "Success".
                    if (ItemSense.ResponseCode.Success == ResponseCode)
                    {
                        // Open a stream to access the response data which
                        // contains the AMQP URL and queue identifier
                        Stream ItemSenseDataStream = ItemSenseResponse.GetResponseStream();

                        // Only continue if an actual response data stream exists
                        if (null != ItemSenseDataStream)
                        {
                            // Create a StreamReader to access the resulting data
                            StreamReader objReader = new StreamReader(ItemSenseDataStream);

                            // Read the complete POST request results as a raw string
                            string itemSenseData = objReader.ReadToEnd();

                            // Now convert the raw JSON into a 
                            // AmqpMessageQueueDetails class
                            // representation
                            MsgQueueDetails =
                                JsonConvert.DeserializeObject<ItemSense.AmqpMessageQueueDetails>(
                                itemSenseData
                                );

                            MsgQueueDetails.ServerUrl = MsgQueueDetails.ServerUrl.Replace(":5672/%2F", string.Empty);

                            Console.WriteLine("Message Queue details:{0}URI: {1}{0}QueueID: {2}",
                                Environment.NewLine,
                                MsgQueueDetails.ServerUrl,
                                MsgQueueDetails.Queue
                                );

                            // Close the data stream. If we have got here,
                            // everything has gone well and there are no
                            // errors.
                            ItemSenseDataStream.Close();
                        }
                        else
                        {
                            Console.WriteLine("null ItemSense data stream.");
                        }
                    }
                    else
                    {
                        throw (new Exception(string.Format(
                            "ItemSense POST Response returned status of {0}",
                            ItemSenseResponse.StatusCode
                            )));
                    }
                }

                // Now that we have our MessageQueue, we can create a RabbitMQ
                // factory to handle connections to ItemSense AMQP broker
                ConnectionFactory factory = new ConnectionFactory()
                {
                    Uri = MsgQueueDetails.ServerUrl,
                    AutomaticRecoveryEnabled = true,
                    UserName = SolutionConstants.ItemSenseUsername,
                    Password = SolutionConstants.ItemSensePassword
                };

                // Now connect to the ItemSense factory
                using (var connection = factory.CreateConnection())

                // Create a fresh channel to handle message queue interactions
                using (var channel = connection.CreateModel())
                {
                    // Create an event consumer to receive incoming events
                    EventingBasicConsumer consumer =
                        new EventingBasicConsumer(channel);
                    // Bind an event handler to the message received event
                    consumer.Received += consumer_Received;

                    // Initiate consumption of data from the ItemSense queue
                    channel.BasicConsume(queue: MsgQueueDetails.Queue,
                                         noAck: true,
                                         consumer: consumer);

                    // Hang on here until user presses Enter
                    Console.WriteLine(" Press <Enter> to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0} ({1}){2}",
                    ex.Message,
                    ex.GetType(),
                    (null == ex.InnerException) ? string.Empty : Environment.NewLine + ex.InnerException.Message
                    );
            }
        }

        // Message received event handler
        static void consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
        }
    }
}
