using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

public class MessageProcessor
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;

    public MessageProcessor(ServiceBusClient client)
    {
        _client = client;
        _processor = client.CreateProcessor("main", new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += TracedMessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;
    }

    public async Task StartReceiving()
    {
        await _processor.StartProcessingAsync();
    }

    async Task TracedMessageHandler(ProcessMessageEventArgs args)
    {
        var context = Propagators.DefaultTextMapPropagator.Extract(new PropagationContext(
            new ActivityContext(),
            Baggage.Current
        ), 
            args.Message.ApplicationProperties, 
            ExtractContextFromApplicationProperties);

        using var activity = ActivityConfig.Source
            .StartActivity("Receive Message", 
                ActivityKind.Consumer, null, 
                links: new List<ActivityLink> {
                    new ActivityLink(context.ActivityContext)
            });
        
        await MessageHandler(args);
    }

    async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        Console.WriteLine($"Received: {body}");

        // complete the message. message is deleted from the queue. 
        await args.CompleteMessageAsync(args.Message);
    }

    // handle any errors when receiving messages
    Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extract values from the 
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    internal static IEnumerable<string> ExtractContextFromApplicationProperties(IReadOnlyDictionary<string, object> properties, string key)
    {
        var valueFromProps = properties.TryGetValue(key, out var propertyValue)
                ? propertyValue?.ToString() ?? ""
                : "";

        return new List<string> { valueFromProps };
    }

}