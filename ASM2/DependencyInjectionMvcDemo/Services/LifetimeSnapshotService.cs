using DependencyInjectionMvcDemo.Interfaces;

namespace DependencyInjectionMvcDemo.Services;

/// <summary>
/// Nhận 3 marker qua Constructor Injection để làm điểm đối chứng thứ 2 trong cùng request.
/// </summary>
public sealed class LifetimeSnapshotService : ILifetimeSnapshotService
{
    public LifetimeSnapshotService(
        ITransientMarker transientMarker,
        IScopedMarker scopedMarker,
        ISingletonMarker singletonMarker)
    {
        TransientId = transientMarker.Id;
        ScopedId = scopedMarker.Id;
        SingletonId = singletonMarker.Id;
    }

    public Guid TransientId { get; }
    public Guid ScopedId { get; }
    public Guid SingletonId { get; }
}
