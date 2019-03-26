using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Receiver
{
    public class Handler : IHandleMessages<DoSomethingWithFileCommand>
    {
        internal static ConcurrentDictionary<string, string> messageIds = new ConcurrentDictionary<string, string>();

        public Task Handle(DoSomethingWithFileCommand message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Received {message.Content}");

            if (message.Content == "transient" && !messageIds.TryRemove(context.MessageId, out _))
            {
                messageIds.TryAdd(context.MessageId, context.MessageId);
                throw new TimeoutException();
            }

            if (message.Content == "permanent" && !messageIds.TryRemove(context.MessageId, out _))
            {
                throw new InvalidOperationException(message.Content);
            }

            Console.WriteLine($"Handled {message.Content}");

            return Task.CompletedTask;
        }
    }
}