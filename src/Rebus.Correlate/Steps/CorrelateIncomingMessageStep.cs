using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Correlate.Steps;

[StepDocumentation("Sets ambient 'Correlate.CorrelationContext' based on Correlation ID in incoming message header. If no header is found, a new Correlation ID is generated.")]
internal class CorrelateIncomingMessageStep : IIncomingStep
{
    private readonly IAsyncCorrelationManager _asyncCorrelationManager;
    private readonly ILog _logger;

    public CorrelateIncomingMessageStep(IAsyncCorrelationManager asyncCorrelationManager, IRebusLoggerFactory? rebusLoggerFactory)
    {
        _asyncCorrelationManager = asyncCorrelationManager ?? throw new ArgumentNullException(nameof(asyncCorrelationManager));

        _logger = (rebusLoggerFactory ?? new NullLoggerFactory()).GetLogger<CorrelateIncomingMessageStep>();
    }

    public Task Process(IncomingStepContext context, Func<Task> next)
    {
        // Read the header off the raw TransportMessage rather than the deserialized Message: this step
        // now runs before DefaultRetryStep (so the ambient CorrelationContext stays alive for the retry
        // strategy's own exception logging), which means DeserializeIncomingMessageStep has not run yet
        // and no Message is available in the context.
        TransportMessage transportMessage = context.Load<TransportMessage>();
        transportMessage.Headers.TryGetValue(Headers.CorrelationId, out string? correlationId);
        if (correlationId is not null)
        {
            _logger.Debug("Correlation ID: {CorrelationId}", correlationId);
        }

        // If id is null, we just let manager assign new one.
        return _asyncCorrelationManager.CorrelateAsync(correlationId, next);
    }
}
