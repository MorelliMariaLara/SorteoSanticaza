import Link from "next/link";

export function SiteHeader() {
  return (
    <header className="relative z-20 mx-auto flex w-full max-w-6xl items-center justify-between px-4 py-5 md:px-6">
      <Link href="/" className="group flex items-baseline gap-2">
        <span className="font-[family-name:var(--font-display)] text-3xl tracking-[0.08em] text-accent md:text-4xl">
          SANTICAZA
        </span>
        <span className="hidden text-xs uppercase tracking-[0.22em] text-text-muted sm:inline">
          Sorteos
        </span>
      </Link>

      <nav className="flex items-center gap-2 sm:gap-3">
        <Link
          href="/ganadores"
          className="inline-flex items-center gap-2 rounded-lg bg-accent px-3 py-2 text-sm font-semibold text-accent-on transition hover:bg-accent-hover sm:px-4"
        >
          <TrophyIcon />
          Ganadores
        </Link>
        <Link
          href="/mis-numeros"
          className="inline-flex items-center gap-2 rounded-lg border border-accent/70 px-3 py-2 text-sm font-semibold text-accent transition hover:bg-accent-soft sm:px-4"
        >
          <TicketIcon />
          Mis Números
        </Link>
      </nav>
    </header>
  );
}

function TrophyIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M8 4h8v3a4 4 0 0 1-8 0V4Z"
        stroke="currentColor"
        strokeWidth="1.8"
      />
      <path
        d="M8 6H5a2 2 0 0 0 2 4h1M16 6h3a2 2 0 0 1-2 4h-1"
        stroke="currentColor"
        strokeWidth="1.8"
      />
      <path d="M12 11v4M9 19h6M10 15h4" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}

function TicketIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M4 8a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4V8Z"
        stroke="currentColor"
        strokeWidth="1.8"
      />
      <path d="M10 8v8" stroke="currentColor" strokeWidth="1.8" strokeDasharray="2 2" />
    </svg>
  );
}
