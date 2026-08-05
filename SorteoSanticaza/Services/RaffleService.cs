using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
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
            cmd.CommandText = "SELECT * FROM raffles WHERE status = 'active' ORDER BY id DESC LIMIT 1";
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
                        Popular = packReader.GetInt32(packReader.GetOrdinal("popular")) == 1,
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
  @birthDate, @email, @phone, @chances, @amount, 'pending'
);
SELECT last_insert_rowid();";
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
SET status = 'paid', payment_ref = @ref, paid_at = datetime('now')
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
WHERE public_id = @id AND status != 'paid'";
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@ref", (object)paymentRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@method", (object)paymentMethod ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@detail", (object)statusDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", publicId);
            cmd.ExecuteNonQuery();
        }

        private static PaidOrderView MapOrder(SqliteDataReader reader)
        {
            string Safe(string col)
            {
                try
                {
                    var ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? null : reader.GetString(ord);
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
WHERE status = 'paid' AND (lower(email) = @email OR dni = @dni)
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
                        PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at")) ? null : reader.GetString(reader.GetOrdinal("paid_at")),
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
SELECT o.*,
  (SELECT GROUP_CONCAT(number) FROM tickets t WHERE t.order_id = o.id) as ticket_numbers
FROM orders o
ORDER BY o.created_at DESC
LIMIT @limit";
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
                    CreatedAt = reader.GetString(reader.GetOrdinal("created_at")),
                    PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at")) ? null : reader.GetString(reader.GetOrdinal("paid_at")),
                });
            }

            return list;
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
SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("@r", raffleId);
            insert.Parameters.AddWithValue("@n", ticketNumber);
            insert.Parameters.AddWithValue("@p", prizeLabel);
            insert.Parameters.AddWithValue("@w", winnerName);
            var id = Convert.ToInt32(insert.ExecuteScalar());
            return (id, winnerName);
        }

        private static List<int> GetTicketNumbers(SqliteConnection conn, int orderId)
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

        private static List<int> AllocateTicketNumbers(SqliteConnection conn, int raffleId, int count)
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
