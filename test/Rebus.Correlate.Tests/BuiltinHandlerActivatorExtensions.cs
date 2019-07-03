using System;
using System.Threading.Tasks;
using Rebus.Activation;

namespace Rebus.Correlate
{
	internal static class BuiltinHandlerActivatorExtensions
	{
		/// <summary>
		/// Handles message inline with the ability to catch exceptions and perform an action. This method is useful in unit tests to track message handler state.
		/// </summary>
		public static BuiltinHandlerActivator Handle<TMessage>(this BuiltinHandlerActivator activator, Func<TMessage, Task> handlerFunction, Action<Exception> onError)
		{
			return activator.Handle<TMessage>(async message =>
			{
				try
				{
					await handlerFunction(message);
				}
				catch (Exception ex) when (onError != null)
				{
					onError(ex);
				}
			});
		}
	}
}