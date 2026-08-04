import { SiteFooter } from "@/components/SiteFooter";
import { SiteHeader } from "@/components/SiteHeader";

export default function TerminosPage() {
  return (
    <main className="min-h-screen">
      <SiteHeader />
      <section className="mx-auto w-full max-w-3xl px-4 py-10 md:px-6">
        <h1 className="font-[family-name:var(--font-display)] text-4xl tracking-wide text-accent">
          Términos y Condiciones
        </h1>
        <div className="mt-6 space-y-4 text-sm leading-relaxed text-text-muted">
          <p>
            Los sorteos de SANTICAZA están dirigidos exclusivamente a mayores de 18
            años residentes en Argentina. Al comprar chances, el participante
            acepta estas condiciones.
          </p>
          <p>
            Cada chance corresponde a un número único asignado al azar una vez
            confirmado el pago. Los números se pueden consultar en la sección
            “Mis Números” con el email o DNI utilizado en la compra.
          </p>
          <p>
            El sorteo se realiza en vivo en la fecha y hora publicadas. SANTICAZA
            se reserva el derecho de modificar premios equivalentes ante falta de
            stock, comunicándolo previamente.
          </p>
          <p>
            Los pagos se procesan a través de Mercado Pago. Las chances solo se
            acreditan ante pagos aprobados. Ante contracargos o cancelaciones, los
            números asociados quedan anulados.
          </p>
          <p>
            Los datos personales se utilizan únicamente para gestionar la
            participación, contacto al ganador y cumplimiento legal. No se
            comparten con terceros ajenos a la operación del sorteo.
          </p>
          <p>
            Para consultas: sorteos@santicaza.com · Tienda:{" "}
            <a
              href="https://santicazaarmeria.com.ar/"
              className="text-accent underline"
              target="_blank"
              rel="noreferrer"
            >
              santicazaarmeria.com.ar
            </a>
          </p>
        </div>
      </section>
      <SiteFooter />
    </main>
  );
}
