"use client";

import { useEffect, useState } from "react";
import { formatARS, padTicket } from "@/lib/format";

type OrderRow = {
  id: number;
  publicId: string;
  name: string;
  email: string;
  dni: string;
  chances: number;
  amountCents: number;
  status: string;
  ticketNumbers: string | null;
  createdAt: string;
  paidAt: string | null;
};

export default function AdminPage() {
  const [password, setPassword] = useState("");
  const [authed, setAuthed] = useState(false);
  const [orders, setOrders] = useState<OrderRow[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [ticketNumber, setTicketNumber] = useState("");
  const [prizeLabel, setPrizeLabel] = useState("Premio principal");
  const [message, setMessage] = useState<string | null>(null);

  async function login(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const res = await fetch("/api/admin/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ password }),
    });
    if (!res.ok) {
      const data = await res.json();
      setError(data.error || "Error de acceso");
      return;
    }
    setAuthed(true);
  }

  async function loadOrders() {
    const res = await fetch("/api/admin/orders");
    if (res.status === 401) {
      setAuthed(false);
      return;
    }
    const data = await res.json();
    setOrders(data.orders ?? []);
  }

  useEffect(() => {
    if (authed) {
      void loadOrders();
    }
  }, [authed]);

  async function publishWinner(e: React.FormEvent) {
    e.preventDefault();
    setMessage(null);
    const res = await fetch("/api/admin/winners", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ticketNumber: Number(ticketNumber),
        prizeLabel,
      }),
    });
    const data = await res.json();
    if (!res.ok) {
      setMessage(data.error || "No se pudo publicar");
      return;
    }
    setMessage(`Ganador publicado: ${data.winnerName}`);
    setTicketNumber("");
  }

  if (!authed) {
    return (
      <main className="mx-auto flex min-h-screen w-full max-w-md flex-col justify-center px-4">
        <h1 className="font-[family-name:var(--font-display)] text-3xl tracking-wide text-accent">
          Admin SANTICAZA
        </h1>
        <form onSubmit={login} className="mt-6 space-y-3">
          <input
            type="password"
            className="field"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          {error ? <p className="text-sm text-danger">{error}</p> : null}
          <button
            type="submit"
            className="w-full rounded-xl bg-accent px-4 py-3 font-semibold text-accent-on"
          >
            Ingresar
          </button>
        </form>
      </main>
    );
  }

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-8 md:px-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-[family-name:var(--font-display)] text-3xl tracking-wide text-accent">
          Panel Admin
        </h1>
        <button
          type="button"
          onClick={() => void loadOrders()}
          className="rounded-lg border border-border px-3 py-2 text-sm"
        >
          Actualizar
        </button>
      </div>

      <form
        onSubmit={publishWinner}
        className="mt-6 grid gap-3 rounded-2xl border border-border bg-bg-panel p-4 md:grid-cols-3"
      >
        <input
          className="field"
          placeholder="Número ganador"
          value={ticketNumber}
          onChange={(e) => setTicketNumber(e.target.value)}
          required
        />
        <input
          className="field"
          placeholder="Premio"
          value={prizeLabel}
          onChange={(e) => setPrizeLabel(e.target.value)}
          required
        />
        <button type="submit" className="rounded-xl bg-accent px-4 py-3 font-semibold text-accent-on">
          Publicar ganador
        </button>
        {message ? (
          <p className="md:col-span-3 text-sm text-text-muted">{message}</p>
        ) : null}
      </form>

      <div className="mt-8 overflow-x-auto rounded-2xl border border-border">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-bg-elevated text-text-muted">
            <tr>
              <th className="px-3 py-3">Cliente</th>
              <th className="px-3 py-3">Estado</th>
              <th className="px-3 py-3">Monto</th>
              <th className="px-3 py-3">Números</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id} className="border-t border-border">
                <td className="px-3 py-3">
                  <div className="font-medium">{o.name}</div>
                  <div className="text-xs text-text-muted">
                    {o.email} · DNI {o.dni}
                  </div>
                </td>
                <td className="px-3 py-3 uppercase tracking-wide text-xs">{o.status}</td>
                <td className="px-3 py-3">
                  {formatARS(o.amountCents)}
                  <div className="text-xs text-text-muted">{o.chances} chances</div>
                </td>
                <td className="px-3 py-3 text-accent">
                  {o.ticketNumbers
                    ? o.ticketNumbers
                        .split(",")
                        .map((n) => `#${padTicket(Number(n))}`)
                        .join(" ")
                    : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </main>
  );
}
