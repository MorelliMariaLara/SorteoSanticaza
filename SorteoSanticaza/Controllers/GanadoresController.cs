using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers;

public class GanadoresController : Controller
{
    private readonly RaffleService _raffle;
    public GanadoresController(RaffleService raffle) => _raffle = raffle;

    public IActionResult Index() => View(_raffle.GetWinners());
}
