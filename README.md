# Sorteo SANTICAZA

Solución web **ASP.NET Core MVC (.NET 5)** para vender chances de sorteos de SANTICAZA.

## Abrir y ejecutar en Visual Studio

1. Abrí el archivo de solución:
   - `SorteoSanticaza.sln`
2. Marcá `SorteoSanticaza` como **proyecto de inicio** (ya es el único proyecto).
3. Presioná **F5** o **Ctrl+F5**.
4. Se abre en `http://localhost:5165`.

También podés ejecutar desde terminal:

```bash
dotnet restore
dotnet run --project SorteoSanticaza
```

## Qué incluye

- Landing de sorteo (hero, countdown, packs, checkout)
- **Mercado Pago Checkout Bricks** (Wallet Brick), igual que VentaCursos
- API REST en `/api/*`
- Mis Números, Ganadores, Términos, Admin, pago exitoso
- SQLite local en `SorteoSanticaza/App_Data/santicaza.db`
- Simulación local si no hay credenciales MP

## Mercado Pago

Las **credenciales de prueba** ya están en `SorteoSanticaza/appsettings.json`.
Abrís la solución, F5, y el Wallet Brick debería cargar.

Cuando pasemos a producción: sacar `MP_PUBLIC_KEY` / `MP_ACCESS_TOKEN` de `appsettings` y usar secretos del hosting (o `.env` fuera de git).

Guía: [`docs/MERCADOPAGO.md`](docs/MERCADOPAGO.md)

## Admin

- URL: `/Admin`
- Password: `santicaza-admin` (configurable en `appsettings.json` → `AdminPassword`)

## API

- `GET /api/raffle`
- `POST /api/orders`
- `POST /api/payments/checkout`
- `GET /api/my-numbers?q=`
- `GET /api/winners`
- `POST /api/admin/login`
- `GET /api/admin/orders`

## Requisitos

- Visual Studio 2019/2022 con workload **ASP.NET and web development**
- .NET 5 SDK (compatible con `C:\Program Files\dotnet\sdk\5.0.x`)

No usa npm/Node.js.
