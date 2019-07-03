using System;
using System.Collections.Generic;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Startup;
using Rebus.Transport.InMem;

namespace Rebus.Correlate.Fixtures
{
	public abstract class RebusFixture
	{
		private readonly List<Action<RebusConfigurer>> _configureActions = new List<Action<RebusConfigurer>>();

		public RebusFixture()
		{
			_configureActions.Add(configurer => configurer
				.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "input"))
				.Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
				// Route all to input.
				.Routing(r => r.TypeBased().MapFallback("input"))
			);
		}

		public BuiltinHandlerActivator Start()
		{
			var activator = new BuiltinHandlerActivator();
			ConfigureRebus(activator).Create().Start();
			return activator;
		}

		protected void Configure(Action<RebusConfigurer> configureRebus)
		{
			if (configureRebus == null)
			{
				throw new ArgumentNullException(nameof(configureRebus));
			}

			_configureActions.Add(configureRebus);
		}

		private RebusConfigurer ConfigureRebus(IHandlerActivator activator)
		{
			RebusConfigurer rebusConfigurer = Config.Configure
				.With(activator)
;
			_configureActions.ForEach(ca => ca(rebusConfigurer));

			return rebusConfigurer;
		}
	}
}