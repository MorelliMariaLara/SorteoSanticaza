using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Models;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly RaffleService _raffle;

        public CheckoutController(RaffleService raffle)
        {
            _raffle = raffle;
        }

        [HttpGet]
        public IActionResult Index(string order, string status)
        {
            if (string.IsNullOrWhiteSpace(order))
                return RedirectToAction("Index", "Home");

            var model = _raffle.GetOrderByPublicId(order);
            if (model == null)
                return RedirectToAction("Index", "Home");

            if (model.Status == "paid")
                return RedirectToAction("Exito", "Pago", new { order });

            return View(new CheckoutPageModel
            {
                Order = model,
                StatusHint = status,
            });
        }
    }
}
