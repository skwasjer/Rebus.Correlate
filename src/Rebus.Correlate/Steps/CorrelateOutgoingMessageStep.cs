using System;
using System.Globalization;
using System.Threading.Tasks;
using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Correlate.Steps
{
	[StepDocumentation("Sets Correlation ID on outgoing message header based on ambient 'Correlate.CorrelationContext'.")]
	internal class CorrelateOutgoingMessageStep : IOutgoingStep
	{
		private readonly ICorrelationContextAccessor _correlationContextAccessor;
		private readonly ICorrelationIdFactory _correlationIdFactory;
		private readonly ILog _logger;

		public CorrelateOutgoingMessageStep(ICorrelationContextAccessor correlationContextAccessor, ICorrelationIdFactory correlationIdFactory, IRebusLoggerFactory rebusLoggerFactory)
		{
			_correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
			_correlationIdFactory = correlationIdFactory ?? throw new ArgumentNullException(nameof(correlationIdFactory));

			_logger = (rebusLoggerFactory ?? new NullLoggerFactory()).GetLogger<CorrelateOutgoingMessageStep>();
		}

		public Task Process(OutgoingStepContext context, Func<Task> next)
		{
			var message = context.Load<Message>();
			if (!message.Headers.ContainsKey(Headers.CorrelationId))
			{
				string correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ?? _correlationIdFactory.Create();
				message.Headers[Headers.CorrelationId] = correlationId;

				int correlationSequence = 0;
				var transactionContext = context.Load<ITransactionContext>();
				var incomingStepContext = transactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);
				if (incomingStepContext != null)
				{
					correlationSequence = GetCorrelationSequence(incomingStepContext) + 1;
				}

				message.Headers[Headers.CorrelationSequence] = correlationSequence.ToString(CultureInfo.InvariantCulture);

				_logger.Debug("Correlation ID: {CorrelationId}, sequence: {CorrelationSequence}", correlationId, correlationSequence);
			}

			return next();
		}

		private static int GetCorrelationSequence(StepContext stepContext)
		{
			var message = stepContext.Load<Message>();
			if (!message.Headers.TryGetValue(Headers.CorrelationSequence, out string strValue)
				|| string.IsNullOrEmpty(strValue))
			{
				return 0;
			}

			int.TryParse(strValue, NumberStyles.None, CultureInfo.InvariantCulture, out int correlationSequence);
			return correlationSequence;
		}
	}
}