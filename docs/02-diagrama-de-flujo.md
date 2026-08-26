# Diagrama de flujo principal

Flujo típico de uso de la aplicación desde el login hasta el cierre de una sesión de viaje,
incluyendo las tres ramas del paso 6: **6A cerrar** (dejar de trabajar sin liquidar),
**6B break** (liquidación parcial) y **6C finalizar** (liquidación final).

```mermaid
flowchart TD
    Inicio([Inicio]) --> Login[1 - Iniciar sesión]
    Login --> Sesion[2 - Crear o seleccionar sesión de viaje]
    Sesion --> Participantes[3 - Agregar participantes simulados]
    Participantes --> CargarGasto[4 - Registrar gasto]
    CargarGasto --> MasGastos{5 - ¿Cargar otro gasto?}
    MasGastos -->|Sí| CargarGasto
    MasGastos -->|No| Decision{6 - ¿Qué desea hacer el organizador?}

    Decision -->|6A - Cerrar por ahora| CerrarApp[Salir de la aplicación<br/>sin liquidar]
    CerrarApp --> FinA([Fin - puede volver luego])

    Decision -->|6B - Break| CalcularParcial[Calcular liquidación Parcial]
    CalcularParcial --> MarcarSaldados[Marcar gastos incluidos como saldados]
    MarcarSaldados --> MostrarDetalleParcial[Mostrar detalle matemático<br/>y transferencias sugeridas]
    MostrarDetalleParcial --> PdfParcial[Generar comprobante PDF]
    PdfParcial --> CompartirParcial[Compartir por WhatsApp]
    CompartirParcial --> SesionSigueAbierta[La sesión sigue Abierta]
    SesionSigueAbierta --> CargarGasto

    Decision -->|6C - Finalizar| CalcularFinal[Calcular liquidación Final]
    CalcularFinal --> CerrarSesion[Sesión pasa a Estado = Cerrada<br/>se registra FechaCierre]
    CerrarSesion --> MostrarDetalleFinal[Mostrar detalle matemático<br/>y transferencias finales]
    MostrarDetalleFinal --> PdfFinal[Generar comprobante PDF]
    PdfFinal --> CompartirFinal[Compartir por WhatsApp]
    CompartirFinal --> FinC([Fin - sesión cerrada,<br/>no admite nuevos gastos])
```

## Notas del flujo

- El paso 4 (Registrar gasto) funciona con o sin conexión (RF06, RNF01); si no hay conexión el gasto
  se guarda en `IndexedDB` y se sincroniza más tarde.
- La rama **6B (break)** no cambia el `Estado` de la sesión: solo asigna una `Liquidacion` de tipo
  `Parcial` a los gastos pendientes y permite seguir cargando gastos nuevos.
- La rama **6C (finalizar)** requiere conexión (el cálculo de balance no corre offline) y deja la
  sesión en `Estado = Cerrada`, bloqueando la carga de gastos nuevos.
- Generar y compartir el comprobante (pasos posteriores a 6B/6C) cubren RF13 y RF14.
