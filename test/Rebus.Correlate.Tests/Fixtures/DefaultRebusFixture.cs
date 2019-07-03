using System;
using Microsoft.Extensions.Logging;
using Rebus.Correlate.Extensions;

namespace Rebus.Correlate.Fixtures
{
	public sealed class DefaultRebusFixture : RebusFixture, IDisposable
	{
		private readonly ILoggerFactory _loggerFactory;

		public DefaultRebusFixture()
		{
			_loggerFactory = new LoggerFactory()
				.ForceEnableLogging();

			Configure(configurer => 
				configurer.Options(opts => 
					opts.EnableCorrelate(_loggerFactory)
				)
			);
		}

		public void Dispose()
		{
			_loggerFactory?.Dispose();
		}
	}
}