using System;
using System.Threading.Tasks;
using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Correlate.Steps
{
	[StepDocumentation("Sets ambient 'Correlate.CorrelationContext' based on Correlation ID in incoming message header. If no header is found, a new Correlation ID is generated.")]
	internal class CorrelateIncomingMessageStep : IIncomingStep
	{
		private readonly CorrelationManager _correlationManager;
		private readonly ILog _logger;

		public CorrelateIncomingMessageStep(CorrelationManager correlationManager, IRebusLoggerFactory rebusLoggerFactory)
		{
			_correlationManager = correlationManager ?? throw new ArgumentNullException(nameof(correlationManager));

			_logger = rebusLoggerFactory.GetLogger<CorrelateIncomingMessageStep>();
		}

		public Task Process(IncomingStepContext context, Func<Task> next)
		{
			var message = context.Load<Message>();
			message.Headers.TryGetValue(Headers.CorrelationId, out string correlationId);
			if (correlationId != null)
			{
				_logger.Debug("Correlation ID: {CorrelationId}", correlationId);
			}
			// If id is null, we just let manager assign new one.
			return _correlationManager.CorrelateAsync(correlationId, next);
		}
	}
}