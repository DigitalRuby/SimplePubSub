namespace DigitalRuby.SimplePubSub.Sandbox;

/// <summary>
/// Message
/// </summary>
public sealed class Message
{
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Test consumer
/// </summary>
[Consumer]
public sealed class TestConsumer : IConsumer<Message>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="provider">Just making sure dependency injection works</param>
    public TestConsumer(IServiceProvider provider)
    {
        Console.WriteLine("Creating test consumer with provider != null: {0}", provider != null);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<Message> context)
    {
        Console.WriteLine("Consumed message with text {0}, sent at {1}", context.Message.Text, context.SentTime);
        return Task.CompletedTask;
    }
}
