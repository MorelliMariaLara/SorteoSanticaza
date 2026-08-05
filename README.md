# Sorteo SANTICAZA

Solución web **ASP.NET Core MVC (.NET 5)** para vender chances de sorteos de SANTICAZA.

## Abrir y ejecutar en Visual Studio

1. Asegurate de tener **SQL Server Express** corriendo (`LARA-NB\SQLEXPRESS02`) y, si querés, ejecutá `database/01_CreateDatabaseAndTables.sql` en SSMS (la app también puede crear la BD/tablas al arrancar).
2. Abrí el archivo de solución:
   - `SorteoSanticaza.sln`
3. Marcá `SorteoSanticaza` como **proyecto de inicio** (ya es el único proyecto).
4. Presioná **F5** o **Ctrl+F5**.
5. Se abre en `http://localhost:5165`.

También podés ejecutar desde terminal:

```bash
dotnet restore
dotnet run --project SorteoSanticaza
```

## Base de datos (SQL Server)

- Servidor: `LARA-NB\SQLEXPRESS02`
- Base: `SorteosSantiCaza`
- Connection string en `SorteoSanticaza/appsettings.json` → `ConnectionStrings:SorteosSantiCaza`
- Scripts: [`database/`](database/README.md)

Para cambiar de servidor/BD, editá el connection string o seteá `CONNECTION_STRING` en `.env`.

## Qué incluye

- Landing de sorteo (hero, countdown, packs, checkout)
- **Mercado Pago Checkout Bricks** (Wallet Brick), igual que VentaCursos
- API REST en `/api/*`
- Mis Números, Ganadores, Términos, Admin, pago exitoso
- SQL Server (`SorteosSantiCaza`)
- Simulación local si no hay credenciales MP

## Mercado Pago

Las **credenciales de prueba** ya están en `SorteoSanticaza/appsettings.json`.
Abrís la solución, F5, y el Wallet Brick debería cargar.

Cuando pasemos a producción: sacar `MP_PUBLIC_KEY` / `MP_ACCESS_TOKEN` de `appsettings` y usar secretos del hosting (o `.env` fuera de git).

Guía: [`docs/MERCADOPAGO.md`](docs/MERCADOPAGO.md)

## Admin (privado)

- URL: `/Admin` (no aparece en el menú público)
- Password: `santicaza-admin` (configurable en `appsettings.json` → `AdminPassword`)
- Pestañas:
  - **Sorteos**: crear/editar sorteos, chances, precios, imagen del premio, activar/cerrar
  - **Pedidos**: listado de compras y publicación de ganadores
- Imágenes subidas: `wwwroot/images/uploads/`

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
