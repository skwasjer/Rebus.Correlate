using Microsoft.Extensions.Logging;

namespace Rebus.Correlate;

public class TestLogger<T> : TestLogger, ILogger<T>
{
	public TestLogger(bool isEnabled = true)
		: base(isEnabled)
	{
	}
}

public class TestLogger : ILogger
{
	private readonly bool _isEnabled;

	public TestLogger(bool isEnabled = true)
	{
		_isEnabled = isEnabled;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return _isEnabled;
	}

	public IDisposable BeginScope<TState>(TState state)
	{
		return NullScope.Instance;
	}

	private class NullScope : IDisposable
	{
		private NullScope()
		{
		}

		public static IDisposable Instance { get; } = new NullScope();

		public void Dispose()
		{
		}
	}
}