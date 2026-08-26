using GastosDeViaje.Models;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.Services;

/// <summary>
/// Cálculo de liquidaciones de una sesión de viaje: reparte los gastos pendientes en
/// partes iguales entre los participantes y determina las transferencias mínimas para
/// saldar las cuentas.
/// </summary>
/// Cubre RF08, RF09, RF10, RNF06, RNF07.
public interface IBalanceService
{
    /// <summary>
    /// Calcula y persiste una liquidación (<see cref="TipoLiquidacion.Parcial"/> o
    /// <see cref="TipoLiquidacion.Final"/>) sobre los gastos pendientes de la sesión.
    /// </summary>
    Task<Liquidacion> CalcularLiquidacionAsync(int sesionViajeId, TipoLiquidacion tipo);
}
