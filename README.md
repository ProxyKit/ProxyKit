## ECMAScript Websockets and .NET Framework ##
When a websocket message is sent using ECMAScript ProxyKit fails to Proxy the message along if using .NET framework instead of .NET core.
### Steps to Recreate ###
Run the following commands from the ProxyKit folder
  1. Open a command prompt and run `dotnet run --project Receive/Receive`
  2. Open a second command prompt and run `dotnet run --project Failing/Failing --framework net471`
  3. Using a web browser open `Sender.html`

### .Net Core VS .Net Framework ###
To see the app working with .NET Core simply run the second command with a framework of `netcoreapp2.1` instead of `net471`
With this change you should see a message in the console for the Receive project saying "Message Received."