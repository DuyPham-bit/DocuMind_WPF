using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagerMvc.Data;
using ProductManagerMvc.Models;

namespace ProductManagerMvc.Controllers;

/// <summary>
/// Controller điều phối các thao tác CRUD và tìm kiếm sản phẩm bằng LINQ + EF Core.
/// </summary>
public class ProductsController : Controller
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Products?q=...
    public async Task<IActionResult> Index(string? q)
    {
        ViewData["Query"] = q;

        IQueryable<Product> query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string keyword = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                (p.Description != null && p.Description.Contains(keyword)));
        }

        List<Product> products = await query
            .OrderBy(p => p.Id)
            .ToListAsync();

        return View(products);
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        Product? product = await FindProductAsync(id);
        return product is null ? NotFound() : View(product);
    }

    // GET: /Products/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock")] Product product)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        SetSuccessMessage($"Đã thêm sản phẩm \"{product.Name}\" thành công.");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Products/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        Product? product = await FindProductAsync(id);
        return product is null ? NotFound() : View(product);
    }

    // POST: /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Stock = product.Stock;

        await _db.SaveChangesAsync();

        SetSuccessMessage($"Đã cập nhật sản phẩm \"{existing.Name}\" thành công.");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Products/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        Product? product = await FindProductAsync(id);
        return product is null ? NotFound() : View(product);
    }

    // POST: /Products/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Product? product = await _db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        SetSuccessMessage($"Đã xóa sản phẩm \"{product.Name}\".");
        return RedirectToAction(nameof(Index));
    }

    private void SetSuccessMessage(string message)
    {
        if (TempData is not null)
        {
            TempData["SuccessMessage"] = message;
        }
    }

    private async Task<Product?> FindProductAsync(int? id)
    {
        if (id is null)
        {
            return null;
        }

        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id.Value);
    }
}
