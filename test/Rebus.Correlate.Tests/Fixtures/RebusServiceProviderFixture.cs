using Correlate.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Correlate.Extensions;

namespace Rebus.Correlate.Fixtures;

public sealed class RebusServiceProviderFixture : RebusFixture, IDisposable
{
	private readonly IServiceProvider _serviceProvider;

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
		(_serviceProvider as IDisposable)?.Dispose();
	}
}