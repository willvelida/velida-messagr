using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VelidaMessagr.Functions
{
    public class ReceiveMessageFromServiceBus
    {
        private readonly ILogger<ReceiveMessageFromServiceBus> _logger;


        public ReceiveMessageFromServiceBus(
            ILogger<ReceiveMessageFromServiceBus> logger)
        {
            _logger = logger;
        }

        [FunctionName("ReceiveMessageFromServiceBus")]
        public void Run([ServiceBusTrigger("velidatopic", "velidasubscripion", Connection = "ServiceBusConnectionString")] string mySbMsg)
        {
            Message message = new Message(Encoding.UTF8.GetBytes(mySbMsg));
            var jsonPayload = JsonConvert.SerializeObject(message.Body);
            _logger.LogInformation($"Message processed: {jsonPayload}");
        }

    }
}
