using Correlate;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Correlate.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;

namespace Rebus.Correlate;

/// <summary>
/// Regression test for the fix that anchors <see cref="Steps.CorrelateIncomingMessageStep" /> before
/// <see cref="DefaultRetryStep" /> (rather than after <see cref="Pipeline.Receive.DeserializeIncomingMessageStep" />).
/// Previously, when a handler threw and the retry strategy gave up on the message, Rebus' own
/// "Unhandled exception ..." log entry was written *after* the ambient CorrelationContext had already
/// been torn down, so that specific log entry never carried a Correlation ID - even though the handler
/// itself ran with the correct one the whole time.
/// </summary>
public class RetryStrategyCorrelationTests : IDisposable
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor = new CorrelationContextAccessor();
    private readonly BuiltinHandlerActivator _activator = new();
    private readonly IBusStarter _busStarter;
    private readonly TaskCompletionSource<string?> _unhandledExceptionLogged = new();

    private sealed class CapturingLog(ICorrelationContextAccessor accessor, TaskCompletionSource<string?> signal) : ILog
    {
        public void Debug(string message, params object[] objs)
        {
        }

        public void Info(string message, params object[] objs)
        {
        }

        public void Warn(string message, params object[] objs) => Capture(message);
        public void Warn(Exception exception, string message, params object[] objs) => Capture(message);
        public void Error(string message, params object[] objs) => Capture(message);
        public void Error(Exception exception, string message, params object[] objs) => Capture(message);

        private void Capture(string message)
        {
            // This is the log entry DefaultExceptionLogger writes when the retry strategy gives up on a message.
            if (message.StartsWith("Unhandled exception"))
            {
                signal.TrySetResult(accessor.CorrelationContext?.CorrelationId);
            }
        }
    }

    private sealed class CapturingLoggerFactory(ICorrelationContextAccessor accessor, TaskCompletionSource<string?> signal) : IRebusLoggerFactory
    {
        public ILog GetLogger<T>() => new CapturingLog(accessor, signal);
    }

    public RetryStrategyCorrelationTests()
    {
        _busStarter = Configure.With(_activator)
            .Logging(l => l.Use(new CapturingLoggerFactory(_correlationContextAccessor, _unhandledExceptionLogged)))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "retry-strategy-correlation-input"))
            .Options(o =>
            {
                o.EnableCorrelate(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                // Fail straight to the error queue after the first attempt, so the test doesn't have to
                // wait through the default 5 delivery attempts.
                o.RetryStrategy(maxDeliveryAttempts: 1);
            })
            .Create();
    }

    public void Dispose()
    {
        _busStarter.Bus.Dispose();
        _activator.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task When_handler_throws_the_unhandled_exception_log_entry_should_still_carry_the_correlation_id()
    {
        const string correlationId = "my-correlation-id";

        _activator.Handle<string>(_ => throw new InvalidOperationException("Simulated handler failure"));
        IBus bus = _busStarter.Start();

        // Act
        await bus.SendLocal("hello", new Dictionary<string, string>
        {
            [Headers.CorrelationId] = correlationId
        });

        string? correlationIdAtLogTime = await _unhandledExceptionLogged.Task.WithTimeout();

        // Assert
        correlationIdAtLogTime.Should().Be(correlationId,
            "the ambient CorrelationContext should still be active when Rebus' retry strategy logs the unhandled exception");
    }
}
