using System.Collections.Concurrent;
using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Correlate.Steps;

public class CorrelateIncomingMessageStepTests
{
    private readonly IAsyncCorrelationManager _asyncCorrelationManagerMock;
    private readonly Dictionary<string, string> _messageHeaders = new();
    private readonly IncomingStepContext _stepContext;
    private readonly Func<Task> _next;
    private readonly CorrelateIncomingMessageStep _sut;

    public CorrelateIncomingMessageStepTests()
    {
        _asyncCorrelationManagerMock = Substitute.For<IAsyncCorrelationManager>();

        var txItems = new ConcurrentDictionary<string, object>();
        ITransactionContext transactionContextMock = Substitute.For<ITransactionContext>();
        transactionContextMock
            .Items
            .Returns(txItems);

        var transportMessage = new TransportMessage(_messageHeaders, Array.Empty<byte>());
        _stepContext = new IncomingStepContext(transportMessage, transactionContextMock);
        _stepContext.Save(new Message(_messageHeaders, new { }));

        _next = () => Task.CompletedTask;

        _sut = new CorrelateIncomingMessageStep(_asyncCorrelationManagerMock, new NullLoggerFactory());
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
        Func<CorrelateIncomingMessageStep> act = () => new CorrelateIncomingMessageStep(_asyncCorrelationManagerMock, rebusLoggerFactory);

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
        await _asyncCorrelationManagerMock
            .Received(1)
            .CorrelateAsync(expectedCorrelationId, _next, Arg.Any<OnError>());
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
        await _asyncCorrelationManagerMock
            .Received(1)
            .CorrelateAsync(expectedCorrelationId, _next, Arg.Any<OnError>());
    }

    [Fact]
    public async Task Given_no_deserialized_message_is_available_in_context_yet_it_should_still_use_header_value_from_transport_message()
    {
        const string correlationId = nameof(correlationId);
        const string expectedCorrelationId = correlationId;
        _messageHeaders.Add(Headers.CorrelationId, correlationId);

        // Builds a context the way it looks when this step runs before DefaultRetryStep (and thus before
        // DeserializeIncomingMessageStep has run) - i.e. no deserialized Message has been saved to the
        // context yet, only the raw TransportMessage is available.
        var txItems = new ConcurrentDictionary<string, object>();
        ITransactionContext transactionContextMock = Substitute.For<ITransactionContext>();
        transactionContextMock
            .Items
            .Returns(txItems);
        var transportMessage = new TransportMessage(_messageHeaders, Array.Empty<byte>());
        var stepContextWithoutDeserializedMessage = new IncomingStepContext(transportMessage, transactionContextMock);

        // Act
        await _sut.Process(stepContextWithoutDeserializedMessage, _next);

        // Assert
        await _asyncCorrelationManagerMock
            .Received(1)
            .CorrelateAsync(expectedCorrelationId, _next, Arg.Any<OnError>());
    }
}
