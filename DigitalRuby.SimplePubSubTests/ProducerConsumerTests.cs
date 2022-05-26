using MassTransit;

namespace DigitalRuby.SimplePubSubTests;

/// <summary>
/// Test message
/// </summary>
public sealed class TestMessage
{
    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => Message;
}

/// <summary>
/// Consumer for test messages, all queues
/// </summary>
[Consumer]
public sealed class TestConsumer0 : MassTransit.IConsumer<TestMessage>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        ConsumerProducerTests.Messages0.Add(context.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Consumer for test message, queue 1
/// </summary>
[Consumer("InMemory1")]
public sealed class TestConsumer1 : MassTransit.IConsumer<TestMessage>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        ConsumerProducerTests.Messages1.Add(context.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Consumer for test message, queue 2
/// </summary>
[Consumer("InMemory2")]
public sealed class TestConsumer2 : MassTransit.IConsumer<TestMessage>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TestMessage> context)
    {
        ConsumerProducerTests.Messages2.Add(context.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tests that producer/consumer is working
/// </summary>
[TestFixture]
public class ConsumerProducerTests
{
    public static readonly System.Collections.Concurrent.ConcurrentBag<TestMessage> Messages0 = new();
    public static readonly System.Collections.Concurrent.ConcurrentBag<TestMessage> Messages1 = new();
    public static readonly System.Collections.Concurrent.ConcurrentBag<TestMessage> Messages2 = new();

    /// <summary>
    /// Setup
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Messages0.Clear();
        Messages1.Clear();
        Messages2.Clear();
    }

    /// <summary>
    /// Test pub/sub works
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestPubSub()
    {
        // build test harness
        var started = false;
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((context, services) =>
        {
            Assert.That(services.SimplePubSubAdded(), Is.False);
            services.AddSimplePubSub(context.Configuration, "digitalruby");
            Assert.That(services.SimplePubSubAdded(), Is.True);
        });
        using var host = builder.Build();
        var lifeTime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifeTime.ApplicationStarted.Register(() => started = true);
        host.RunAsync().GetAwaiter();
        while (!started)
        {
            await Task.Delay(1);
        }
        // produce some messages
        var producer = host.Services.GetRequiredService<IProducer>();

        // message will be consumed by all providers (2,1,1)
        await producer.ProduceAsync(new TestMessage { Message = "all" });

        // message will only be consumed by provider 1 (3,2,1)
        await producer.ProduceAsync(new TestMessage { Message = "one" }, default, "InMemory1");

        // message will only be consumed by provider 1 and provider 2 (4,2,2)
        await producer.ProduceAsync(new TestMessage { Message = "two" }, default, "InMemory2");
        Assert.ThrowsAsync<ArgumentException>(() => producer.ProduceAsync(new TestMessage { Message = "notexists" }, default, "NotExists"));
        await Task.Delay(100);

        Assert.Multiple(() =>
        {
            Assert.That(Messages0, Has.Count.EqualTo(4));
            Assert.That(Messages1, Has.Count.EqualTo(2));
            Assert.That(Messages2, Has.Count.EqualTo(2));
        });
    }
}