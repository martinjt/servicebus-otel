using Azure.Messaging.ServiceBus;

public class MessageProcessor
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;

    public MessageProcessor(ServiceBusClient client)
    {
        _client = client;
        _processor = client.CreateProcessor("main", new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

    }

    public async Task StartReceiving()
    {
        await _processor.StartProcessingAsync();
    }

    async Task MessageHandler(ProcessMessageEventArgs args)
    {
        using var activity = ActivityConfig.Source.StartActivity("Receive Message");
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
}