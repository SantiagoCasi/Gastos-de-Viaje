# Gastos de Viaje

Aplicación web ASP.NET Core MVC para que un organizador cargue los gastos compartidos de
un viaje, calcule balances (parciales o finales) y genere/comparta comprobantes. Ver la
especificación completa en [`PROMPT_MAESTRO_GastosDeViaje.md`](PROMPT_MAESTRO_GastosDeViaje.md)
y los diagramas en [`docs/`](docs/).

## Stack

| Ítem | Versión / detalle |
|---|---|
| .NET | **10.0** (LTS) |
| Framework web | ASP.NET Core MVC (Razor, server-side rendering) |
| ORM | Entity Framework Core, Code First |
| Base de datos | SQL Server (Express/Developer) |
| Autenticación | ASP.NET Core Identity (Individual Accounts) |
| PDF | QuestPDF |
| Tests | xUnit |

## Requisitos previos

- .NET SDK 10.0 o superior.
- SQL Server Express o Developer, con una instancia accesible en `localhost\SQLEXPRESS`
  (o ajustar la cadena de conexión, ver abajo).
- (Opcional) SQL Server Management Studio para inspeccionar la base.

## Cómo levantar la base de datos

1. Verificar/ajustar la cadena de conexión en `appsettings.json`:
   ```
   Server=localhost\SQLEXPRESS;Database=GastosDeViaje;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
   ```
2. Aplicar las migraciones (desde la carpeta del proyecto, o con la Package Manager Console
   de Visual Studio usando `Update-Database`):
   ```
   dotnet ef database update
   ```
   *(Las migraciones y el seed de datos de ejemplo se agregan en la Fase 2.)*

## Cómo correr el proyecto

```
dotnet restore
dotnet build
dotnet run
```

O bien abrir `GastosDeViaje.slnx` en Visual Studio 2026 y ejecutar con F5.

## Estado del proyecto

En construcción, siguiendo las fases del prompt maestro (sección 8). Progreso:

- [x] Fase 0 — Documentación y diagramas (`docs/`).
- [x] Fase 1 — Solución y esqueleto (Identity Individual Accounts, estructura de carpetas).
- [ ] Fase 2 — Modelo de datos.
- [ ] Fase 3 — CRUD scaffoldeado.
- [ ] Fase 4 — Motor de cálculo de balance.
- [ ] Fase 5 — Comprobantes PDF.
- [ ] Fase 6 — PWA / offline.
- [ ] Fase 7 — Cierre.

## Decisiones de arquitectura

- **Un único proyecto MVC**, sin Clean Architecture ni capas en proyectos separados
  (ver sección 7 del prompt maestro): la complejidad no se justifica para el tamaño del sistema.
- **Identity Individual Accounts** provee registro, login y hash de contraseñas de fábrica;
  no se agrega ninguna entidad de usuario propia salvo `NombreCompleto`.
- **Sin confirmación de cuenta por email** (`RequireConfirmedAccount = false`): no hay
  servicio de SMTP configurado, es un servicio externo prescindible para el alcance actual.
- El cálculo de balance vive en `BalanceService` y **no corre offline** (ver sección 6):
  offline se usa solo para cargar y consultar gastos.
- El envío por WhatsApp (RF14) se resuelve con el enlace `wa.me` y `navigator.share()`,
  sin la API de Meta (sección 6).

## Empaquetado futuro como app nativa (no implementado aún)

Una vez construida la PWA (Fase 6), se puede empaquetar como app instalable de Google Play
mediante **TWA (Trusted Web Activity)**, usando PWABuilder o Bubblewrap, sin reescribir código.
Esto queda para una etapa posterior, fuera del alcance de la v1.
