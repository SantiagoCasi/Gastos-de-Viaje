# PROMPT MAESTRO — Proyecto "Gastos de Viaje"

> Copiar y pegar íntegro en el agente de IA. Es la única especificación válida del proyecto.

---

## 0. ROL Y REGLAS PERMANENTES

Actuás como **desarrollador de software senior .NET** a cargo de este proyecto de principio a fin. No sos un generador de código suelto: sos responsable de que el proyecto quede prolijo, coherente, documentado y defendible ante otro desarrollador que lo abra por primera vez.

Reglas que rigen **toda** la conversación, en cada respuesta:

1. **No agregues nada que no esté pedido.** Ante la duda entre "más completo" y "más simple", elegí más simple. Cada archivo, clase, paquete NuGet o carpeta que crees tiene que ser imprescindible para un requisito listado abajo.
2. **Trabajá por fases** (sección 8). Al terminar una fase: pará, listá los archivos que creaste o modificaste, informá decisiones tomadas y esperá mi confirmación. No arranques la fase siguiente por tu cuenta.
3. **Si algo de esta especificación es ambiguo o contradictorio, preguntá antes de codear.** No inventes ni asumas en silencio.
4. **No instales ningún paquete NuGet fuera de los autorizados** (sección 1) sin pedirme permiso y justificarlo en una línea.
5. Todo el código, comentarios, nombres de variables, vistas y documentación van **en español** (excepto lo que impone el framework: `Program.cs`, `Index`, `Create`, etc.).
6. Si detectás un error, un riesgo o una mala práctica en lo que te pido, **decímelo** en vez de implementarlo callado.

---

## 1. STACK Y RESTRICCIONES TÉCNICAS

| Ítem | Definición |
|---|---|
| Framework | ASP.NET Core **MVC** (server-side rendering, Razor). **No** Blazor, **no** SPA, **no** Razor Pages para el negocio. |
| Versión .NET | La versión **LTS** instalada (mínimo .NET 8). Dejala anotada en el README. |
| ORM | **Entity Framework Core**, enfoque **Code First** con Migrations. |
| Base de datos | **SQL Server** (Express o Developer), administrada desde SQL Server Management Studio. |
| IDE | Visual Studio 2026. |
| UI | Bootstrap (el que trae la plantilla MVC, servido localmente) + `site.css` propio. Sin frameworks CSS adicionales. |
| Autenticación | **ASP.NET Core Identity** (opción *Individual Accounts* de la plantilla). |
| PDF | **QuestPDF** (único NuGet extra autorizado). |
| Tests | **xUnit**, un solo proyecto, exclusivamente para el algoritmo de balance. |

**Paquetes autorizados (lista cerrada):** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.UI`, `QuestPDF`, `xUnit` (+ runner). **Nada más.**

**Cadena de conexión** en `appsettings.json` (ajustar la instancia si hace falta):

```
Server=localhost\SQLEXPRESS;Database=GastosDeViaje;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

---

## 2. ALCANCE

### Versión 1 (lo que vamos a construir ahora)
- **Un solo usuario real: el organizador.** Se registra, inicia sesión y administra todo.
- Los demás participantes son **participantes simulados**: solo tienen nombre, no tienen cuenta ni acceso.
- El organizador carga los gastos de todos, calcula balances, cierra la sesión y genera/comparte comprobantes.
- Funcionamiento **offline para la carga y la consulta** de gastos, mediante PWA (ver sección 6).

### Versión 2 (NO implementar ahora — solo dejar el modelo preparado)
- Varios usuarios reales cargando desde distintos dispositivos.
- Links de invitación a la sesión.
- Sincronización multi-dispositivo con resolución de conflictos (RF11, RF12).

Para eso, y **solo** para eso, la entidad `Participante` lleva un `UsuarioId` **nullable**. No construyas nada más de la v2.

### Explícitamente fuera de alcance (no lo implementes ni lo sugieras)
Multi-moneda o conversión de divisas · división desigual o por porcentajes de un gasto · roles y permisos · 2FA · logins externos (Google/Facebook) · confirmación de cuenta por email · WhatsApp Business Cloud API · notificaciones push · Docker · dashboards o estadísticas.

---

## 3. MODELO DE DATOS

Este es el modelo **exacto** a implementar. Deriva del diagrama de clases del proyecto, con **cuatro correcciones técnicas** que están justificadas al final de la sección. Respetalo al pie de la letra.

### Entidades

**`Usuario`** — usar `ApplicationUser : IdentityUser`. Identity ya provee `Id`, `Email` y `PasswordHash`. No agregar propiedades salvo `NombreCompleto` (string, requerido, 100).

**`SesionViaje`**
- `Id` (int, PK)
- `Nombre` (string, requerido, 100)
- `Estado` (enum `EstadoSesion`: `Abierta`, `Cerrada`) — por defecto `Abierta`
- `FechaCreacion` (DateTime, requerido)
- `FechaCierre` (DateTime?, nullable)
- `Moneda` (string, 3, default `"ARS"`) — **solo etiqueta para mostrar en pantalla y en el PDF; no se hace ninguna conversión**
- `OrganizadorId` (string, FK → `ApplicationUser`, requerido)
- Navegación: `ICollection<Participante>`, `ICollection<Gasto>`, `ICollection<Liquidacion>`

**`Participante`**
- `Id` (int, PK)
- `Nombre` (string, requerido, 80)
- `EsSimulado` (bool, default `true`)
- `SesionViajeId` (int, FK, requerido)
- `UsuarioId` (string?, FK → `ApplicationUser`, **nullable** — reservado para v2)

**`Gasto`**
- `Id` (int, PK)
- `SesionViajeId` (int, FK, requerido)
- `ParticipanteId` (int, FK, requerido) — **quién pagó el gasto**
- `Monto` (**`decimal`**, requerido, mayor a 0, columna `decimal(18,2)`)
- `Fecha` (DateTime, requerido)
- `Lugar` (string, requerido, 120)
- `Motivo` (string, requerido, 200)
- `MetodoPago` (enum `MetodoPago`: `Efectivo`, `Debito`, `Credito`, `Transferencia`, `Otro`)
- `LiquidacionId` (int?, FK, **nullable**) — `null` = gasto pendiente; con valor = gasto ya saldado
- `[NotMapped] public bool Saldado => LiquidacionId != null;`

**`Liquidacion`** — representa un corte de cuentas (un "break" o el cierre final)
- `Id` (int, PK)
- `SesionViajeId` (int, FK, requerido)
- `Fecha` (DateTime, requerido)
- `Tipo` (enum `TipoLiquidacion`: `Parcial`, `Final`)
- `TotalGastado` (decimal(18,2))
- `CantidadParticipantes` (int)
- `CuotaIdeal` (decimal(18,2))
- Navegación: `ICollection<Gasto>`, `ICollection<MovimientoLiquidacion>`

**`MovimientoLiquidacion`** — una transferencia concreta "X le paga $N a Y"
- `Id` (int, PK)
- `LiquidacionId` (int, FK, requerido)
- `DeudorId` (int, FK → `Participante`, requerido)
- `AcreedorId` (int, FK → `Participante`, requerido)
- `Monto` (decimal(18,2), mayor a 0)

### Configuración de EF Core (Fluent API en `AppDbContext.OnModelCreating`)
- Precisión explícita `HasPrecision(18, 2)` en **todos** los campos `decimal`.
- `DeleteBehavior.Restrict` en las FK de `MovimientoLiquidacion` hacia `Participante` (evita el error de múltiples caminos de cascada en SQL Server).
- `DeleteBehavior.Cascade` de `SesionViaje` hacia `Participante`, `Gasto` y `Liquidacion`.
- Índice sobre `Gasto.SesionViajeId` y sobre `Gasto.LiquidacionId`.

### Las 4 correcciones respecto del diagrama original (leelas, son parte del pedido)
1. **`Monto: float` → `decimal(18,2)`.** `float`/`double` son binarios y arrastran error de redondeo. Con dinero eso rompe el RNF06 (exactitud matemática). En .NET, dinero es siempre `decimal`.
2. **`Password: string` → `PasswordHash` de Identity.** Guardar contraseñas en texto plano es una falla de seguridad grave e injustificable. Identity ya resuelve hash, salt y bloqueo por intentos fallidos.
3. **Se agregan `Liquidacion` y `MovimientoLiquidacion`.** Sin ellas el "break" del RF08 es imposible de implementar: no habría forma de saber qué gastos ya se saldaron, ni de reconstruir el detalle matemático del RF10, ni de emitir el comprobante del RF13 de ese corte. Es la única incorporación estructural y es imprescindible.
4. **`Balance` y `Comprobante` NO son tablas.** En el diagrama solo tienen métodos (`calcularBalance()`, `generarPdf()`), no datos propios: son **servicios**, no entidades. Se implementan como `BalanceService` y `ComprobanteService`. Persistirlas sería duplicar información que ya vive en `Liquidacion`. `Gasto.Estado` (bool) se elimina por el mismo motivo: se deriva de `LiquidacionId`.

---

## 4. REGLAS DE NEGOCIO Y ALGORITMO DE BALANCE

Esta es la parte crítica del sistema (RNF06). Implementala **exactamente** así, en `BalanceService`, con `decimal` en todos los pasos.

### Regla de división
Todo gasto pendiente de la sesión se divide **en partes iguales entre todos los participantes de esa sesión**, sin importar quién lo pagó ni quién lo consumió. No hay divisiones parciales ni por porcentaje en la v1.

### Algoritmo `CalcularLiquidacion(sesionId, tipo)`

1. Traer los gastos de la sesión con `LiquidacionId == null` (los pendientes). Si no hay ninguno, cortar con un mensaje claro al usuario.
2. `Total = Σ Monto` de esos gastos.
3. `N = ` cantidad de participantes de la sesión. Si `N < 2`, cortar con mensaje.
4. `CuotaIdeal = Math.Round(Total / N, 2, MidpointRounding.AwayFromZero)`.
5. **Ajuste de centavos:** `Residuo = Total - (CuotaIdeal * N)`. Repartir ese residuo de a `0.01` entre los participantes ordenados por `Id` ascendente, hasta agotarlo. Así se garantiza que `Σ cuotas == Total` exacto.
6. Para cada participante: `Pagado_i = Σ` montos de los gastos pendientes donde él figura como pagador. `Saldo_i = Pagado_i - Cuota_i`. Saldo positivo = **acreedor** (le deben); negativo = **deudor** (debe).
7. **Verificación obligatoria:** `Σ Saldo_i` debe dar exactamente `0`. Si no da 0, lanzar excepción y **no** persistir nada.
8. **Minimización de transferencias:** ordenar acreedores por saldo descendente y deudores por saldo ascendente. Mientras queden deudores: tomar el mayor deudor y el mayor acreedor, generar un `MovimientoLiquidacion` por `Math.Min(|saldoDeudor|, saldoAcreedor)`, restar de ambos y descartar al que quede en cero. Esto produce la menor cantidad posible de pagos.
9. Persistir `Liquidacion` + sus `MovimientoLiquidacion` + asignar el `LiquidacionId` a todos los gastos incluidos. **Todo dentro de una única transacción de EF Core.**
10. Según el tipo:
    - **`Parcial` (break, RF08):** la sesión queda `Abierta`. Los gastos quedan saldados, así que los balances vuelven a cero y se pueden seguir cargando gastos nuevos. El viaje continúa.
    - **`Final` (RF09):** la sesión pasa a `Cerrada` y se setea `FechaCierre`. A partir de ahí **no se admiten nuevos gastos** en esa sesión (validar en el controller y deshabilitar el botón en la vista).

### Detalle matemático (RF10)
La vista de resultado no muestra solo el resultado final. Muestra, en una tabla: total gastado, cantidad de participantes, cuota ideal, y por cada participante `Pagado / Cuota / Saldo`; y debajo, la lista de transferencias "X le paga $N a Y".

---

## 5. REQUISITOS (referencia para trazabilidad)

**Funcionales.** RF01 registrar usuario con email · RF02 iniciar sesión · RF03 crear sesión de viaje con nombre · RF04 agregar participantes (simulados en v1) · RF05 registrar gasto con monto, fecha, lugar, motivo, pagador, método de pago y estado · RF06 cargar gastos sin conexión · RF07 el organizador carga en nombre de participantes simulados · RF08 balance parcial sin cerrar la sesión ("break") · RF09 finalizar sesión con balance final · RF10 mostrar el detalle matemático · RF11 sincronización multi-usuario *(v2)* · RF12 resolución de conflictos *(v2)* · RF13 comprobante en PDF · RF14 compartir por WhatsApp.

**No funcionales.** RNF01 disponibilidad offline · RNF02 web MVC responsive · RNF03 persistencia local hasta sincronizar · RNF04 cargar un gasto en pocos pasos (uso en movimiento) · RNF05 servicios externos mínimos · RNF06 exactitud matemática · RNF07 soportar sesiones con muchos participantes.

**Casos de uso.** Registrarse · Iniciar sesión · Crear sesión de viaje · Agregar participante simulado · Registrar gasto · Calcular balance parcial (break) · Finalizar sesión · Generar comprobante PDF · Compartir comprobante.

Cada clase que implementes debe llevar en su comentario XML el/los códigos de requisito que cubre. Ej: `/// Cubre RF08, RF09, RF10, RNF06.`

---

## 6. DECISIONES TÉCNICAS YA TOMADAS (no las cambies ni propongas alternativas)

**Offline + icono en el celular → PWA.** Una app MVC clásica renderiza en el servidor: sin conexión no hay página. La solución, sin cambiar de arquitectura, es convertir el sitio en Progressive Web App:
- `wwwroot/manifest.json` con nombre, iconos (192px y 512px), `display: standalone` y colores. Esto es lo que permite "Agregar a pantalla de inicio" y deja el icono en el escritorio del celular, exactamente como si fuera una app.
- `wwwroot/js/sw.js` (service worker): estrategia *cache-first* para el shell, CSS, JS e iconos; *network-first* para los datos.
- `IndexedDB` para la cola de gastos creados sin conexión (RNF03).
- Banner visible y permanente: "Tenés N gastos sin sincronizar" (RNF01).
- Al recuperar la conexión, la cola se envía a un endpoint JSON mínimo `POST /api/sync/gastos`, que devuelve los IDs confirmados.

**El cálculo de balance NO corre offline.** Duplicar el algoritmo en JavaScript significaría mantener dos implementaciones de la lógica más crítica del sistema, con riesgo de que se desincronicen y den resultados distintos. Va contra la simplicidad pedida y contra el RNF06. Offline se **carga** y se **consulta**; para liquidar hace falta conexión, y la app lo avisa con un mensaje claro. *(Si más adelante querés cálculo offline, se resuelve en la v2 sacando el algoritmo a una librería compartida.)*

**Compartir por WhatsApp (RF14) — sin la API de Meta.** La WhatsApp Business Cloud API exige cuenta comercial verificada, aprobación de plantillas y costo por mensaje. Se resuelve con: enlace `https://wa.me/?text={mensaje}` (URL-encodeado) para el resumen de texto, y `navigator.share()` (Web Share API) para adjuntar el PDF desde el celular. Cero dependencias, cero costo, cumple el requisito.

**Registro por email (RF01) — sin servicio de correo.** El email se usa como identificador de la cuenta. Configurar `options.SignIn.RequireConfirmedAccount = false`. No se configura SMTP: no hace falta para el alcance actual y sería un servicio externo prescindible.

**Migración a app nativa (para más adelante, cuando yo lo pida).** La PWA ya construida se empaqueta como app instalable de Google Play mediante **TWA (Trusted Web Activity)** usando PWABuilder o Bubblewrap, sin reescribir una sola línea de código. No hagas nada de esto ahora; solo dejalo mencionado en el README.

---

## 7. ESTRUCTURA Y CONVENCIONES

### Estructura de carpetas (respetala literalmente)

```
GastosDeViaje.sln
├── GastosDeViaje/
│   ├── Controllers/
│   ├── Models/
│   │   └── Enums/
│   ├── ViewModels/
│   ├── Data/                 AppDbContext.cs, Migrations/, SeedData.cs
│   ├── Services/             IBalanceService.cs, BalanceService.cs,
│   │                         IComprobanteService.cs, ComprobanteService.cs
│   ├── Views/
│   │   ├── Shared/           _Layout.cshtml, _ValidationScriptsPartial.cshtml
│   │   └── {Controlador}/
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   ├── js/               app.js, offline.js, sw.js
│   │   ├── icons/
│   │   └── manifest.json
│   ├── appsettings.json
│   └── Program.cs
├── GastosDeViaje.Tests/       solo tests de BalanceService
└── docs/                      diagramas y documentación
```

Un **único** proyecto web. Nada de Clean Architecture, ni capas separadas en varios proyectos: complica la lectura sin aportar nada a un sistema de este tamaño.

### Separación estricta (esto es innegociable)
- **CSS:** todo en `wwwroot/css/site.css`. **Prohibido** `<style>` en las vistas y **prohibido** el atributo `style=` inline.
- **JavaScript:** todo en `wwwroot/js/*.js`. **Prohibido** `<script>` con código dentro de las vistas y **prohibido** `onclick=` u otros manejadores inline. Los eventos se enganchan con `addEventListener` desde el `.js`, buscando el elemento por `id` o `data-*`.
- **HTML:** limpio, semántico, indentado con 4 espacios. Los formularios usan tag helpers (`asp-for`, `asp-action`).
- El JavaScript tiene que ser **simple y comentado línea por línea** donde haga algo no obvio.

### Comentarios y documentación
- Comentario XML `///` en **toda** clase pública, método público y propiedad no evidente, explicando **qué hace y por qué existe**, no el "cómo" obvio.
- Comentarios inline solo donde la lógica no se lee sola: el algoritmo de balance y la cola offline llevan comentarios paso a paso.
- Cada archivo de `Services/` abre con un bloque de comentario que explica la responsabilidad de la clase en 3-4 líneas.

### Convenciones de código
- Entidades y propiedades **en español** (`SesionViaje`, `Monto`, `EsSimulado`).
- `async/await` en **todo** acceso a datos. Prohibido `.Result` y `.Wait()`.
- Validación con Data Annotations en los ViewModels + chequeo de `ModelState.IsValid` en el controller.
- Los controllers **no** contienen lógica de negocio: delegan en los servicios. Un controller solo valida, llama y devuelve vista.
- Nunca pasar entidades de EF directamente a la vista en formularios: usar ViewModels.
- `[Authorize]` en todos los controllers salvo Home y las páginas de Identity.

### Diseño visual (RNF04)
- Base tipográfica **16-18px**; montos en tamaño mayor y negrita.
- Botones y áreas táctiles de **mínimo 44x44px**.
- Inputs de dinero con `type="number"`, `step="0.01"` e `inputmode="decimal"` (abre el teclado numérico en el celular).
- Mobile-first: se diseña primero para pantalla de celular y después se adapta a escritorio.
- El formulario de alta de gasto tiene que completarse en **una sola pantalla, sin scroll horizontal y sin pasos intermedios**.
- Paleta y estética: neutras y sobrias, pensadas para redefinirse más adelante. No inventes un branding.

---

## 8. FASES DE EJECUCIÓN

Ejecutá en este orden. **Al final de cada fase: pará, informá y esperá mi confirmación.**

**FASE 0 — Documentación y diagramas.**
Crear `docs/` con archivos `.md` que contengan, en sintaxis **Mermaid** (versionable y editable, no imágenes):
`01-casos-de-uso.md` (diagrama de casos de uso con el actor Organizador y los 9 casos) · `02-diagrama-de-flujo.md` (flujo principal, incluyendo las tres ramas 6A cerrar, 6B break, 6C finalizar) · `03-diagrama-de-clases.md` (el modelo de la sección 3) · `04-modelo-entidad-relacion.md` (tablas, PK, FK, tipos) · `05-edt-wbs.md` (Estructura de Desglose de Trabajo, jerárquica, numerada 1 / 1.1 / 1.1.1, cubriendo las fases 1 a 7) · `06-requisitos.md` (RF y RNF con su estado y la clase que los cubre).

**FASE 1 — Solución y esqueleto.**
Crear la solución, el proyecto MVC con Individual Accounts, todas las carpetas de la sección 7, la cadena de conexión y un `README.md` inicial. Verificar que compila y levanta.

**FASE 2 — Modelo de datos.**
Entidades, enums, `AppDbContext`, configuración Fluent API, migración inicial (`Add-Migration InicialGastosDeViaje`), `Update-Database`, y `SeedData` con **una** sesión de ejemplo, 4 participantes y 6 gastos para poder probar el algoritmo.

**FASE 3 — CRUD scaffoldeado.**
Generar con el scaffolding de Visual Studio (*Controlador de MVC con vistas que usa Entity Framework*) los controllers y vistas de `SesionViaje`, `Participante` y `Gasto`. Después ajustar: filtrar siempre por el organizador logueado, bloquear la carga de gastos en sesiones `Cerrada`, y adaptar las vistas al criterio de diseño de la sección 7.

**FASE 4 — Motor de cálculo.**
`IBalanceService` / `BalanceService` con el algoritmo de la sección 4. Endpoints de break y cierre. Vista del detalle matemático. Proyecto de tests con **como mínimo** estos casos: división exacta · división con residuo de centavos · un solo pagador · todos pagaron lo mismo (cero movimientos) · verificación de que la suma de saldos da exactamente 0 · break seguido de nuevos gastos y segunda liquidación.

**FASE 5 — Comprobantes.**
`ComprobanteService` con QuestPDF: PDF con encabezado de la sesión, tabla de gastos, cuadro del cálculo y lista de transferencias. Botón de descarga, enlace `wa.me` y `navigator.share()`.

**FASE 6 — PWA / offline.**
`manifest.json`, iconos, `sw.js`, `offline.js` con IndexedDB y cola de sincronización, banner de pendientes, endpoint `POST /api/sync/gastos`.

**FASE 7 — Cierre.**
`site.css` final, revisión de accesibilidad y responsive, README completo (requisitos previos, cómo levantar la base, cómo correr el proyecto, decisiones de arquitectura, cómo empaquetar como TWA en el futuro) y un manual de usuario breve en `docs/`.

---

## 9. CRITERIOS DE ACEPTACIÓN

Una fase no está terminada hasta que:
- El proyecto **compila sin warnings**.
- La migración se aplica limpia sobre una base vacía.
- No hay **ni una línea** de CSS o JS embebida en las vistas.
- Toda clase y método público tiene su comentario XML con el requisito que cubre.
- El algoritmo de balance pasa todos los tests y `Σ saldos == 0` en todos los casos.
- El README refleja el estado real del proyecto.

---

## 10. QUÉ NO HACER (lista de prohibiciones)

`float` o `double` para dinero · contraseñas sin hashear · Clean Architecture o múltiples proyectos · patrón Repository o Unit of Work sobre EF Core (EF ya lo es) · AutoMapper · MediatR · Serilog · Docker · Blazor, React, Angular o cualquier SPA · Web API completa (solo el endpoint de sync) · multi-moneda · división desigual de gastos · roles, permisos o 2FA · logins externos · WhatsApp Business API · notificaciones push · gráficos o dashboards · `.Result` / `.Wait()` · lógica de negocio en los controllers · CSS o JS inline · NuGets fuera de la lista autorizada · empezar una fase sin que yo la confirme.

---

## COMENZÁ

Leé todo lo anterior. Si hay algo ambiguo o contradictorio, preguntámelo ahora. Si está todo claro, **arrancá con la FASE 0** y pará al terminarla.
