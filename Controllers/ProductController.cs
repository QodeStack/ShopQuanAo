using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Data;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Product()
    {
        var products = _context.Products.ToList();
        return View(products);
    }
}