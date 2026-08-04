"use client";

import { useState } from "react";
import { SiteFooter } from "@/components/SiteFooter";
import { SiteHeader } from "@/components/SiteHeader";
import { formatARS, padTicket } from "@/lib/format";

type Result = {
  orderPublicId: string;
  name: string;
  email: string;
  chances: number;
  amountCents: number;
  paidAt: string | null;
  numbers: number[];
};

export default function MisNumerosPage() {
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [results, setResults] = useState<Result[] | null>(null);

  async function onSearch(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`/api/my-numbers?q=${encodeURIComponent(query)}`);
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "No se pudo buscar");
      setResults(data.results);
    } catch (err) {
      setResults(null);
      setError(err instanceof Error ? err.message : "Error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="min-h-screen">
      <SiteHeader />
      <section className="mx-auto w-full max-w-3xl px-4 py-10 md:px-6">
        <h1 className="font-[family-name:var(--font-display)] text-4xl tracking-wide text-accent">
          Mis Números
        </h1>
        <p className="mt-2 text-text-muted">
          Consultá tus chances con el email o DNI de la compra.
        </p>

        <form onSubmit={onSearch} className="mt-6 flex flex-col gap-3 sm:flex-row">
          <input
            className="field"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="email@ejemplo.com o DNI"
            required
          />
          <button
            type="submit"
            disabled={loading}
            className="rounded-xl bg-accent px-5 py-3 font-semibold text-accent-on hover:bg-accent-hover disabled:opacity-60"
          >
            {loading ? "Buscando..." : "Buscar"}
          </button>
        </form>

        {error ? <p className="mt-4 text-sm text-danger">{error}</p> : null}

        {results ? (
          <div className="mt-8 space-y-4">
            {results.length === 0 ? (
              <p className="text-text-muted">No encontramos compras pagadas con esos datos.</p>
            ) : (
              results.map((r) => (
                <article
                  key={r.orderPublicId}
                  className="rounded-2xl border border-border bg-bg-panel p-5"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <h2 className="font-semibold text-text">{r.name}</h2>
                    <span className="text-sm text-accent">{formatARS(r.amountCents)}</span>
                  </div>
                  <p className="mt-1 text-sm text-text-muted">
                    {r.chances} chances · {r.email}
                  </p>
                  <div className="mt-4 flex flex-wrap gap-2">
                    {r.numbers.map((n) => (
                      <span
                        key={n}
                        className="rounded-md border border-accent/40 bg-accent-soft px-2.5 py-1 font-[family-name:var(--font-display)] tracking-wider text-accent"
                      >
                        #{padTicket(n)}
                      </span>
                    ))}
                  </div>
                </article>
              ))
            )}
          </div>
        ) : null}
      </section>
      <SiteFooter />
    </main>
  );
}
