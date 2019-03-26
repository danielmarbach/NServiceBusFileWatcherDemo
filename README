# NServiceBus FileWatcher Demo

Quick PoC for a prospective customer

## Start

- Open FileWatcherDemo.sln in Visual Studio 2017 or higher
- Build
- Setup Start projects to FileWatcherService, Platform and Receiver
- F5
- By default Receiver will be started as `destination1` but it is possible to start more instances by providing a console argument like `destination2`
- Sample files are available in `Files` folder
  - Copy `destination1.success.txt` into `FileWatcherService\bin\Debug\net462\input` to send a command that will succeed
  - Copy `destination1.transient.txt` into `FileWatcherService\bin\Debug\net462\input` to send a command that will be retried once
  - Copy `destination1.permanent.txt` into `FileWatcherService\bin\Debug\net462\input` to send a command that will fail all attemps
- Permanently failed commands can be sent back by using the Failed Messages view in the opened browser window and will then succeed  

## Multiple destinations

- Start like above
- Open command window or powershell in `Receiver\bin\Debug\net462` and run `Receiver.exe destination2`
- Copy the `destination1.*.txt` files and rename them to `destination2.*.txt`
- Copy them like before into `FileWatcherService\bin\Debug\net462\input` to see how messages get sent to the new destination