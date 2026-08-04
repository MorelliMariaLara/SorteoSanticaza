import { SiteFooter } from "@/components/SiteFooter";
import { SiteHeader } from "@/components/SiteHeader";
import { formatDateTimeAR, padTicket } from "@/lib/format";
import { getWinners } from "@/lib/raffle-service";

export const dynamic = "force-dynamic";

export default function GanadoresPage() {
  const winners = getWinners();

  return (
    <main className="min-h-screen">
      <SiteHeader />
      <section className="mx-auto w-full max-w-4xl px-4 py-10 md:px-6">
        <h1 className="font-[family-name:var(--font-display)] text-4xl tracking-wide text-accent">
          Ganadores
        </h1>
        <p className="mt-2 text-text-muted">
          Resultados de los sorteos en vivo de SANTICAZA.
        </p>

        <div className="mt-8 space-y-3">
          {winners.length === 0 ? (
            <p className="text-text-muted">Todavía no hay ganadores publicados.</p>
          ) : (
            winners.map((w) => (
              <article
                key={w.id}
                className="flex flex-col gap-2 rounded-2xl border border-border bg-bg-panel p-5 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <p className="text-xs uppercase tracking-[0.16em] text-text-muted">
                    {w.raffle_title}
                  </p>
                  <h2 className="mt-1 font-semibold text-text">{w.winner_name}</h2>
                  <p className="text-sm text-text-muted">{w.prize_label}</p>
                </div>
                <div className="text-left sm:text-right">
                  <p className="font-[family-name:var(--font-display)] text-2xl text-accent">
                    #{padTicket(w.ticket_number)}
                  </p>
                  <p className="text-xs text-text-muted">
                    {formatDateTimeAR(w.drawn_at)}
                  </p>
                </div>
              </article>
            ))
          )}
        </div>
      </section>
      <SiteFooter />
    </main>
  );
}
