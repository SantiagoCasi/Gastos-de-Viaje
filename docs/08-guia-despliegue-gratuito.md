# Guía de despliegue gratuito (sin tarjeta de crédito)

Esta guía explica cómo publicar GastosDeViaje en internet sin costo y sin cargar
una tarjeta de crédito en ningún servicio, usando:

- **Neon** (https://neon.tech) para la base de datos PostgreSQL gestionada.
- **Render** (https://render.com) para alojar la aplicación ASP.NET Core.

Ninguno de los dos pide tarjeta para el plan gratuito. La combinación elegida
evita SQLite: los planes gratuitos de hosting (Render incluido) usan un disco
efímero que se borra en cada redeploy, así que un archivo `.db` local no
sobreviviría. Una base externa (Neon) resuelve eso.

Limitaciones a tener en cuenta del plan gratuito:
- Render "duerme" el servicio tras ~15 minutos sin tráfico; el primer pedido
  después de eso tarda unos segundos en levantar (cold start).
- Neon escala a cero cómputo tras inactividad; el primer query después de un
  rato de inactividad también tiene una demora extra de reconexión.
- Ambas limitaciones son aceptables para un proyecto académico/demo, no para
  producción con usuarios reales simultáneos.

---

> **Estado:** los pasos 1 y 5 (migrar el proveedor a PostgreSQL y crear el
> Dockerfile) ya están hechos en el repo — quedan documentados igual acá
> para referencia, pero podés saltar directo al paso 2 (crear la base en
> Neon). Lo que falta es todo lo que requiere una cuenta tuya: Neon,
> GitHub y Render.

---

## 1. Cambiar el proveedor de base de datos: SQL Server → PostgreSQL ✅ hecho

El proyecto usa actualmente `Microsoft.EntityFrameworkCore.SqlServer`
([GastosDeViaje.csproj](../GastosDeViaje.csproj)) apuntando a un SQL Server
local con autenticación de Windows. Eso no es utilizable desde la nube, así
que hay que migrar el proveedor de EF Core a PostgreSQL (paquete `Npgsql`).

### 1.1. Cambiar el paquete NuGet

En [GastosDeViaje.csproj](../GastosDeViaje.csproj), reemplazar:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
```

por:

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

(o ejecutar desde la terminal, parado en la carpeta del proyecto):

```
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 1.2. Cambiar el provider en Program.cs

En [Program.cs](../Program.cs) línea 18, reemplazar:

```csharp
options.UseSqlServer(connectionString));
```

por:

```csharp
options.UseNpgsql(connectionString));
```

### 1.3. Actualizar el fixture de tests (opcional pero recomendado)

`GastosDeViaje.Tests/BalanceServiceFixture.cs` también llama a
`.UseSqlServer(...)`. Si querés que los tests sigan corriendo contra el mismo
motor que producción, cambiala también a `.UseNpgsql(...)`. Si preferís que
los tests no dependan de una base real, se puede migrar a
`Microsoft.EntityFrameworkCore.InMemory` o a SQLite en memoria — pero eso es
un cambio aparte, no hace falta para el despliegue.

### 1.4. Borrar y regenerar las migraciones

Las migraciones actuales (`Data/Migrations/*`) están generadas para SQL
Server y no aplican tal cual a PostgreSQL. Borrá la carpeta y generá una
migración nueva:

```
rm -r Data/Migrations
dotnet ef migrations add InicialPostgres
```

Revisá que el build compile (`dotnet build`) antes de seguir. Los
`[Column(TypeName = "decimal(18,2)")]` que ya están en los modelos
([Gasto.cs](../Models/Gasto.cs), [Liquidacion.cs](../Models/Liquidacion.cs),
[MovimientoLiquidacion.cs](../Models/MovimientoLiquidacion.cs)) son
compatibles con Postgres (se mapean a `numeric(18,2)`), así que no deberían
hacer falta más cambios en los modelos.

---

## 2. Crear la base de datos en Neon

1. Entrá a https://neon.tech y creá una cuenta (con GitHub o email; no pide
   tarjeta).
2. Creá un proyecto nuevo (por ejemplo `gastosdeviaje`).
3. Neon crea automáticamente una base y te muestra una **cadena de conexión**
   con este formato:
   ```
   postgresql://usuario:password@ep-xxxx.region.aws.neon.tech/neondb?sslmode=require
   ```
4. Convertila al formato que usa Npgsql (clave=valor). Ejemplo:
   ```
   Host=ep-xxxx.region.aws.neon.tech;Database=neondb;Username=usuario;Password=password;SSL Mode=Require;Trust Server Certificate=true
   ```
   Guardá este valor, lo vas a necesitar dos veces: para aplicar las
   migraciones (paso 3) y como variable de entorno en Render (paso 5).

**No lo pegues en `appsettings.json` y lo subas a git.** Para uso local,
guardalo con user-secrets:

```
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```

Y dejá en [appsettings.json](../appsettings.json) solo un valor vacío o de
ejemplo en `ConnectionStrings:DefaultConnection`, ya que en producción lo va
a proveer la variable de entorno de Render (paso 5).

---

## 3. Aplicar las migraciones a Neon

Con el connection string de Neon cargado (vía user-secrets, como en el paso
anterior), desde tu máquina corré:

```
dotnet ef database update
```

Esto crea todas las tablas (Identity, SesionViaje, Participante, Gasto,
Liquidacion, MovimientoLiquidacion, etc.) directamente en la base de Neon.
Podés verificarlo en el dashboard de Neon, pestaña "Tables".

---

## 4. Subir el código a GitHub

Si el repo todavía no está en GitHub:

```
git add -A
git commit -m "Migración a PostgreSQL para despliegue"
git push -u origin master
```

(Render se conecta directo al repo de GitHub para desplegar.)

---

## 5. Crear un Dockerfile ✅ hecho

Render construye y corre la app dentro de un contenedor Docker. Creá un
archivo `Dockerfile` en la raíz del repo (mismo nivel que
[GastosDeViaje.csproj](../GastosDeViaje.csproj)):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish GastosDeViaje.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "GastosDeViaje.dll"]
```

Render inyecta la variable de entorno `PORT`, pero fijar el puerto a `10000`
explícitamente (vía `ASPNETCORE_URLS`) y avisarle a Render que ese es el
puerto del servicio (se configura en el dashboard, paso 6) es más simple que
leer `PORT` dinámicamente.

Agregá también un `.dockerignore` para no copiar basura de build al
contenedor:

```
bin/
obj/
GastosDeViaje.Tests/
.git/
```

Commiteá y pusheá estos dos archivos nuevos.

---

## 6. Crear el Web Service en Render

1. Entrá a https://render.com y creá una cuenta con GitHub (no pide tarjeta
   para el plan Free).
2. "New +" → "Web Service".
3. Conectá el repositorio `GastosDeViaje` de tu GitHub.
4. Configuración:
   - **Runtime**: Docker (Render detecta el `Dockerfile` solo).
   - **Instance Type**: Free.
   - **Port**: `10000` (tiene que coincidir con el `EXPOSE`/`ASPNETCORE_URLS`
     del Dockerfile).
5. En "Environment Variables" agregá:
   - `ConnectionStrings__DefaultConnection` = el connection string de Neon
     del paso 2.4 (con doble guión bajo `__`, así lee ASP.NET Core la config
     jerárquica desde variables de entorno).
   - `ASPNETCORE_ENVIRONMENT` = `Production`.
6. "Create Web Service". Render clona el repo, construye la imagen y la
   despliega. El primer build tarda varios minutos.

Al terminar te da una URL pública tipo
`https://gastosdeviaje.onrender.com` — accesible desde el navegador de
cualquier dispositivo, con HTTPS automático (necesario para que el service
worker de la PWA funcione, ver
[Views](../Views) y el manifest de la Fase 6).

---

## 7. Verificar que funciona

- Abrí la URL de Render, registrate como usuario (Identity) y probá crear una
  sesión de viaje, agregar gastos y hacer una liquidación, para confirmar que
  la base de Neon está recibiendo los datos.
- Si algo falla al arrancar, en el dashboard de Render → pestaña "Logs" se ve
  el stack trace completo (errores de connection string son los más
  comunes: revisá que no haya espacios de más ni falte `SSL Mode=Require`).

---

## 8. Cada vez que hagas un cambio

Con este esquema, el flujo de ahí en adelante es:

1. `git push` a la rama conectada en Render → Render redeploya solo
   (auto-deploy está activado por defecto).
2. Si el cambio agrega una migración nueva (`dotnet ef migrations add ...`),
   hay que aplicarla a Neon manualmente corriendo `dotnet ef database update`
   desde tu máquina (con el connection string de Neon en user-secrets) antes
   o después del deploy — este proyecto no corre migraciones automáticas al
   arrancar.
