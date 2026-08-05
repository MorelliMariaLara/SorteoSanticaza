using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers;

public class AdminController : Controller
{
    private readonly RaffleService _raffle;
    private readonly IConfiguration _config;

    public AdminController(RaffleService raffle, IConfiguration config)
    {
        _raffle = raffle;
        _config = config;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!IsAdmin())
            return View("Login");

        return View(_raffle.ListOrders());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string password)
    {
        var expected = _config["AdminPassword"] ?? "santicaza-admin";
        if (password != expected)
        {
            ViewBag.Error = "Credenciales inválidas";
            return View("Login");
        }

        HttpContext.Session.SetString("admin", "1");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PublicarGanador(int ticketNumber, string prizeLabel)
    {
        if (!IsAdmin()) return RedirectToAction(nameof(Index));

        try
        {
            var raffle = _raffle.GetActiveRaffle()
                ?? throw new InvalidOperationException("Sin sorteo activo");
            var result = _raffle.AddWinner(raffle.Id, ticketNumber, prizeLabel);
            TempData["Message"] = $"Ganador publicado: {result.WinnerName}";
        }
        catch (Exception ex)
        {
            TempData["Message"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private bool IsAdmin() => HttpContext.Session.GetString("admin") == "1";
}
