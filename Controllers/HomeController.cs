using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models;
using System.Diagnostics;

namespace ShopQuanAo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult CartDetail()
        {

            return View();
        }
        public IActionResult Product()
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
