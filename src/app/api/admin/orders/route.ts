import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { listOrders } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

async function assertAdmin() {
  const jar = await cookies();
  return jar.get("santicaza_admin")?.value === "1";
}

export async function GET() {
  if (!(await assertAdmin())) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }
  const orders = listOrders(200).map((o) => ({
    id: o.id,
    publicId: o.public_id,
    name: `${o.first_name} ${o.last_name}`,
    email: o.email,
    dni: o.dni,
    phone: o.phone,
    chances: o.chances,
    amountCents: o.amount_cents,
    status: o.status,
    ticketNumbers: o.ticket_numbers,
    createdAt: o.created_at,
    paidAt: o.paid_at,
  }));
  return NextResponse.json({ orders });
}
