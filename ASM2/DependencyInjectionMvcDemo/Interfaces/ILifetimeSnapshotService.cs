namespace DependencyInjectionMvcDemo.Interfaces;

/// <summary>
/// Điểm inject thứ 2 trong cùng một HTTP request (Slide 10),
/// dùng để so sánh GUID với điểm inject thứ 1 tại LifetimeController.
/// </summary>
public interface ILifetimeSnapshotService
{
    Guid TransientId { get; }
    Guid ScopedId { get; }
    Guid SingletonId { get; }
}
