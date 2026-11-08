using System;
using System.Collections.Generic;
using AirTortilla.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirTortilla.Web.Data;

public partial class AirTortillaContext : DbContext
{
    public AirTortillaContext(DbContextOptions<AirTortillaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditoria> Auditoria { get; set; }

    public virtual DbSet<Pais> Paises { get; set; }

    public virtual DbSet<Pasajero> Pasajeros { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Rol> Roles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("auditoria");

            entity.HasIndex(e => new { e.UsuarioId, e.Fecha }, "IX_Auditoria_Usuario_Fecha");

            entity.Property(e => e.Accion).HasColumnType("enum('INSERT','UPDATE','DELETE','LOGIN','LOGIN_FALLIDO','IMPORTAR','EXPORTAR')");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.RegistroId).HasMaxLength(50);
            entity.Property(e => e.Tabla).HasMaxLength(50);
            entity.Property(e => e.ValoresAnteriores).HasColumnType("json");
            entity.Property(e => e.ValoresNuevos).HasColumnType("json");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auditoria_Usuario");
        });

        modelBuilder.Entity<Pais>(entity =>
        {
            entity.HasKey(e => e.CodigoIso).HasName("PRIMARY");

            entity.ToTable("pais");

            entity.Property(e => e.CodigoIso)
                .HasMaxLength(2)
                .IsFixedLength();
        });

        modelBuilder.Entity<Pasajero>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pasajero");

            entity.HasIndex(e => e.DocumentoIdentidad, "UQ_Pasajero_Documento").IsUnique();

            entity.Property(e => e.DocumentoIdentidad).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(60);
            entity.Property(e => e.PrimerApellido).HasMaxLength(60);
            entity.Property(e => e.SegundoApellido).HasMaxLength(60);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reserva");

            entity.HasIndex(e => e.Origen, "FK_Reserva_Origen");

            entity.HasIndex(e => new { e.Destino, e.FechaReserva }, "IX_Reserva_Destino_Fecha");

            entity.HasIndex(e => e.Localizador, "UQ_Reserva_Localizador").IsUnique();

            entity.Property(e => e.Destino)
                .HasMaxLength(2)
                .IsFixedLength();
            entity.Property(e => e.FechaReserva).HasColumnType("date");
            entity.Property(e => e.Localizador).HasMaxLength(20);
            entity.Property(e => e.Origen)
                .HasMaxLength(2)
                .IsFixedLength();

            entity.HasOne(d => d.DestinoNavigation).WithMany(p => p.ReservaDestinoNavigations)
                .HasForeignKey(d => d.Destino)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Destino");

            entity.HasOne(d => d.OrigenNavigation).WithMany(p => p.ReservaOrigenNavigations)
                .HasForeignKey(d => d.Origen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Origen");

            entity.HasMany(d => d.Pasajeros).WithMany(p => p.Reservas)
                .UsingEntity<Dictionary<string, object>>(
                    "Reservapasajero",
                    r => r.HasOne<Pasajero>().WithMany()
                        .HasForeignKey("PasajeroId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_RP_Pasajero"),
                    l => l.HasOne<Reserva>().WithMany()
                        .HasForeignKey("ReservaId")
                        .HasConstraintName("FK_RP_Reserva"),
                    j =>
                    {
                        j.HasKey("ReservaId", "PasajeroId").HasName("PRIMARY");
                        j.ToTable("reservapasajero");
                        j.HasIndex(new[] { "PasajeroId" }, "FK_RP_Pasajero");
                    });
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rol");

            entity.HasIndex(e => e.Nombre, "UQ_Rol_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(30);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.CreadoPorId, "FK_Usuario_CreadoPor");

            entity.HasIndex(e => e.RolId, "FK_Usuario_Rol");

            entity.HasIndex(e => e.NombreUsuario, "UQ_Usuario_Nombre").IsUnique();

            entity.Property(e => e.Activo)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreUsuario).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.Property(e => e.UltimoAcceso).HasColumnType("datetime");

            entity.HasOne(d => d.CreadoPor).WithMany(p => p.InverseCreadoPor)
                .HasForeignKey(d => d.CreadoPorId)
                .HasConstraintName("FK_Usuario_CreadoPor");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
