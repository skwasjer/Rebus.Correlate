using System;
using System.Threading.Tasks;
using Correlate;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Correlate.Steps
{
	[StepDocumentation("Sets ambient 'Correlate.CorrelationContext' based on Correlation ID in incoming message header. If no header is found, a new Correlation ID is generated.")]
	internal class CorrelateIncomingMessageStep : IIncomingStep
	{
		private readonly CorrelationManager _correlationManager;

		public CorrelateIncomingMessageStep(CorrelationManager correlationManager)
		{
			_correlationManager = correlationManager ?? throw new ArgumentNullException(nameof(correlationManager));
		}

		public Task Process(IncomingStepContext context, Func<Task> next)
		{
			var message = context.Load<Message>();
			message.Headers.TryGetValue(Headers.CorrelationId, out string correlationId);
			return _correlationManager.CorrelateAsync(correlationId, next);
		}
	}
}