# Base SQL Server · SorteosSantiCaza

Servidor: `LARA-NB\SQLEXPRESS02`  
Base: `SorteosSantiCaza`

## Pasos

1. Abrí **SQL Server Management Studio** contra `LARA-NB\SQLEXPRESS02`.
2. Ejecutá `01_CreateDatabaseAndTables.sql`.
3. (Opcional) Ejecutá `02_Seed.sql` — si no, la app siembra sola al arrancar.

## Connection string

Ya está en `SorteoSanticaza/appsettings.json`:

```text
Server=LARA-NB\SQLEXPRESS02;Database=SorteosSantiCaza;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```
