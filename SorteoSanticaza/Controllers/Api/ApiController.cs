using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SorteoSanticaza.Models;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers.Api
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly RaffleService _raffle;
        private readonly IConfiguration _config;

        public ApiController(RaffleService raffle, IConfiguration config)
        {
            _raffle = raffle;
            _config = config;
        }

        [HttpGet("raffle")]
        public IActionResult GetRaffle()
        {
            var raffle = _raffle.GetActiveRaffle();
            return raffle == null
                ? NotFound(new { error = "No hay sorteo activo" })
                : Ok(raffle);
        }

        [HttpPost("orders")]
        public IActionResult CreateOrder([FromBody] PurchaseForm form)
        {
            try
            {
                var order = _raffle.CreateOrder(form);
                return StatusCode(201, order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("payments/checkout")]
        public IActionResult Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PublicId))
                {
                    return BadRequest(new { error = "publicId inválido" });
                }

                var result = _raffle.ConfirmPayment(request.PublicId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("my-numbers")]
        public IActionResult MyNumbers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            {
                return BadRequest(new { error = "Ingresá email o DNI (mínimo 3 caracteres)" });
            }

            return Ok(new { results = _raffle.GetNumbersByEmailOrDni(q) });
        }

        [HttpGet("winners")]
        public IActionResult Winners()
        {
            return Ok(new { winners = _raffle.GetWinners() });
        }

        [HttpPost("admin/login")]
        public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
        {
            var expected = _config["AdminPassword"] ?? "santicaza-admin";
            if (request.Password != expected)
            {
                return Unauthorized(new { error = "Credenciales inválidas" });
            }

            HttpContext.Session.SetString("admin", "1");
            return Ok(new { ok = true });
        }

        [HttpGet("admin/orders")]
        public IActionResult AdminOrders()
        {
            if (HttpContext.Session.GetString("admin") != "1")
            {
                return Unauthorized(new { error = "No autorizado" });
            }

            return Ok(new { orders = _raffle.ListOrders() });
        }

        public class CheckoutRequest
        {
            public string PublicId { get; set; } = "";
        }

        public class AdminLoginRequest
        {
            public string Password { get; set; } = "";
        }
    }
}
