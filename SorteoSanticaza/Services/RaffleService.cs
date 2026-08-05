using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using SorteoSanticaza.Data;
using SorteoSanticaza.Models;

namespace SorteoSanticaza.Services
{
    public class RaffleService
    {
        private readonly Db _db;

        public RaffleService(Db db)
        {
            _db = db;
        }

        public RafflePublic? GetActiveRaffle()
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 * FROM raffles WHERE status = N'active' ORDER BY id DESC";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var raffle = new RafflePublic
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                PrizeTitle = reader.GetString(reader.GetOrdinal("prize_title")),
                PrizeDescription = reader.GetString(reader.GetOrdinal("prize_description")),
                DrawAt = reader.GetString(reader.GetOrdinal("draw_at")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                TotalTickets = reader.GetInt32(reader.GetOrdinal("total_tickets")),
                VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url")) ? null : reader.GetString(reader.GetOrdinal("video_url")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
            };
            reader.Close();

            using (var soldCmd = conn.CreateCommand())
            {
                soldCmd.CommandText = "SELECT COUNT(*) FROM tickets WHERE raffle_id = @id";
                soldCmd.Parameters.AddWithValue("@id", raffle.Id);
                raffle.SoldTickets = Convert.ToInt32(soldCmd.ExecuteScalar() ?? 0);
                raffle.RemainingTickets = Math.Max(raffle.TotalTickets - raffle.SoldTickets, 0);
            }

            using (var packCmd = conn.CreateCommand())
            {
                packCmd.CommandText = "SELECT * FROM packages WHERE raffle_id = @id AND active = 1 ORDER BY sort_order ASC";
                packCmd.Parameters.AddWithValue("@id", raffle.Id);
                using var packReader = packCmd.ExecuteReader();
                while (packReader.Read())
                {
                    raffle.Packages.Add(new PackagePublic
                    {
                        Id = packReader.GetInt32(packReader.GetOrdinal("id")),
                        Chances = packReader.GetInt32(packReader.GetOrdinal("chances")),
                        PriceCents = packReader.GetInt64(packReader.GetOrdinal("price_cents")),
                        Label = packReader.GetString(packReader.GetOrdinal("label")),
                        Popular = Convert.ToBoolean(packReader.GetValue(packReader.GetOrdinal("popular"))),
                    });
                }
            }

            return raffle;
        }

        public OrderResult CreateOrder(PurchaseForm input)
        {
            if (!input.AcceptTerms)
            {
                throw new InvalidOperationException("Debés aceptar los términos y condiciones.");
            }

            var raffle = GetActiveRaffle();
            if (raffle == null)
            {
                throw new InvalidOperationException("No hay un sorteo activo.");
            }

            using var conn = _db.Open();
            int packRaffleId;
            int chances;
            long price;
            string label;

            using (var packCmd = conn.CreateCommand())
            {
                packCmd.CommandText = "SELECT * FROM packages WHERE id = @id AND active = 1";
                packCmd.Parameters.AddWithValue("@id", input.PackageId);
                using var packReader = packCmd.ExecuteReader();
                if (!packReader.Read())
                {
                    throw new InvalidOperationException("Pack inválido.");
                }

                packRaffleId = packReader.GetInt32(packReader.GetOrdinal("raffle_id"));
                chances = packReader.GetInt32(packReader.GetOrdinal("chances"));
                price = packReader.GetInt64(packReader.GetOrdinal("price_cents"));
                label = packReader.GetString(packReader.GetOrdinal("label"));
            }

            if (packRaffleId != raffle.Id)
            {
                throw new InvalidOperationException("Pack inválido.");
            }

            if (raffle.RemainingTickets < chances)
            {
                throw new InvalidOperationException("No quedan chances suficientes para este pack.");
            }

            var birth = ParseBirthDate(input.BirthDate);
            if (birth == null)
            {
                throw new InvalidOperationException("Fecha de nacimiento inválida. Usá DD/MM/AAAA.");
            }

            var age = (DateTime.UtcNow - birth.Value.ToUniversalTime()).TotalDays / 365.25;
            if (age < 18)
            {
                throw new InvalidOperationException("Solo mayores de 18 años pueden participar.");
            }

            var dni = Regex.Replace(input.Dni, @"\D", "");
            if (dni.Length < 7 || dni.Length > 8)
            {
                throw new InvalidOperationException("DNI inválido.");
            }

            var email = input.Email.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                throw new InvalidOperationException("Email inválido.");
            }

            var publicId = Guid.NewGuid().ToString();
            using var insert = conn.CreateCommand();
            insert.CommandText = @"
INSERT INTO orders (
  public_id, raffle_id, package_id, first_name, last_name, dni,
  birth_date, email, phone, chances, amount_cents, status
) VALUES (
  @publicId, @raffleId, @packageId, @firstName, @lastName, @dni,
  @birthDate, @email, @phone, @chances, @amount, N'pending'
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            insert.Parameters.AddWithValue("@publicId", publicId);
            insert.Parameters.AddWithValue("@raffleId", raffle.Id);
            insert.Parameters.AddWithValue("@packageId", input.PackageId);
            insert.Parameters.AddWithValue("@firstName", input.FirstName.Trim());
            insert.Parameters.AddWithValue("@lastName", input.LastName.Trim());
            insert.Parameters.AddWithValue("@dni", dni);
            insert.Parameters.AddWithValue("@birthDate", birth.Value.ToString("yyyy-MM-dd"));
            insert.Parameters.AddWithValue("@email", email);
            insert.Parameters.AddWithValue("@phone", input.Phone.Trim());
            insert.Parameters.AddWithValue("@chances", chances);
            insert.Parameters.AddWithValue("@amount", price);
            var orderId = Convert.ToInt32(insert.ExecuteScalar());

            return new OrderResult
            {
                OrderId = orderId,
                PublicId = publicId,
                AmountCents = price,
                Chances = chances,
                Label = label,
            };
        }

        public CheckoutResult ConfirmPayment(string publicId, string? paymentRef = null)
        {
            using var conn = _db.Open();
            int orderId;
            int raffleId;
            int chances;
            long amount;
            string status;

            using (var orderCmd = conn.CreateCommand())
            {
                orderCmd.CommandText = "SELECT * FROM orders WHERE public_id = @id";
                orderCmd.Parameters.AddWithValue("@id", publicId);
                using var reader = orderCmd.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Orden no encontrada.");
                }

                orderId = reader.GetInt32(reader.GetOrdinal("id"));
                raffleId = reader.GetInt32(reader.GetOrdinal("raffle_id"));
                chances = reader.GetInt32(reader.GetOrdinal("chances"));
                amount = reader.GetInt64(reader.GetOrdinal("amount_cents"));
                status = reader.GetString(reader.GetOrdinal("status"));
            }

            if (status == "paid")
            {
                return new CheckoutResult
                {
                    Mode = "demo",
                    CheckoutUrl = "/Pago/Exito?order=" + publicId,
                    PublicId = publicId,
                    Status = "paid",
                    Chances = chances,
                    AmountCents = amount,
                    Tickets = GetTicketNumbers(conn, orderId),
                };
            }

            if (status != "pending" && status != "in_process")
            {
                throw new InvalidOperationException("La orden no puede pagarse.");
            }

            var numbers = AllocateTicketNumbers(conn, raffleId, chances);
            using (var tx = conn.BeginTransaction())
            {
                foreach (var number in numbers)
                {
                    using var insertTicket = conn.CreateCommand();
                    insertTicket.Transaction = tx;
                    insertTicket.CommandText = "INSERT INTO tickets (raffle_id, order_id, number) VALUES (@r, @o, @n)";
                    insertTicket.Parameters.AddWithValue("@r", raffleId);
                    insertTicket.Parameters.AddWithValue("@o", orderId);
                    insertTicket.Parameters.AddWithValue("@n", number);
                    insertTicket.ExecuteNonQuery();
                }

                using (var update = conn.CreateCommand())
                {
                    update.Transaction = tx;
                    update.CommandText = @"
UPDATE orders
SET status = N'paid', payment_ref = @ref, paid_at = SYSUTCDATETIME()
WHERE id = @id";
                    update.Parameters.AddWithValue("@ref", paymentRef ?? ("demo_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                    update.Parameters.AddWithValue("@id", orderId);
                    update.ExecuteNonQuery();
                }

                tx.Commit();
            }

            return new CheckoutResult
            {
                Mode = "mercadopago",
                CheckoutUrl = "/Pago/Exito?order=" + publicId,
                PublicId = publicId,
                Status = "paid",
                Chances = chances,
                AmountCents = amount,
                Tickets = numbers,
            };
        }

        public PaidOrderView? GetOrderByPublicId(string publicId)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT o.*, p.label as package_label
FROM orders o
LEFT JOIN packages p ON p.id = o.package_id
WHERE o.public_id = @id";
            cmd.Parameters.AddWithValue("@id", publicId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var orderId = reader.GetInt32(reader.GetOrdinal("id"));
            var view = MapOrder(reader);
            reader.Close();
            view.Tickets = GetTicketNumbers(conn, orderId);
            return view;
        }

        public void SetPreferenceId(string publicId, string preferenceId)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE orders SET preference_id = @pref WHERE public_id = @id";
            cmd.Parameters.AddWithValue("@pref", preferenceId);
            cmd.Parameters.AddWithValue("@id", publicId);
            cmd.ExecuteNonQuery();
        }

        public void UpdateOrderPaymentMeta(
            string publicId,
            string status,
            string paymentRef,
            string paymentMethod,
            string statusDetail)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE orders
SET status = @status,
    payment_ref = COALESCE(@ref, payment_ref),
    payment_method = COALESCE(@method, payment_method),
    status_detail = COALESCE(@detail, status_detail)
WHERE public_id = @id AND status <> N'paid'";
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@ref", (object?)paymentRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@method", (object?)paymentMethod ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@detail", (object?)statusDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", publicId);
            cmd.ExecuteNonQuery();
        }

        private static PaidOrderView MapOrder(SqlDataReader reader)
        {
            string? Safe(string col)
            {
                try
                {
                    var ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? null : Convert.ToString(reader.GetValue(ord), CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }

            var labelOrd = -1;
            try { labelOrd = reader.GetOrdinal("package_label"); } catch { /* optional */ }

            return new PaidOrderView
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                PublicId = reader.GetString(reader.GetOrdinal("public_id")),
                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                PackageId = reader.GetInt32(reader.GetOrdinal("package_id")),
                Chances = reader.GetInt32(reader.GetOrdinal("chances")),
                AmountCents = reader.GetInt64(reader.GetOrdinal("amount_cents")),
                PreferenceId = Safe("preference_id"),
                PaymentRef = Safe("payment_ref"),
                PaymentMethod = Safe("payment_method"),
                StatusDetail = Safe("status_detail"),
                Label = labelOrd >= 0 && !reader.IsDBNull(labelOrd)
                    ? reader.GetString(labelOrd)
                    : "Chances SANTICAZA",
            };
        }

        public List<MyNumbersResult> GetNumbersByEmailOrDni(string query)
        {
            var q = query.Trim().ToLowerInvariant();
            var dni = Regex.Replace(query, @"\D", "");
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT * FROM orders
WHERE status = N'paid' AND (LOWER(email) = @email OR dni = @dni)
ORDER BY paid_at DESC";
            cmd.Parameters.AddWithValue("@email", q);
            cmd.Parameters.AddWithValue("@dni", dni);

            var rows = new List<(int id, MyNumbersResult row)>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(reader.GetOrdinal("id"));
                    rows.Add((id, new MyNumbersResult
                    {
                        OrderPublicId = reader.GetString(reader.GetOrdinal("public_id")),
                        Name = reader.GetString(reader.GetOrdinal("first_name")) + " " + reader.GetString(reader.GetOrdinal("last_name")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Chances = reader.GetInt32(reader.GetOrdinal("chances")),
                        AmountCents = reader.GetInt64(reader.GetOrdinal("amount_cents")),
                        PaidAt = ReadDateTimeString(reader, "paid_at"),
                    }));
                }
            }

            var results = new List<MyNumbersResult>();
            foreach (var (id, row) in rows)
            {
                row.Numbers = GetTicketNumbers(conn, id);
                results.Add(row);
            }

            return results;
        }

        public List<WinnerPublic> GetWinners()
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT w.*, r.title as raffle_title
FROM winners w
JOIN raffles r ON r.id = w.raffle_id
ORDER BY w.drawn_at DESC";
            using var reader = cmd.ExecuteReader();
            var list = new List<WinnerPublic>();
            while (reader.Read())
            {
                list.Add(new WinnerPublic
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    RaffleTitle = reader.GetString(reader.GetOrdinal("raffle_title")),
                    TicketNumber = reader.GetInt32(reader.GetOrdinal("ticket_number")),
                    PrizeLabel = reader.GetString(reader.GetOrdinal("prize_label")),
                    WinnerName = reader.GetString(reader.GetOrdinal("winner_name")),
                    DrawnAt = reader.GetString(reader.GetOrdinal("drawn_at")),
                });
            }

            return list;
        }

        public List<AdminOrderRow> ListOrders(int limit = 200)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT TOP (@limit) o.*,
  (SELECT STRING_AGG(CAST(t.number AS VARCHAR(20)), ',') WITHIN GROUP (ORDER BY t.number)
   FROM tickets t WHERE t.order_id = o.id) as ticket_numbers
FROM orders o
ORDER BY o.created_at DESC";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = cmd.ExecuteReader();
            var list = new List<AdminOrderRow>();
            while (reader.Read())
            {
                list.Add(new AdminOrderRow
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PublicId = reader.GetString(reader.GetOrdinal("public_id")),
                    Name = reader.GetString(reader.GetOrdinal("first_name")) + " " + reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Dni = reader.GetString(reader.GetOrdinal("dni")),
                    Phone = reader.GetString(reader.GetOrdinal("phone")),
                    Chances = reader.GetInt32(reader.GetOrdinal("chances")),
                    AmountCents = reader.GetInt64(reader.GetOrdinal("amount_cents")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    TicketNumbers = reader.IsDBNull(reader.GetOrdinal("ticket_numbers")) ? null : reader.GetString(reader.GetOrdinal("ticket_numbers")),
                    CreatedAt = ReadDateTimeString(reader, "created_at") ?? "",
                    PaidAt = ReadDateTimeString(reader, "paid_at"),
                });
            }

            return list;
        }

        public List<RaffleAdminListItem> ListRafflesAdmin()
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT r.id, r.title, r.prize_title, r.status, r.draw_at, r.image_url, r.total_tickets,
       (SELECT COUNT(*) FROM tickets t WHERE t.raffle_id = r.id) AS sold,
       (SELECT COUNT(*) FROM packages p WHERE p.raffle_id = r.id AND p.active = 1) AS pack_count
FROM raffles r
ORDER BY CASE WHEN r.status = N'active' THEN 0 ELSE 1 END, r.id DESC;";
            using var reader = cmd.ExecuteReader();
            var list = new List<RaffleAdminListItem>();
            while (reader.Read())
            {
                list.Add(new RaffleAdminListItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    PrizeTitle = reader.GetString(2),
                    Status = reader.GetString(3),
                    DrawAt = reader.GetString(4),
                    ImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TotalTickets = reader.GetInt32(6),
                    SoldTickets = reader.GetInt32(7),
                    PackageCount = reader.GetInt32(8),
                });
            }

            return list;
        }

        public RaffleAdminForm? GetRaffleAdmin(int id)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT id, title, subtitle, description, prize_title, prize_description, draw_at, status,
       total_tickets, ticket_start, video_url, image_url
FROM raffles WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var form = new RaffleAdminForm
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Subtitle = reader.GetString(2),
                Description = reader.GetString(3),
                PrizeTitle = reader.GetString(4),
                PrizeDescription = reader.GetString(5),
                DrawAtLocal = ToLocalInput(reader.GetString(6)),
                Status = reader.GetString(7),
                TotalTickets = reader.GetInt32(8),
                TicketStart = reader.GetInt32(9),
                VideoUrl = reader.IsDBNull(10) ? null : reader.GetString(10),
                ImageUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
            };
            reader.Close();

            using (var soldCmd = conn.CreateCommand())
            {
                soldCmd.CommandText = "SELECT COUNT(*) FROM tickets WHERE raffle_id = @id";
                soldCmd.Parameters.AddWithValue("@id", id);
                form.SoldTickets = Convert.ToInt32(soldCmd.ExecuteScalar() ?? 0);
            }

            form.Packages = LoadPackagesAdmin(conn, id);
            return form;
        }

        public RaffleAdminForm NewRaffleTemplate()
        {
            var draw = DateTime.UtcNow.AddDays(30);
            try
            {
                draw = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FormatHelper.GetArgentinaTimeZone()).AddDays(30);
            }
            catch { /* ignore */ }

            return new RaffleAdminForm
            {
                Title = "Sorteo SANTICAZA",
                Subtitle = "Participá y ganá.",
                Description = "Cada compra suma chances automáticamente para el sorteo en vivo de SANTICAZA.",
                PrizeTitle = "",
                PrizeDescription = "",
                DrawAtLocal = draw.ToString("yyyy-MM-ddTHH:mm"),
                Status = "draft",
                TotalTickets = 10000,
                TicketStart = 1,
                Packages = new List<PackageAdminRow>
                {
                    new PackageAdminRow { Chances = 1, PriceArs = 1000, Label = "1 chance", SortOrder = 1, Active = true },
                    new PackageAdminRow { Chances = 3, PriceArs = 2000, Label = "3 chances", SortOrder = 2, Active = true },
                    new PackageAdminRow { Chances = 5, PriceArs = 3000, Label = "5 chances", SortOrder = 3, Active = true },
                    new PackageAdminRow { Chances = 10, PriceArs = 5000, Label = "10 chances", SortOrder = 4, Active = true },
                    new PackageAdminRow { Chances = 25, PriceArs = 10000, Label = "25 chances", Popular = true, SortOrder = 5, Active = true },
                    new PackageAdminRow { Chances = 50, PriceArs = 17000, Label = "50 chances", SortOrder = 6, Active = true },
                    new PackageAdminRow { Chances = 100, PriceArs = 30000, Label = "100 super chances", SortOrder = 7, Active = true },
                },
            };
        }

        public int SaveRaffleAdmin(RaffleAdminForm form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (string.IsNullOrWhiteSpace(form.Title)) throw new InvalidOperationException("El título es obligatorio.");
            if (string.IsNullOrWhiteSpace(form.PrizeTitle)) throw new InvalidOperationException("El premio es obligatorio.");
            if (form.TotalTickets < 1) throw new InvalidOperationException("Total de chances inválido.");
            if (form.TicketStart < 1) throw new InvalidOperationException("Número inicial inválido.");

            var drawAt = NormalizeDrawAt(form.DrawAtLocal);
            var status = string.IsNullOrWhiteSpace(form.Status) ? "draft" : form.Status.Trim().ToLowerInvariant();
            if (status != "active" && status != "draft" && status != "closed")
                status = "draft";

            var packages = (form.Packages ?? new List<PackageAdminRow>())
                .Where(p => p != null && p.Chances > 0 && p.PriceArs > 0)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Chances)
                .ToList();

            if (packages.Count == 0)
                throw new InvalidOperationException("Agregá al menos un pack de chances con precio.");

            using var conn = _db.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                int raffleId = form.Id;
                if (raffleId <= 0)
                {
                    using var insert = conn.CreateCommand();
                    insert.Transaction = tx;
                    insert.CommandText = @"
INSERT INTO raffles (
  title, subtitle, description, prize_title, prize_description,
  draw_at, status, total_tickets, ticket_start, video_url, image_url
) VALUES (
  @title, @subtitle, @description, @prizeTitle, @prizeDescription,
  @drawAt, @status, @total, @start, @video, @image
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    BindRaffleParams(insert, form, drawAt, status);
                    raffleId = Convert.ToInt32(insert.ExecuteScalar());
                }
                else
                {
                    using var update = conn.CreateCommand();
                    update.Transaction = tx;
                    update.CommandText = @"
UPDATE raffles SET
  title = @title, subtitle = @subtitle, description = @description,
  prize_title = @prizeTitle, prize_description = @prizeDescription,
  draw_at = @drawAt, status = @status, total_tickets = @total,
  ticket_start = @start, video_url = @video, image_url = @image
WHERE id = @id;";
                    BindRaffleParams(update, form, drawAt, status);
                    update.Parameters.AddWithValue("@id", raffleId);
                    var rows = update.ExecuteNonQuery();
                    if (rows == 0) throw new InvalidOperationException("Sorteo no encontrado.");
                }

                if (status == "active")
                {
                    using var deactivate = conn.CreateCommand();
                    deactivate.Transaction = tx;
                    deactivate.CommandText = "UPDATE raffles SET status = N'closed' WHERE id <> @id AND status = N'active';";
                    deactivate.Parameters.AddWithValue("@id", raffleId);
                    deactivate.ExecuteNonQuery();
                }

                SavePackagesAdmin(conn, tx, raffleId, packages);
                tx.Commit();
                return raffleId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void SetRaffleStatus(int id, string status)
        {
            status = (status ?? "").Trim().ToLowerInvariant();
            if (status != "active" && status != "draft" && status != "closed")
                throw new InvalidOperationException("Estado inválido.");

            using var conn = _db.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                if (status == "active")
                {
                    using var deactivate = conn.CreateCommand();
                    deactivate.Transaction = tx;
                    deactivate.CommandText = "UPDATE raffles SET status = N'closed' WHERE id <> @id AND status = N'active';";
                    deactivate.Parameters.AddWithValue("@id", id);
                    deactivate.ExecuteNonQuery();
                }

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE raffles SET status = @status WHERE id = @id;";
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", id);
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Sorteo no encontrado.");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void UpdateRaffleImage(int raffleId, string imageUrl)
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE raffles SET image_url = @img WHERE id = @id;";
            cmd.Parameters.AddWithValue("@img", (object?)imageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", raffleId);
            if (cmd.ExecuteNonQuery() == 0)
                throw new InvalidOperationException("Sorteo no encontrado.");
        }

        private static List<PackageAdminRow> LoadPackagesAdmin(SqlConnection conn, int raffleId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT id, chances, price_cents, label, popular, sort_order, active
FROM packages WHERE raffle_id = @id
ORDER BY sort_order ASC, chances ASC;";
            cmd.Parameters.AddWithValue("@id", raffleId);
            using var reader = cmd.ExecuteReader();
            var list = new List<PackageAdminRow>();
            while (reader.Read())
            {
                list.Add(new PackageAdminRow
                {
                    Id = reader.GetInt32(0),
                    Chances = reader.GetInt32(1),
                    PriceArs = reader.GetInt64(2) / 100m,
                    Label = reader.GetString(3),
                    Popular = Convert.ToBoolean(reader.GetValue(4)),
                    SortOrder = reader.GetInt32(5),
                    Active = Convert.ToBoolean(reader.GetValue(6)),
                });
            }

            return list;
        }

        private static void SavePackagesAdmin(SqlConnection conn, SqlTransaction tx, int raffleId, List<PackageAdminRow> packages)
        {
            var keepIds = new List<int>();
            var sort = 1;
            foreach (var p in packages)
            {
                var label = string.IsNullOrWhiteSpace(p.Label)
                    ? (p.Chances + (p.Chances == 1 ? " chance" : " chances"))
                    : p.Label.Trim();
                var priceCents = (long)Math.Round(p.PriceArs * 100m, MidpointRounding.AwayFromZero);
                if (priceCents < 1) continue;

                if (p.Id > 0)
                {
                    using var update = conn.CreateCommand();
                    update.Transaction = tx;
                    update.CommandText = @"
UPDATE packages SET
  chances = @chances, price_cents = @price, label = @label,
  popular = @popular, sort_order = @sort, active = @active
WHERE id = @id AND raffle_id = @raffleId;";
                    update.Parameters.AddWithValue("@chances", p.Chances);
                    update.Parameters.AddWithValue("@price", priceCents);
                    update.Parameters.AddWithValue("@label", label);
                    update.Parameters.AddWithValue("@popular", p.Popular);
                    update.Parameters.AddWithValue("@sort", sort);
                    update.Parameters.AddWithValue("@active", p.Active);
                    update.Parameters.AddWithValue("@id", p.Id);
                    update.Parameters.AddWithValue("@raffleId", raffleId);
                    if (update.ExecuteNonQuery() > 0)
                        keepIds.Add(p.Id);
                }
                else
                {
                    using var insert = conn.CreateCommand();
                    insert.Transaction = tx;
                    insert.CommandText = @"
INSERT INTO packages (raffle_id, chances, price_cents, label, popular, sort_order, active)
VALUES (@raffleId, @chances, @price, @label, @popular, @sort, @active);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    insert.Parameters.AddWithValue("@raffleId", raffleId);
                    insert.Parameters.AddWithValue("@chances", p.Chances);
                    insert.Parameters.AddWithValue("@price", priceCents);
                    insert.Parameters.AddWithValue("@label", label);
                    insert.Parameters.AddWithValue("@popular", p.Popular);
                    insert.Parameters.AddWithValue("@sort", sort);
                    insert.Parameters.AddWithValue("@active", p.Active);
                    keepIds.Add(Convert.ToInt32(insert.ExecuteScalar()));
                }

                sort++;
            }

            // Soft-delete packs removed from the form (keep FK integrity with orders)
            using var deactivate = conn.CreateCommand();
            deactivate.Transaction = tx;
            if (keepIds.Count == 0)
            {
                deactivate.CommandText = "UPDATE packages SET active = 0 WHERE raffle_id = @raffleId;";
                deactivate.Parameters.AddWithValue("@raffleId", raffleId);
            }
            else
            {
                var paramNames = new List<string>();
                for (var i = 0; i < keepIds.Count; i++)
                {
                    var name = "@k" + i;
                    paramNames.Add(name);
                    deactivate.Parameters.AddWithValue(name, keepIds[i]);
                }

                deactivate.CommandText =
                    "UPDATE packages SET active = 0 WHERE raffle_id = @raffleId AND id NOT IN (" +
                    string.Join(",", paramNames) + ");";
                deactivate.Parameters.AddWithValue("@raffleId", raffleId);
            }

            deactivate.ExecuteNonQuery();
        }

        private static void BindRaffleParams(SqlCommand cmd, RaffleAdminForm form, string drawAt, string status)
        {
            cmd.Parameters.AddWithValue("@title", form.Title.Trim());
            cmd.Parameters.AddWithValue("@subtitle", (form.Subtitle ?? "").Trim());
            cmd.Parameters.AddWithValue("@description", form.Description ?? "");
            cmd.Parameters.AddWithValue("@prizeTitle", form.PrizeTitle.Trim());
            cmd.Parameters.AddWithValue("@prizeDescription", form.PrizeDescription ?? "");
            cmd.Parameters.AddWithValue("@drawAt", drawAt);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@total", form.TotalTickets);
            cmd.Parameters.AddWithValue("@start", form.TicketStart);
            cmd.Parameters.AddWithValue("@video", string.IsNullOrWhiteSpace(form.VideoUrl) ? (object)DBNull.Value : form.VideoUrl.Trim());
            cmd.Parameters.AddWithValue("@image", string.IsNullOrWhiteSpace(form.ImageUrl) ? (object)DBNull.Value : form.ImageUrl.Trim());
        }

        private static string NormalizeDrawAt(string? local)
        {
            if (string.IsNullOrWhiteSpace(local))
                throw new InvalidOperationException("La fecha del sorteo es obligatoria.");

            var raw = local.Trim();
            if (DateTime.TryParseExact(raw, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDt) ||
                DateTime.TryParseExact(raw, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out localDt) ||
                DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out localDt))
            {
                var tz = FormatHelper.GetArgentinaTimeZone();
                var offset = tz.GetUtcOffset(localDt);
                var dto = new DateTimeOffset(localDt, offset);
                return dto.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            }

            if (DateTimeOffset.TryParse(raw, out var dto2))
                return dto2.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

            throw new InvalidOperationException("Fecha del sorteo inválida.");
        }

        private static string ToLocalInput(string iso)
        {
            if (DateTimeOffset.TryParse(iso, out var dto))
            {
                var local = TimeZoneInfo.ConvertTime(dto, FormatHelper.GetArgentinaTimeZone());
                return local.ToString("yyyy-MM-ddTHH:mm");
            }

            return DateTime.Now.AddDays(30).ToString("yyyy-MM-ddTHH:mm");
        }

        public (int Id, string WinnerName) AddWinner(int raffleId, int ticketNumber, string prizeLabel)
        {
            using var conn = _db.Open();
            string first;
            string last;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT t.number, o.first_name, o.last_name
FROM tickets t
JOIN orders o ON o.id = t.order_id
WHERE t.raffle_id = @r AND t.number = @n";
                cmd.Parameters.AddWithValue("@r", raffleId);
                cmd.Parameters.AddWithValue("@n", ticketNumber);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Número no vendido.");
                }

                first = reader.GetString(reader.GetOrdinal("first_name"));
                last = reader.GetString(reader.GetOrdinal("last_name"));
            }

            var winnerName = first + " " + last[0] + ".";
            using var insert = conn.CreateCommand();
            insert.CommandText = @"
INSERT INTO winners (raffle_id, ticket_number, prize_label, winner_name)
VALUES (@r, @n, @p, @w);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            insert.Parameters.AddWithValue("@r", raffleId);
            insert.Parameters.AddWithValue("@n", ticketNumber);
            insert.Parameters.AddWithValue("@p", prizeLabel);
            insert.Parameters.AddWithValue("@w", winnerName);
            var id = Convert.ToInt32(insert.ExecuteScalar());
            return (id, winnerName);
        }

        private static List<int> GetTicketNumbers(SqlConnection conn, int orderId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT number FROM tickets WHERE order_id = @id ORDER BY number ASC";
            cmd.Parameters.AddWithValue("@id", orderId);
            using var reader = cmd.ExecuteReader();
            var list = new List<int>();
            while (reader.Read())
            {
                list.Add(reader.GetInt32(0));
            }

            return list;
        }

        private static List<int> AllocateTicketNumbers(SqlConnection conn, int raffleId, int count)
        {
            int total;
            int start;
            using (var raffleCmd = conn.CreateCommand())
            {
                raffleCmd.CommandText = "SELECT total_tickets, ticket_start FROM raffles WHERE id = @id";
                raffleCmd.Parameters.AddWithValue("@id", raffleId);
                using var r = raffleCmd.ExecuteReader();
                if (!r.Read())
                {
                    throw new InvalidOperationException("Sorteo no encontrado.");
                }

                total = r.GetInt32(0);
                start = r.GetInt32(1);
            }

            var used = new HashSet<int>();
            using (var usedCmd = conn.CreateCommand())
            {
                usedCmd.CommandText = "SELECT number FROM tickets WHERE raffle_id = @id";
                usedCmd.Parameters.AddWithValue("@id", raffleId);
                using var r = usedCmd.ExecuteReader();
                while (r.Read())
                {
                    used.Add(r.GetInt32(0));
                }
            }

            var available = new List<int>();
            for (var n = start; n < start + total; n++)
            {
                if (!used.Contains(n))
                {
                    available.Add(n);
                }
            }

            if (available.Count < count)
            {
                throw new InvalidOperationException("No hay números disponibles.");
            }

            var rng = new Random();
            for (var i = available.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = available[i];
                available[i] = available[j];
                available[j] = tmp;
            }

            return available.Take(count).OrderBy(x => x).ToList();
        }

        private static string? ReadDateTimeString(SqlDataReader reader, string column)
        {
            var ord = reader.GetOrdinal(column);
            if (reader.IsDBNull(ord)) return null;
            var value = reader.GetValue(ord);
            if (value is DateTime dt)
                return dt.ToString("o", CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static DateTime? ParseBirthDate(string value)
        {
            var raw = value.Trim();
            if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            {
                return iso;
            }

            if (DateTime.TryParseExact(raw, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ar))
            {
                return ar;
            }

            return null;
        }
    }
}
