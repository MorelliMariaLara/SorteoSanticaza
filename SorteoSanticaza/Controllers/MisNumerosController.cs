using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers;

public class MisNumerosController : Controller
{
    private readonly RaffleService _raffle;
    public MisNumerosController(RaffleService raffle) => _raffle = raffle;

    [HttpGet]
    public IActionResult Index(string? q)
    {
        ViewBag.Query = q ?? "";
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
        {
            return View(model: null);
        }

        return View(_raffle.GetNumbersByEmailOrDni(q));
    }
}
