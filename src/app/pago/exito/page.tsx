import Link from "next/link";
import { SiteFooter } from "@/components/SiteFooter";
import { SiteHeader } from "@/components/SiteHeader";
import { formatARS, padTicket } from "@/lib/format";
import { getOrderByPublicId } from "@/lib/raffle-service";

export const dynamic = "force-dynamic";

export default async function PagoExitoPage({
  searchParams,
}: {
  searchParams: Promise<{ order?: string }>;
}) {
  const { order: publicId } = await searchParams;
  const data = publicId ? getOrderByPublicId(publicId) : null;

  return (
    <main className="min-h-screen">
      <SiteHeader />
      <section className="mx-auto w-full max-w-2xl px-4 py-12 md:px-6">
        <p className="text-xs uppercase tracking-[0.18em] text-accent">Pago confirmado</p>
        <h1 className="mt-2 font-[family-name:var(--font-display)] text-4xl tracking-wide">
          ¡Ya estás participando!
        </h1>

        {!data || data.order.status !== "paid" ? (
          <p className="mt-4 text-text-muted">
            No encontramos una orden paga con ese identificador.
          </p>
        ) : (
          <div className="mt-6 rounded-2xl border border-border bg-bg-panel p-6">
            <p className="text-text">
              {data.order.first_name} {data.order.last_name}
            </p>
            <p className="mt-1 text-sm text-text-muted">
              {data.order.chances} chances · {formatARS(data.order.amount_cents)}
            </p>
            <div className="mt-5 flex flex-wrap gap-2">
              {data.tickets.map((n) => (
                <span
                  key={n}
                  className="rounded-md border border-accent/40 bg-accent-soft px-2.5 py-1 font-[family-name:var(--font-display)] tracking-wider text-accent"
                >
                  #{padTicket(n)}
                </span>
              ))}
            </div>
            <p className="mt-5 text-sm text-text-muted">
              Guardá estos números. También podés consultarlos luego en Mis Números.
            </p>
          </div>
        )}

        <div className="mt-6 flex flex-wrap gap-3">
          <Link
            href="/"
            className="rounded-xl bg-accent px-5 py-3 font-semibold text-accent-on hover:bg-accent-hover"
          >
            Volver al sorteo
          </Link>
          <Link
            href="/mis-numeros"
            className="rounded-xl border border-accent/60 px-5 py-3 font-semibold text-accent"
          >
            Mis Números
          </Link>
        </div>
      </section>
      <SiteFooter />
    </main>
  );
}
