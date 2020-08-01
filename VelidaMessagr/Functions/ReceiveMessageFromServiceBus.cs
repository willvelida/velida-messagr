using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace VelidaMessagr.Functions
{
    public static class ReceiveMessageFromServiceBus
    {
        [FunctionName("ReceiveMessageFromServiceBus")]
        public static void Run([ServiceBusTrigger("mytopic", "mysubscription", Connection = "")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}
