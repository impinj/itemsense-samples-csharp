using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace DotNetExamples
{
    class Program
    {
        /// <summary>
        /// This program is an example of a very simple ItemSense Coordinator
        /// Connector. After starting an ItemSense job via a POST request,
        /// the system periodically performs a GET request of the Items API
        /// endpoint and presents to the useronly item data that has been
        /// updated since the start of the job.
        /// </summary>
        /// <param name="args">
        /// Command line parameters ignored.
        /// </param>
        static void Main(string[] args)
        {
            int itemUpdateIntervalInSeconds = 20;
            int jobDurationInSeconds = 60;
            DateTime jobCreationDateTime = DateTime.Now;
            Dictionary<string, ItemSense.Item> itemsFromCurrentJob = new Dictionary<string, ItemSense.Item>();

            try
            {
                // Create and configure a ReaderDefinition object
                ItemSense.Job newJobDefinition =
                    new ItemSense.Job()
                {
                    RecipeName = "IMPINJ_BasicLocation",
                    DurationSeconds = jobDurationInSeconds,
                    StartDelay = 0,
                    ReportToDatabaseEnabled = true,
                    ReportToMessageQueueEnabled = true
                };

                // Create a string-based JSON object of the object
                string objectAsJson = JsonConvert.SerializeObject(newJobDefinition);
                // Now translate the JSON into bytes
                byte[] objectAsBytes = Encoding.UTF8.GetBytes(objectAsJson);

                // Create the full path to the configure zoneTransitions
                // Message Queu endpoint from default ItemSense URI
                string ReaderDefinitionPostApiEndpoint =
                    SolutionConstants.ItemSenseUri +
                    "/control/v1/jobs/start";

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

                // Execute the POST request and retain the response.
                using (HttpWebResponse ItemSenseResponse = (HttpWebResponse)ItemSensePostRequest.GetResponse())
                {
                    // The response StatusCode is a .NET Enum, so convert it to
                    // an ItemSense response code
                    ItemSense.ResponseCode ResponseCode =
                        (ItemSense.ResponseCode)ItemSenseResponse.StatusCode;

                    // In this instance, we are only interested in whether
                    // the ItemSense response to the POST request was a "Success".
                    if (ItemSense.ResponseCode.Success == ResponseCode)
                    {
                        // Open a stream to access the response data which
                        // contains the Job identifier
                        Stream ItemSenseDataStream = ItemSenseResponse.GetResponseStream();

                        // Only continue if an actual response data stream exists
                        if (null != ItemSenseDataStream)
                        {
                            // Create a StreamReader to access the resulting data
                            StreamReader objReader = new StreamReader(ItemSenseDataStream);

                            // Read the complete POST request results as a raw string
                            string itemSenseData = objReader.ReadToEnd();

                            // Now convert the raw JSON into an Items class
                            // representation
                            ItemSense.JobResponse PostResponseData =
                                JsonConvert.DeserializeObject<ItemSense.JobResponse>(
                                itemSenseData
                                );

                            string jobCreationTime = PostResponseData.CreationTime.Replace("Z[Etc/UTC]", string.Empty);

                            Console.WriteLine("Job started; ID: {0}",
                                PostResponseData.Id
                                );

                            jobCreationDateTime = DateTime.Parse(
                                jobCreationTime,
                                null,
                                System.Globalization.DateTimeStyles.RoundtripKind
                                );

                            Console.WriteLine("Job created at time: {0} [Etc/UTC]",
                                jobCreationDateTime
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

                // Periodically poll the Items endpoint for data
                for (int i = 0; i < jobDurationInSeconds; i += itemUpdateIntervalInSeconds)
                {
                    Console.Write("Getting Items data update ... ");
                    // Request all of the Items data from the Items endpoint
                    List<ItemSense.Item> allItems = GetItems();

                    // Now iterate through the returned list and only add those
                    // items identified during this job to the final list
                    foreach(ItemSense.Item currentItem in allItems)
                    {
                        // Convert the current item time into a value that can be
                        // compared
                        DateTime itemTime = DateTime.Parse(
                            currentItem.LastModifiedTime,
                            null,
                            System.Globalization.DateTimeStyles.RoundtripKind
                            );

                        // Write the item data to AllItemData only if the time
                        // filter requiremente are met
                        if (itemTime >= jobCreationDateTime)
                        {
                            ItemSense.Item itemFromDictionary;

                            if (false == itemsFromCurrentJob.TryGetValue(currentItem.Epc, out itemFromDictionary))
                            {
                                itemsFromCurrentJob.Add(currentItem.Epc, currentItem);
                            }
                            else
                            {
                                itemsFromCurrentJob[currentItem.Epc] = currentItem;
                            }
                        }
                    }
                    Console.WriteLine("Done.");

                    Thread.Sleep(itemUpdateIntervalInSeconds * 1000);
                }

                // Print out the results
                Console.WriteLine("Items data:");
                foreach (KeyValuePair<string, ItemSense.Item> kvp in itemsFromCurrentJob)
                {
                    Console.WriteLine(kvp.Value.itemToCsvString());
                }

                // Hang on here until user presses Enter
                Console.WriteLine(" Press <Enter> to exit.");
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

        static private List<ItemSense.Item> GetItems()
        {
            List<ItemSense.Item> itemsToReturn = new List<ItemSense.Item>();

            try
            {
                // Create an ItemsEndpointOptions object to specify the
                // filter(s) to use for the GET request
                ItemSense.ItemsEndpointOptions itemsFilter = new ItemSense.ItemsEndpointOptions();
                string nextPageMarker = null;

                do
                {
                    itemsFilter.SetPageMarker(nextPageMarker);
                    // Create the full path to the Items show (i.e. GET) endpoint
                    // from default ItemSense URI and the defined filter values
                    string ShowApiEndpointPath =
                        "/data/v1/items/show" +
                        itemsFilter.GetFullOptionsString();
                    string ItemSenseShowItemsApiEndpoint =
                        SolutionConstants.ItemSenseUri +
                        ShowApiEndpointPath;

                    string itemSenseData = GetItemSenseData(ItemSenseShowItemsApiEndpoint);

                    // Now convert the raw JSON into an Items class
                    // representation
                    ItemSense.Items GetResultData =
                        JsonConvert.DeserializeObject<ItemSense.Items>(
                        itemSenseData
                        );
                    itemsToReturn.AddRange(GetResultData.ItemList);

                    nextPageMarker = GetResultData.NextPageMarker;
                }
                while (null != nextPageMarker);

                return itemsToReturn;
            }
            catch
            {
                throw;
            }
        }

        static private string GetItemSenseData(string endpointUri)
        {
            string itemSenseData = string.Empty;

            // Create the web request, with credentials, setting the Proxy
            // parameter to null in order to improve performance for
            // environments that do not use a proxy. Without this setting,
            // a request can take up to 20 seconds to identify that there
            // is no proxy present.
            WebRequest ItemSenseGetRequest = WebRequest.Create(endpointUri);
            ItemSenseGetRequest.Proxy = null;
            ItemSenseGetRequest.Credentials =
                new System.Net.NetworkCredential(
                    SolutionConstants.ItemSenseUsername,
                    SolutionConstants.ItemSensePassword
                    );

            // Execute the GET request and retain the JSON data returned
            // in the response.
            using (HttpWebResponse ItemSenseResponse = (HttpWebResponse)ItemSenseGetRequest.GetResponse())
            {
                // The response StatusCode is a .NET Enum, so convert it to
                // integer so that we can verify it against the status
                // codes that ItemSense returns
                ItemSense.ResponseCode ResponseCode = 
                    (ItemSense.ResponseCode)ItemSenseResponse.StatusCode;
                if (ItemSense.ResponseCode.Success != ResponseCode)
                {
                    Console.WriteLine("ItemSense GET request response: {0}", ResponseCode);
                }

                // Open a stream to access the response data.
                Stream ItemSenseDataStream = ItemSenseResponse.GetResponseStream();

                // Only continue if an actual response data stream exists
                if (null != ItemSenseDataStream)
                {
                    // Create a StreamReader to access the resulting data
                    StreamReader objReader = new StreamReader(ItemSenseDataStream);

                    // Read the complete GET request results as a raw string
                    itemSenseData = objReader.ReadToEnd();

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

            return itemSenseData;
        }
    }
}
