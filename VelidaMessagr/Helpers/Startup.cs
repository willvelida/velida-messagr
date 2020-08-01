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

            builder.Services.AddSingleton(s => new TopicClient(config["ServiceBusConnectionString"], config["TopicName"]));
            builder.Services.AddSingleton(s => new SubscriptionClient(config["ServiceBusConnectionString"], config["TopicName"], config["SubscriptionName"]));

            var storageAccount = CloudStorageAccount.Parse(config["StorageConnectionString"]);
            builder.Services.AddSingleton((s) => storageAccount.CreateCloudQueueClient());
        }
    }
}
