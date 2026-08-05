/*
  SANTICAZA Sorteos — creación de base y tablas
  Servidor: LARA-NB\SQLEXPRESS02
  Base:     SorteosSantiCaza

  Ejecutar en SSMS contra el servidor LARA-NB\SQLEXPRESS02.
*/

IF DB_ID(N'SorteosSantiCaza') IS NULL
BEGIN
    CREATE DATABASE [SorteosSantiCaza];
END
GO

USE [SorteosSantiCaza];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

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
GO

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
GO

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
GO

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
GO

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
GO

PRINT 'SorteosSantiCaza: tablas listas.';
GO
