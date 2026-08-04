import { NextResponse } from "next/server";
import { getNumbersByEmailOrDni } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const q = searchParams.get("q")?.trim() ?? "";
  if (q.length < 3) {
    return NextResponse.json(
      { error: "Ingresá email o DNI (mínimo 3 caracteres)" },
      { status: 400 },
    );
  }

  const results = getNumbersByEmailOrDni(q);
  return NextResponse.json({ results });
}
