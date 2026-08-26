using GastosDeViaje.Models.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GastosDeViaje.Services;

// Arma el comprobante en PDF de una liquidación con QuestPDF: encabezado de la sesión,
// tabla de los gastos incluidos, cuadro del cálculo (total/cuota/participantes) y la
// lista de transferencias sugeridas. No calcula nada: solo le da formato a lo que ya
// devolvió IBalanceService.ObtenerDetalleAsync.
/// <summary>
/// Implementación de <see cref="IComprobanteService"/>.
/// </summary>
/// Cubre RF13.
public class ComprobanteService : IComprobanteService
{
    private readonly IBalanceService _balanceService;

    public ComprobanteService(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    public async Task<byte[]> GenerarPdfAsync(int liquidacionId)
    {
        var detalle = await _balanceService.ObtenerDetalleAsync(liquidacionId);
        var tipoTexto = detalle.Tipo == TipoLiquidacion.Parcial ? "Break (liquidación parcial)" : "Liquidación final";

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                // Fuente explícita: la fuente por defecto de QuestPDF sustituye "ti"/"ft"
                // por ligaduras sin mapeo Unicode, lo que rompe la copia de texto del PDF.
                pagina.DefaultTextStyle(estilo => estilo.FontFamily(Fonts.Arial).FontSize(10));

                pagina.Header().Column(columna =>
                {
                    columna.Item().Text(detalle.SesionNombre).FontSize(18).Bold();
                    columna.Item().Text(tipoTexto).FontSize(12);
                    columna.Item().Text($"Calculada el {detalle.Fecha:dd/MM/yyyy}").FontSize(9);
                });

                pagina.Content().PaddingTop(15).Column(columna =>
                {
                    columna.Spacing(15);
                    columna.Item().Element(c => ComponerCuadroCalculo(c, detalle));
                    columna.Item().Element(c => ComponerTablaGastos(c, detalle));
                    columna.Item().Element(c => ComponerTablaParticipantes(c, detalle));
                    columna.Item().Element(c => ComponerTransferencias(c, detalle));
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.CurrentPageNumber();
                    texto.Span(" / ");
                    texto.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static void ComponerCuadroCalculo(IContainer contenedor, ResultadoLiquidacion detalle)
    {
        contenedor.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Row(fila =>
        {
            fila.RelativeItem().Column(c =>
            {
                c.Item().Text("Total gastado").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().Text($"{detalle.TotalGastado:C2} {detalle.Moneda}").Bold();
            });
            fila.RelativeItem().Column(c =>
            {
                c.Item().Text("Participantes").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().Text(detalle.CantidadParticipantes.ToString()).Bold();
            });
            fila.RelativeItem().Column(c =>
            {
                c.Item().Text("Cuota ideal").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().Text($"{detalle.CuotaIdeal:C2} {detalle.Moneda}").Bold();
            });
        });
    }

    private static void ComponerTablaGastos(IContainer contenedor, ResultadoLiquidacion detalle)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Text("Gastos incluidos").Bold();
            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.ConstantColumn(45);
                    columnas.RelativeColumn(2);
                    columnas.RelativeColumn(2);
                    columnas.RelativeColumn(1.3f);
                    columnas.ConstantColumn(65);
                });

                tabla.Header(encabezado =>
                {
                    encabezado.Cell().Text("Fecha").Bold();
                    encabezado.Cell().Text("Lugar").Bold();
                    encabezado.Cell().Text("Motivo").Bold();
                    encabezado.Cell().Text("Pagó").Bold();
                    encabezado.Cell().AlignRight().Text("Monto").Bold();
                });

                foreach (var gasto in detalle.Gastos)
                {
                    tabla.Cell().Text(gasto.Fecha.ToString("dd/MM"));
                    tabla.Cell().Text(gasto.Lugar);
                    tabla.Cell().Text(gasto.Motivo);
                    tabla.Cell().Text(gasto.ParticipanteNombre);
                    tabla.Cell().AlignRight().Text(gasto.Monto.ToString("C2"));
                }
            });
        });
    }

    private static void ComponerTablaParticipantes(IContainer contenedor, ResultadoLiquidacion detalle)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Text("Detalle por participante").Bold();
            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(2);
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                });

                tabla.Header(encabezado =>
                {
                    encabezado.Cell().Text("Participante").Bold();
                    encabezado.Cell().AlignRight().Text("Pagado").Bold();
                    encabezado.Cell().AlignRight().Text("Cuota").Bold();
                    encabezado.Cell().AlignRight().Text("Saldo").Bold();
                });

                foreach (var fila in detalle.Participantes)
                {
                    tabla.Cell().Text(fila.Nombre);
                    tabla.Cell().AlignRight().Text(fila.Pagado.ToString("C2"));
                    tabla.Cell().AlignRight().Text(fila.Cuota.ToString("C2"));
                    tabla.Cell().AlignRight().Text(fila.Saldo.ToString("C2"));
                }
            });
        });
    }

    private static void ComponerTransferencias(IContainer contenedor, ResultadoLiquidacion detalle)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Text("Transferencias sugeridas").Bold();

            if (detalle.Transferencias.Count == 0)
            {
                columna.Item().Text("Nadie le debe nada a nadie: todos pagaron exactamente su cuota.");
                return;
            }

            foreach (var transferencia in detalle.Transferencias)
            {
                columna.Item().Text($"{transferencia.DeudorNombre} le paga {transferencia.Monto:C2} a {transferencia.AcreedorNombre}");
            }
        });
    }
}
