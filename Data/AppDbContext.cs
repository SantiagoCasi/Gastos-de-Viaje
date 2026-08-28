using GastosDeViaje.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GastosDeViaje.Data;

/// <summary>
/// Contexto de EF Core de la aplicación. Extiende <see cref="IdentityDbContext{TUser}"/>
/// para incluir, además de las tablas propias de Identity, el modelo de dominio de
/// Gastos de Viaje: sesiones de viaje, participantes, gastos y liquidaciones.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SesionViaje> SesionesViaje => Set<SesionViaje>();
    public DbSet<Participante> Participantes => Set<Participante>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<MovimientoLiquidacion> MovimientosLiquidacion => Set<MovimientoLiquidacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Precisión explícita en todos los campos decimal (RNF06: exactitud matemática).
        builder.Entity<Gasto>().Property(g => g.Monto).HasPrecision(18, 2);
        builder.Entity<Liquidacion>().Property(l => l.TotalGastado).HasPrecision(18, 2);
        builder.Entity<Liquidacion>().Property(l => l.CuotaIdeal).HasPrecision(18, 2);
        builder.Entity<MovimientoLiquidacion>().Property(m => m.Monto).HasPrecision(18, 2);

        // SesionViaje -> Participante / Gasto / Liquidacion: si se borra la sesión,
        // se borra todo lo que le pertenece.
        builder.Entity<SesionViaje>()
            .HasMany(s => s.Participantes)
            .WithOne()
            .HasForeignKey(p => p.SesionViajeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SesionViaje>()
            .HasMany(s => s.Gastos)
            .WithOne()
            .HasForeignKey(g => g.SesionViajeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SesionViaje>()
            .HasMany(s => s.Liquidaciones)
            .WithOne()
            .HasForeignKey(l => l.SesionViajeId)
            .OnDelete(DeleteBehavior.Cascade);

        // SesionViaje.OrganizadorId y Participante.UsuarioId son FK hacia ApplicationUser
        // (Identity). Restrict/SetNull para no arrastrar borrados de cuentas de usuario.
        builder.Entity<SesionViaje>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.OrganizadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Participante>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // Gasto -> Participante (quién pagó). Restrict: si fuera Cascade se sumaría un
        // segundo camino SesionViaje -> Participante -> Gasto además del directo
        // SesionViaje -> Gasto, y SQL Server no permite múltiples caminos de cascada.
        builder.Entity<Gasto>()
            .HasOne<Participante>()
            .WithMany()
            .HasForeignKey(g => g.ParticipanteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Liquidacion -> Gasto: los gastos que saldó (LiquidacionId nullable, no se
        // borran en cascada: si se borrara una liquidación los gastos deben poder
        // quedar como pendientes otra vez, no desaparecer).
        builder.Entity<Liquidacion>()
            .HasMany(l => l.Gastos)
            .WithOne()
            .HasForeignKey(g => g.LiquidacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Liquidacion -> MovimientoLiquidacion.
        builder.Entity<Liquidacion>()
            .HasMany(l => l.Movimientos)
            .WithOne()
            .HasForeignKey(m => m.LiquidacionId)
            .OnDelete(DeleteBehavior.Cascade);

        // MovimientoLiquidacion -> Participante (Deudor/Acreedor): Restrict en ambas FK
        // para evitar el error de múltiples caminos de cascada en SQL Server, ya que un
        // mismo Participante puede figurar como deudor y como acreedor.
        builder.Entity<MovimientoLiquidacion>()
            .HasOne<Participante>()
            .WithMany()
            .HasForeignKey(m => m.DeudorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MovimientoLiquidacion>()
            .HasOne<Participante>()
            .WithMany()
            .HasForeignKey(m => m.AcreedorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices sobre las columnas más consultadas (RNF07: sesiones con muchos participantes).
        builder.Entity<Gasto>().HasIndex(g => g.SesionViajeId);
        builder.Entity<Gasto>().HasIndex(g => g.LiquidacionId);

        // Npgsql exige Kind=Utc para "timestamp with time zone": tanto los DateTime.Now
        // del código (Kind=Local) como los que llegan del model binder de un <input
        // type="date"> (Kind=Unspecified) lo rompen. Se normaliza acá para todas las
        // entidades en vez de tocar cada punto donde se asigna una fecha.
        var conversorUtc = new ValueConverter<DateTime, DateTime>(
            v => AUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var conversorUtcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? AUtc(v.Value) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var propiedad in builder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            if (propiedad.ClrType == typeof(DateTime))
            {
                propiedad.SetValueConverter(conversorUtc);
            }
            else if (propiedad.ClrType == typeof(DateTime?))
            {
                propiedad.SetValueConverter(conversorUtcNullable);
            }
        }
    }

    private static DateTime AUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };
}
