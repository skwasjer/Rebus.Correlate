using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Rebus.Injection;

namespace Rebus.Correlate;

/// <summary>
/// Simple dependency resolver adapter using func for Rebus to resolve Correlate dependencies.
/// </summary>
public class DependencyResolverAdapter : IResolutionContext
{
    private readonly Func<Type, object?> _optionalResolve;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyResolverAdapter" /> class using specified <paramref name="optionalResolve" /> func.
    /// </summary>
    /// <param name="optionalResolve">The resolver func that resolves optional services by type.</param>
    public DependencyResolverAdapter(Func<Type, object?> optionalResolve)
    {
        _optionalResolve = optionalResolve ?? throw new ArgumentNullException(nameof(optionalResolve));
    }

    /// <summary>
    /// Gets an instance of the specified <typeparamref name="TService" />.
    /// </summary>
    public TService Get<TService>()
    {
        TService service = GetOrNull<TService>();
        if (service is null)
        {
            throw new InvalidOperationException($"Correlate can not be enabled, the service '{typeof(TService).FullName}' can not be resolved.");
        }

        return service;
    }

    /// <summary>
    /// Gets an instance of the specified <typeparamref name="TService" />.
    /// </summary>
    public TService GetOrNull<TService>()
    {
        return (TService)_optionalResolve(typeof(TService))!;
    }

    [ExcludeFromCodeCoverage]
    bool IResolutionContext.Has<TService>(bool primary)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    IEnumerable IResolutionContext.TrackedInstances => throw new NotImplementedException();
}
