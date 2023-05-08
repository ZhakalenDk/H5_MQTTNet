using H5_MQTTNet.Console;
using MQTTnet;
using MQTTnet.Client;

Console.WriteLine("MQTT.NET example");

using var client = new MyMQTTClient(Console.WriteLine);

var resultCode = await client.ConnectWithTLS("a2de5ce195014803803d985cd0c6346c.s2.eu.hivemq.cloud", "DOTNET", "P@ssw0rd");
if (resultCode != MqttClientConnectResultCode.Success)
    Console.WriteLine(resultCode);

await client.SubAsync("home/test");

client.HandleMessage(Console.WriteLine);

//await client.PubAsync("home/test", Console.ReadLine()!);

Console.WriteLine("Press Enter to exit");
Console.ReadLine();