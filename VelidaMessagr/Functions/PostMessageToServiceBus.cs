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
                    _logger.LogInformation($"Sending message: {message.Body}");
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
