using System.Collections.Concurrent;
using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Correlate.Steps;

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
        var transportMessage = new TransportMessage(_messageHeaders, Array.Empty<byte>());
        _stepContext = new IncomingStepContext(transportMessage, transactionContextMock.Object);
        _stepContext.Save(new Message(_messageHeaders, new { }));

        _next = () => Task.CompletedTask;

        _sut = new CorrelateIncomingMessageStep(_asyncCorrelationManagerMock.Object, new NullLoggerFactory());
    }

    [Fact]
    public void When_creating_instance_without_asyncCorrelationManager_it_should_throw()
    {
        IAsyncCorrelationManager? asyncCorrelationManager = null;

        // Act
        Func<CorrelateIncomingMessageStep> act = () => new CorrelateIncomingMessageStep(asyncCorrelationManager!, new NullLoggerFactory());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .Where(exception => exception.ParamName == nameof(asyncCorrelationManager));
    }

    [Fact]
    public void When_creating_instance_without_loggerFactory_it_should_not_throw()
    {
        IRebusLoggerFactory? rebusLoggerFactory = null;

        // Act
        Func<CorrelateIncomingMessageStep> act = () => new CorrelateIncomingMessageStep(_asyncCorrelationManagerMock.Object, rebusLoggerFactory);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Given_no_correlation_id_is_stored_with_message_it_should_use_message_id()
    {
        const string? expectedCorrelationId = null;
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
