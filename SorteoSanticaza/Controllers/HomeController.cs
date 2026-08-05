using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Models;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers
{
    public class HomeController : Controller
    {
        private readonly RaffleService _raffle;

        public HomeController(RaffleService raffle)
        {
            _raffle = raffle;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var raffle = _raffle.GetActiveRaffle();
            return View(raffle);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Comprar(PurchaseForm form)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Completá todos los datos correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                var order = _raffle.CreateOrder(form);
                return RedirectToAction("Index", "Checkout", new { order = order.PublicId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
