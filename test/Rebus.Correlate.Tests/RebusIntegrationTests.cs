using System;
using System.Threading.Tasks;
using Correlate;
using FluentAssertions;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Correlate.Extensions;
using Rebus.Correlate.Fixtures;
using Rebus.Messages;
using Rebus.Pipeline;
using Xunit;

namespace Rebus.Correlate
{
	public abstract class RebusIntegrationTests : IDisposable
	{
		private readonly RebusFixture _fixture;
		private readonly BuiltinHandlerActivator _activator;
		private readonly IBus _bus;

		private readonly CorrelationManager _correlationManager;
		private readonly ICorrelationContextAccessor _correlationContextAccessor;

		private readonly TaskCompletionSource<string> _tcs;

		public RebusIntegrationTests(RebusFixture fixture)
		{
			_fixture = fixture;
			_activator = fixture.CreateActivator();
			_bus = _activator.Bus;

			_correlationContextAccessor = new CorrelationContextAccessor();
			_correlationManager = new CorrelationManager(new CorrelationContextFactory(_correlationContextAccessor), new GuidCorrelationIdFactory(), _correlationContextAccessor, new TestLogger<CorrelationManager>());

			_tcs = new TaskCompletionSource<string>();
		}

		public void Dispose()
		{
			_tcs.TrySetCanceled();
			_bus?.Dispose();
			_activator?.Dispose();
		}

		[Fact]
		public async Task When_sending_in_correlation_context_should_enrich_with_correlationId_and_handle_in_context()
		{
			const string correlationId = "my-id";
			const int maxSequence = 5;
			const string expectedResult = "01234" + correlationId;

			ArrangeHandler(maxSequence, (message, sequence) => _bus.Send(message.Extend(sequence)));
			_fixture.Start();

			// Act
			await _correlationManager.CorrelateAsync(correlationId, () => _bus.Send(new TestMessage()));

			// Assert
			string handlerResult = await _tcs.Task.WithTimeout();
			handlerResult.Should().Be(expectedResult, "each handler iteration adds the sequence number and ends with the correlation id");
		}

		[Fact]
		public async Task When_not_sending_in_correlation_context_should_enrich_with_new_correlationId_and_still_handle_in_context()
		{
			const int maxSequence = 5;
			const string expectedResultBegin = "01234";

			ArrangeHandler(maxSequence, (message, sequence) => _bus.Send(message.Extend(sequence)));
			_fixture.Start();

			// Act
			await _bus.Send(new TestMessage());

			// Assert
			string handlerResult = await _tcs.Task.WithTimeout();
			handlerResult.Should().StartWith(expectedResultBegin, "each handler iteration adds the sequence number and ends with the correlation id");
			Guid.TryParse(handlerResult.Substring(expectedResultBegin.Length), out _).Should().BeTrue();
		}

		[Fact]
		public async Task When_publishing_in_correlation_context_should_enrich_with_correlationId_and_handle_in_context()
		{
			const string correlationId = "my-id";
			const int maxSequence = 5;
			const string expectedResult = "01234" + correlationId;

			await _bus.Subscribe<TestMessage>();
			ArrangeHandler(maxSequence, (message, sequence) => _bus.Publish(message.Extend(sequence)));
			_fixture.Start();

			// Act
			try
			{
				await _correlationManager.CorrelateAsync(correlationId, () => _bus.Publish(new TestMessage()));

				// Assert
				string handlerResult = await _tcs.Task.WithTimeout();
				handlerResult.Should().Be(expectedResult, "each handler iteration adds the sequence number and ends with the correlation id");
			}
			finally
			{
				await _bus.Unsubscribe<TestMessage>();
			}
		}

		[Fact]
		public async Task When_not_publishing_in_correlation_context_should_enrich_with_new_correlationId_and_still_handle_in_context()
		{
			const int maxSequence = 5;
			const string expectedResultBegin = "01234";

			await _bus.Subscribe<TestMessage>();
			ArrangeHandler(maxSequence, (message, sequence) => _bus.Publish(message.Extend(sequence)));
			_fixture.Start();

			// Act
			try
			{
				await _bus.Publish(new TestMessage());

				// Assert
				string handlerResult = await _tcs.Task.WithTimeout();
				handlerResult.Should().StartWith(expectedResultBegin, "each handler iteration adds the sequence number and ends with the correlation id");
				Guid.TryParse(handlerResult.Substring(expectedResultBegin.Length), out _).Should().BeTrue();
			}
			finally
			{
				await _bus.Unsubscribe<TestMessage>();
			}
		}

		/// <summary>
		/// Sets up handler to handle string message, and execute it until <paramref name="maxSequence"/> is reached.
		/// </summary>
		private void ArrangeHandler(int maxSequence, Func<TestMessage, int, Task> execute)
		{
			_activator.Handle<TestMessage>(async message =>
			{
				IMessageContext ctx = MessageContext.Current;
				ctx.Headers.TryGetValue(Headers.CorrelationId, out string cid);
				ctx.Headers.TryGetValue(Headers.CorrelationSequence, out string sequenceStr);
				int.TryParse(sequenceStr, out int sequence);

				// Assert context.
				CorrelationContext correlationContext = _correlationContextAccessor.CorrelationContext;
				correlationContext.Should().NotBeNull();
				correlationContext.CorrelationId.Should().Be(cid);

				if (sequence < maxSequence)
				{
					await execute(message, sequence);
				}
				else
				{
					_tcs.SetResult(message.Value + cid);
				}
			}, _tcs.SetException);
		}
	}
}
