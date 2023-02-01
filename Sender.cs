using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

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
            var message = new ServiceBusMessage($"Message {i}");
            Propagators.DefaultTextMapPropagator.Inject(
                new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
                message,
                InjectPropertiesToServiceBusMessage
            );

            // try adding a message to the batch
            if (!messageBatch.TryAddMessage(message))
            {
                // if it is too large for the batch
                throw new Exception($"The message {i} is too large to fit in the batch.");
            }
        }
        await _sender.SendMessagesAsync(messageBatch);
        Console.WriteLine($"A batch of {numOfMessages} messages has been published to the queue.");
    }

    /// <summary>
    /// Inject values into the ServiceBus Message ApplicationProperties
    /// </summary>
    /// <param name="message">The message to inject into</param>
    /// <param name="key">The key to set</param>
    /// <param name="value">The value to set in the provided key</param>
    internal static void InjectPropertiesToServiceBusMessage(ServiceBusMessage message, string key, string value)
        => message.ApplicationProperties[key] = value;

}