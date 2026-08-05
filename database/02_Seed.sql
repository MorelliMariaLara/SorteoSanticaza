/*
  SANTICAZA Sorteos — seed inicial (opcional)
  La app también siembra sola si raffles está vacío.
*/

USE [SorteosSantiCaza];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.raffles)
BEGIN
    DECLARE @raffleId INT;

    INSERT INTO dbo.raffles (
        title, subtitle, description, prize_title, prize_description,
        draw_at, status, total_tickets, ticket_start, video_url, image_url
    )
    VALUES (
        N'Sorteo SANTICAZA',
        N'Participá y ganá.',
        N'Cada compra suma chances automáticamente para el sorteo en vivo de SANTICAZA.',
        N'Kit premium de caza y óptica',
        N'Participá por un kit SANTICAZA con óptica térmica, rifle PCP y accesorios seleccionados de nuestra armería.',
        N'2026-09-15T22:00:00-03:00',
        N'active',
        10000,
        1,
        NULL,
        N'/images/premio-kit.jpg'
    );

    SET @raffleId = SCOPE_IDENTITY();

    INSERT INTO dbo.packages (raffle_id, chances, price_cents, label, popular, sort_order, active) VALUES
    (@raffleId, 1, 100000, N'1 chance', 0, 1, 1),
    (@raffleId, 3, 200000, N'3 chances', 0, 2, 1),
    (@raffleId, 5, 300000, N'5 chances', 0, 3, 1),
    (@raffleId, 10, 500000, N'10 chances', 0, 4, 1),
    (@raffleId, 25, 1000000, N'25 chances', 1, 5, 1),
    (@raffleId, 50, 1700000, N'50 chances', 0, 6, 1),
    (@raffleId, 100, 3000000, N'100 super chances', 0, 7, 1);

    INSERT INTO dbo.winners (raffle_id, ticket_number, prize_label, winner_name, drawn_at)
    VALUES (@raffleId, 4521, N'Sorteo anterior — Accesorio premium', N'M. González', N'2026-07-10T22:00:00-03:00');

    PRINT 'Seed insertado.';
END
ELSE
BEGIN
    PRINT 'Ya hay raffles; seed omitido.';
END
GO
