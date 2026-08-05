using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers
{
    public class PagoController : Controller
    {
        private readonly RaffleService _raffle;

        public PagoController(RaffleService raffle)
        {
            _raffle = raffle;
        }

        [HttpGet]
        public IActionResult Exito(string? order)
        {
            if (string.IsNullOrWhiteSpace(order))
            {
                return View(model: null);
            }

            return View(_raffle.GetOrderByPublicId(order));
        }
    }
}
