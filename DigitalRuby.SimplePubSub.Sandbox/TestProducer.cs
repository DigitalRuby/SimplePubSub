namespace DigitalRuby.SimplePubSub.Sandbox;

/// <summary>
/// Test producer
/// </summary>
[Binding(ServiceLifetime.Singleton)]
public class TestProducer : BackgroundService
{
    private readonly IProducer producer;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="producer">Producer</param>
    public TestProducer(IProducer producer)
    {
        this.producer = producer;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Producing...");
            await producer.ProduceAsync(new Message { Text = "hello:" + Guid.NewGuid().ToString("N") }, stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
