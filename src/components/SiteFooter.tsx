import Link from "next/link";

export function SiteFooter() {
  return (
    <footer className="border-t border-border bg-bg-elevated/80">
      <div className="mx-auto grid w-full max-w-6xl gap-8 px-4 py-10 md:grid-cols-3 md:px-6">
        <div>
          <h3 className="font-[family-name:var(--font-display)] text-xl tracking-wide text-accent">
            Contacto
          </h3>
          <p className="mt-2 text-sm text-text-muted">sorteos@santicaza.com</p>
          <a
            href="https://santicazaarmeria.com.ar/"
            target="_blank"
            rel="noreferrer"
            className="mt-3 inline-block text-sm text-accent underline underline-offset-2"
          >
            Tienda SANTICAZA
          </a>
        </div>
        <div>
          <h3 className="font-[family-name:var(--font-display)] text-xl tracking-wide text-accent">
            Nuestras redes
          </h3>
          <div className="mt-2 flex flex-col gap-1 text-sm text-text-muted">
            <span>Instagram · @santicaza</span>
            <span>Facebook · SANTICAZA</span>
          </div>
        </div>
        <div>
          <h3 className="font-[family-name:var(--font-display)] text-xl tracking-wide text-accent">
            SANTICAZA
          </h3>
          <p className="mt-2 text-sm leading-relaxed text-text-muted">
            Sorteos exclusivos de caza, óptica y equipamiento outdoor. Con cada
            compra participás en nuestros sorteos en vivo.
          </p>
          <Link
            href="/terminos"
            className="mt-3 inline-block text-sm text-accent underline underline-offset-2"
          >
            Términos y Condiciones del Sorteo
          </Link>
        </div>
      </div>
      <div className="border-t border-border px-4 py-4 text-center text-xs text-text-muted md:px-6">
        <p>+18 SOLO MAYORES DE 18 AÑOS</p>
        <p className="mt-1">
          © {new Date().getFullYear()} SANTICAZA. Todos los derechos reservados.{" "}
          <Link href="/admin" className="text-accent/80 hover:text-accent">
            Acceso Admin
          </Link>
        </p>
      </div>
    </footer>
  );
}
