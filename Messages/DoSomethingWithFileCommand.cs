using System;
using NServiceBus;

namespace Messages
{
    public class DoSomethingWithFileCommand : ICommand
    {
        public string Content { get; set; }
    }
}
