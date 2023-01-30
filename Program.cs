using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json");
var configuration = configBuilder.Build();

var traceProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(ActivityConfig.Source.Name)
    .AddHoneycomb(configuration)
    .Build();

ServiceBusClient client = new ServiceBusClient(
    configuration["ServiceBus:ConnectionString"],
    new ServiceBusClientOptions()
    {
        TransportType = ServiceBusTransportType.AmqpWebSockets
    });

var messageProcessor = new MessageProcessor(client);
var messageSender = new MessageSender(client);

await Task.Run(() => messageProcessor.StartReceiving());

await Task.Run(() => messageSender.SendMessageBatch(4));

// Use the producer client to send the batch of messages to the Service Bus queue


Console.WriteLine("Press any key to end the application");
Console.ReadKey();
traceProvider.ForceFlush();


public class ActivityConfig
{
    public static ActivitySource Source = new ActivitySource("servicebus-otel");
}

public partial class Program {}
