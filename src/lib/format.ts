export function formatARS(cents: number): string {
  const value = cents / 100;
  return new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatDateTimeAR(iso: string): string {
  const date = new Date(iso);
  return new Intl.DateTimeFormat("es-AR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone: "America/Argentina/Buenos_Aires",
  })
    .format(date)
    .replace(",", " a las")
    .concat(" hs");
}

export function padTicket(n: number, width = 5): string {
  return String(n).padStart(width, "0");
}
