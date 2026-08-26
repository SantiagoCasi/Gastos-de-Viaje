# Requisitos y trazabilidad

Estado y clase/componente que cubre cada requisito. Este archivo se actualiza al final de
cada fase (sección 8 del prompt maestro).

## Requisitos funcionales

| Código | Descripción | Estado | Cubierto por |
|---|---|---|---|
| RF01 | Registrar usuario con email | Cumplido | ASP.NET Core Identity (Fase 1) |
| RF02 | Iniciar sesión | Cumplido | ASP.NET Core Identity (Fase 1) |
| RF03 | Crear sesión de viaje con nombre | Cumplido | `SesionViajeController` (Fase 3) |
| RF04 | Agregar participantes (simulados en v1) | Cumplido | `ParticipanteController` (Fase 3) |
| RF05 | Registrar gasto (monto, fecha, lugar, motivo, pagador, método de pago, estado) | Cumplido | `GastoController` (Fase 3) |
| RF06 | Cargar gastos sin conexión | Cumplido | `offline.js` + IndexedDB + `POST /api/sync/gastos` (Fase 6) |
| RF07 | El organizador carga en nombre de participantes simulados | Cumplido | `GastoController`, `ParticipanteController` (Fase 3) |
| RF08 | Balance parcial sin cerrar la sesión ("break") | Cumplido | `BalanceService.CalcularLiquidacionAsync(tipo: Parcial)` (Fase 4) |
| RF09 | Finalizar sesión con balance final | Cumplido | `BalanceService.CalcularLiquidacionAsync(tipo: Final)` (Fase 4) |
| RF10 | Mostrar el detalle matemático | Cumplido | Vista `Liquidacion/Detalle` (Fase 4) |
| RF11 | Sincronización multi-usuario | Fuera de alcance v1 | Solo `Participante.UsuarioId` nullable preparado |
| RF12 | Resolución de conflictos | Fuera de alcance v1 | — |
| RF13 | Comprobante en PDF | Cumplido | `ComprobanteService` con QuestPDF (Fase 5) |
| RF14 | Compartir por WhatsApp | Cumplido | Enlace `wa.me` + `navigator.share()` (Fase 5) |

## Requisitos no funcionales

| Código | Descripción | Estado | Cubierto por |
|---|---|---|---|
| RNF01 | Disponibilidad offline | Cumplido | PWA: manifest + service worker + banner (Fase 6) |
| RNF02 | Web MVC responsive | Cumplido (revisión final en Fase 7) | Bootstrap + `site.css` mobile-first |
| RNF03 | Persistencia local hasta sincronizar | Cumplido | IndexedDB (Fase 6) |
| RNF04 | Cargar un gasto en pocos pasos | Cumplido | Vista `Gasto/Create` en una sola pantalla (Fase 3) |
| RNF05 | Servicios externos mínimos | Cumplido por diseño | Sin SMTP, sin WhatsApp Business API (secciones 6 y 10) |
| RNF06 | Exactitud matemática | Cumplido | `decimal(18,2)` + `BalanceService` + tests xUnit (Fase 4) |
| RNF07 | Soportar sesiones con muchos participantes | Cumplido | Algoritmo de minimización de transferencias (Fase 4) |

## Casos de uso → clases

| Caso de uso | Clases/controladores principales |
|---|---|
| Registrarse | Identity (`Account/Register`) |
| Iniciar sesión | Identity (`Account/Login`) |
| Crear sesión de viaje | `SesionViajeController` |
| Agregar participante simulado | `ParticipanteController` |
| Registrar gasto | `GastoController` |
| Calcular balance parcial (break) | `BalanceService`, `LiquidacionController` |
| Finalizar sesión | `BalanceService`, `LiquidacionController` |
| Generar comprobante PDF | `ComprobanteService` |
| Compartir comprobante | Vista de detalle de liquidación (`wa.me`, `navigator.share`) |
