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
    {
        /// <summary>
        /// This program performs a POST request to the create ReaderDefinitions
        /// endpoint of theItemSense instance specified in the SolutionConstants
        /// class in the SolutionItems project.
        /// </summary>
        /// <param name="args">
        /// Command line parameters ignored.
        /// </param>
        static void Main(string[] args)
        {
            try
            {
                // Create and configure a ReaderDefinition object
                ItemSense.ReaderDefinition newReaderDefinition = 
                    new ItemSense.ReaderDefinition()
                {
                    Name = "postTest",
                    Address = "postTest.Impinj.com",
                    Type = ReaderType.XARRAY,
                    Placement = new ItemSense.Placement()
                    {
                        X = 0,
                        Y = 0,
                        Z = 0,
                        Pitch = 0,
                        Yaw = 0,
                        Roll = 0
                    },
                    Facility = "",
                    ReaderZone = "postTestZone"
                };

                // Create a string-based JSON object of the object
                string objectAsJson = JsonConvert.SerializeObject(newReaderDefinition);
                // Now translate the JSON into bytes
                byte[] objectAsBytes = Encoding.UTF8.GetBytes(objectAsJson);

                // Create the full path to the configure zoneTransitions
                // Message Queu endpoint from default ItemSense URI
                string ReaderDefinitionPostApiEndpoint =
                    SolutionConstants.ItemSenseUri +
                    "/configuration/v1/readerDefinitions/create";

                // Create a WebRequest, identifying it as a POST request
                // with a JSON payload, and assign it the specified
                // credentials.
                WebRequest ItemSensePostRequest =
                    WebRequest.Create(ReaderDefinitionPostApiEndpoint);
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

                Console.Write("POSTing ReaderDefinition ... ");

                // Execute the POST request and retain the response.
                using (HttpWebResponse ItemSenseResponse = (HttpWebResponse)ItemSensePostRequest.GetResponse())
                {
                    // The response StatusCode is a .NET Enum, so convert it to
                    // an ItemSense response code
                    ItemSense.ResponseCode ResponseCode =
                        (ItemSense.ResponseCode)ItemSenseResponse.StatusCode;

                    // In this instance, we are only interested in whether
                    // the ItemSense response to the POST request was a "Success".
                    if (ItemSense.ResponseCode.Success != ResponseCode)
                    {
                        throw (new Exception(string.Format(
                            "ItemSense POST Response returned status of {0}",
                            ItemSenseResponse.StatusCode
                            )));
                    }
                }

                Console.WriteLine("Done.");

                // Hang on here until user presses Enter
                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
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

        static void consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
        }
    }
}
