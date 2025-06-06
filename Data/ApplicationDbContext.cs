using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using app1.Models;

namespace app1.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Viaje> Viajes { get; set; }
    public DbSet<Departamento> Departamentos { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<Direccion> Direcciones { get; set; }
    public DbSet<FechaSalidaViaje> FechasSalidaViaje { get; set; }
    public DbSet<Compra> Compras { get; set; }
    public DbSet<DetalleCompra> DetallesCompra { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Contacto> Contacto { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.Cliente)
            .WithMany()
            .HasForeignKey(r => r.ClienteId);
        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.Viaje)
            .WithMany()
            .HasForeignKey(r => r.ViajeId);
        modelBuilder.Entity<Viaje>()
            .HasOne(v => v.Departamento)
            .WithMany(d => d.Viajes)
            .HasForeignKey(v => v.DepartamentoId);
        modelBuilder.Entity<DetalleCompra>()
            .HasOne(d => d.Compra)
            .WithMany(c => c.Detalles)
            .HasForeignKey(d => d.CompraId);
        modelBuilder.Entity<Contacto>()
            .HasOne(c => c.Viaje)
            .WithMany()
            .HasForeignKey(c => c.ViajeId)
            .OnDelete(DeleteBehavior.SetNull);
        // No se requiere relación especial para Pago
    }
}
