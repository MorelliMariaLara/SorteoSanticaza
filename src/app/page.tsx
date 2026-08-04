import Image from "next/image";
import Link from "next/link";
import { Countdown } from "@/components/Countdown";
import { PurchasePanel } from "@/components/PurchasePanel";
import { SiteFooter } from "@/components/SiteFooter";
import { SiteHeader } from "@/components/SiteHeader";
import { TrustBar } from "@/components/TrustBar";
import { formatDateTimeAR } from "@/lib/format";
import { getActiveRaffle } from "@/lib/raffle-service";

export const dynamic = "force-dynamic";

export default function HomePage() {
  const raffle = getActiveRaffle();

  if (!raffle) {
    return (
      <main className="mx-auto flex min-h-screen max-w-3xl flex-col items-center justify-center px-4 text-center">
        <h1 className="font-[family-name:var(--font-display)] text-5xl tracking-wide text-accent">
          SANTICAZA
        </h1>
        <p className="mt-4 text-text-muted">No hay un sorteo activo por el momento.</p>
      </main>
    );
  }

  return (
    <main>
      <div className="relative min-h-[100svh] overflow-hidden">
        <Image
          src="/images/hero-caza.jpg"
          alt="Ambiente de caza SANTICAZA"
          fill
          priority
          className="object-cover object-center"
          sizes="100vw"
        />
        <div className="absolute inset-0 bg-gradient-to-b from-black/70 via-black/55 to-[var(--bg)]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,rgba(212,162,74,0.18),transparent_40%)]" />

        <div className="relative z-10 flex min-h-[100svh] flex-col">
          <SiteHeader />

          <section className="mx-auto flex w-full max-w-6xl flex-1 flex-col justify-end px-4 pb-14 pt-10 md:px-6 md:pb-20">
            <p className="animate-rise font-[family-name:var(--font-display)] text-5xl tracking-[0.12em] text-accent sm:text-6xl md:text-7xl">
              SANTICAZA
            </p>
            <h1 className="animate-rise-delay mt-3 max-w-2xl font-[family-name:var(--font-display)] text-3xl leading-tight tracking-wide text-text sm:text-4xl md:text-5xl">
              {raffle.subtitle}
            </h1>
            <p className="animate-rise-delay-2 mt-3 max-w-xl text-base text-text-muted md:text-lg">
              {raffle.description}
            </p>
            <div className="animate-rise-delay-2 mt-7 flex flex-wrap items-center gap-3">
              <a
                href="#comprar"
                className="animate-cta rounded-xl bg-accent px-6 py-3 font-semibold text-accent-on transition hover:bg-accent-hover"
              >
                Comprar chances
              </a>
              <Link
                href="/mis-numeros"
                className="rounded-xl border border-white/25 px-6 py-3 font-semibold text-text transition hover:border-accent hover:text-accent"
              >
                Ver mis números
              </Link>
            </div>
          </section>
        </div>
      </div>

      <section className="mx-auto w-full max-w-6xl px-4 py-10 md:px-6">
        <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="inline-flex rounded-md border border-accent/40 bg-accent-soft px-3 py-1 text-xs font-semibold tracking-[0.14em] text-accent">
              SORTEO EN VIVO
            </p>
            <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl tracking-wide md:text-4xl">
              {raffle.prizeTitle}
            </h2>
            <p className="mt-2 max-w-2xl text-text-muted">{raffle.prizeDescription}</p>
            <p className="mt-3 text-sm text-text">
              Fecha del sorteo:{" "}
              <span className="text-accent">{formatDateTimeAR(raffle.drawAt)}</span>
            </p>
          </div>
          <div className="w-full md:max-w-md">
            <Countdown target={raffle.drawAt} />
          </div>
        </div>

        <div className="mt-8 grid gap-4 md:grid-cols-2">
          <div className="overflow-hidden rounded-2xl border border-border bg-bg-panel">
            {raffle.videoUrl ? (
              <div className="relative aspect-[9/16] max-h-[520px] w-full md:aspect-video md:max-h-none">
                <iframe
                  src={raffle.videoUrl}
                  title="Video del sorteo SANTICAZA"
                  className="absolute inset-0 h-full w-full"
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowFullScreen
                />
              </div>
            ) : (
              <div className="flex aspect-video items-center justify-center text-text-muted">
                Próximamente video del premio
              </div>
            )}
          </div>
          <div className="relative min-h-[280px] overflow-hidden rounded-2xl border border-border">
            <Image
              src={raffle.imageUrl || "/images/premio-kit.jpg"}
              alt={raffle.prizeTitle}
              fill
              className="object-cover"
              sizes="(max-width: 768px) 100vw, 50vw"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-transparent to-transparent" />
            <p className="absolute bottom-4 left-4 right-4 font-[family-name:var(--font-display)] text-2xl tracking-wide text-accent">
              Empezamos nuevo sorteo
            </p>
          </div>
        </div>

        <p className="mt-4 text-sm text-text-muted">
          Chances vendidas: {raffle.soldTickets.toLocaleString("es-AR")} /{" "}
          {raffle.totalTickets.toLocaleString("es-AR")}
        </p>
      </section>

      <PurchasePanel packages={raffle.packages} />
      <TrustBar />
      <SiteFooter />
    </main>
  );
}
