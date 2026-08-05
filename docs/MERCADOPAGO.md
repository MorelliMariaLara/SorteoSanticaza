# Mercado Pago · Checkout Bricks (Sorteo SANTICAZA)

Misma integración que **VentaCursos / Nexa**: Wallet Brick + Preference + polling/webhook.

## 1. Credenciales

1. [Mercado Pago Developers](https://www.mercadopago.com.ar/developers) → **Tus integraciones** → tu app.
2. Copiá el par de **Pruebas**:
   - `MP_PUBLIC_KEY` (TEST-… o APP_USR-…)
   - `MP_ACCESS_TOKEN` (TEST-… o APP_USR-…)

## 2. Configurar el proyecto

Las claves de **prueba** ya están en:

- `SorteoSanticaza/appsettings.json`
- `SorteoSanticaza/appsettings.Development.json`
- `.env.example` (referencia)

No hace falta configurar nada para probar en local: F5 y listo.

En consola deberías ver:

```text
MP configurado=True PK=TEST-1733… TK=TEST-2319…
```

**Producción:** sacar las claves de `appsettings` y cargarlas por variables de entorno / secretos del hosting.

## 3. Flujo de pago

1. El comprador elige pack y completa datos → se crea orden `pending`.
2. Va a `/Checkout?order={publicId}`.
3. Backend crea **preference** (`POST /api/payments/preference`).
4. Front monta **Wallet Brick** (`mp.bricks().create("wallet", …)`).
5. El usuario paga en Mercado Pago.
6. La pantalla hace **polling** a `GET /api/payments/order/{id}` hasta `approved`.
7. Recién ahí se asignan los números y redirige a `/Pago/Exito`.

Sin credenciales válidas aparece **Simular pago** (demo local).

## 4. Webhook (producción)

```text
https://TU-DOMINIO/api/webhooks/mercadopago
```

```env
MP_WEBHOOK_URL=https://TU-DOMINIO/api/webhooks/mercadopago
APP_URL=https://TU-DOMINIO
```

En `localhost` MP no notifica; el front usa polling.

## 5. Endpoints

| Método | Ruta | Uso |
| --- | --- | --- |
| GET | `/api/payments/config` | Public Key + diagnóstico |
| POST | `/api/payments/preference` | Preference + Wallet Brick |
| POST | `/api/payments/process` | Simulación local / Brick payment |
| GET | `/api/payments/order/{id}` | Polling hasta acreditar |
| POST/GET | `/api/webhooks/mercadopago` | Notificaciones MP |

## 6. Errores frecuentes

- `TEST-APP_USR-…` → no antepongas `TEST-` a una clave `APP_USR`.
- Mezclar TEST- con APP_USR- → usá el mismo par.
- Access Token solo en backend (`Authorization: Bearer …`), nunca en el front.
