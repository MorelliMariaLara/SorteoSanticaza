import Database from "better-sqlite3";
import fs from "fs";
import path from "path";

const dataDir = path.join(process.cwd(), "data");
const dbPath = path.join(dataDir, "santicaza.db");

let db: Database.Database | null = null;

function ensureSchema(database: Database.Database) {
  database.exec(`
    PRAGMA journal_mode = WAL;
    PRAGMA foreign_keys = ON;

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
  `);
}

function seedIfEmpty(database: Database.Database) {
  const count = database.prepare("SELECT COUNT(*) as c FROM raffles").get() as {
    c: number;
  };
  if (count.c > 0) return;

  const insertRaffle = database.prepare(`
    INSERT INTO raffles (
      title, subtitle, description, prize_title, prize_description,
      draw_at, status, total_tickets, ticket_start, video_url, image_url
    ) VALUES (?, ?, ?, ?, ?, ?, 'active', ?, 1, ?, ?)
  `);

  const result = insertRaffle.run(
    "Sorteo SANTICAZA",
    "Participá y ganá.",
    "Cada compra suma chances automáticamente para el sorteo en vivo de SANTICAZA.",
    "Kit premium de caza y óptica",
    "Participá por un kit SANTICAZA con óptica térmica, rifle PCP y accesorios seleccionados de nuestra armería.",
    "2026-09-15T22:00:00-03:00",
    10000,
    null,
    "/images/premio-kit.jpg",
  );

  const raffleId = Number(result.lastInsertRowid);

  const insertPackage = database.prepare(`
    INSERT INTO packages (raffle_id, chances, price_cents, label, popular, sort_order)
    VALUES (?, ?, ?, ?, ?, ?)
  `);

  const packs: Array<[number, number, string, number, number]> = [
    [1, 100000, "1 chance", 0, 1],
    [3, 200000, "3 chances", 0, 2],
    [5, 300000, "5 chances", 0, 3],
    [10, 500000, "10 chances", 0, 4],
    [25, 1000000, "25 chances", 1, 5],
    [50, 1700000, "50 chances", 0, 6],
    [100, 3000000, "100 super chances", 0, 7],
  ];

  for (const [chances, price, label, popular, order] of packs) {
    insertPackage.run(raffleId, chances, price, label, popular, order);
  }

  const insertWinner = database.prepare(`
    INSERT INTO winners (raffle_id, ticket_number, prize_label, winner_name, drawn_at)
    VALUES (?, ?, ?, ?, ?)
  `);

  insertWinner.run(
    raffleId,
    4521,
    "Sorteo anterior — Accesorio premium",
    "M. González",
    "2026-07-10T22:00:00-03:00",
  );
}

export function getDb() {
  if (db) return db;
  if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
  }
  db = new Database(dbPath);
  ensureSchema(db);
  seedIfEmpty(db);
  return db;
}
