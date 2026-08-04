# Sorteo SANTICAZA

Plataforma web + API para vender chances de sorteos de **SANTICAZA**, inspirada en el flujo de compra de [Pesca Urbana /sorteo](https://pescaurbana.com/sorteo/).

## Qué incluye

- Landing de sorteo con hero, countdown, packs y checkout
- API REST para raffle, órdenes, pagos (modo demo), mis números, ganadores y admin
- Asignación aleatoria de números al confirmar pago
- Páginas: inicio, Mis Números, Ganadores, Términos, Admin, pago exitoso
- Persistencia SQLite (`data/santicaza.db`)

## Stack

- Next.js (App Router) + TypeScript + Tailwind
- better-sqlite3
- Zod para validación

## Desarrollo

```bash
npm install
cp .env.example .env.local
npm run dev
```

Abrí [http://localhost:3000](http://localhost:3000).

### Variables

| Variable | Descripción |
|---|---|
| `ADMIN_PASSWORD` | Password del panel `/admin` |
| `NEXT_PUBLIC_APP_URL` | URL base de la app |
| `PAYMENT_MODE` | `demo` (default) confirma pago y asigna números |
| `MERCADOPAGO_ACCESS_TOKEN` | Token real de MP (opcional; listo para Preference API) |

## API principal

- `GET /api/raffle` — sorteo activo + packs
- `POST /api/orders` — crea orden pendiente
- `POST /api/payments/checkout` — checkout (demo o MP)
- `GET /api/my-numbers?q=` — consulta por email/DNI
- `GET /api/winners` — listado público
- `POST /api/admin/login` — cookie de admin
- `GET /api/admin/orders` — órdenes (admin)
- `POST /api/admin/winners` — publicar ganador (admin)

## Flujo de compra

1. El usuario elige un pack y completa datos
2. `POST /api/orders` crea la orden
3. `POST /api/payments/checkout` confirma el pago (demo) y asigna números
4. Redirección a `/pago/exito?order=<uuid>`

## Producción

```bash
npm run build
npm start
```
