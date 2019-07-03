using System;
using Correlate.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Correlate.Extensions;

namespace Rebus.Correlate.Fixtures
{
	public sealed class RebusServiceProviderFixture : RebusFixture, IDisposable
	{
		private readonly ServiceProvider _serviceProvider;

		public RebusServiceProviderFixture()
		{
			_serviceProvider = new ServiceCollection()
				.ForceEnableLogging()
				.AddCorrelate()
				.BuildServiceProvider();

			Configure(configurer => 
				configurer.Options(opts => 
					opts.EnableCorrelate(_serviceProvider)
				)
			);
		}

		public void Dispose()
		{
			_serviceProvider?.Dispose();
		}
	}
}