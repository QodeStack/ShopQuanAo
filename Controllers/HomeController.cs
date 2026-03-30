using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;
using System.Diagnostics;

namespace ShopQuanAo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;  

        public HomeController(ApplicationDbContext context)  
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Product(int? categoryId = null)
        {
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.TotalCount = products.Count();
            ViewBag.Categories = _context.Categories.ToList();

            return View(products.ToList());
        }

        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult CartDetail()
        {

            return View();
        }
        public IActionResult Sale()
        {

            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
