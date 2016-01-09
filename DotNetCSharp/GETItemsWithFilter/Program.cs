using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace DotNetExamples
{
    class Program
    {
        /// <summary>
        /// This program performs a GET request to the Items endpoint of the
        /// ItemSense instance specified in the SolutionConstants class in
        /// the SolutionItems project. The GET request is qualified by a filter,
        /// and the results are converted into an Items class representation
        /// and output in CSV format.
        /// </summary>
        /// <param name="args">
        /// Command line parameters ignored.
        /// </param>
        static void Main(string[] args)
        {
            try
            {
                // Create an ItemsEndpointOptions object to specify the
                // filter(s) to use for the GET request
                ItemSense.ItemsEndpointOptions itemsFilter = new ItemSense.ItemsEndpointOptions();

                // Add an EPC prefix filter
                itemsFilter.SetEpcPrefix("3030");

                // Create the full path to the Items show (i.e. GET) endpoint
                // from default ItemSense URI and the defined filter values
                string ShowApiEndpointPath = 
                    "/data/v1/items/show" + 
                    itemsFilter.GetFullOptionsString();
                string ItemSenseShowItemsApiEndpoint =
                    SolutionConstants.ItemSenseUri +
                    ShowApiEndpointPath;

                Console.WriteLine("Full GET request URI:{0}{1}",
                    Environment.NewLine,
                    ItemSenseShowItemsApiEndpoint
                    );

                // Create the web request, with credentials, setting the Proxy
                // parameter to null in order to improve performance for
                // environments that do not use a proxy. Without this setting,
                // a request can take up to 20 seconds to identify that there
                // is no proxy present.
                WebRequest ItemSenseGetRequest = WebRequest.Create(ItemSenseShowItemsApiEndpoint);
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
                    int ResponseCode = (int)ItemSenseResponse.StatusCode;
                    Console.WriteLine("ItemSense GET request response: {0}", ResponseCode);

                    // Open a stream to access the response data.
                    Stream ItemSenseDataStream = ItemSenseResponse.GetResponseStream();

                    // Only continue if an actual response data stream exists
                    if (null != ItemSenseDataStream)
                    {
                        // Create a StreamReader to access the resulting data
                        StreamReader objReader = new StreamReader(ItemSenseDataStream);

                        // Read the complete GET request results as a raw string
                        string RawResultData = objReader.ReadToEnd();

                        // Now convert the raw JSON into an Items class
                        // representation
                        ItemSense.Items GetResultData = 
                            JsonConvert.DeserializeObject<ItemSense.Items>(
                            RawResultData
                            );

                        // Now iterate through the list of items and output in
                        // CSV format
                        Console.WriteLine("Items data:");
                        foreach (ItemSense.Item currentItem in GetResultData.ItemList)
                        {
                            Console.WriteLine(currentItem.itemToCsvString());
                        }

                        // Close the data stream. If we have got here,
                        // everything has gone well and there are no
                        // errors.
                        ItemSenseDataStream.Close();

                        // Wait for the user to say when to close the application
                        Console.WriteLine("Press <Enter> to exit application.");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("null ItemSense data stream.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Print out the details of any exception that occur
                Console.WriteLine("Exception: {0} ({1}){2}",
                    ex.Message,
                    ex.GetType(),
                    (null == ex.InnerException) ?
                        string.Empty :
                        Environment.NewLine + ex.InnerException.Message
                    );
            }
        }
    }
}
