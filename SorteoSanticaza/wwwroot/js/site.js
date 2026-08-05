async function api(path, options = {}) {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    credentials: "same-origin",
    ...options,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(data.error || "Error");
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

(function () {
  const countdown = document.getElementById("countdown");
  if (countdown) {
    const target = new Date(countdown.dataset.target).getTime();
    const units = {
      days: countdown.querySelector('[data-unit="days"]'),
      hours: countdown.querySelector('[data-unit="hours"]'),
      minutes: countdown.querySelector('[data-unit="minutes"]'),
      seconds: countdown.querySelector('[data-unit="seconds"]'),
    };

    function tick() {
      const diff = Math.max(target - Date.now(), 0);
      const total = Math.floor(diff / 1000);
      const days = Math.floor(total / 86400);
      const hours = Math.floor((total % 86400) / 3600);
      const minutes = Math.floor((total % 3600) / 60);
      const seconds = total % 60;
      units.days.textContent = String(days).padStart(2, "0");
      units.hours.textContent = String(hours).padStart(2, "0");
      units.minutes.textContent = String(minutes).padStart(2, "0");
      units.seconds.textContent = String(seconds).padStart(2, "0");
    }

    tick();
    setInterval(tick, 1000);
  }

  const packList = document.getElementById("pack-list");
  const packageInput = document.getElementById("PackageId");
  const summaryLabel = document.getElementById("summary-label");
  const summaryPrice = document.getElementById("summary-price");

  if (packList && packageInput) {
    packList.addEventListener("click", function (event) {
      const btn = event.target.closest(".pack-btn");
      if (!btn) return;
      packList.querySelectorAll(".pack-btn").forEach((el) => el.classList.remove("active"));
      btn.classList.add("active");
      packageInput.value = btn.dataset.packId;
      if (summaryLabel) summaryLabel.textContent = btn.dataset.label;
      if (summaryPrice) summaryPrice.textContent = btn.dataset.price;
    });
  }
})();
