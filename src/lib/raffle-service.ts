import { randomUUID } from "crypto";
import { getDb } from "./db";
import type {
  CreateOrderInput,
  Order,
  Package,
  PackagePublic,
  Raffle,
  RafflePublic,
  Winner,
} from "./types";

function mapPackage(row: Package): PackagePublic {
  return {
    id: row.id,
    chances: row.chances,
    priceCents: row.price_cents,
    label: row.label,
    popular: Boolean(row.popular),
  };
}

export function getActiveRaffle(): RafflePublic | null {
  const db = getDb();
  const raffle = db
    .prepare(
      `SELECT * FROM raffles WHERE status = 'active' ORDER BY id DESC LIMIT 1`,
    )
    .get() as Raffle | undefined;

  if (!raffle) return null;

  const packages = db
    .prepare(
      `SELECT * FROM packages WHERE raffle_id = ? AND active = 1 ORDER BY sort_order ASC`,
    )
    .all(raffle.id) as Package[];

  const sold = db
    .prepare(
      `SELECT COUNT(*) as c FROM tickets WHERE raffle_id = ?`,
    )
    .get(raffle.id) as { c: number };

  return {
    id: raffle.id,
    title: raffle.title,
    subtitle: raffle.subtitle,
    description: raffle.description,
    prizeTitle: raffle.prize_title,
    prizeDescription: raffle.prize_description,
    drawAt: raffle.draw_at,
    status: raffle.status,
    totalTickets: raffle.total_tickets,
    soldTickets: sold.c,
    remainingTickets: Math.max(raffle.total_tickets - sold.c, 0),
    videoUrl: raffle.video_url,
    imageUrl: raffle.image_url,
    packages: packages.map(mapPackage),
  };
}

export function createOrder(input: CreateOrderInput) {
  const db = getDb();

  if (!input.acceptTerms) {
    throw new Error("Debés aceptar los términos y condiciones.");
  }

  const raffle = getActiveRaffle();
  if (!raffle || raffle.status !== "active") {
    throw new Error("No hay un sorteo activo.");
  }

  const pack = db
    .prepare(`SELECT * FROM packages WHERE id = ? AND active = 1`)
    .get(input.packageId) as Package | undefined;

  if (!pack || pack.raffle_id !== raffle.id) {
    throw new Error("Pack inválido.");
  }

  if (raffle.remainingTickets < pack.chances) {
    throw new Error("No quedan chances suficientes para este pack.");
  }

  const birth = new Date(input.birthDate);
  const ageMs = Date.now() - birth.getTime();
  const age = ageMs / (365.25 * 24 * 60 * 60 * 1000);
  if (Number.isNaN(birth.getTime()) || age < 18) {
    throw new Error("Solo mayores de 18 años pueden participar.");
  }

  const email = input.email.trim().toLowerCase();
  const dni = input.dni.replace(/\D/g, "");
  if (dni.length < 7 || dni.length > 8) {
    throw new Error("DNI inválido.");
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    throw new Error("Email inválido.");
  }

  const publicId = randomUUID();
  const result = db
    .prepare(
      `INSERT INTO orders (
        public_id, raffle_id, package_id, first_name, last_name, dni,
        birth_date, email, phone, chances, amount_cents, status
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'pending')`,
    )
    .run(
      publicId,
      raffle.id,
      pack.id,
      input.firstName.trim(),
      input.lastName.trim(),
      dni,
      input.birthDate,
      email,
      input.phone.trim(),
      pack.chances,
      pack.price_cents,
    );

  return {
    orderId: Number(result.lastInsertRowid),
    publicId,
    amountCents: pack.price_cents,
    chances: pack.chances,
    label: pack.label,
  };
}

function allocateTicketNumbers(raffleId: number, count: number): number[] {
  const db = getDb();
  const raffle = db
    .prepare(`SELECT * FROM raffles WHERE id = ?`)
    .get(raffleId) as Raffle;
  const used = new Set(
    (
      db
        .prepare(`SELECT number FROM tickets WHERE raffle_id = ?`)
        .all(raffleId) as Array<{ number: number }>
    ).map((r) => r.number),
  );

  const available: number[] = [];
  for (
    let n = raffle.ticket_start;
    n < raffle.ticket_start + raffle.total_tickets;
    n += 1
  ) {
    if (!used.has(n)) available.push(n);
  }

  if (available.length < count) {
    throw new Error("No hay números disponibles.");
  }

  // Fisher-Yates partial shuffle for random assignment
  for (let i = available.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1));
    [available[i], available[j]] = [available[j], available[i]];
  }

  return available.slice(0, count).sort((a, b) => a - b);
}

export function confirmPayment(publicId: string, paymentRef?: string) {
  const db = getDb();
  const order = db
    .prepare(`SELECT * FROM orders WHERE public_id = ?`)
    .get(publicId) as Order | undefined;

  if (!order) throw new Error("Orden no encontrada.");
  if (order.status === "paid") {
    const tickets = db
      .prepare(`SELECT number FROM tickets WHERE order_id = ? ORDER BY number`)
      .all(order.id) as Array<{ number: number }>;
    return { order, tickets: tickets.map((t) => t.number), alreadyPaid: true };
  }
  if (order.status !== "pending") {
    throw new Error("La orden no puede pagarse.");
  }

  const numbers = allocateTicketNumbers(order.raffle_id, order.chances);
  const insertTicket = db.prepare(
    `INSERT INTO tickets (raffle_id, order_id, number) VALUES (?, ?, ?)`,
  );

  const tx = db.transaction(() => {
    for (const number of numbers) {
      insertTicket.run(order.raffle_id, order.id, number);
    }
    db.prepare(
      `UPDATE orders SET status = 'paid', payment_ref = ?, paid_at = datetime('now') WHERE id = ?`,
    ).run(paymentRef ?? `demo_${Date.now()}`, order.id);
  });

  tx();

  const paid = db
    .prepare(`SELECT * FROM orders WHERE id = ?`)
    .get(order.id) as Order;

  return { order: paid, tickets: numbers, alreadyPaid: false };
}

export function getNumbersByEmailOrDni(query: string) {
  const db = getDb();
  const q = query.trim().toLowerCase();
  const dni = query.replace(/\D/g, "");

  const orders = db
    .prepare(
      `SELECT * FROM orders
       WHERE status = 'paid' AND (lower(email) = ? OR dni = ?)
       ORDER BY paid_at DESC`,
    )
    .all(q, dni) as Order[];

  return orders.map((order) => {
    const tickets = db
      .prepare(
        `SELECT number FROM tickets WHERE order_id = ? ORDER BY number ASC`,
      )
      .all(order.id) as Array<{ number: number }>;

    return {
      orderPublicId: order.public_id,
      name: `${order.first_name} ${order.last_name}`,
      email: order.email,
      chances: order.chances,
      amountCents: order.amount_cents,
      paidAt: order.paid_at,
      numbers: tickets.map((t) => t.number),
    };
  });
}

export function getWinners() {
  const db = getDb();
  return db
    .prepare(
      `SELECT w.*, r.title as raffle_title
       FROM winners w
       JOIN raffles r ON r.id = w.raffle_id
       ORDER BY w.drawn_at DESC`,
    )
    .all() as Array<Winner & { raffle_title: string }>;
}

export function listOrders(limit = 100) {
  const db = getDb();
  return db
    .prepare(
      `SELECT o.*,
        (SELECT GROUP_CONCAT(number) FROM tickets t WHERE t.order_id = o.id) as ticket_numbers
       FROM orders o
       ORDER BY o.created_at DESC
       LIMIT ?`,
    )
    .all(limit) as Array<Order & { ticket_numbers: string | null }>;
}

export function addWinner(input: {
  raffleId: number;
  ticketNumber: number;
  prizeLabel: string;
}) {
  const db = getDb();
  const ticket = db
    .prepare(
      `SELECT t.*, o.first_name, o.last_name
       FROM tickets t
       JOIN orders o ON o.id = t.order_id
       WHERE t.raffle_id = ? AND t.number = ?`,
    )
    .get(input.raffleId, input.ticketNumber) as
    | (TicketLite & { first_name: string; last_name: string })
    | undefined;

  if (!ticket) throw new Error("Número no vendido.");

  const winnerName = `${ticket.first_name} ${ticket.last_name.charAt(0)}.`;
  const result = db
    .prepare(
      `INSERT INTO winners (raffle_id, ticket_number, prize_label, winner_name)
       VALUES (?, ?, ?, ?)`,
    )
    .run(input.raffleId, input.ticketNumber, input.prizeLabel, winnerName);

  return { id: Number(result.lastInsertRowid), winnerName };
}

type TicketLite = { id: number; raffle_id: number; order_id: number; number: number };

export function getOrderByPublicId(publicId: string) {
  const db = getDb();
  const order = db
    .prepare(`SELECT * FROM orders WHERE public_id = ?`)
    .get(publicId) as Order | undefined;
  if (!order) return null;
  const tickets = db
    .prepare(`SELECT number FROM tickets WHERE order_id = ? ORDER BY number`)
    .all(order.id) as Array<{ number: number }>;
  return { order, tickets: tickets.map((t) => t.number) };
}
