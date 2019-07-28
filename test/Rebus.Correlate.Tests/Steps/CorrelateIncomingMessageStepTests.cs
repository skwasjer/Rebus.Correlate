using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Correlate;
using Moq;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;
using Xunit;

namespace Rebus.Correlate.Steps
{
	public class CorrelateIncomingMessageStepTests
	{
		private readonly Mock<IAsyncCorrelationManager> _asyncCorrelationManagerMock;
		private readonly Dictionary<string, string> _messageHeaders;
		private readonly IncomingStepContext _stepContext;
		private readonly Func<Task> _next;
		private readonly CorrelateIncomingMessageStep _sut;

		public CorrelateIncomingMessageStepTests()
		{
			_asyncCorrelationManagerMock = new Mock<IAsyncCorrelationManager>();

			var txItems = new ConcurrentDictionary<string, object>();
			var transactionContextMock = new Mock<ITransactionContext>();
			transactionContextMock
				.Setup(m => m.Items)
				.Returns(txItems);

			_messageHeaders = new Dictionary<string, string>();
			var transportMessage = new TransportMessage(_messageHeaders, new byte[0]);
			_stepContext = new IncomingStepContext(transportMessage, transactionContextMock.Object);
			_stepContext.Save(new Message(_messageHeaders, new { }));

			_next = () => Task.CompletedTask;

			_sut = new CorrelateIncomingMessageStep(_asyncCorrelationManagerMock.Object, new NullLoggerFactory());
		}

		[Fact]
		public async Task Given_no_correlation_id_is_stored_with_message_it_should_use_message_id()
		{
			const string expectedCorrelationId = null;
			_messageHeaders.Clear();

			// Act
			await _sut.Process(_stepContext, _next);

			// Assert
			_asyncCorrelationManagerMock.Verify(m => m.CorrelateAsync(expectedCorrelationId, _next, It.IsAny<OnException>()), Times.Once);
		}

		[Fact]
		public async Task Given_correlation_id_is_stored_with_message_it_should_use_header_value()
		{
			const string correlationId = nameof(correlationId);
			const string expectedCorrelationId = correlationId;
			_messageHeaders.Add(Headers.CorrelationId, correlationId);

			// Act
			await _sut.Process(_stepContext, _next);

			// Assert
			_asyncCorrelationManagerMock.Verify(m => m.CorrelateAsync(expectedCorrelationId, _next, It.IsAny<OnException>()), Times.Once);
		}
	}
}
