namespace DependencyInjectionMvcDemo.Interfaces;

/// <summary>
/// Marker chung giúp quan sát instance được DI container tạo ra thông qua Guid duy nhất.
/// </summary>
public interface ILifetimeMarker
{
    Guid Id { get; }
}

/// <summary>
/// Đăng ký với AddTransient: mỗi lần inject/resolve sẽ tạo mới một instance.
/// </summary>
public interface ITransientMarker : ILifetimeMarker;

/// <summary>
/// Đăng ký với AddScoped: dùng chung 1 instance trong cùng HTTP request, đổi mới khi refresh.
/// </summary>
public interface IScopedMarker : ILifetimeMarker;

/// <summary>
/// Đăng ký với AddSingleton: dùng chung 1 instance duy nhất trong suốt vòng đời ứng dụng.
/// </summary>
public interface ISingletonMarker : ILifetimeMarker;
