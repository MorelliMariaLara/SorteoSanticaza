import { NextResponse } from "next/server";
import { getWinners } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET() {
  const winners = getWinners().map((w) => ({
    id: w.id,
    raffleTitle: w.raffle_title,
    ticketNumber: w.ticket_number,
    prizeLabel: w.prize_label,
    winnerName: w.winner_name,
    drawnAt: w.drawn_at,
  }));
  return NextResponse.json({ winners });
}
