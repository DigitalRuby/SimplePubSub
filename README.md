<h1 align='center'>Simple Pub Sub</h1>

Declarative and simple pub/sub for .NET using MassTransit.

This framework was created to have a simple abstraction layer on top of MassTransit and eliminate the boilerplate and setup code that is common when needing pub/sub functionality.

Another core feature of Simple Pub Sub is the ability to handle multiple queue providers. This can be handy for complex systems, or if you are needing to migrate from one provider to another. For example, you could produce to just one provider, but consume from two.

## Implementation

```cs
using DigitalRuby.SimplePubSub;

// create your builder, add simple pub sub
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSimplePubSub(builder.Configuration);

// like simple di, you can add a second optional parameter for a regex to filter assembly names to scan for consumers, by default only assemblies prefixed by the first part of your entry assembly name are scanned
//builder.Services.AddSimplePubSub(builder.Configuration, "myassembly1|myassembly2");

// for web apps (not needed for non-web apps):
var host = builder.Build();
host.UseSimplePubSub(host.Configuration);

// if you are using SimpleDi, you do not need to call `AddSimpleDi` or `UseSimpleDi`.
```

## Dependencies

Simple pub sub uses `DigitalRuby.SimpleDi` nuget package underneath, and the wonderful MassTransit library.

The following providers are supported:

- InMemory
- RabbitMq
- ActiveMq
- AmazonSqs
- AzureServiceBus
- Grpc

Email support@digitalruby.com if you want another provider added.

## Configuration

Your `IConfiguration (appsettings.json)` needs to contain the following:

```json
{
  "DigitalRuby.SimplePubSub.Configuration":
  {
    "Providers":
    {
      /* each provider must have a unique key */
      "MyRabbitInstance1":
      {
        /* configurations can have the following fields, fields can be removed if empty. */
        "Type": "RabbitMq", /* One of the allowed provider strings, see below after this configuration sinppet */
        "ConnectionString": "amqp://guest:guest@localhost:5672", /* provider specific */
        "UserName": "", /* if needed, maps to AccessKey for AmazonSqs */
        "Password": "", /* if needed, maps to SecretKey for AmazonSqs */
        "UseSsl": false, /* whether ssl is used, can be removed if false */
        "SslCertificatePath": "", /* if using a client certificate */
        "SslCertificatePassphrase": "", /* if using a client certificate */
        "Servers": /* full server url of the mesh, used only by grpc topology currently */
        [
        ],
        "Retries": /* DD:HH:MM:SS retries, will cause consumer to try/catch and loop the message for each failure */
        [
          "00:00:00:02",
          "00:00:00:05"
        ],
        "Redeliveries": /* DD:HH:MM:SS redeliveries, message will be re-queued after each failure */
        [
          "00:00:01:00",
          "00:00:15:00",
          "00:01:00:00",
          "00:04:00:00",
          "00:08:00:00",
          "00:12:00:00",
          "01:00:00:00"
        ]
      },
      "MyRabbitInstance2":
      {
        "Type": "RabbitMq" /* can have multiple providers of the same type, no problem */
        /* properties left out for brevity */
      },
      "MyAmazonSqsInstance":
      {
        "Type": "AmazonSqs"
        /* properties left out for brevity */
      },
      "MyInMemoryInstance":
      {
        "Type": "InMemory" /* great for unit testing */
        /* properties left out for brevity */
      }
    }
  }
}
```

Each key under the `Configurations` element must be one of the following:

- RabbitMq
- ActiveMq
- AmazonSqs
- AzureServiceBus
- Grpc
- InMemory

## Consumers

To implement a consumer, create a class:

```cs
public class MyMessage
{
  public string Text { get; set; } = string.Empty;
}

/*
Apply the consumer attribute to your consumer and implement the MassTransit consumer interface.
This consumer consumes from all configued providers
*/
[Consumer]
public class MyConsumer : MassTransit.IConsumer<MyMessage>
{
  /// <inheritdoc />
  public Task Consume(ConsumeContext<MyMessage> context)
  {
    // handle the message...
  }
}

// Only consumes from MyRabbitInstance1
[Consumer("MyRabbitInstance1")]
public class MyConsumerSpecificProvider : MassTransit.IConsumer<MyMessage>
{
  /// <inheritdoc />
  public Task Consume(ConsumeContext<MyMessage> context)
  {
    // handle the message...
  }
}
```

The consumer will be automatically registered and start consuming when your application finishes starting.

## Producers

Consumers aren't very useful without being able to produce messages.

The `IProducer` interface is available to send messages.

```cs
[Binding(ServiceLifetime.Singleton)]
public class MyBackgroundService : BackgroundService
{
  private readonly IProducer producer;

  public MyBackgroundService(IProducer producer)
  {
    this.producer = producer;
  }

  /// <inheritdoc />
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // produce a message to all providers
    await producer.ProduceAsync(new MyMessage { Text = "test" });

    // you can also produce to just specific queue providers
    await producer.ProduceAsync(new MyMessage { Text = "test" }, default, "MyRabbitInstance1");
  }
}
```

---

Thank you for visiting!

-- Jeff

https://www.digitalruby.com
