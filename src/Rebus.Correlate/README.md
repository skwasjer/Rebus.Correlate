[Rebus](https://github.com/rebus-org/Rebus) integration of [Correlate](https://github.com/skwasjer/Correlate) to correlate message flow via any supported Rebus transport.

## Correlation ID flow

The Correlate library provides an ambient correlation context scope, that makes it easy to track a Correlation ID passing through microservices.

This library provides pipeline steps for Rebus for incoming and outgoing messages, and takes precedence over Rebus' own `FlowCorrelationIdStep`.

### Outgoing messages
When an ambient correlation context is present, the Correlation ID associated with this context is attached to the outgoing message. When no ambient correlation context is active, a new Correlation ID is generated instead using the `ICorrelationIdFactory`.

> If a CorrelationID header is already present, no action is performed.

### Incoming messages
For each new incoming message a new ambient correlation context is created. 
If the incoming message contains a Correlation ID header, this id is associated with the correlation context. If no Correlation ID header is present, a new Correlation ID is generated instead using the `ICorrelationIdFactory`.

## Usage ###

Configure Rebus to use Correlate.

### Using built-in configuration extensions ###

Use Rebus' built-in configuration extensions to enable Correlate.

```csharp
ILoggerFactory loggerFactory = new LoggerFactory();
loggerFactory.AddConsole();

Configure.With(....)
    .Options(o => o.EnableCorrelate(loggerFactory))
    .(...)
```

### Using a `IServiceProvider`

Alternatively, use `IServiceProvider` to configure Rebus with Correlate.

Add package dependencies:
- [Rebus.ServiceProvider](https://github.com/rebus-org/Rebus.ServiceProvider) 
- [Correlate.DependencyInjection](https://github.com/skwasjer/Correlate)

```csharp
services
    .AddLogging(logging => logging.AddConsole())
    .AddCorrelate()
    .AddRebus((configure, serviceProvider) => configure
        .Options(o => o.EnableCorrelate(serviceProvider))
        .(...)
    );
```

### Using a custom DI adapter

For example, provided the Correlate dependencies are registered with Autofac:

```csharp
var builder = new ContainerBuilder();
... // Register Correlate dependencies.
var container = builder.Build();
var scope = container.BeginLifetimeScope(); // Dispose on app shutdown.

Configure.With(....)
    .Options(o => o.EnableCorrelate(new DependencyResolverAdapter(scope.ResolveOptional)))
    .(...)
```

## Send/publish message in an ambient correlation context scope

This example illustrates how messages that are sent/published, inherit the Correlation ID from the ambient correlation context.

```csharp
public class MyService
{
    private IAsyncCorrelationManager _asyncCorrelationManager;
    private IBus _bus;

    public MyService(IAsyncCorrelationManager asyncCorrelationManager, IBus bus)
    {
        _asyncCorrelationManager = asyncCorrelationManager;
        _bus = bus;
    }

    public async Task DoWork()
    {
        // Without ambient correlation context, the message is still published 
        // with a Correlation ID, but it is generated specifically for this message.
        await _bus.Publish(new DoWorkCalledEvent());

        // Perform work in new correlation context.
        await _asyncCorrelationManager.CorrelateAsync(async () =>
        {
            // This command will be sent with the Correlation ID from
            // the ambient correlation context.
            await _bus.Send(new DoSomethingElseCommand());

            // Do other work in ambient correlation context,
            // like call other microservice (using Correlate support)
            // ...

            // This event will be published with the same Correlation ID.
            await _bus.Publish(new WorkFinishedEvent());
        });
    }
}
```

> Note: when using Correlate integration for ASP.NET Core, each request is already scoped to a correlation context, and so there is no need to wrap the send/publish of messages with `IAsyncCorrelationManager`/`ICorrelationManager`.

## Handle message in an ambient correlation context scope

With Correlate enabled, any incoming message is handled in its own ambient correlation context automatically. If you wish to access the Correlation ID, inject the `ICorrelationContextAccessor` into your handler.

```csharp
public class MyHandler : IHandleMessages<MyMessage>
{
    private ICorrelationContextAccessor _correlationContextAccessor;

    public MyHandler(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public Task Handle(MyMessage message)
    {
        string correlationId = _correlationContextAccessor.CorrelationContext.CorrelationId; 
    }
}
```

> Do not keep a reference to the `CorrelationContext`, always use the `ICorrelationContextAccessor` to get the current context.

## More info

- [Correlate](https://github.com/skwasjer/Correlate) documentation for further integration with ASP.NET Core, `IHttpClientFactory` and for other extensions/libraries.
- [Release notes](https://github.com/skwasjer/Rebus.Correlate/releases)

### Contributions

Please check out the [contribution guidelines](https://github.com/skwasjer/MockHttp/blob/main/CONTRIBUTING.md).
