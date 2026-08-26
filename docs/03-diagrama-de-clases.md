# Diagrama de clases

Modelo de la sección 3 del prompt maestro, ya con las cuatro correcciones aplicadas
(`decimal` para dinero, `PasswordHash` de Identity, `Liquidacion`/`MovimientoLiquidacion`
como entidades, `Balance`/`Comprobante` como servicios sin estado propio).

```mermaid
classDiagram
    class ApplicationUser {
        +string Id
        +string Email
        +string PasswordHash
        +string NombreCompleto
    }

    class SesionViaje {
        +int Id
        +string Nombre
        +EstadoSesion Estado
        +DateTime FechaCreacion
        +DateTime? FechaCierre
        +string Moneda
        +string OrganizadorId
    }

    class EstadoSesion {
        <<enumeration>>
        Abierta
        Cerrada
    }

    class Participante {
        +int Id
        +string Nombre
        +bool EsSimulado
        +int SesionViajeId
        +string? UsuarioId
    }

    class Gasto {
        +int Id
        +int SesionViajeId
        +int ParticipanteId
        +decimal Monto
        +DateTime Fecha
        +string Lugar
        +string Motivo
        +MetodoPago MetodoPago
        +int? LiquidacionId
        +bool Saldado
    }

    class MetodoPago {
        <<enumeration>>
        Efectivo
        Debito
        Credito
        Transferencia
        Otro
    }

    class Liquidacion {
        +int Id
        +int SesionViajeId
        +DateTime Fecha
        +TipoLiquidacion Tipo
        +decimal TotalGastado
        +int CantidadParticipantes
        +decimal CuotaIdeal
    }

    class TipoLiquidacion {
        <<enumeration>>
        Parcial
        Final
    }

    class MovimientoLiquidacion {
        +int Id
        +int LiquidacionId
        +int DeudorId
        +int AcreedorId
        +decimal Monto
    }

    class IBalanceService {
        <<interface>>
        +CalcularLiquidacion(sesionId, tipo) Liquidacion
    }

    class IComprobanteService {
        <<interface>>
        +GenerarPdf(liquidacionId) byte[]
    }

    ApplicationUser "1" --> "0..*" SesionViaje : organiza
    ApplicationUser "0..1" --> "0..*" Participante : v2 - opcional
    SesionViaje "1" *-- "2..*" Participante
    SesionViaje "1" *-- "0..*" Gasto
    SesionViaje "1" *-- "0..*" Liquidacion
    Participante "1" --> "0..*" Gasto : paga
    Liquidacion "1" *-- "0..*" MovimientoLiquidacion
    Liquidacion "1" o-- "0..*" Gasto : salda
    Participante "1" --> "0..*" MovimientoLiquidacion : deudor
    Participante "1" --> "0..*" MovimientoLiquidacion : acreedor
    SesionViaje --> EstadoSesion
    Gasto --> MetodoPago
    Liquidacion --> TipoLiquidacion
    IBalanceService ..> Liquidacion : crea
    IComprobanteService ..> Liquidacion : lee
```

## Por qué `Balance` y `Comprobante` no son clases de dominio

En el diagrama original, `Balance` y `Comprobante` solo tenían métodos (`calcularBalance()`,
`generarPdf()`) y ningún dato propio: son **comportamiento**, no **estado**. Persistirlos
duplicaría información que ya vive en `Liquidacion` y sus `MovimientoLiquidacion`. Por eso se
implementan como servicios sin estado (`BalanceService`, `ComprobanteService`), inyectados por
DI, y no como entidades de EF Core.
