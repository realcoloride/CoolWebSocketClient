## CoolWebSocketClient

CoolWebSocket is a 0 bullshit and straightforward [ClientWebSocket](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket) wrapper that aims at making WebSockets easy and friendly to use, whilst getting the advantages from [ClientWebSocket](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket).

## Installation

You can install via the [nuget package](https://www.nuget.org/packages/CoolWebSocketClient/) or via the following command: 

```bash
dotnet add package CoolWebSocketClient
```

## Usage

Basic usage

```cs
var ws = new CoolWebSocket();
await ws.Open(new Uri("wss://echo.websocket.org"));

// when connected
ws.OnOpen += () =>
{
    Console.WriteLine("Connected to server");
};
// when a message is received, use messagetype to check if its binary or a text
// and use ReadString() to parse if it is the case.
ws.OnMessage += (messageType, message) =>
{
    Console.WriteLine("Received the following message: " + ws.ReadString(message));
};
// when disconnected or kicked
ws.OnClose += (closeStatus, closeMessage) => {
    Console.WriteLine($"Disconnected from server ({closeStatus}): {closeMessage}");
};

// send a string message, binary messages can also be sent
await ws.Send("Hello world!");

...

// close the websocket when done and disconnect (you can also specify a code and reason)
await ws.Close();
```

Changing options (headers, security and more)

```cs
// ClientWebSocket options are available here
ws.Options...

// example
ws.Options.SetRequestHeader("x-hello-world", "coolwebsocketclient");
```

## Extra

🚀 If you have an issue or idea, let me know in the [**Issues**](https://github.com/realcoloride/CoolWebSocketClient/issues) section.

☕ **Want to support me?** You can send me a coffee on ko.fi: https://ko-fi.com/coloride. 

*(real)coloride - 2024, Licensed MIT.*