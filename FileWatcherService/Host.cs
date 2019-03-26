using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

namespace FileWatcherService
{
    class Host
    {
        // TODO: optionally choose a custom logging library
        // https://docs.particular.net/nservicebus/logging/#custom-logging
        // LogManager.Use<TheLoggingFactory>();
        static readonly ILog log = LogManager.GetLogger<Host>();

        IEndpointInstance endpoint;
        private FileSystemWatcher fileSystemWatcher;

        // TODO: give the endpoint an appropriate name
        public string EndpointName => "FileWatcherService";

        public async Task Start()
        {
            try
            {
                // TODO: consider moving common endpoint configuration into a shared project
                // for use by all endpoints in the system
                var endpointConfiguration = new EndpointConfiguration(EndpointName);

                // TODO: ensure the most appropriate serializer is chosen
                // https://docs.particular.net/nservicebus/serialization/
                endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

                endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);

                // TODO: remove this condition after choosing a transport, persistence and deployment method suitable for production
                if (Environment.UserInteractive && Debugger.IsAttached)
                {
                    // TODO: choose a durable transport for production
                    // https://docs.particular.net/transports/
                    var transportExtensions = endpointConfiguration.UseTransport<LearningTransport>();

                    // TODO: choose a durable persistence for production
                    // https://docs.particular.net/persistence/
                    endpointConfiguration.UsePersistence<LearningPersistence>();

                    // TODO: create a script for deployment to production
                    endpointConfiguration.EnableInstallers();
                }

                var metrics = endpointConfiguration.EnableMetrics();
                metrics.SendMetricDataToServiceControl(
                    "Particular.Monitoring",
                    TimeSpan.FromMilliseconds(500)
                );

                // TODO: perform any futher start up operations before or after starting the endpoint
                endpoint = await Endpoint.Start(endpointConfiguration);

                var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                fileSystemWatcher = new FileSystemWatcher {Path = directoryPath };
                fileSystemWatcher.Created += (sender, args) => FileCreated(sender, args, endpoint);
                fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                FailFast("Failed to start.", ex);
            }
        }

        private static async void FileCreated(object sender, FileSystemEventArgs e, IMessageSession session)
        {
            while (IsFileLocked(e.FullPath))
            {
                await Task.Delay(100);
            }

            // plainly read everything for demo purposes
            var content = File.ReadAllText(e.FullPath);
            // assume file name is also the destination to show dynamic sending
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(e.FullPath);
            var destination = fileNameWithoutExtension.Split(Convert.ToChar("."))[0];
            await session.Send(destination, new DoSomethingWithFileCommand { Content = content });

            Console.WriteLine($"Sent command with content '{content}' to destination '{destination}'");
            File.Delete(e.FullPath);
        }
        static bool IsFileLocked(string filePath)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(filePath,FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        public async Task Stop()
        {
            try
            {
                // TODO: perform any futher shutdown operations before or after stopping the endpoint
                await endpoint?.Stop();
            }
            catch (Exception ex)
            {
                FailFast("Failed to stop correctly.", ex);
            }
        }

        async Task OnCriticalError(ICriticalErrorContext context)
        {
            // TODO: decide if stopping the endpoint and exiting the process is the best response to a critical error
            // https://docs.particular.net/nservicebus/hosting/critical-errors
            // and consider setting up service recovery
            // https://docs.particular.net/nservicebus/hosting/windows-service#installation-restart-recovery
            try
            {
                await context.Stop();
            }
            finally
            {
                FailFast($"Critical error, shutting down: {context.Error}", context.Exception);
            }
        }

        void FailFast(string message, Exception exception)
        {
            try
            {
                log.Fatal(message, exception);

                // TODO: when using an external logging framework it is important to flush any pending entries prior to calling FailFast
                // https://docs.particular.net/nservicebus/hosting/critical-errors#when-to-override-the-default-critical-error-action
            }
            finally
            {
                Environment.FailFast(message, exception);
            }
        }
    }
}
