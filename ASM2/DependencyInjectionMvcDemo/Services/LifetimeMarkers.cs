using DependencyInjectionMvcDemo.Interfaces;

namespace DependencyInjectionMvcDemo.Services;

public abstract class LifetimeMarkerBase : ILifetimeMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class TransientMarker : LifetimeMarkerBase, ITransientMarker;

public sealed class ScopedMarker : LifetimeMarkerBase, IScopedMarker;

public sealed class SingletonMarker : LifetimeMarkerBase, ISingletonMarker;
