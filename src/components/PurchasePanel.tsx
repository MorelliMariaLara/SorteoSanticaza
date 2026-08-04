"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { PackagePublic } from "@/lib/types";
import { formatARS } from "@/lib/format";

type Props = {
  packages: PackagePublic[];
};

type FormState = {
  firstName: string;
  lastName: string;
  dni: string;
  birthDate: string;
  email: string;
  phone: string;
  acceptTerms: boolean;
};

const initialForm: FormState = {
  firstName: "",
  lastName: "",
  dni: "",
  birthDate: "",
  email: "",
  phone: "",
  acceptTerms: false,
};

export function PurchasePanel({ packages }: Props) {
  const router = useRouter();
  const defaultPack =
    packages.find((p) => p.popular)?.id ?? packages[0]?.id ?? 0;
  const [packageId, setPackageId] = useState(defaultPack);
  const [form, setForm] = useState<FormState>(initialForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selected = useMemo(
    () => packages.find((p) => p.id === packageId) ?? packages[0],
    [packageId, packages],
  );

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const orderRes = await fetch("/api/orders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          packageId,
          ...form,
        }),
      });
      const orderData = await orderRes.json();
      if (!orderRes.ok) {
        throw new Error(orderData.error || "No se pudo crear la orden");
      }

      const payRes = await fetch("/api/payments/checkout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ publicId: orderData.publicId }),
      });
      const payData = await payRes.json();
      if (!payRes.ok) {
        throw new Error(payData.error || "No se pudo iniciar el pago");
      }

      router.push(payData.checkoutUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Error inesperado");
      setLoading(false);
    }
  }

  return (
    <section id="comprar" className="mx-auto w-full max-w-6xl px-4 py-10 md:px-6 md:py-14">
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-bg-panel/80 p-5 md:p-6">
          <h2 className="font-[family-name:var(--font-display)] text-3xl tracking-wide text-text">
            Elegí tu pack
          </h2>
          <p className="mt-1 text-sm text-text-muted">
            Más chances, más oportunidades de llevarte el kit SANTICAZA.
          </p>

          <div className="mt-5 space-y-2">
            {packages.map((pack) => {
              const active = pack.id === packageId;
              return (
                <button
                  key={pack.id}
                  type="button"
                  onClick={() => setPackageId(pack.id)}
                  className={`relative flex w-full items-center justify-between rounded-xl border px-4 py-3.5 text-left transition ${
                    active
                      ? "border-accent bg-accent-soft"
                      : "border-border bg-bg hover:border-border-strong"
                  }`}
                >
                  {pack.popular ? (
                    <span className="absolute -top-2.5 left-4 rounded-md bg-accent px-2 py-0.5 text-[10px] font-bold tracking-wide text-accent-on">
                      MÁS POPULAR
                    </span>
                  ) : null}
                  <span className="flex items-center gap-3">
                    <span
                      className={`h-4 w-4 rounded-full border ${
                        active
                          ? "border-accent bg-accent"
                          : "border-border-strong"
                      }`}
                    />
                    <span className="font-medium">{pack.label}</span>
                  </span>
                  <span className="font-[family-name:var(--font-display)] text-xl text-accent">
                    {formatARS(pack.priceCents)}
                  </span>
                </button>
              );
            })}
          </div>
        </div>

        <form
          onSubmit={onSubmit}
          className="rounded-2xl border border-border bg-bg-panel/80 p-5 md:p-6"
        >
          <h2 className="font-[family-name:var(--font-display)] text-3xl tracking-wide text-text">
            Datos del comprador
          </h2>
          <p className="mt-1 text-sm text-text-muted">
            Completá tus datos para recibir tus números por email.
          </p>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <Field
              label="Nombre"
              value={form.firstName}
              onChange={(v) => setForm((f) => ({ ...f, firstName: v }))}
              placeholder="Juan"
              required
            />
            <Field
              label="Apellido"
              value={form.lastName}
              onChange={(v) => setForm((f) => ({ ...f, lastName: v }))}
              placeholder="Pérez"
              required
            />
            <Field
              label="DNI"
              value={form.dni}
              onChange={(v) => setForm((f) => ({ ...f, dni: v }))}
              placeholder="12345678"
              required
            />
            <Field
              label="Fecha de nacimiento"
              value={form.birthDate}
              onChange={(v) => setForm((f) => ({ ...f, birthDate: v }))}
              placeholder="DD/MM/AAAA"
              required
            />
            <Field
              label="Email"
              type="email"
              value={form.email}
              onChange={(v) => setForm((f) => ({ ...f, email: v }))}
              placeholder="tu@email.com"
              required
            />
            <div>
              <label className="label" htmlFor="phone">
                Teléfono
              </label>
              <div className="flex gap-2">
                <div className="field flex w-[96px] items-center justify-center text-sm text-text-muted">
                  ARG +54
                </div>
                <input
                  id="phone"
                  className="field"
                  value={form.phone}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, phone: e.target.value }))
                  }
                  placeholder="11 2345 6789"
                  required
                />
              </div>
            </div>
          </div>

          <label className="mt-5 flex items-start gap-3 text-sm text-text-muted">
            <input
              type="checkbox"
              className="mt-1 h-4 w-4 accent-[var(--accent)]"
              checked={form.acceptTerms}
              onChange={(e) =>
                setForm((f) => ({ ...f, acceptTerms: e.target.checked }))
              }
              required
            />
            <span>
              Acepto los{" "}
              <a href="/terminos" className="text-accent underline underline-offset-2">
                Términos y Condiciones
              </a>
            </span>
          </label>

          {selected ? (
            <p className="mt-4 text-sm text-text-muted">
              Vas a comprar{" "}
              <span className="text-text">{selected.label}</span> por{" "}
              <span className="text-accent">{formatARS(selected.priceCents)}</span>
            </p>
          ) : null}

          {error ? (
            <p className="mt-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger">
              {error}
            </p>
          ) : null}

          <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="text-xs uppercase tracking-[0.16em] text-text-muted">
              Pago seguro · Mercado Pago
            </div>
            <button
              type="submit"
              disabled={loading || !selected}
              className="animate-cta rounded-xl bg-accent px-6 py-3 font-semibold text-accent-on transition hover:bg-accent-hover disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Procesando..." : "Comprar ahora"}
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder,
  required,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  required?: boolean;
  type?: string;
}) {
  const id = label.toLowerCase().replace(/\s+/g, "-");
  return (
    <div>
      <label className="label" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        type={type}
        className="field"
        value={value}
        placeholder={placeholder}
        required={required}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  );
}
