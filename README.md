# ItemSense API Client Examples
This is a repository of C# .NET example code that demonstrates how to interact with both the ItemSense RESTful API endpoints and AMQP message queues. This code is provided as a set of Visual Studio 2010 projects within a single solution.

## Projects
The following projects, in alphabetical order, are included in this solution:


| Project | Description |
| ------ | ----------- |
| AmqpExampleClient | An example of how to use the ItemSense RESTful API to create an AMQP message queue that tag transition events are sent to, and to then bind to the queue using an AMQP client |
| GETItemsWithFilter | An example of how to use an HTTP GET request to access the Items  endpoint to retrieve a single page of item level data, using filters to qualify the data that is returned by ItemSense (e.g. request only tags with a specific EPC prefix) |
| GETItemsWithJsonDecode | An example of how to use an HTTP GET request to access the Items  endpoint to retrieve a single page of item level JSON data and to decode the returned data into native .NET objects |
| GETItemsWithPageMarker | An example of how to use an HTTP GET request to access the Items  endpoint to retrieve all of the item level JSON data in the ItemSense repository by using the NextPageMarker to navigate through the individual pages of data |
| POSTReaderDefinition | An example of how to use an HTTP POST request to create a new RAIN RFID reader definition using the ReaderDefinition  endpoint |
| SimpleCoordinatorExample | An example of a simple ItemSense connector that runs a RAIN RFID data collection job and then polls the Items  endpoint to access the Items in the ItemSense repository |
| SimpleGETItems | An example of how to use an HTTP GET request to access the Items  endpoint to retrieve a page of item level JSON data |

## SolutionItems
Included within the solution is a project named `SolutionItems` that contains common classes that are used in many of the projects. Especially important within this project is the `SolutionConstants.cs` file. Defined in this file are the data members that are used to identify the ItemSense instance that is to be used by every project in the solution. The data members are:

| Data member | Description |
| ----------- | ----------- |
| `ItemSenseUri` | The URI of the ItemSense instance to use with these examples, e.g. **http://itemsense.example.impinj.net/itemsense** |
| `ItemSenseUsername` | The role-based access username to use when accessing the ItemSense instance |
| `ItemSensePassword` | The password associated with the ItemSense user defined by `ItemSenseUsername` | 

## Third Party Libraries
The following third party libraries, located within the Libs directory, are used in this solution:

- The Json.Net high-performance framework for .NET from [Newtonsoft](http://www.newtonsoft.com/json "Newtonsoft")
- The [RabbitMQ](https://www.rabbitmq.com/ "RabbitMQ") AMQP client
