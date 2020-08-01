using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace VelidaMessagr.Functions
{
    public class ReceiveMessageFromQueue
    {
        private readonly ILogger<ReceiveMessageFromQueue> _logger;

        public ReceiveMessageFromQueue(
            ILogger<ReceiveMessageFromQueue> logger)
        {
            _logger = logger;
        }

        [FunctionName("ReceiveMessageFromQueue")]
        public void Run([QueueTrigger("velidaqueue", Connection = "StorageConnectionString")] string myQueueItem)
        {
            _logger.LogInformation($"Queue item processed: {myQueueItem}");
        }
    }
}
