# Modelo entidad-relación

Tablas, claves primarias/foráneas y tipos tal como se generan con EF Core Code First
(convención `Id` como PK, sufijo `Id` como FK). `AspNetUsers` y el resto de las tablas de
Identity (`AspNetRoles`, `AspNetUserClaims`, etc.) las crea el framework; solo se documenta
la columna agregada.

```mermaid
erDiagram
    AspNetUsers ||--o{ SesionesViaje : organiza
    AspNetUsers |o--o{ Participantes : "v2 - opcional"
    SesionesViaje ||--o{ Participantes : tiene
    SesionesViaje ||--o{ Gastos : tiene
    SesionesViaje ||--o{ Liquidaciones : tiene
    Participantes ||--o{ Gastos : paga
    Liquidaciones ||--o{ Gastos : salda
    Liquidaciones ||--o{ MovimientosLiquidacion : contiene
    Participantes ||--o{ MovimientosLiquidacion : "es deudor en"
    Participantes ||--o{ MovimientosLiquidacion : "es acreedor en"

    AspNetUsers {
        string Id PK
        string Email
        string PasswordHash
        string NombreCompleto
    }

    SesionesViaje {
        int Id PK
        string Nombre
        int Estado "enum: 0 Abierta, 1 Cerrada"
        datetime FechaCreacion
        datetime FechaCierre "nullable"
        string Moneda "3 caracteres, default ARS"
        string OrganizadorId FK
    }

    Participantes {
        int Id PK
        string Nombre
        bit EsSimulado "default 1"
        int SesionViajeId FK
        string UsuarioId FK "nullable, reservado v2"
    }

    Gastos {
        int Id PK
        int SesionViajeId FK
        int ParticipanteId FK "quien pagó"
        decimal Monto "decimal(18,2), mayor a 0"
        datetime Fecha
        string Lugar "120"
        string Motivo "200"
        int MetodoPago "enum: Efectivo,Debito,Credito,Transferencia,Otro"
        int LiquidacionId FK "nullable, null = pendiente"
    }

    Liquidaciones {
        int Id PK
        int SesionViajeId FK
        datetime Fecha
        int Tipo "enum: 0 Parcial, 1 Final"
        decimal TotalGastado "decimal(18,2)"
        int CantidadParticipantes
        decimal CuotaIdeal "decimal(18,2)"
    }

    MovimientosLiquidacion {
        int Id PK
        int LiquidacionId FK
        int DeudorId FK "Participante"
        int AcreedorId FK "Participante"
        decimal Monto "decimal(18,2), mayor a 0"
    }
```

## Comportamiento de borrado (`OnDelete`)

| Relación | Comportamiento | Motivo |
|---|---|---|
| `SesionViaje` → `Participante` | `Cascade` | Al borrar una sesión de viaje no tiene sentido dejar participantes huérfanos. |
| `SesionViaje` → `Gasto` | `Cascade` | Ídem: los gastos pertenecen exclusivamente a una sesión. |
| `SesionViaje` → `Liquidacion` | `Cascade` | Ídem. |
| `MovimientoLiquidacion` → `Participante` (Deudor) | `Restrict` | Evita el error de SQL Server por múltiples caminos de cascada (un `Participante` puede ser deudor y acreedor a la vez). |
| `MovimientoLiquidacion` → `Participante` (Acreedor) | `Restrict` | Ídem. |

## Índices

- `Gasto.SesionViajeId` (consultas frecuentes: "gastos de esta sesión").
- `Gasto.LiquidacionId` (consultas frecuentes: "gastos pendientes vs. saldados").
