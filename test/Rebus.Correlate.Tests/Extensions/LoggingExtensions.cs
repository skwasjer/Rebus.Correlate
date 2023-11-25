using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rebus.Correlate.Extensions;

public static class LoggingExtensions
{
	public static IServiceCollection ForceEnableLogging(this IServiceCollection services)
	{
		return services.AddLogging(logging => logging.AddProvider(new TestLoggerProvider()));
	}

	public static ILoggerFactory ForceEnableLogging(this ILoggerFactory loggerFactory)
	{
		loggerFactory.AddProvider(new TestLoggerProvider());
		return loggerFactory;
	}

	private class TestLoggerProvider : ILoggerProvider
	{
		private TestLogger _testLogger;

		public void Dispose()
		{
		}

		public ILogger CreateLogger(string categoryName)
		{
			return _testLogger ??= new TestLogger();
		}
	}
}
