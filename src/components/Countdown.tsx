"use client";

import { useEffect, useState } from "react";

type Parts = { days: number; hours: number; minutes: number; seconds: number };

function getParts(target: string): Parts {
  const diff = Math.max(new Date(target).getTime() - Date.now(), 0);
  const total = Math.floor(diff / 1000);
  return {
    days: Math.floor(total / 86400),
    hours: Math.floor((total % 86400) / 3600),
    minutes: Math.floor((total % 3600) / 60),
    seconds: total % 60,
  };
}

export function Countdown({ target }: { target: string }) {
  const [parts, setParts] = useState<Parts>(() => getParts(target));

  useEffect(() => {
    const id = window.setInterval(() => setParts(getParts(target)), 1000);
    return () => window.clearInterval(id);
  }, [target]);

  const items = [
    { label: "DÍAS", value: parts.days },
    { label: "HS", value: parts.hours },
    { label: "MIN", value: parts.minutes },
    { label: "SEG", value: parts.seconds },
  ];

  return (
    <div className="rounded-2xl border border-border bg-bg-panel/90 p-4 backdrop-blur md:p-5">
      <p className="mb-3 text-sm text-text-muted">Falta para el gran día</p>
      <div className="grid grid-cols-4 gap-2 md:gap-3">
        {items.map((item) => (
          <div
            key={item.label}
            className="rounded-xl border border-border bg-bg px-2 py-3 text-center"
          >
            <div
              key={`${item.label}-${item.value}`}
              className="animate-tick font-[family-name:var(--font-display)] text-2xl tracking-wide text-accent md:text-4xl"
            >
              {String(item.value).padStart(2, "0")}
            </div>
            <div className="mt-1 text-[10px] tracking-[0.18em] text-text-muted">
              {item.label}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
