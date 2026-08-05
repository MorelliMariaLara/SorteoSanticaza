using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SorteoSanticaza.Models;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers
{
    public class AdminController : Controller
    {
        private static readonly string[] AllowedImageExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private readonly RaffleService _raffle;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AdminController(RaffleService raffle, IConfiguration config, IWebHostEnvironment env)
        {
            _raffle = raffle;
            _config = config;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsAdmin()) return View("Login");
            return RedirectToAction(nameof(Sorteos));
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
            return RedirectToAction(nameof(Sorteos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("admin");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Sorteos()
        {
            if (!IsAdmin()) return View("Login");
            ViewData["AdminTab"] = "sorteos";
            return View(_raffle.ListRafflesAdmin());
        }

        [HttpGet]
        public IActionResult NuevoSorteo()
        {
            if (!IsAdmin()) return View("Login");
            ViewData["AdminTab"] = "sorteos";
            return View("EditSorteo", _raffle.NewRaffleTemplate());
        }

        [HttpGet]
        public IActionResult EditSorteo(int id)
        {
            if (!IsAdmin()) return View("Login");
            var form = _raffle.GetRaffleAdmin(id);
            if (form == null)
            {
                TempData["Message"] = "Sorteo no encontrado.";
                return RedirectToAction(nameof(Sorteos));
            }

            ViewData["AdminTab"] = "sorteos";
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
        public IActionResult GuardarSorteo(RaffleAdminForm form, IFormFile? imageFile)
        {
            if (!IsAdmin()) return View("Login");
            ViewData["AdminTab"] = "sorteos";

            try
            {
                NormalizePackageBind(form);

                if (imageFile != null && imageFile.Length > 0)
                {
                    form.ImageUrl = SaveRaffleImage(imageFile);
                }

                var id = _raffle.SaveRaffleAdmin(form);
                TempData["Message"] = form.Id <= 0
                    ? "Sorteo creado correctamente."
                    : "Sorteo actualizado correctamente.";
                return RedirectToAction(nameof(EditSorteo), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                if (form.Packages == null || form.Packages.Count == 0)
                    form.Packages = _raffle.NewRaffleTemplate().Packages;
                return View("EditSorteo", form);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActivarSorteo(int id)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));
            try
            {
                _raffle.SetRaffleStatus(id, "active");
                TempData["Message"] = "Sorteo activado. Los demás activos pasaron a cerrado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Sorteos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarSorteo(int id)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));
            try
            {
                _raffle.SetRaffleStatus(id, "closed");
                TempData["Message"] = "Sorteo cerrado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Sorteos));
        }

        [HttpGet]
        public IActionResult Pedidos()
        {
            if (!IsAdmin()) return View("Login");
            ViewData["AdminTab"] = "pedidos";
            ViewBag.ActiveRaffle = _raffle.GetActiveRaffle();
            return View(_raffle.ListOrders());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublicarGanador(int ticketNumber, string prizeLabel)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));

            try
            {
                var raffle = _raffle.GetActiveRaffle();
                if (raffle == null)
                    throw new InvalidOperationException("Sin sorteo activo");

                var result = _raffle.AddWinner(raffle.Id, ticketNumber, prizeLabel);
                TempData["Message"] = "Ganador publicado: " + result.WinnerName;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Pedidos));
        }

        private string SaveRaffleImage(IFormFile file)
        {
            if (file.Length > MaxImageBytes)
                throw new InvalidOperationException("La imagen no puede superar 5 MB.");

            var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedImageExt.Contains(ext))
                throw new InvalidOperationException("Formato de imagen no permitido. Usá JPG, PNG, WEBP o GIF.");

            var contentType = (file.ContentType ?? "").ToLowerInvariant();
            if (!contentType.StartsWith("image/"))
                throw new InvalidOperationException("El archivo no es una imagen válida.");

            var uploads = Path.Combine(_env.WebRootPath, "images", "uploads");
            Directory.CreateDirectory(uploads);

            var name = "sorteo-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ext;
            var fullPath = Path.Combine(uploads, name);
            using (var stream = System.IO.File.Create(fullPath))
            {
                file.CopyTo(stream);
            }

            return "/images/uploads/" + name;
        }

        private static void NormalizePackageBind(RaffleAdminForm form)
        {
            if (form.Packages == null)
            {
                form.Packages = new System.Collections.Generic.List<PackageAdminRow>();
                return;
            }

            // Model binder may leave gaps; drop empty rows
            form.Packages = form.Packages
                .Where(p => p != null && p.Chances > 0)
                .ToList();
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("admin") == "1";
        }
    }
}
