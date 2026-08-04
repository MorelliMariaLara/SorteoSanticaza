const items = [
  { title: "Envíos a todo el país", icon: "truck" },
  { title: "Pagos seguros", icon: "shield" },
  { title: "Sorteos exclusivos", icon: "gift" },
  { title: "Atención personalizada", icon: "user" },
] as const;

export function TrustBar() {
  return (
    <section className="mx-auto w-full max-w-6xl px-4 pb-6 md:px-6">
      <div className="grid gap-3 rounded-2xl border border-border bg-bg-panel/70 p-4 sm:grid-cols-2 lg:grid-cols-4">
        {items.map((item) => (
          <div key={item.title} className="flex items-center gap-3 px-2 py-2">
            <span className="flex h-10 w-10 items-center justify-center rounded-full border border-accent/40 text-accent">
              <Icon name={item.icon} />
            </span>
            <span className="text-sm font-medium text-text">{item.title}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function Icon({ name }: { name: (typeof items)[number]["icon"] }) {
  if (name === "truck") {
    return (
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
        <path d="M3 7h11v10H3V7Zm11 3h4l3 3v4h-7V10Z" stroke="currentColor" strokeWidth="1.7" />
        <circle cx="7" cy="18" r="1.5" fill="currentColor" />
        <circle cx="17" cy="18" r="1.5" fill="currentColor" />
      </svg>
    );
  }
  if (name === "shield") {
    return (
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
        <path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3Z" stroke="currentColor" strokeWidth="1.7" />
        <path d="m9 12 2 2 4-4" stroke="currentColor" strokeWidth="1.7" />
      </svg>
    );
  }
  if (name === "gift") {
    return (
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
        <path d="M4 12h16v8H4v-8ZM3 8h18v4H3V8Zm9 0V4m0 4H8a2 2 0 1 1 0-4c2 0 4 2 4 4Zm0 0h4a2 2 0 1 0 0-4c-2 0-4 2-4 4Z" stroke="currentColor" strokeWidth="1.7" />
      </svg>
    );
  }
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="8" r="3.5" stroke="currentColor" strokeWidth="1.7" />
      <path d="M5 19c1.5-3 4-4.5 7-4.5S17.5 16 19 19" stroke="currentColor" strokeWidth="1.7" />
    </svg>
  );
}
