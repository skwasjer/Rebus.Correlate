using System;
using Correlate;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Correlate.Steps;
using Rebus.Injection;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Correlate
{
	/// <summary>
	/// Rebus extensions to configure/enable Correlate.
	/// </summary>
	public static class CorrelateConfigurationExtensions
	{
		/// <summary>
		/// Configures Rebus to use Correlate as the Correlation ID provider.
		/// </summary>
		/// <param name="configurer">The options configurer.</param>
		/// <param name="loggerFactory">The logger factory.</param>
		/// <returns>The <see cref="OptionsConfigurer"/> instance.</returns>
		public static OptionsConfigurer EnableCorrelate(this OptionsConfigurer configurer, ILoggerFactory loggerFactory)
		{
			if (configurer == null)
			{
				throw new ArgumentNullException(nameof(configurer));
			}

			if (loggerFactory == null)
			{
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			// Singletons
			var correlationIdFactory = new GuidCorrelationIdFactory();
			var correlationContextAccessor = new CorrelationContextAccessor();
			configurer.Register<ICorrelationIdFactory>(ctx => correlationIdFactory);
			configurer.Register<ICorrelationContextAccessor>(ctx => correlationContextAccessor);

			// Transient
			configurer.Register<ICorrelationContextFactory>(ctx =>
				new CorrelationContextFactory(
					ctx.Get<ICorrelationContextAccessor>()
				)
			);
			configurer.Register<IAsyncCorrelationManager>(ctx =>
				new CorrelationManager(
					ctx.Get<ICorrelationContextFactory>(),
					ctx.Get<ICorrelationIdFactory>(),
					ctx.Get<ICorrelationContextAccessor>(),
					loggerFactory.CreateLogger<CorrelationManager>()
				)
			);

			return configurer
				.RegisterSteps()
				.ConfigurePipeline();
		}

		/// <summary>
		/// Configures Rebus to use Correlate as the Correlation ID provider by resolving Correlate dependencies using the specified <paramref name="serviceProvider"/>. To make sure all required dependencies are registered, use the registration extensions from the <c>Correlate.DependencyInjection</c> package.
		/// </summary>
		/// <param name="configurer">The options configurer.</param>
		/// <param name="serviceProvider">The service provider to resolve Correlate dependencies with.</param>
		/// <returns>The <see cref="OptionsConfigurer"/> instance.</returns>
		public static OptionsConfigurer EnableCorrelate(this OptionsConfigurer configurer, IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException(nameof(serviceProvider));
			}

			return configurer.EnableCorrelate(new DependencyResolverAdapter(serviceProvider.GetService));
		}

		/// <summary>
		/// Configures Rebus to use Correlate as the Correlation ID provider by resolving Correlate dependencies using the specified <paramref name="dependencyResolverAdapter"/>.
		/// </summary>
		/// <param name="configurer">The options configurer.</param>
		/// <param name="dependencyResolverAdapter">The dependency resolver adapter to resolve Correlate dependencies with.</param>
		/// <returns>The <see cref="OptionsConfigurer"/> instance.</returns>
		public static OptionsConfigurer EnableCorrelate(this OptionsConfigurer configurer, DependencyResolverAdapter dependencyResolverAdapter)
		{
			if (configurer == null)
			{
				throw new ArgumentNullException(nameof(configurer));
			}

			if (dependencyResolverAdapter == null)
			{
				throw new ArgumentNullException(nameof(dependencyResolverAdapter));
			}

			// Register Correlate steps using custom resolver.
			return configurer
				.RegisterSteps(dependencyResolverAdapter)
				.ConfigurePipeline();
		}

		private static OptionsConfigurer RegisterSteps(this OptionsConfigurer configurer, IResolutionContext? resolver = null)
		{
			configurer.Register(ctx =>
				new CorrelateOutgoingMessageStep(
					(resolver ?? ctx).Get<ICorrelationContextAccessor>(),
					(resolver ?? ctx).Get<ICorrelationIdFactory>(),
					ctx.Get<IRebusLoggerFactory>()
				)
			);
			configurer.Register(ctx =>
				new CorrelateIncomingMessageStep(
					(resolver ?? ctx).Get<IAsyncCorrelationManager>(),
					ctx.Get<IRebusLoggerFactory>()
				)
			);

			return configurer;
		}

		private static OptionsConfigurer ConfigurePipeline(this OptionsConfigurer configurer)
		{
			configurer.Decorate<IPipeline>(ctx =>
			{
				IPipeline pipeline = ctx.Get<IPipeline>();
				CorrelateOutgoingMessageStep outgoingStep = ctx.Get<CorrelateOutgoingMessageStep>();
				CorrelateIncomingMessageStep incomingStep = ctx.Get<CorrelateIncomingMessageStep>();
				return new PipelineStepInjector(pipeline)
					.OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(FlowCorrelationIdStep))
					.OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
			});

			return configurer;
		}
	}
}
