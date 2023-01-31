using Azure.Messaging.ServiceBus;

public class MessageSender
{
    private readonly ServiceBusSender _sender;
    public MessageSender(ServiceBusClient client)
    {
        _sender = client.CreateSender("main");
    }

    public async Task SendMessageBatch(int numOfMessages)
    {
        using var activity = ActivityConfig.Source.StartActivity("Send Batch");
        using ServiceBusMessageBatch messageBatch = await _sender.CreateMessageBatchAsync();

        for (int i = 1; i <= numOfMessages; i++)
        {
            // try adding a message to the batch
            if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
            {
                // if it is too large for the batch
                throw new Exception($"The message {i} is too large to fit in the batch.");
            }
        }
        await _sender.SendMessagesAsync(messageBatch);
        Console.WriteLine($"A batch of {numOfMessages} messages has been published to the queue.");
    }
}