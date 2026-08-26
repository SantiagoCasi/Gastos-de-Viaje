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
- SQL Server Express o Developer, con una instancia local accesible (ver "Cómo levantar
  la base de datos" para ajustar la cadena de conexión a tu instancia).
- (Opcional) SQL Server Management Studio para inspeccionar la base.

## Cómo levantar la base de datos

1. Verificar/ajustar la cadena de conexión en `appsettings.json`. En esta máquina de
   desarrollo el servicio de SQL Server corre en la **instancia por defecto** (no
   `SQLEXPRESS`), por eso quedó así:
   ```
   Server=localhost;Database=GastosDeViaje;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
   ```
   Si en tu máquina SQL Server corre como instancia con nombre, usá
   `Server=localhost\NOMBRE_INSTANCIA;...`.
2. Aplicar las migraciones (desde la carpeta del proyecto, o con la Package Manager Console
   de Visual Studio usando `Update-Database`):
   ```
   dotnet ef database update
   ```
3. Al correr la app en modo `Development`, `SeedData` carga automáticamente una sesión de
   ejemplo ("Viaje a Bariloche") con 4 participantes y 6 gastos, y crea un usuario
   organizador de demostración si no existe (`organizador@demo.com` / `Demo123$`) para
   poder loguearse y ver los datos sin tener que registrarse a mano. La carga es
   idempotente: si ya hay una sesión de viaje, no hace nada.

## Cómo correr el proyecto

```
dotnet restore
dotnet build
dotnet run
```

O bien abrir `GastosDeViaje.slnx` en Visual Studio 2026 y ejecutar con F5.

## Cómo correr los tests

Los tests de `GastosDeViaje.Tests` (algoritmo de balance, Fase 4) corren contra una base
SQL Server real llamada `GastosDeViajeTests`, en la misma instancia local
(`Server=localhost`): no hay ningún paquete de mocking/in-memory en la lista de NuGets
autorizados. La base de tests se crea y se borra sola en cada corrida.

```
dotnet test GastosDeViaje.Tests
```

## Estado del proyecto

En construcción, siguiendo las fases del prompt maestro (sección 8). Progreso:

- [x] Fase 0 — Documentación y diagramas (`docs/`).
- [x] Fase 1 — Solución y esqueleto (Identity Individual Accounts, estructura de carpetas).
- [x] Fase 2 — Modelo de datos.
- [x] Fase 3 — CRUD scaffoldeado.
- [x] Fase 4 — Motor de cálculo de balance.
- [x] Fase 5 — Comprobantes PDF.
- [x] Fase 6 — PWA / offline.
- [x] Fase 7 — Cierre.

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

## Desviaciones respecto de la especificación (transparencia, sección 0.6)

- **Estructura de carpetas.** El esqueleto que generó Visual Studio (Fase 1) dejó el
  proyecto web en la **raíz** del repositorio en vez de en una subcarpeta `GastosDeViaje/`
  como muestra el árbol de la sección 7. No se reestructuró para no arriesgar romper
  referencias ya en uso; `GastosDeViaje.Tests/` quedó como hermano dentro de esa misma
  carpeta, con un `<Compile Remove>` en el `.csproj` principal para que sus archivos no
  se compilen dos veces (el SDK de .NET globa `**/*.cs` recursivamente por defecto).
- **Un paquete NuGet fuera de la lista cerrada de la sección 1:**
  `Microsoft.EntityFrameworkCore.Design`. Es necesario para que `Add-Migration` /
  `Update-Database` (el paquete `Tools`, que sí está autorizado) funcionen: sin él,
  Visual Studio y la CLI de EF Core dan el error "this package is required for the
  Package Manager Console tools to work". Está marcado `PrivateAssets="all"`: no se
  publica en runtime, solo se usa en tiempo de diseño/build.
- Durante el scaffolding de la Fase 3 se agregó **temporalmente**
  `Microsoft.VisualStudio.Web.CodeGeneration.Design` —el motor real detrás de "Agregar
  > Controlador > MVC con vistas, usando Entity Framework" de Visual Studio, reproducido
  acá por CLI (`dotnet-aspnet-codegenerator`) porque no había una instancia interactiva
  de Visual Studio disponible— y se quitó del `.csproj` apenas terminó de generar los
  controllers y vistas. No queda en el proyecto final.
- La cadena de conexión quedó apuntando a `Server=localhost` (instancia por defecto)
  en vez de `localhost\SQLEXPRESS`, porque en esta máquina de desarrollo SQL Server
  corre como instancia por defecto, no como instancia con nombre `SQLEXPRESS`. El
  prompt maestro ya preveía "ajustar la instancia si hace falta".

## PWA y funcionamiento offline

La app se puede "instalar" desde el navegador del celular (Agregar a pantalla de
inicio) gracias a `wwwroot/manifest.json` y al service worker `wwwroot/js/sw.js`
(cache-first para el shell estático, network-first para el resto). Los gastos
cargados sin conexión se guardan en IndexedDB (`wwwroot/js/offline.js`) y se
sincronizan solos contra `POST /api/sync/gastos` apenas vuelve la señal; mientras
haya gastos sin sincronizar, un banner permanente lo indica en cualquier pantalla.
El cálculo de balance **no** funciona offline (ver sección 6 del prompt maestro):
para eso hace falta conexión.

> Nota: los service workers solo se registran en `https://` o en `localhost`. En
> producción, la app tiene que servirse por HTTPS para que la PWA funcione.

## Empaquetado futuro como app nativa (no implementado aún)

Una vez construida la PWA (Fase 6), se puede empaquetar como app instalable de Google Play
mediante **TWA (Trusted Web Activity)**, usando PWABuilder o Bubblewrap, sin reescribir código.
Esto queda para una etapa posterior, fuera del alcance de la v1.
