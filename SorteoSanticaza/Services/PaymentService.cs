using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SorteoSanticaza.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private string AccessToken => (_config["MP_ACCESS_TOKEN"] ?? "").Trim();
        private string PublicKey => (_config["MP_PUBLIC_KEY"] ?? "").Trim();

        public bool IsMercadoPagoConfigured() =>
            !string.IsNullOrWhiteSpace(AccessToken) &&
            !string.IsNullOrWhiteSpace(PublicKey) &&
            !PublicKey.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase) &&
            !AccessToken.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase) &&
            HasValidCredentialPrefix(PublicKey) &&
            HasValidCredentialPrefix(AccessToken) &&
            !HasMalformedTestAppUsrPrefix();

        public static bool HasValidCredentialPrefix(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            (value.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
             value.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase));

        public bool HasMalformedTestAppUsrPrefix() =>
            PublicKey.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase) ||
            AccessToken.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase);

        public bool HasStrippedAppUsrPrefix() =>
            PublicKey.StartsWith("-", StringComparison.Ordinal) ||
            AccessToken.StartsWith("-", StringComparison.Ordinal);

        public bool IsTestCredentials() =>
            IsMercadoPagoConfigured() &&
            PublicKey.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) &&
            AccessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);

        public bool CredentialsPairLooksConsistent()
        {
            if (!IsMercadoPagoConfigured()) return false;
            var pkTest = PublicKey.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
            var tkTest = AccessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
            var pkApp = PublicKey.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase);
            var tkApp = AccessToken.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase);
            return (pkTest && tkTest) || (pkApp && tkApp);
        }

        public string CredentialProblem()
        {
            if (HasMalformedTestAppUsrPrefix())
            {
                return "Tus claves empiezan con TEST-APP_USR-… Eso está mal: no le agregues \"TEST-\" a una clave APP_USR. " +
                       "Pegá el par de Pruebas que ya empieza con TEST-.";
            }

            if (HasStrippedAppUsrPrefix() ||
                (!string.IsNullOrWhiteSpace(PublicKey) && !HasValidCredentialPrefix(PublicKey)) ||
                (!string.IsNullOrWhiteSpace(AccessToken) && !HasValidCredentialPrefix(AccessToken)))
            {
                return "Tus claves están incompletas. Pegá exactamente MP_PUBLIC_KEY y MP_ACCESS_TOKEN del panel de Pruebas.";
            }

            if (string.IsNullOrWhiteSpace(PublicKey) || string.IsNullOrWhiteSpace(AccessToken))
                return "Faltan MP_PUBLIC_KEY o MP_ACCESS_TOKEN en .env";

            if (!CredentialsPairLooksConsistent())
                return "MP_PUBLIC_KEY y MP_ACCESS_TOKEN no son del mismo tipo (uno TEST- y el otro APP_USR-). Usá el par completo de Pruebas.";

            return null;
        }

        public bool AllowSimulatePayments()
        {
            if (IsMercadoPagoConfigured()) return false;
            var flag = _config["MP_ALLOW_SIMULATE"];
            if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public string GetPublicKey() => PublicKey;

        public string AppUrl => (_config["APP_URL"] ?? "http://localhost:5165").TrimEnd('/');

        public bool IsPublicHttpsApp =>
            AppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !AppUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        public string WebhookUrl
        {
            get
            {
                var configured = (_config["MP_WEBHOOK_URL"] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(configured) &&
                    configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !configured.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return configured;
                }

                if (!IsPublicHttpsApp) return null;
                return AppUrl + "/api/webhooks/mercadopago";
            }
        }

        public Task<JsonElement> CreatePreferenceAsync(PreferenceInput input)
        {
            var items = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = input.PackageId.ToString(),
                    ["title"] = input.Title,
                    ["description"] = input.Description,
                    ["quantity"] = 1,
                    ["unit_price"] = (double)input.Amount,
                    ["currency_id"] = input.Currency,
                }
            };

            var root = new Dictionary<string, object>
            {
                ["items"] = items,
                ["payer"] = new Dictionary<string, object> { ["email"] = input.PayerEmail },
                ["external_reference"] = input.OrderPublicId,
                ["metadata"] = new Dictionary<string, object>
                {
                    ["order_id"] = input.OrderPublicId,
                    ["package_id"] = input.PackageId,
                    ["chances"] = input.Chances,
                },
            };

            if (IsPublicHttpsApp)
            {
                root["back_urls"] = new Dictionary<string, object>
                {
                    ["success"] = AppUrl + "/Checkout?order=" + Uri.EscapeDataString(input.OrderPublicId) + "&status=success",
                    ["failure"] = AppUrl + "/Checkout?order=" + Uri.EscapeDataString(input.OrderPublicId) + "&status=failure",
                    ["pending"] = AppUrl + "/Checkout?order=" + Uri.EscapeDataString(input.OrderPublicId) + "&status=pending",
                };
                root["auto_return"] = "approved";
            }

            var hook = WebhookUrl;
            if (!string.IsNullOrEmpty(hook))
                root["notification_url"] = hook;

            return MpFetchAsync("/checkout/preferences", HttpMethod.Post, root);
        }

        public Task<JsonElement> CreatePaymentFromBrickAsync(
            JsonElement? formData,
            PreferenceInput orderInfo,
            string idempotencyKey)
        {
            var raw = formData.HasValue && formData.Value.ValueKind == JsonValueKind.Object
                ? formData.Value.GetRawText()
                : "{}";
            var payload = FlattenToObjectDict(JsonDocument.Parse(raw).RootElement);

            if (!payload.ContainsKey("transaction_amount") || payload["transaction_amount"] == null)
                payload["transaction_amount"] = (double)orderInfo.Amount;

            payload["description"] = "SANTICAZA · " + orderInfo.Title;
            payload["external_reference"] = orderInfo.OrderPublicId;

            var hook = WebhookUrl;
            if (!string.IsNullOrEmpty(hook))
                payload["notification_url"] = hook;
            else
                payload.Remove("notification_url");

            if (!payload.ContainsKey("payer") || payload["payer"] == null)
            {
                payload["payer"] = new Dictionary<string, object> { ["email"] = orderInfo.PayerEmail };
            }

            if (!payload.ContainsKey("metadata") || payload["metadata"] == null)
            {
                payload["metadata"] = new Dictionary<string, object>
                {
                    ["order_id"] = orderInfo.OrderPublicId,
                    ["package_id"] = orderInfo.PackageId,
                    ["chances"] = orderInfo.Chances,
                };
            }

            var key = idempotencyKey + "-" + Guid.NewGuid().ToString("N");
            if (key.Length > 64) key = key.Substring(0, 64);
            return MpFetchAsync("/v1/payments", HttpMethod.Post, payload, key);
        }

        public Task<JsonElement> GetPaymentAsync(string paymentId) =>
            MpFetchAsync("/v1/payments/" + paymentId, HttpMethod.Get);

        public async Task<JsonElement?> FindLatestPaymentByExternalReferenceAsync(string externalReference)
        {
            var path =
                "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" +
                Uri.EscapeDataString(externalReference);
            var result = await MpFetchAsync(path, HttpMethod.Get);
            if (!result.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var item in results.EnumerateArray())
                return item.Clone();
            return null;
        }

        public string CredentialDiagnostics()
        {
            var pk = PublicKey ?? "";
            var tk = AccessToken ?? "";
            string Mask(string v) =>
                v.Length <= 12 ? "(corto)" : v.Substring(0, Math.Min(10, v.Length)) + "…" + v.Substring(Math.Max(0, v.Length - 6)) + " (len=" + v.Length + ")";
            return "MP configurado=" + IsMercadoPagoConfigured() +
                   " PK=" + Mask(pk) +
                   " TK=" + Mask(tk) +
                   " AppUrl=" + AppUrl +
                   " Webhook=" + (WebhookUrl ?? "(local: omitido)");
        }

        public static string MapMpStatus(string status) => status switch
        {
            "approved" => "paid",
            "pending" => "pending",
            "authorized" => "pending",
            "in_process" => "in_process",
            "in_mediation" => "in_process",
            "rejected" => "rejected",
            "cancelled" => "cancelled",
            "refunded" => "refunded",
            "charged_back" => "refunded",
            _ => "failed",
        };

        public static bool IsAccredited(string mappedStatus) => mappedStatus == "paid";

        public static string FriendlyError(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Error de Mercado Pago";
            if (raw.Contains("UNAUTHORIZED", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("pa_unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                return "Mercado Pago rechazó la autorización. Verificá que MP_PUBLIC_KEY y MP_ACCESS_TOKEN sean el par de la misma aplicación (Pruebas), sin espacios, y reiniciá la app.";
            }

            if (raw.Contains("invalid_token", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("Invalid access token", StringComparison.OrdinalIgnoreCase))
            {
                return "Access Token inválido. Copiá de nuevo el Access Token de prueba desde Tus integraciones.";
            }

            return raw.Length > 220 ? raw.Substring(0, 220) + "…" : raw;
        }

        private static Dictionary<string, object> FlattenToObjectDict(JsonElement el)
        {
            var dict = new Dictionary<string, object>();
            if (el.ValueKind != JsonValueKind.Object) return dict;
            foreach (var prop in el.EnumerateObject())
            {
                dict[prop.Name] = ConvertElement(prop.Value);
            }

            return dict;
        }

        private static object ConvertElement(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    return FlattenToObjectDict(el);
                case JsonValueKind.Array:
                    return el.EnumerateArray().Select(ConvertElement).ToList();
                case JsonValueKind.String:
                    return el.GetString();
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    return el.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                default:
                    return null;
            }
        }

        private async Task<JsonElement> MpFetchAsync(string path, HttpMethod method, object body = null, string idempotencyKey = null)
        {
            var token = AccessToken;
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("MP_ACCESS_TOKEN_MISSING");
            if (token.Contains(" ") || token.Contains("\n") || token.Contains("\r"))
                throw new InvalidOperationException("MP_ACCESS_TOKEN tiene espacios o saltos de línea. Pegalo en una sola línea.");

            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(method, "https://api.mercadopago.com" + path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(idempotencyKey))
                req.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);

            if (body != null)
            {
                var json = body is JsonElement el
                    ? el.GetRawText()
                    : JsonSerializer.Serialize(body);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var res = await client.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            var clone = doc.RootElement.Clone();
            if (!res.IsSuccessStatusCode)
            {
                var msg = clone.TryGetProperty("message", out var m)
                    ? m.GetString()
                    : clone.TryGetProperty("error", out var e)
                        ? e.GetString()
                        : "MP_API_ERROR_" + (int)res.StatusCode;
                var cause = "";
                if (clone.TryGetProperty("cause", out var causes) && causes.ValueKind == JsonValueKind.Array)
                {
                    cause = string.Join("; ", causes.EnumerateArray()
                        .Select(c =>
                        {
                            var d = c.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                            var cde = c.TryGetProperty("code", out var code) ? code.ToString() : null;
                            return string.Join(" ", new[] { cde, d }.Where(x => !string.IsNullOrEmpty(x)));
                        })
                        .Where(x => !string.IsNullOrEmpty(x)));
                }

                var raw = string.IsNullOrEmpty(cause) ? (msg ?? "MP_API_ERROR") : msg + ": " + cause;
                Console.Error.WriteLine("[MP] " + (int)res.StatusCode + " " + path + " → " + raw);
                throw new InvalidOperationException(FriendlyError(raw));
            }

            return clone;
        }
    }

    public class PreferenceInput
    {
        public string OrderPublicId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PayerEmail { get; set; }
        public int PackageId { get; set; }
        public int Chances { get; set; }
    }
}
