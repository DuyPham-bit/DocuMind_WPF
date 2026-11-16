namespace DependencyInjectionMvcDemo.Models;

/// <summary>
/// ViewModel chứa GUID của 3 lifetime tại 2 điểm inject trong cùng một HTTP request (Slide 10).
/// </summary>
public sealed class LifetimeComparisonViewModel
{
    public required Guid ControllerTransientId { get; init; }
    public required Guid SnapshotTransientId { get; init; }

    public required Guid ControllerScopedId { get; init; }
    public required Guid SnapshotScopedId { get; init; }

    public required Guid ControllerSingletonId { get; init; }
    public required Guid SnapshotSingletonId { get; init; }

    public DateTimeOffset MeasuredAt { get; init; } = DateTimeOffset.Now;

    public bool IsTransientSameInRequest => ControllerTransientId == SnapshotTransientId;
    public bool IsScopedSameInRequest => ControllerScopedId == SnapshotScopedId;
    public bool IsSingletonSameInRequest => ControllerSingletonId == SnapshotSingletonId;
}
