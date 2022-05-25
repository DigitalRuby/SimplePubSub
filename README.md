# SimplePubSub
Declarative and simple pub/sub for .NET using Mass Transit.

## Implementation

```cs
using DigitalRuby.SimplePubSub;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSimplePubSub(builder.Configuration);

// for web apps (not needed for non-web apps):
var host = builder.Build();
host.UseSimplePubSub();

// if you are using SimpleDi, you do not need to call AddSimpleDi or UseSimpleDi.
```

## Dependencies

Simple pub sub uses `DigitalRuby.SimpleDi` nuget package underneath, along with the following built in providers for pub/sub:

- MassTransit.RabbitMQ
- MassTransit.Redis
- MassTransit.ActiveMQ
- MassTransit.AmazonSQS

You can add additional providers as needed.

## Configuration

Your `IConfiguration` needs to contain the following:

```json
{
	"DigitalRuby.SimplePubSub.Configuration":
	{
		/* each provider must have a unique key */
		"Providers":
		{
			"MyRabbitInstance1":
			{
				"Type": "RabbitMq"
			},
			"MyRabbitInstance2":
			{
				"Type": "RabbitMq"
			},
			"MyAmazonSqsInstance":
			{
				"Type": "AmazonSqs"
			}
		}
	}
}
```

The `Type` can be one of the following for built in providers:

- RabbitMq
- Redis
- ActiveMq
- AmazonSqs

You can also specify a `Custom` type, you will need to implement the provider binding as follows:

TODO: Figure this out

When you create publishers and consumers, you will bind them to one or more of the unique keys from the `DigitalRuby.SimplePubSub.Configuration:Providers` element in the configuration.

You can put all this in `appsettings.json` or another config file.

TODO: Specify common config data and provider specific