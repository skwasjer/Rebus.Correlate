using System;
using System.Threading.Tasks;

namespace Rebus.Correlate.Extensions
{
	public static class TaskExtensions
	{
		public static T GetResult<T>(this Task<T> task, TimeSpan timeout)
		{
			return task.GetResult(timeout.Milliseconds);
		}

		public static T GetResult<T>(this Task<T> task, int millisecondsTimeout = 5000)
		{
			if (task.Wait(millisecondsTimeout))
			{
				return task.GetAwaiter().GetResult();
			}

			throw new TimeoutException();
		}

		public static async Task<T> WithTimeout<T>(this Task<T> task, int millisecondsTimeout = 5000)
		{
			await Task.WhenAny(task, Task.Delay(millisecondsTimeout));
			if (task.IsCompletedSuccessfully || task.IsFaulted)
			{
				return await task;
			}

			throw new TimeoutException();
		}
	}
}
