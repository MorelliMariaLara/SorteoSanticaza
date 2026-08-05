using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SorteoSanticaza.Services;

namespace SorteoSanticaza.Controllers.Api
{
    [ApiController]
    [Route("api")]
    public class PaymentsApiController : ControllerBase
    {
        private readonly RaffleService _raffle;
        private readonly PaymentService _payments;

        public PaymentsApiController(RaffleService raffle, PaymentService payments)
        {
            _raffle = raffle;
            _payments = payments;
        }

        [HttpGet("payments/config")]
        public IActionResult Config()
        {
            return Ok(new
            {
                configured = _payments.IsMercadoPagoConfigured(),
                simulate = _payments.AllowSimulatePayments(),
                publicKey = _payments.IsMercadoPagoConfigured() ? _payments.GetPublicKey() : null,
                testCredentials = _payments.IsTestCredentials(),
                pairOk = _payments.CredentialsPairLooksConsistent(),
                problem = _payments.CredentialProblem(),
                diagnostics = _payments.CredentialDiagnostics(),
                webhookUrl = _payments.IsMercadoPagoConfigured() ? _payments.WebhookUrl : null,
            });
        }

        public class PrefBody
        {
            public string OrderId { get; set; }
        }

        public class ProcessBody
        {
            public string OrderId { get; set; }
            public bool Simulate { get; set; }
            public JsonElement? FormData { get; set; }
            public string SelectedPaymentMethod { get; set; }
        }

        [HttpPost("payments/preference")]
        public async Task<IActionResult> Preference([FromBody] PrefBody body)
        {
            var order = _raffle.GetOrderByPublicId(body?.OrderId ?? "");
            if (order == null) return NotFound(new { error = "Orden no encontrada" });
            if (order.Status == "paid")
            {
                return Ok(new
                {
                    orderId = order.PublicId,
                    alreadyPaid = true,
                    redirect = "/Pago/Exito?order=" + order.PublicId,
                });
            }

            try
            {
                var credProblem = _payments.CredentialProblem();
                if (!_payments.IsMercadoPagoConfigured())
                {
                    if (credProblem != null && !_payments.AllowSimulatePayments())
                    {
                        return BadRequest(new
                        {
                            error = credProblem,
                            diagnostics = _payments.CredentialDiagnostics(),
                        });
                    }

                    return Ok(new
                    {
                        orderId = order.PublicId,
                        simulateOnly = true,
                        preferenceId = (string)null,
                        amount = order.AmountCents / 100m,
                        currency = "ARS",
                        chances = order.Chances,
                        label = order.Label,
                    });
                }

                if (!string.IsNullOrEmpty(order.PreferenceId))
                {
                    return Ok(new
                    {
                        orderId = order.PublicId,
                        preferenceId = order.PreferenceId,
                        simulateOnly = false,
                        amount = order.AmountCents / 100m,
                        currency = "ARS",
                        chances = order.Chances,
                        label = order.Label,
                    });
                }

                var pref = await _payments.CreatePreferenceAsync(new PreferenceInput
                {
                    OrderPublicId = order.PublicId,
                    Title = order.Label + " · SANTICAZA",
                    Description = "Sorteo SANTICAZA · " + order.Chances + " chances",
                    Amount = order.AmountCents / 100m,
                    Currency = "ARS",
                    PayerEmail = order.Email,
                    PackageId = order.PackageId,
                    Chances = order.Chances,
                });

                var prefId = pref.GetProperty("id").ToString();
                _raffle.SetPreferenceId(order.PublicId, prefId);

                string initPoint = pref.TryGetProperty("init_point", out var ip) ? ip.GetString() : null;
                string sandboxInit = pref.TryGetProperty("sandbox_init_point", out var sip) ? sip.GetString() : null;
                var checkoutUrl = _payments.IsTestCredentials()
                    ? (sandboxInit ?? initPoint)
                    : (initPoint ?? sandboxInit);

                return Ok(new
                {
                    orderId = order.PublicId,
                    preferenceId = prefId,
                    initPoint,
                    sandboxInitPoint = sandboxInit,
                    checkoutUrl,
                    simulateOnly = false,
                    amount = order.AmountCents / 100m,
                    currency = "ARS",
                    chances = order.Chances,
                    label = order.Label,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine(_payments.CredentialDiagnostics());
                return StatusCode(500, new
                {
                    error = PaymentService.FriendlyError(ex.Message),
                    diagnostics = _payments.CredentialDiagnostics(),
                    pairOk = _payments.CredentialsPairLooksConsistent(),
                });
            }
        }

        [HttpPost("payments/process")]
        public async Task<IActionResult> Process([FromBody] ProcessBody body)
        {
            var order = _raffle.GetOrderByPublicId(body?.OrderId ?? "");
            if (order == null) return NotFound(new { error = "Orden no encontrada" });

            if (order.Status == "paid")
            {
                return Ok(new
                {
                    status = "paid",
                    accredited = true,
                    paymentId = order.PaymentRef,
                    redirect = "/Pago/Exito?order=" + order.PublicId,
                });
            }

            try
            {
                if (body.Simulate)
                {
                    if (!_payments.AllowSimulatePayments())
                        return BadRequest(new { error = "Simulación deshabilitada. Configurá Mercado Pago." });

                    var paid = _raffle.ConfirmPayment(order.PublicId, "simulate_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    return Ok(new
                    {
                        status = "paid",
                        accredited = true,
                        paymentId = (string)null,
                        tickets = paid.Tickets,
                        redirect = "/Pago/Exito?order=" + order.PublicId,
                    });
                }

                if (!_payments.IsMercadoPagoConfigured())
                    return BadRequest(new { error = "Mercado Pago no configurado (MP_PUBLIC_KEY / MP_ACCESS_TOKEN)" });

                var payment = await _payments.CreatePaymentFromBrickAsync(
                    body.FormData,
                    new PreferenceInput
                    {
                        OrderPublicId = order.PublicId,
                        Title = order.Label + " · SANTICAZA",
                        Description = "Sorteo SANTICAZA · " + order.Chances + " chances",
                        Amount = order.AmountCents / 100m,
                        Currency = "ARS",
                        PayerEmail = order.Email,
                        PackageId = order.PackageId,
                        Chances = order.Chances,
                    },
                    order.PublicId);

                var mpStatus = payment.TryGetProperty("status", out var st) ? st.GetString() : null;
                var status = PaymentService.MapMpStatus(mpStatus);
                var paymentId = payment.TryGetProperty("id", out var id) ? id.ToString() : null;
                var method = payment.TryGetProperty("payment_method_id", out var pm)
                    ? pm.GetString()
                    : body.SelectedPaymentMethod;
                var detail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;

                if (PaymentService.IsAccredited(status))
                {
                    _raffle.ConfirmPayment(order.PublicId, paymentId);
                }
                else
                {
                    _raffle.UpdateOrderPaymentMeta(order.PublicId, status, paymentId, method, detail);
                }

                return Ok(new
                {
                    status,
                    accredited = PaymentService.IsAccredited(status),
                    paymentId,
                    statusDetail = detail,
                    redirect = PaymentService.IsAccredited(status)
                        ? "/Pago/Exito?order=" + order.PublicId
                        : "/Checkout?order=" + order.PublicId + "&status=" + status,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return StatusCode(500, new { error = PaymentService.FriendlyError(ex.Message) });
            }
        }

        [HttpGet("payments/order/{orderId}")]
        public async Task<IActionResult> OrderStatus(string orderId)
        {
            var order = _raffle.GetOrderByPublicId(orderId);
            if (order == null) return NotFound(new { error = "Orden no encontrada" });

            if (order.Status != "paid" && _payments.IsMercadoPagoConfigured())
            {
                try
                {
                    JsonElement? payment = null;
                    if (!string.IsNullOrEmpty(order.PaymentRef) &&
                        !order.PaymentRef.StartsWith("demo_", StringComparison.OrdinalIgnoreCase) &&
                        !order.PaymentRef.StartsWith("simulate_", StringComparison.OrdinalIgnoreCase))
                    {
                        payment = await _payments.GetPaymentAsync(order.PaymentRef);
                    }
                    else
                    {
                        payment = await _payments.FindLatestPaymentByExternalReferenceAsync(order.PublicId);
                    }

                    if (payment.HasValue)
                    {
                        var p = payment.Value;
                        var status = PaymentService.MapMpStatus(
                            p.TryGetProperty("status", out var st) ? st.GetString() : null);
                        var paymentId = p.TryGetProperty("id", out var id) ? id.ToString() : order.PaymentRef;
                        var method = p.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : order.PaymentMethod;
                        var detail = p.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;

                        if (PaymentService.IsAccredited(status))
                        {
                            _raffle.ConfirmPayment(order.PublicId, paymentId);
                            order = _raffle.GetOrderByPublicId(order.PublicId);
                        }
                        else
                        {
                            _raffle.UpdateOrderPaymentMeta(order.PublicId, status, paymentId, method, detail);
                            order = _raffle.GetOrderByPublicId(order.PublicId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

            var accredited = order.Status == "paid";
            return Ok(new
            {
                orderId = order.PublicId,
                status = order.Status,
                accredited,
                paymentId = order.PaymentRef,
                tickets = accredited ? order.Tickets : null,
                redirect = accredited ? "/Pago/Exito?order=" + order.PublicId : null,
            });
        }

        [HttpGet("webhooks/mercadopago")]
        [HttpPost("webhooks/mercadopago")]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                string paymentId =
                    Request.Query["data.id"].FirstOrDefault()
                    ?? Request.Query["id"].FirstOrDefault();

                var topic = Request.Query["topic"].FirstOrDefault()
                    ?? Request.Query["type"].FirstOrDefault();

                if (Request.ContentLength > 0 ||
                    (Request.ContentType != null && Request.ContentType.Contains("json")))
                {
                    try
                    {
                        using var doc = await JsonDocument.ParseAsync(Request.Body);
                        if (doc.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("id", out var idEl))
                            paymentId ??= idEl.ToString();
                        if (doc.RootElement.TryGetProperty("type", out var typeEl))
                            topic ??= typeEl.GetString();
                        if (doc.RootElement.TryGetProperty("action", out var actionEl))
                            topic ??= actionEl.GetString();
                    }
                    catch
                    {
                        /* body vacío o no JSON */
                    }
                }

                if (!string.IsNullOrEmpty(topic) &&
                    !topic.Contains("payment", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { ok = true });
                }

                if (string.IsNullOrEmpty(paymentId)) return Ok(new { ok = true });

                var payment = await _payments.GetPaymentAsync(paymentId);
                string orderId = null;
                if (payment.TryGetProperty("external_reference", out var orderIdEl))
                    orderId = orderIdEl.GetString();
                if (string.IsNullOrEmpty(orderId) &&
                    payment.TryGetProperty("metadata", out var meta) &&
                    meta.TryGetProperty("order_id", out var metaOrder))
                    orderId = metaOrder.GetString();

                if (string.IsNullOrEmpty(orderId)) return Ok(new { ok = true });

                var status = PaymentService.MapMpStatus(
                    payment.TryGetProperty("status", out var st) ? st.GetString() : null);
                var method = payment.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : null;
                var detail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
                var pid = payment.TryGetProperty("id", out var id) ? id.ToString() : paymentId;

                if (PaymentService.IsAccredited(status))
                    _raffle.ConfirmPayment(orderId, pid);
                else
                    _raffle.UpdateOrderPaymentMeta(orderId, status, pid, method, detail);

                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Ok(new { ok = true });
            }
        }
    }
}
