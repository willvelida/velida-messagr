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
