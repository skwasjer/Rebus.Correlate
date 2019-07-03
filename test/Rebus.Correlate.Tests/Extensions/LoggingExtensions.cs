﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rebus.Correlate.Extensions
{
	public static class LoggingExtensions
	{
#if NETSTANDARD1_3 || NETFRAMEWORK
		public static IServiceCollection ForceEnableLogging(this IServiceCollection services)
		{
			return services
				.AddLogging()
				.AddLoggingProvider(new TestLoggerProvider());
		}

		private static IServiceCollection AddLoggingProvider(this IServiceCollection services, ILoggerProvider loggerProvider)
		{
			return services.AddSingleton(loggerProvider);
		}
#else
		public static IServiceCollection ForceEnableLogging(this IServiceCollection services)
		{
			return services.AddLogging(logging => logging.AddProvider(new TestLoggerProvider()));
		}
#endif

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
				return _testLogger ?? (_testLogger = new TestLogger());
			}
		}
	}
}