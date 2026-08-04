import { NextResponse } from "next/server";
import { z } from "zod";
import { createOrder } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

const schema = z.object({
  packageId: z.number().int().positive(),
  firstName: z.string().min(2).max(80),
  lastName: z.string().min(2).max(80),
  dni: z.string().min(7).max(12),
  birthDate: z.string().min(8),
  email: z.string().email(),
  phone: z.string().min(8).max(30),
  acceptTerms: z.literal(true),
});

export async function POST(request: Request) {
  try {
    const body = await request.json();
    const parsed = schema.safeParse(body);
    if (!parsed.success) {
      return NextResponse.json(
        { error: "Datos incompletos o inválidos", details: parsed.error.flatten() },
        { status: 400 },
      );
    }

    const order = createOrder(parsed.data);
    return NextResponse.json(order, { status: 201 });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "No se pudo crear la orden";
    return NextResponse.json({ error: message }, { status: 400 });
  }
}
