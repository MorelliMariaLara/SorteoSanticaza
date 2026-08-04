import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { z } from "zod";
import { addWinner, getActiveRaffle } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

const schema = z.object({
  ticketNumber: z.number().int().positive(),
  prizeLabel: z.string().min(2).max(120),
  raffleId: z.number().int().positive().optional(),
});

async function assertAdmin() {
  const jar = await cookies();
  return jar.get("santicaza_admin")?.value === "1";
}

export async function POST(request: Request) {
  if (!(await assertAdmin())) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = await request.json();
    const parsed = schema.safeParse(body);
    if (!parsed.success) {
      return NextResponse.json({ error: "Datos inválidos" }, { status: 400 });
    }

    const raffle = getActiveRaffle();
    const raffleId = parsed.data.raffleId ?? raffle?.id;
    if (!raffleId) {
      return NextResponse.json({ error: "Sin sorteo" }, { status: 400 });
    }

    const winner = addWinner({
      raffleId,
      ticketNumber: parsed.data.ticketNumber,
      prizeLabel: parsed.data.prizeLabel,
    });
    return NextResponse.json(winner, { status: 201 });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "No se pudo registrar ganador";
    return NextResponse.json({ error: message }, { status: 400 });
  }
}
