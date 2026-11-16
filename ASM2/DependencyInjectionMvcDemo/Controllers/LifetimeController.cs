using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace DependencyInjectionMvcDemo.Controllers;

/// <summary>
/// Minh họa 3 service lifetime (Transient, Scoped, Singleton) bằng cách so sánh
/// GUID tại Controller (điểm 1) và LifetimeSnapshotService (điểm 2) trong cùng request (Slide 10, 21).
/// </summary>
public sealed class LifetimeController : Controller
{
    private readonly ITransientMarker _transient;
    private readonly IScopedMarker _scoped;
    private readonly ISingletonMarker _singleton;
    private readonly ILifetimeSnapshotService _snapshot;

    public LifetimeController(
        ITransientMarker transient,
        IScopedMarker scoped,
        ISingletonMarker singleton,
        ILifetimeSnapshotService snapshot)
    {
        _transient = transient ?? throw new ArgumentNullException(nameof(transient));
        _scoped = scoped ?? throw new ArgumentNullException(nameof(scoped));
        _singleton = singleton ?? throw new ArgumentNullException(nameof(singleton));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public IActionResult Index()
    {
        var model = new LifetimeComparisonViewModel
        {
            ControllerTransientId = _transient.Id,
            SnapshotTransientId = _snapshot.TransientId,
            ControllerScopedId = _scoped.Id,
            SnapshotScopedId = _snapshot.ScopedId,
            ControllerSingletonId = _singleton.Id,
            SnapshotSingletonId = _snapshot.SingletonId
        };

        return View(model);
    }
}
