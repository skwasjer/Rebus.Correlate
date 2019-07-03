using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Rebus.Injection;

namespace Rebus.Correlate
{
	/// <summary>
	/// Simple dependency resolver adapter using func to resolve Correlate dependencies.
	/// </summary>
	public class DependencyResolverAdapter : IResolutionContext
	{
		private readonly Func<Type, object> _resolver;

		/// <summary>
		/// Initializes a new instance of the <see cref="DependencyResolverAdapter"/> class using specified <paramref name="resolver"/> func.
		/// </summary>
		/// <param name="resolver">The resolver func that resolves a service type.</param>
		public DependencyResolverAdapter(Func<Type, object> resolver)
		{
			_resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
		}

		/// <summary>
		/// Gets an instance of the specified <typeparamref name="TService" />.
		/// </summary>
		public TService Get<TService>()
		{
			var service = (TService)_resolver(typeof(TService));
			if (service == null)
			{
				throw new InvalidOperationException($"Correlate can not be enabled, the service '{typeof(TService).FullName}' can not be resolved.");
			}

			return service;
		}

		[ExcludeFromCodeCoverage]
		bool IResolutionContext.Has<TService>(bool primary)
		{
			throw new NotImplementedException();
		}

		[ExcludeFromCodeCoverage]
		IEnumerable IResolutionContext.TrackedInstances => throw new NotImplementedException();
	}
}