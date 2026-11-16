using DependencyInjectionMvcDemo.Interfaces;
using DependencyInjectionMvcDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace DependencyInjectionMvcDemo.Controllers;

/// <summary>
/// Controller nhận IProductService qua Constructor Injection (Slide 13).
/// Không gọi "new ProductService(...)" hay "new InMemoryProductRepository()".
/// </summary>
public sealed class ProductsController : Controller
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IActionResult Index(string? q)
    {
        ViewData["Keyword"] = q;
        ViewData["ServiceImplementation"] = _service.GetType().Name;
        var products = _service.GetAll(q);
        return View(products);
    }

    public IActionResult Details(int? id)
    {
        var product = FindProduct(id);
        return product is null ? NotFound() : View(product);
    }

    public IActionResult Create()
    {
        return View(new Product { Price = 100_000m, Stock = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Name,Description,Price,Stock")] Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        _service.Create(product);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int? id)
    {
        var product = FindProduct(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,Name,Description,Price,Stock")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        return _service.Update(product) ? RedirectToAction(nameof(Index)) : NotFound();
    }

    public IActionResult Delete(int? id)
    {
        var product = FindProduct(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        return _service.Delete(id) ? RedirectToAction(nameof(Index)) : NotFound();
    }

    private Product? FindProduct(int? id)
        => id is null ? null : _service.GetById(id.Value);
}
