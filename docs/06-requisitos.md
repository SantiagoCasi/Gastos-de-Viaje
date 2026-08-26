# Requisitos y trazabilidad

Estado y clase/componente que cubre cada requisito. Este archivo se actualiza al final de
cada fase (sección 8 del prompt maestro).

## Requisitos funcionales

| Código | Descripción | Estado | Cubierto por |
|---|---|---|---|
| RF01 | Registrar usuario con email | Cumplido | ASP.NET Core Identity (Fase 1) |
| RF02 | Iniciar sesión | Cumplido | ASP.NET Core Identity (Fase 1) |
| RF03 | Crear sesión de viaje con nombre | Pendiente | `SesionViajeController` (Fase 3) |
| RF04 | Agregar participantes (simulados en v1) | Pendiente | `ParticipanteController` (Fase 3) |
| RF05 | Registrar gasto (monto, fecha, lugar, motivo, pagador, método de pago, estado) | Pendiente | `GastoController` (Fase 3) |
| RF06 | Cargar gastos sin conexión | Pendiente | `offline.js` + IndexedDB (Fase 6) |
| RF07 | El organizador carga en nombre de participantes simulados | Pendiente | `GastoController`, `ParticipanteController` (Fase 3) |
| RF08 | Balance parcial sin cerrar la sesión ("break") | Pendiente | `BalanceService.CalcularLiquidacion(tipo: Parcial)` (Fase 4) |
| RF09 | Finalizar sesión con balance final | Pendiente | `BalanceService.CalcularLiquidacion(tipo: Final)` (Fase 4) |
| RF10 | Mostrar el detalle matemático | Pendiente | Vista `Liquidacion/Detalle` (Fase 4) |
| RF11 | Sincronización multi-usuario | Fuera de alcance v1 | Solo `Participante.UsuarioId` nullable preparado |
| RF12 | Resolución de conflictos | Fuera de alcance v1 | — |
| RF13 | Comprobante en PDF | Pendiente | `ComprobanteService` con QuestPDF (Fase 5) |
| RF14 | Compartir por WhatsApp | Pendiente | Enlace `wa.me` + `navigator.share()` (Fase 5) |

## Requisitos no funcionales

| Código | Descripción | Estado | Cubierto por |
|---|---|---|---|
| RNF01 | Disponibilidad offline | Pendiente | PWA: manifest + service worker + banner (Fase 6) |
| RNF02 | Web MVC responsive | Pendiente | Bootstrap + `site.css` mobile-first (Fase 7) |
| RNF03 | Persistencia local hasta sincronizar | Pendiente | IndexedDB (Fase 6) |
| RNF04 | Cargar un gasto en pocos pasos | Pendiente | Vista `Gasto/Create` en una sola pantalla (Fase 3) |
| RNF05 | Servicios externos mínimos | Cumplido por diseño | Sin SMTP, sin WhatsApp Business API (secciones 6 y 10) |
| RNF06 | Exactitud matemática | Pendiente | `decimal(18,2)` + `BalanceService` + tests xUnit (Fase 4) |
| RNF07 | Soportar sesiones con muchos participantes | Pendiente | Algoritmo de minimización de transferencias (Fase 4) |

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
