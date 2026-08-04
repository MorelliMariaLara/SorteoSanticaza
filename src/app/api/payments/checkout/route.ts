import { NextResponse } from "next/server";
import { z } from "zod";
import { confirmPayment, getOrderByPublicId } from "@/lib/raffle-service";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

const schema = z.object({
  publicId: z.string().uuid(),
});

export async function POST(request: Request) {
  try {
    const body = await request.json();
    const parsed = schema.safeParse(body);
    if (!parsed.success) {
      return NextResponse.json({ error: "publicId inválido" }, { status: 400 });
    }

    const existing = getOrderByPublicId(parsed.data.publicId);
    if (!existing) {
      return NextResponse.json({ error: "Orden no encontrada" }, { status: 404 });
    }

    const mode = process.env.PAYMENT_MODE ?? "demo";
    const mpToken = process.env.MERCADOPAGO_ACCESS_TOKEN;
    const appUrl = process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000";

    // Demo / sandbox-ready path: confirma y asigna números inmediatamente.
    // Con token real de Mercado Pago se puede reemplazar por Preference API.
    if (mode === "demo" || !mpToken) {
      const result = confirmPayment(parsed.data.publicId, `demo_${Date.now()}`);
      return NextResponse.json({
        mode: "demo",
        checkoutUrl: `${appUrl}/pago/exito?order=${parsed.data.publicId}`,
        tickets: result.tickets,
        order: {
          publicId: result.order.public_id,
          status: result.order.status,
          chances: result.order.chances,
          amountCents: result.order.amount_cents,
        },
      });
    }

    // Placeholder structure for Mercado Pago Preference integration.
    return NextResponse.json({
      mode: "mercadopago",
      message:
        "Configurá la Preference API de Mercado Pago con MERCADOPAGO_ACCESS_TOKEN.",
      checkoutUrl: `${appUrl}/pago/exito?order=${parsed.data.publicId}`,
    });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Error en checkout";
    return NextResponse.json({ error: message }, { status: 400 });
  }
}
