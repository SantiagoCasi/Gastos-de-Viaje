namespace GastosDeViaje.Models.Enums;

/// <summary>
/// Distingue un corte de cuentas a mitad de viaje (<see cref="Parcial"/>, RF08) del
/// cierre definitivo de la sesión (<see cref="Final"/>, RF09).
/// </summary>
public enum TipoLiquidacion
{
    Parcial,
    Final
}
