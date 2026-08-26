using System.ComponentModel.DataAnnotations;
using GastosDeViaje.Models.Enums;

namespace GastosDeViaje.ViewModels;

/// <summary>
/// Fila de la tabla de gastos de una sesión: junta el gasto con el nombre de quien lo
/// pagó, para no tener que resolverlo en la vista.
/// </summary>
/// Cubre RF05, RF10.
public class GastoListItemViewModel
{
    public int Id { get; set; }

    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; }

    [Display(Name = "Lugar")]
    public string Lugar { get; set; } = string.Empty;

    [Display(Name = "Motivo")]
    public string Motivo { get; set; } = string.Empty;

    [Display(Name = "Método de pago")]
    public MetodoPago MetodoPago { get; set; }

    [Display(Name = "Pagado por")]
    public string ParticipanteNombre { get; set; } = string.Empty;

    [Display(Name = "Saldado")]
    public bool Saldado { get; set; }
}
