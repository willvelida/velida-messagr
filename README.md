# velida-messagr

Sample Repository that explores both Azure Service Bus and Azure Queue Storage

# Related Blog Posts

[DEV.to](https://dev.to/willvelida/exploring-azure-service-bus-and-azure-queue-storage-queues-4p1g)
[Medium Article](https://medium.com/@willvelida/exploring-azure-service-bus-and-azure-queue-storage-queues-c6b1760ac3e4)

![](https://miro.medium.com/max/512/1*mlzgRZzHla4RbF4LwjxJeg.png)

**In four weeks time**, I plan to take my AZ-204 exam. I’ve been working with Azure in my day-to-day job for almost 2 years now, but taking a look at the exam skills outline, there are a few technologies on there that I’ve never touched before.

So in order to help my preparation for the exam, I plan to write a series of blog posts on each skill measured. Hopefully this will not just reinforce my learning, but also help others who may be taking AZ-204 themselves, or just want to learn something new.

In this article, I’m going to be looking at the two technologies tested for developing message-based solutions, **Azure Service Bus** and **Azure Queue Storage queues**. My team primarily uses Event Hubs as our messaging broker, so it’s going to be interesting to learn what Service Bus and Queue Storage can offer.

For this tutorial, I’ll be developing a Azure Function app that will send messages to both Service Bus and Queue Storage (via separate POST requests). [The full sample code can be found here](https://github.com/willvelida/velida-messagr).

## What is Azure Service Bus?

**Service Bus is a fully managed enterprise-level messaging broker that allows applications to communicate with each other in a way that’s reliable and secure**. Messages are raw data that can be sent asynchronously to Service Bus which can then be processed by another application that is connected to Service Bus. These messages can be in JSON, XML or just text format.

There are a few components that make up Service Bus:

_Namespace_

This is a container for all messaging components. A single namespace can contain multiple queues and topics, and namespaces can serve as application containers. If our app has different components, we can connect these components to the topics and queues that are in the namespace.

_Queues_

These are the container of messages where messages are sent and received from. The queue will store the message until our receiving application retrieves that message and processes it. This works on a FIFO (First-In, First-Out) stack.

When we get a new message in our queue, Service Bus will assign a timestamp to that message. When this message is processed, it will be held in redundant storage.

Messages in our queue are delivered in pull mode, meaning that they will only be delivered when an application requests it.

_Topics_

We can also use topics for sending and receiving messages. The difference here is that topics can have several applications receiving messages rather than just one. This scenario would be referred to as publish/subscribe. Topics can have multiple subscribers. Each subscriber to a topic will receive a copy of the message sent to the topic.

## Provisioning Azure Service Bus

We can create a Service Bus namespace, along with topics within that namespace fairly easily. To do so, we need to search for Service Bus within the Integration section in the Azure Marketplace:

![](https://miro.medium.com/max/700/1*lb7djnL4TVb7GXvwJdfqxA.png)

Now we need to configure our Service Bus. We can give our namespace a name, a location that we want to provision it in and assign it a resource group.

The important thing to note here is that we need to select the Standard pricing tier. _We can’t create topics using just the Basic tier_.

![](https://miro.medium.com/max/700/1*PXcdjx8wpc8hlXVp8ZTOew.png)

Click “create ”and wait for your new Service Bus instance to be deployed. Once it’s been deployed, let’s go to our new resource and create a Topic. We can do this by heading into our overview panel and clicking the Topic button:

![](https://miro.medium.com/max/700/1*nHRBNZvecHtsQpjw6YFL8w.png)

Give your topic a name and enable duplicate detection. This will mean that the topic does store any duplicate messages within our configured Duplicate detection window:

![](https://miro.medium.com/max/700/1*6zXvg1tGY19_9HNUumv4eQ.png)

Click Create to create our topic.

Now we can create a subscription for our Topic, to do this, click on Topics in your Service Bus panel (should be underneath Entities).

Pick your topic (the one that you created earlier):

![](https://miro.medium.com/max/700/1*xUdt5WFCGB7h6o_pEByF9A.png)

In the Overview screen of your Topic, click on the Subscription Button:

![](https://miro.medium.com/max/700/1*EAyhvyT8fTK08YMlY4b1kQ.png)

Give your Topic a name and a Max delivery count value (must be between 1 and 2000):

![](https://miro.medium.com/max/700/1*wHH0HblgdVR4TJwLyhzHJQ.png)

Now that we have our Service Bus, Topic and Subscription created, let’s dive into some code!

## Show me the code!

Instead of using a Console App to send and recieve my messages, I’ll be using a Azure Function App and triggering the sending of messages using a HTTP Trigger. In order to work with both Service Bus and Queue Storage, we’ll need to install the following packages in NuGet:

```
Microsoft.Azure.ServiceBus
Microsoft.Azure.Storage.Common
Microsoft.Azure.Storage.Queue
```

I’m going to be developing a Azure Function using Dependency Injection so I can share Singleton objects across my various Functions. In my Startup class, I’ve set up the following Clients:

* **TopicClient**: Here, I’m allowing my function to subscribe to my Service Bus topic by passing through my connection string to Service Bus along with the name of my topic.
* **SubscriptionClient**: As with my topic, I’m registering my Function App as a subscriber to a topic by passing through my connection string, topic name and subscription name.
* **CreateCloudQueueClient**: Here, we’re passing our connection string to our Storage account and then creating a Singleton of a CloudQueueClient.

_Startup.cs_

```
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VelidaMessagr.Helpers;
[assembly: FunctionsStartup(typeof(Startup))]
namespace VelidaMessagr.Helpers
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });
var config = new ConfigurationBuilder().
                SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
builder.Services.AddSingleton<IConfiguration>(config);
// Setting up our Topic Client
            builder.Services.AddSingleton(s => new TopicClient(config["ServiceBusConnectionString"], config["TopicName"]));
// Setting up our Subscription client
            builder.Services.AddSingleton(s => new SubscriptionClient(config["ServiceBusConnectionString"], config["TopicName"], config["SubscriptionName"]));
// Setting up our Cloud Storage Queue Client
            var storageAccount = CloudStorageAccount.Parse(config["StorageConnectionString"]);
            builder.Services.AddSingleton((s) => storageAccount.CreateCloudQueueClient());
        }
    }
}
```

After we’ve set up our various clients, we can start sending messages to Service Bus. In the code snippet below, we’re doing the following:

* We use Bogus.Faker to generate some fake messages to send. This will be created on our POST request. This will be generated as a list
* For each message in our message list, we convert the object to JSON, create a new Message() object and then send it to our topic client.

_PostMessageToServiceBus.cs_

```
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using Bogus;
using VelidaMessagr.Models;
using System.Collections.Generic;
using System.Text;
namespace VelidaMessagr.Functions
{
    public class PostMessageToServiceBus
    {
        private readonly ILogger<PostMessageToServiceBus> _logger;
        private readonly TopicClient _topicClient;
public PostMessageToServiceBus(
            ILogger<PostMessageToServiceBus> logger,
            TopicClient topicClient)
        {
            _logger = logger;
            _topicClient = topicClient;
        }
[FunctionName("PostMessageToServiceBus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ServiceBus")] HttpRequest req)
        {
            IActionResult result = null;
try
            {
                // Generate fake readings
                var fakeReadings = new Faker<DeviceReading>()
                    .RuleFor(i => i.ReadingId, (fake) => Guid.NewGuid().ToString())
                    .RuleFor(i => i.Temperature, (fake) => Math.Round(fake.Random.Decimal(0.00m, 150.00m), 2))
                    .RuleFor(i => i.Location, (fake) => fake.PickRandom(new List<string> { "New Zealand", "United Kingdom", "Canada" }))
                    .Generate(10);
foreach (var reading in fakeReadings)
                {
                    var jsonPayload = JsonConvert.SerializeObject(reading);
                    var message = new Message(Encoding.UTF8.GetBytes(jsonPayload));
                    await _topicClient.SendAsync(message);
                    _logger.LogInformation($"Sending message: {jsonPayload}");
                }
result = new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
return result;
        }
    }
}
```
We can now create subscribers to our Service Bus topics. We can achieve this by doing the following:

```
subscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, SubscriptionName);
var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
{
  // Handling our message
}
subscriptionClient.RegisterMessageHandler(ProcessMessageAsync, messageHandlerOptions);
subscriptionClient.CloseAsync();
```

Depending on what we need to do with our message, we can configure how we can handle exceptions and process our messages.

Now let’s turn our attention to Queue Storage.

## What are Azure Queue Storage Queues?

Azure Queue Storage is a service for storing large numbers of messages. You can store more than 80GB of messages in a queue, but each message can be up to 64KB in size. We can access these messages from anywhere using either HTTP or HTTPS authenticated calls.

Like Service Bus, queues work on a FIFO basis, but the ordering of our messages will not be guaranteed.

We can only access an Azure Queue by using either the REST API or the .NET Azure Storage SDK.

## Provisioning Azure Queue Storage Queues

Creating an Azure Storage Account is fairly straightforward. Head back to the Azure Portal and this time, look for Storage Account in the Azure Marketplace.

Give your storage account a name, a resource group, and ensure that the replication is set to Locally Redundant Storage (LRS). You can leave the defaults as is:

![](https://miro.medium.com/max/700/1*ZGCVeyeO4rQ9H4xYhqM6lQ.png)

When the deployment finishes, grab your access key as we’ll need that later. Now we can create queues in your Azure Storage account, but let’s do it within our Function

## Show me the code!

I’ve created a new Function that sends messages to our Queue Storage Queue on a POST request. This Function does the following:

* After generating a list of messages to send, it looks for a reference to the name of our queue via the .GetQueueReference() method.
* It creates our queue if it doesn’t exist via the .CreateIfNotExists() method.
* For each message within our list, we create a new CloudQueueMessage instance and send it to our QueueClient.

_PostMessageToQueue.cs_

```
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Storage.Queue;
using VelidaMessagr.Models;
using Bogus;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
namespace VelidaMessagr.Functions
{
    public class PostMessageToQueue
    {
        private readonly ILogger<PostMessageToQueue> _logger;
        private readonly CloudQueueClient _cloudQueueClient;
        private readonly IConfiguration _config;
public PostMessageToQueue(
            ILogger<PostMessageToQueue> logger,
            CloudQueueClient cloudQueueClient,
            IConfiguration config)
        {
            _logger = logger;
            _cloudQueueClient = cloudQueueClient;
            _config = config;
        }
[FunctionName("PostMessageToQueue")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "QueueClient")] HttpRequest req)
        {
            IActionResult actionResult = null;
try
            {
                // Generate fake readings
                var fakeReadings = new Faker<DeviceReading>()
                    .RuleFor(i => i.ReadingId, (fake) => Guid.NewGuid().ToString())
                    .RuleFor(i => i.Temperature, (fake) => Math.Round(fake.Random.Decimal(0.00m, 150.00m), 2))
                    .RuleFor(i => i.Location, (fake) => fake.PickRandom(new List<string> { "New Zealand", "United Kingdom", "Canada" }))
                    .Generate(10);
CloudQueue queue = _cloudQueueClient.GetQueueReference(_config["QueueName"]);
queue.CreateIfNotExists();
foreach (var reading in fakeReadings)
                {
                    var jsonPayload = JsonConvert.SerializeObject(reading);
                    CloudQueueMessage cloudQueueMessage = new CloudQueueMessage(jsonPayload);
                    queue.AddMessage(cloudQueueMessage);
                }
actionResult = new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown: {ex.Message}");
                actionResult = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
return actionResult;
        }
    }
}
```
Now that we have added messages to our queue, we can either read messages without removing it from our queue, or we can read it and remove it.

If we just wanted to read the message, we could do the following:

```
// Get our messages in a List
List<CloudQueueMessage> messages = (queue.PeekMessages((int)cachedMessageCount)).ToList();
// Iterate through our list
foreach (CloudQueueMessage peekedMessage in messages)
{
  Console.WriteLine($"Message: {peekedMessage.AsString}");
}
```

If we want to remove the message, we just need to make a small addition to our code:

```
// Get our messages in a List
List<CloudQueueMessage> messages = (queue.PeekMessages((int)cachedMessageCount)).ToList();
// Iterate through our list
foreach (CloudQueueMessage message in messages)
{
  Console.WriteLine($"Message: {message.AsString}");
  queue.DeleteMessage(message);
}
```

## Conclusion

In this article, we looked at both Azure Service Bus and Azure Queue Storage. Queue Storage Queues can process a large message of messages, while Service Bus allows different applications to communicate with each other in a reliable way.

Both brokers work on a FIFO basis, but Queues don’t guarantee the order. We also can’t create subscriptions to Queue Storage like we can in Azure Service Bus. We can create subscribers to topics in Azure Service Bus that allow multiple subscribers to subscribe to a topic and then each subscriber will receive a copy of a message that is sent to that topic.

If you want to learn more about Azure Service Bus and Queue Storage Queues, be sure to check out the following links:

* [Communicate between applications with Azure Queue Storage](https://docs.microsoft.com/en-us/learn/modules/communicate-between-apps-with-azure-queue-storage/)
* [Service Bus queues, topics and subscriptions](http://service%20bus%20queues%2C%20topics%2C%20and%20subscriptions/)
