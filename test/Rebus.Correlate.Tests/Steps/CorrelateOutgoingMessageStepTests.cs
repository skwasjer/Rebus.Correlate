using System.Collections.Concurrent;
using Correlate;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Transport;

namespace Rebus.Correlate.Steps;

public class CorrelateOutgoingMessageStepTests
{
    private readonly CorrelationContextAccessor _correlationContextAccessor;
    private readonly Mock<ICorrelationIdFactory> _correlationIdFactoryMock;
    private readonly Mock<ITransactionContext> _transactionContextMock;
    private readonly Dictionary<string, string> _messageHeaders;
    private readonly OutgoingStepContext _stepContext;
    private readonly Func<Task> _next;
    private readonly CorrelateOutgoingMessageStep _sut;

    public CorrelateOutgoingMessageStepTests()
    {
        _correlationContextAccessor = new CorrelationContextAccessor();
        _correlationIdFactoryMock = new Mock<ICorrelationIdFactory>();

        var txItems = new ConcurrentDictionary<string, object>();
        _transactionContextMock = new Mock<ITransactionContext>();
        _transactionContextMock
            .Setup(m => m.Items)
            .Returns(txItems);

        _messageHeaders = new Dictionary<string, string>();
        _stepContext = new OutgoingStepContext(new Message(_messageHeaders, new { }), _transactionContextMock.Object, new DestinationAddresses(Enumerable.Empty<string>()));

        _next = () => Task.CompletedTask;

        _sut = new CorrelateOutgoingMessageStep(_correlationContextAccessor, _correlationIdFactoryMock.Object, new NullLoggerFactory());
    }

    [Fact]
    public void When_creating_instance_without_correlationContextAccessor_it_should_throw()
    {
        ICorrelationContextAccessor correlationContextAccessor = null;
        // ReSharper disable once ExpressionIsAlwaysNull
        // ReSharper disable once ObjectCreationAsStatement
        Action act = () => new CorrelateOutgoingMessageStep(correlationContextAccessor, _correlationIdFactoryMock.Object, new NullLoggerFactory());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .Where(exception => exception.ParamName == nameof(correlationContextAccessor));
    }

    [Fact]
    public void When_creating_instance_without_loggerFactory_it_should_not_throw()
    {
        IRebusLoggerFactory rebusLoggerFactory = null;
        // ReSharper disable once ExpressionIsAlwaysNull
        // ReSharper disable once ObjectCreationAsStatement
        Action act = () => new CorrelateOutgoingMessageStep(_correlationContextAccessor, _correlationIdFactoryMock.Object, rebusLoggerFactory);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void When_creating_instance_without_correlationIdFactory_it_should_throw()
    {
        ICorrelationIdFactory correlationIdFactory = null;
        // ReSharper disable once ExpressionIsAlwaysNull
        // ReSharper disable once ObjectCreationAsStatement
        Action act = () => new CorrelateOutgoingMessageStep(_correlationContextAccessor, correlationIdFactory, new NullLoggerFactory());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .Where(exception => exception.ParamName == nameof(correlationIdFactory));
    }

    [Fact]
    public async Task Given_message_already_has_a_correlation_id_stored_it_should_ignore_setting_correlation_id()
    {
        const string correlationId = nameof(correlationId);
        const string expectedCorrelationId = correlationId;
        _messageHeaders.Add(Headers.CorrelationId, correlationId);
        _correlationContextAccessor.CorrelationContext = null;

        bool isNextCalled = false;

        Task Next()
        {
            isNextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _sut.Process(_stepContext, Next);

        // Assert
        _messageHeaders
            .Should()
            .ContainKey(Headers.CorrelationId)
            .WhoseValue
            .Should()
            .Be(expectedCorrelationId);
        _correlationIdFactoryMock.Verify(m => m.Create(), Times.Never);
        isNextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Given_message_has_no_correlation_id_and_correlation_context_is_available_it_should_use_correlation_id_from_context()
    {
        const string correlationId = nameof(correlationId);
        const string expectedCorrelationId = correlationId;
        _messageHeaders.Clear();
        _correlationContextAccessor.CorrelationContext = new CorrelationContext { CorrelationId = correlationId };

        bool isNextCalled = false;

        Task Next()
        {
            isNextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _sut.Process(_stepContext, Next);

        // Assert
        _messageHeaders
            .Should()
            .ContainKey(Headers.CorrelationId)
            .WhoseValue
            .Should()
            .Be(expectedCorrelationId);
        _correlationIdFactoryMock.Verify(m => m.Create(), Times.Never);
        isNextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Given_message_has_no_correlation_id_and_correlation_context_is_not_available_it_should_use_correlation_id_from_factory()
    {
        const string correlationId = nameof(correlationId);
        const string expectedCorrelationId = correlationId;
        _messageHeaders.Clear();
        _correlationContextAccessor.CorrelationContext = null;
        _correlationIdFactoryMock
            .Setup(m => m.Create())
            .Returns(correlationId)
            .Verifiable();

        bool isNextCalled = false;

        Task Next()
        {
            isNextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _sut.Process(_stepContext, Next);

        // Assert
        _messageHeaders
            .Should()
            .ContainKey(Headers.CorrelationId)
            .WhoseValue
            .Should()
            .Be(expectedCorrelationId);
        _correlationIdFactoryMock.Verify();
        isNextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Given_no_incoming_step_context_it_should_start_new_sequence()
    {
        // Act
        await _sut.Process(_stepContext, _next);

        // Assert
        _messageHeaders
            .Should()
            .ContainKey(Headers.CorrelationSequence)
            .WhoseValue
            .Should()
            .Be("0");
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(5, 6)]
    public async Task Given_incoming_step_context_has_sequence_it_should_increment(int? incomingSequenceNr, int expectedOutgoingSequenceNr)
    {
        var incomingHeaders = new Dictionary<string, string>();
        if (incomingSequenceNr.HasValue)
        {
            incomingHeaders.Add(Headers.CorrelationSequence, incomingSequenceNr.ToString());
        }

        var incomingStepContext = new IncomingStepContext(
            new TransportMessage(
                incomingHeaders,
                new byte[0]),
            _transactionContextMock.Object
        );
        incomingStepContext.Save(new Message(incomingHeaders, new { }));
        _stepContext.Save(incomingStepContext);

        // Act
        await _sut.Process(_stepContext, _next);

        // Assert
        _messageHeaders
            .Should()
            .ContainKey(Headers.CorrelationSequence)
            .WhoseValue
            .Should()
            .Be(expectedOutgoingSequenceNr.ToString());
    }
}
