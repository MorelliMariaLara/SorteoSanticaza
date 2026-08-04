import { NextResponse } from "next/server";
import { getActiveRaffle } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET() {
  const raffle = getActiveRaffle();
  if (!raffle) {
    return NextResponse.json(
      { error: "No hay sorteo activo" },
      { status: 404 },
    );
  }
  return NextResponse.json(raffle);
}
