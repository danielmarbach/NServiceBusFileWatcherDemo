using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;

namespace Receiver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var instanceName = args.FirstOrDefault();

            if (string.IsNullOrEmpty(instanceName))
            {
                Console.Title = "Destination1";

                instanceName = "destination1";
            }
            else
            {
                Console.Title = $"{instanceName}";
            }

            var instanceId = DeterministicGuid.Create(instanceName);

            var endpointConfiguration = new EndpointConfiguration(instanceName);
            endpointConfiguration.UseTransport<LearningTransport>();
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");

            // just to show the flexibility
            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(c => c.NumberOfRetries(3));
            recoverability.Delayed(c => c.NumberOfRetries(2).TimeIncrease(new TimeSpan(2)));

            // this is just here to make it possible to retry a message that went into the error queue
            endpointConfiguration.Notifications.Errors.MessageSentToErrorQueue += (sender, message) =>
            {
                Handler.messageIds.TryAdd(message.MessageId, message.MessageId);
            };

            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(instanceName)
                .UsingCustomIdentifier(instanceId);

            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                "Particular.Monitoring",
                TimeSpan.FromMilliseconds(500)
            );

            var endpoint = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine($"Started {instanceName}");
            Console.ReadLine();
            await endpoint.Stop();
        }

        static class DeterministicGuid
        {
            public static Guid Create(params object[] data)
            {
                // use MD5 hash to get a 16-byte hash of the string
                using (var provider = new MD5CryptoServiceProvider())
                {
                    var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
                    var hashBytes = provider.ComputeHash(inputBytes);
                    // generate a guid from the hash:
                    return new Guid(hashBytes);
                }
            }
        }
    }
}
