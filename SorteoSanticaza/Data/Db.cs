using Microsoft.Data.Sqlite;

namespace SorteoSanticaza.Data;

public class Db
{
    private readonly string _connectionString;

    public Db(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configured = configuration.GetConnectionString("Default")
            ?? "Data Source=App_Data/santicaza.db";

        if (configured.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var relative = configured["Data Source=".Length..].Trim();
            if (!Path.IsPathRooted(relative))
            {
                var full = Path.Combine(env.ContentRootPath, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                configured = $"Data Source={full}";
            }
        }

        _connectionString = configured;
    }

    public SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public void EnsureCreatedAndSeeded()
    {
        using var conn = Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS raffles (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  title TEXT NOT NULL,
                  subtitle TEXT NOT NULL,
                  description TEXT NOT NULL,
                  prize_title TEXT NOT NULL,
                  prize_description TEXT NOT NULL,
                  draw_at TEXT NOT NULL,
                  status TEXT NOT NULL DEFAULT 'active',
                  total_tickets INTEGER NOT NULL DEFAULT 10000,
                  ticket_start INTEGER NOT NULL DEFAULT 1,
                  video_url TEXT,
                  image_url TEXT,
                  created_at TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS packages (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  raffle_id INTEGER NOT NULL REFERENCES raffles(id) ON DELETE CASCADE,
                  chances INTEGER NOT NULL,
                  price_cents INTEGER NOT NULL,
                  label TEXT NOT NULL,
                  popular INTEGER NOT NULL DEFAULT 0,
                  sort_order INTEGER NOT NULL DEFAULT 0,
                  active INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS orders (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  public_id TEXT NOT NULL UNIQUE,
                  raffle_id INTEGER NOT NULL REFERENCES raffles(id),
                  package_id INTEGER NOT NULL REFERENCES packages(id),
                  first_name TEXT NOT NULL,
                  last_name TEXT NOT NULL,
                  dni TEXT NOT NULL,
                  birth_date TEXT NOT NULL,
                  email TEXT NOT NULL,
                  phone TEXT NOT NULL,
                  chances INTEGER NOT NULL,
                  amount_cents INTEGER NOT NULL,
                  status TEXT NOT NULL DEFAULT 'pending',
                  payment_ref TEXT,
                  created_at TEXT NOT NULL DEFAULT (datetime('now')),
                  paid_at TEXT
                );

                CREATE TABLE IF NOT EXISTS tickets (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  raffle_id INTEGER NOT NULL REFERENCES raffles(id),
                  order_id INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
                  number INTEGER NOT NULL,
                  created_at TEXT NOT NULL DEFAULT (datetime('now')),
                  UNIQUE(raffle_id, number)
                );

                CREATE TABLE IF NOT EXISTS winners (
                  id INTEGER PRIMARY KEY AUTOINCREMENT,
                  raffle_id INTEGER NOT NULL REFERENCES raffles(id),
                  ticket_number INTEGER NOT NULL,
                  prize_label TEXT NOT NULL,
                  winner_name TEXT NOT NULL,
                  drawn_at TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE INDEX IF NOT EXISTS idx_orders_email ON orders(email);
                CREATE INDEX IF NOT EXISTS idx_orders_dni ON orders(dni);
                CREATE INDEX IF NOT EXISTS idx_orders_public_id ON orders(public_id);
                CREATE INDEX IF NOT EXISTS idx_tickets_order ON tickets(order_id);
                """;
            cmd.ExecuteNonQuery();
        }

        long count;
        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM raffles";
            count = (long)(countCmd.ExecuteScalar() ?? 0L);
        }

        if (count > 0) return;

        using var tx = conn.BeginTransaction();
        long raffleId;
        using (var insert = conn.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = """
                INSERT INTO raffles (
                  title, subtitle, description, prize_title, prize_description,
                  draw_at, status, total_tickets, ticket_start, video_url, image_url
                ) VALUES (
                  @title, @subtitle, @description, @prizeTitle, @prizeDescription,
                  @drawAt, 'active', 10000, 1, NULL, @imageUrl
                );
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("@title", "Sorteo SANTICAZA");
            insert.Parameters.AddWithValue("@subtitle", "Participá y ganá.");
            insert.Parameters.AddWithValue("@description", "Cada compra suma chances automáticamente para el sorteo en vivo de SANTICAZA.");
            insert.Parameters.AddWithValue("@prizeTitle", "Kit premium de caza y óptica");
            insert.Parameters.AddWithValue("@prizeDescription", "Participá por un kit SANTICAZA con óptica térmica, rifle PCP y accesorios seleccionados de nuestra armería.");
            insert.Parameters.AddWithValue("@drawAt", "2026-09-15T22:00:00-03:00");
            insert.Parameters.AddWithValue("@imageUrl", "/images/premio-kit.jpg");
            raffleId = (long)(insert.ExecuteScalar() ?? 0L);
        }

        var packs = new (int chances, long price, string label, int popular, int order)[]
        {
            (1, 100000, "1 chance", 0, 1),
            (3, 200000, "3 chances", 0, 2),
            (5, 300000, "5 chances", 0, 3),
            (10, 500000, "10 chances", 0, 4),
            (25, 1000000, "25 chances", 1, 5),
            (50, 1700000, "50 chances", 0, 6),
            (100, 3000000, "100 super chances", 0, 7),
        };

        foreach (var p in packs)
        {
            using var insertPack = conn.CreateCommand();
            insertPack.Transaction = tx;
            insertPack.CommandText = """
                INSERT INTO packages (raffle_id, chances, price_cents, label, popular, sort_order)
                VALUES (@raffleId, @chances, @price, @label, @popular, @order)
                """;
            insertPack.Parameters.AddWithValue("@raffleId", raffleId);
            insertPack.Parameters.AddWithValue("@chances", p.chances);
            insertPack.Parameters.AddWithValue("@price", p.price);
            insertPack.Parameters.AddWithValue("@label", p.label);
            insertPack.Parameters.AddWithValue("@popular", p.popular);
            insertPack.Parameters.AddWithValue("@order", p.order);
            insertPack.ExecuteNonQuery();
        }

        using (var winner = conn.CreateCommand())
        {
            winner.Transaction = tx;
            winner.CommandText = """
                INSERT INTO winners (raffle_id, ticket_number, prize_label, winner_name, drawn_at)
                VALUES (@raffleId, 4521, 'Sorteo anterior — Accesorio premium', 'M. González', '2026-07-10T22:00:00-03:00')
                """;
            winner.Parameters.AddWithValue("@raffleId", raffleId);
            winner.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
