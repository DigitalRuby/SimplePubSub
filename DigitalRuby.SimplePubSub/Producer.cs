namespace DigitalRuby.SimplePubSub;

/// <summary>
/// Producer interface
/// </summary>
public interface IProducer
{
    /// <summary>
    /// Produce a message
    /// </summary>
    /// <typeparam name="T">Type of message</typeparam>
    /// <param name="message">Message</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <param name="keys">The keys to publish to or empty for all</param>
    /// <returns>Task</returns>
    public Task ProduceAsync<T>(T message, CancellationToken cancelToken = default, params string[] keys) where T : class;
}

/// <summary>
/// Producer implementation
/// </summary>
[Binding(ServiceLifetime.Singleton)]
public class Producer : IProducer
{
    /// <summary>
    /// Queue systems available
    /// </summary>
    public IQueueSystems QueueSystems { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="queueSystems">Queue systems</param>
    public Producer(IQueueSystems queueSystems)
    {
        QueueSystems = queueSystems;
    }

    /// <inheritdoc />
    public Task ProduceAsync<T>(T message, CancellationToken cancelToken = default, params string[] keys) where T : class
    {
        List<Task> tasks = new();
        foreach (var queueSystem in QueueSystems.EnumerateQueueSystems(keys))
        {
            tasks.Add(queueSystem.ProduceAsync(message, cancelToken));
        }
        return Task.WhenAll(tasks);
    }
}

/// <summary>
/// Queue systems enumerator
/// </summary>
public interface IQueueSystems
{
    /// <summary>
    /// Enumerate queue systems
    /// </summary>
    /// <param name="keys">Keys</param>
    /// <returns>Enumerable of queue systems</returns>
    IEnumerable<QueueSystem> EnumerateQueueSystems(params string[] keys);
}

/// <summary>
/// Queue systems implementation
/// </summary>
public sealed class QueueSystems : IQueueSystems
{
    private readonly Dictionary<string, QueueSystem> queues = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Add a queue system
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="bus">Bus</param>
    public void AddQueueSystem(string key, IBusControl bus)
    {
        queues[key] = new QueueSystem(key, bus);
    }

    /// <summary>
    /// Wait for queues to start
    /// </summary>
    public void Wait()
    {
        foreach (var queue in queues.Values)
        {
            queue.Start();
        }
    }

    /// <inheritdoc />
    public IEnumerable<QueueSystem> EnumerateQueueSystems(params string[] keys)
    {
        if (keys is null || keys.Length == 0)
        {
            foreach (var queue in queues.Values)
            {
                yield return queue;
            }
        }
        else
        {
            bool foundOne = false;
            foreach (var key in keys)
            {
                if (queues.TryGetValue(key, out var queueSystem))
                {
                    foundOne = true;
                    yield return queueSystem;
                }
            }
            if (!foundOne)
            {
                throw new ArgumentException("Unable to find queue with specified key(s): " + string.Join(',', keys));
            }
        }
    }
}

/// <summary>
/// Queue system interface
/// </summary>
public interface IQueueSystem
{
    /// <summary>
    /// Produce a message
    /// </summary>
    /// <typeparam name="T">Type of message</typeparam>
    /// <param name="message">Message</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task</returns>
    Task ProduceAsync<T>(T message, CancellationToken cancelToken) where T : class;
}

/// <summary>
/// Queue system implementation
/// </summary>
public sealed class QueueSystem : IQueueSystem
{
    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Bus
    /// </summary>
    private readonly IBusControl bus;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="bus">Bus</param>
    public QueueSystem(string key, IBusControl bus)
    {
        Key = key;
        this.bus = bus;
    }

    /// <inheritdoc />
    public Task ProduceAsync<T>(T message, CancellationToken cancelToken) where T : class
    {
        return bus.Publish<T>(message, cancelToken);
    }

    /// <summary>
    /// Start the queue system
    /// </summary>
    public void Start()
    {
        bus.Start();
    }
}
