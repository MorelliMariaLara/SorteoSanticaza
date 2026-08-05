using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SorteoSanticaza.Data
{
    public class Db
    {
        private readonly string _connectionString;

        public Db(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("SorteosSantiCaza")
                ?? configuration["ConnectionStrings:SorteosSantiCaza"]
                ?? configuration["CONNECTION_STRING"]
                ?? @"Server=LARA-NB\SQLEXPRESS02;Database=SorteosSantiCaza;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
        }

        public string ConnectionString => _connectionString;

        public SqlConnection Open()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public void EnsureCreatedAndSeeded()
        {
            EnsureDatabaseExists();
            using var conn = Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
IF OBJECT_ID(N'dbo.raffles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.raffles
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_raffles PRIMARY KEY,
        title NVARCHAR(200) NOT NULL,
        subtitle NVARCHAR(300) NOT NULL,
        description NVARCHAR(MAX) NOT NULL,
        prize_title NVARCHAR(300) NOT NULL,
        prize_description NVARCHAR(MAX) NOT NULL,
        draw_at NVARCHAR(64) NOT NULL,
        status NVARCHAR(40) NOT NULL CONSTRAINT DF_raffles_status DEFAULT (N'active'),
        total_tickets INT NOT NULL CONSTRAINT DF_raffles_total DEFAULT (10000),
        ticket_start INT NOT NULL CONSTRAINT DF_raffles_start DEFAULT (1),
        video_url NVARCHAR(500) NULL,
        image_url NVARCHAR(500) NULL,
        created_at DATETIME2(3) NOT NULL CONSTRAINT DF_raffles_created DEFAULT (SYSUTCDATETIME())
    );
END

IF OBJECT_ID(N'dbo.packages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.packages
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_packages PRIMARY KEY,
        raffle_id INT NOT NULL,
        chances INT NOT NULL,
        price_cents BIGINT NOT NULL,
        label NVARCHAR(120) NOT NULL,
        popular BIT NOT NULL CONSTRAINT DF_packages_popular DEFAULT (0),
        sort_order INT NOT NULL CONSTRAINT DF_packages_sort DEFAULT (0),
        active BIT NOT NULL CONSTRAINT DF_packages_active DEFAULT (1),
        CONSTRAINT FK_packages_raffles FOREIGN KEY (raffle_id)
            REFERENCES dbo.raffles (id) ON DELETE CASCADE
    );
END

IF OBJECT_ID(N'dbo.orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.orders
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_orders PRIMARY KEY,
        public_id NVARCHAR(64) NOT NULL,
        raffle_id INT NOT NULL,
        package_id INT NOT NULL,
        first_name NVARCHAR(120) NOT NULL,
        last_name NVARCHAR(120) NOT NULL,
        dni NVARCHAR(20) NOT NULL,
        birth_date NVARCHAR(20) NOT NULL,
        email NVARCHAR(256) NOT NULL,
        phone NVARCHAR(40) NOT NULL,
        chances INT NOT NULL,
        amount_cents BIGINT NOT NULL,
        status NVARCHAR(40) NOT NULL CONSTRAINT DF_orders_status DEFAULT (N'pending'),
        payment_ref NVARCHAR(120) NULL,
        preference_id NVARCHAR(120) NULL,
        payment_method NVARCHAR(80) NULL,
        status_detail NVARCHAR(200) NULL,
        created_at DATETIME2(3) NOT NULL CONSTRAINT DF_orders_created DEFAULT (SYSUTCDATETIME()),
        paid_at DATETIME2(3) NULL,
        CONSTRAINT UQ_orders_public_id UNIQUE (public_id),
        CONSTRAINT FK_orders_raffles FOREIGN KEY (raffle_id) REFERENCES dbo.raffles (id),
        CONSTRAINT FK_orders_packages FOREIGN KEY (package_id) REFERENCES dbo.packages (id)
    );
    CREATE INDEX IX_orders_email ON dbo.orders (email);
    CREATE INDEX IX_orders_dni ON dbo.orders (dni);
END

IF OBJECT_ID(N'dbo.tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tickets
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tickets PRIMARY KEY,
        raffle_id INT NOT NULL,
        order_id INT NOT NULL,
        number INT NOT NULL,
        created_at DATETIME2(3) NOT NULL CONSTRAINT DF_tickets_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_tickets_raffle_number UNIQUE (raffle_id, number),
        CONSTRAINT FK_tickets_raffles FOREIGN KEY (raffle_id) REFERENCES dbo.raffles (id),
        CONSTRAINT FK_tickets_orders FOREIGN KEY (order_id) REFERENCES dbo.orders (id) ON DELETE CASCADE
    );
    CREATE INDEX IX_tickets_order ON dbo.tickets (order_id);
END

IF OBJECT_ID(N'dbo.winners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.winners
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_winners PRIMARY KEY,
        raffle_id INT NOT NULL,
        ticket_number INT NOT NULL,
        prize_label NVARCHAR(200) NOT NULL,
        winner_name NVARCHAR(200) NOT NULL,
        drawn_at NVARCHAR(64) NOT NULL CONSTRAINT DF_winners_drawn DEFAULT (CONVERT(NVARCHAR(64), SYSUTCDATETIME(), 126)),
        CONSTRAINT FK_winners_raffles FOREIGN KEY (raffle_id) REFERENCES dbo.raffles (id)
    );
END
";
                cmd.ExecuteNonQuery();
            }

            EnsureColumn(conn, "orders", "preference_id", "NVARCHAR(120) NULL");
            EnsureColumn(conn, "orders", "payment_method", "NVARCHAR(80) NULL");
            EnsureColumn(conn, "orders", "status_detail", "NVARCHAR(200) NULL");

            long count;
            using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM dbo.raffles";
                count = Convert.ToInt64(countCmd.ExecuteScalar() ?? 0L);
            }

            if (count > 0) return;

            using var tx = conn.BeginTransaction();
            long raffleId;
            using (var insert = conn.CreateCommand())
            {
                insert.Transaction = tx;
                insert.CommandText = @"
INSERT INTO dbo.raffles (
  title, subtitle, description, prize_title, prize_description,
  draw_at, status, total_tickets, ticket_start, video_url, image_url
) VALUES (
  @title, @subtitle, @description, @prizeTitle, @prizeDescription,
  @drawAt, N'active', 10000, 1, NULL, @imageUrl
);
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
                insert.Parameters.AddWithValue("@title", "Sorteo SANTICAZA");
                insert.Parameters.AddWithValue("@subtitle", "Participá y ganá.");
                insert.Parameters.AddWithValue("@description", "Cada compra suma chances automáticamente para el sorteo en vivo de SANTICAZA.");
                insert.Parameters.AddWithValue("@prizeTitle", "Kit premium de caza y óptica");
                insert.Parameters.AddWithValue("@prizeDescription", "Participá por un kit SANTICAZA con óptica térmica, rifle PCP y accesorios seleccionados de nuestra armería.");
                insert.Parameters.AddWithValue("@drawAt", "2026-09-15T22:00:00-03:00");
                insert.Parameters.AddWithValue("@imageUrl", "/images/premio-kit.jpg");
                raffleId = Convert.ToInt64(insert.ExecuteScalar() ?? 0L);
            }

            var packs = new (int chances, long price, string label, bool popular, int order)[]
            {
                (1, 100000, "1 chance", false, 1),
                (3, 200000, "3 chances", false, 2),
                (5, 300000, "5 chances", false, 3),
                (10, 500000, "10 chances", false, 4),
                (25, 1000000, "25 chances", true, 5),
                (50, 1700000, "50 chances", false, 6),
                (100, 3000000, "100 super chances", false, 7),
            };

            foreach (var p in packs)
            {
                using var insertPack = conn.CreateCommand();
                insertPack.Transaction = tx;
                insertPack.CommandText = @"
INSERT INTO dbo.packages (raffle_id, chances, price_cents, label, popular, sort_order, active)
VALUES (@raffleId, @chances, @price, @label, @popular, @order, 1)";
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
                winner.CommandText = @"
INSERT INTO dbo.winners (raffle_id, ticket_number, prize_label, winner_name, drawn_at)
VALUES (@raffleId, 4521, N'Sorteo anterior — Accesorio premium', N'M. González', N'2026-07-10T22:00:00-03:00')";
                winner.Parameters.AddWithValue("@raffleId", raffleId);
                winner.ExecuteNonQuery();
            }

            tx.Commit();
        }

        private void EnsureDatabaseExists()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var dbName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(dbName)) return;

            builder.InitialCatalog = "master";
            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
IF DB_ID(@name) IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@name) + N';';
    EXEC sys.sp_executesql @sql;
END";
            cmd.Parameters.AddWithValue("@name", dbName);
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqlConnection conn, string table, string column, string definition)
        {
            using var check = conn.CreateCommand();
            check.CommandText = @"
SELECT 1
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name = @table AND c.name = @column";
            check.Parameters.AddWithValue("@table", table);
            check.Parameters.AddWithValue("@column", column);
            var exists = check.ExecuteScalar();
            if (exists != null) return;

            using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE dbo." + table + " ADD " + column + " " + definition;
            alter.ExecuteNonQuery();
        }
    }
}
