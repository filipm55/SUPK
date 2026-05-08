using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SUPK.Models;

public partial class CaffeBarDbContext : DbContext
{
    public CaffeBarDbContext()
    {
    }

    public CaffeBarDbContext(DbContextOptions<CaffeBarDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Konobar> Konobars { get; set; }

    public virtual DbSet<Narudzba> Narudzbas { get; set; }

    public virtual DbSet<Pozivkonobara> Pozivkonobaras { get; set; }

    public virtual DbSet<Proizvod> Proizvods { get; set; }

    public virtual DbSet<Racun> Racuns { get; set; }

    public virtual DbSet<Stavkanarudzbe> Stavkanarudzbes { get; set; }

    public virtual DbSet<Stol> Stols { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=CaffeBar_db;Username=postgres;Password=gamecih1");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("status_poziva", new[] { "CEKA", "RIJESENO" })
            .HasPostgresEnum("stol_status", new[] { "SLOBODAN", "ZAUZET", "POZIV" })
            .HasPostgresEnum("tip_placanja", new[] { "GOTOVINA", "KARTICA" })
            .HasPostgresEnum("tip_poziva", new[] { "GRESKA", "NARUDZBA", "PLACANJE" });

        modelBuilder.Entity<Konobar>(entity =>
        {
            entity.HasKey(e => e.KonobarId).HasName("konobar_pkey");

            entity.ToTable("konobar");

            entity.Property(e => e.KonobarId).HasColumnName("konobar_id");
            entity.Property(e => e.Aktivan)
                .HasDefaultValue(false)
                .HasColumnName("aktivan");
            entity.Property(e => e.Ime)
                .HasMaxLength(50)
                .HasColumnName("ime");
            entity.Property(e => e.Prezime)
                .HasMaxLength(50)
                .HasColumnName("prezime");
        });

        modelBuilder.Entity<Narudzba>(entity =>
        {
            entity.HasKey(e => e.NarudzbaId).HasName("narudzba_pkey");

            entity.ToTable("narudzba");

            entity.Property(e => e.NarudzbaId).HasColumnName("narudzba_id");
            entity.Property(e => e.RacunId).HasColumnName("racun_id");
            entity.Property(e => e.VrijemeNarudzbe)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("vrijeme_narudzbe");

            entity.HasOne(d => d.Racun).WithMany(p => p.Narudzbas)
                .HasForeignKey(d => d.RacunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("narudzba_racun_id_fkey");
        });

        modelBuilder.Entity<Pozivkonobara>(entity =>
        {
            entity.HasKey(e => e.PozivId).HasName("pozivkonobara_pkey");

            entity.ToTable("pozivkonobara");

            entity.Property(e => e.PozivId).HasColumnName("poziv_id");
            entity.Property(e => e.StolId).HasColumnName("stol_id");
            entity.Property(e => e.VrijemePoziva)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("vrijeme_poziva");

            entity.HasOne(d => d.Stol).WithMany(p => p.Pozivkonobaras)
                .HasForeignKey(d => d.StolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pozivkonobara_stol_id_fkey");
        });

        modelBuilder.Entity<Proizvod>(entity =>
        {
            entity.HasKey(e => e.ProizvodId).HasName("proizvod_pkey");

            entity.ToTable("proizvod");

            entity.Property(e => e.ProizvodId).HasColumnName("proizvod_id");
            entity.Property(e => e.Cijena)
                .HasPrecision(10, 2)
                .HasColumnName("cijena");
            entity.Property(e => e.Naziv)
                .HasMaxLength(50)
                .HasColumnName("naziv");
        });

        modelBuilder.Entity<Racun>(entity =>
        {
            entity.HasKey(e => e.RacunId).HasName("racun_pkey");

            entity.ToTable("racun");

            entity.Property(e => e.RacunId).HasColumnName("racun_id");
            entity.Property(e => e.KonobarId).HasColumnName("konobar_id");
            entity.Property(e => e.StolId).HasColumnName("stol_id");
            entity.Property(e => e.UkupnaCijena)
                .HasPrecision(10, 2)
                .HasDefaultValue(0m)
                .HasColumnName("ukupna_cijena");
            entity.Property(e => e.VrijemeOtvaranja)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("vrijeme_otvaranja");
            entity.Property(e => e.VrijemeZatvaranja)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("vrijeme_zatvaranja");

            entity.HasOne(d => d.Konobar).WithMany(p => p.Racuns)
                .HasForeignKey(d => d.KonobarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("racun_konobar_id_fkey");

            entity.HasOne(d => d.Stol).WithMany(p => p.Racuns)
                .HasForeignKey(d => d.StolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("racun_stol_id_fkey");
        });

        modelBuilder.Entity<Stavkanarudzbe>(entity =>
        {
            entity.HasKey(e => e.StavkaId).HasName("stavkanarudzbe_pkey");

            entity.ToTable("stavkanarudzbe");

            entity.Property(e => e.StavkaId).HasColumnName("stavka_id");
            entity.Property(e => e.Kolicina).HasColumnName("kolicina");
            entity.Property(e => e.NarudzbaId).HasColumnName("narudzba_id");
            entity.Property(e => e.ProizvodId).HasColumnName("proizvod_id");

            entity.HasOne(d => d.Narudzba).WithMany(p => p.Stavkanarudzbes)
                .HasForeignKey(d => d.NarudzbaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stavkanarudzbe_narudzba_id_fkey");

            entity.HasOne(d => d.Proizvod).WithMany(p => p.Stavkanarudzbes)
                .HasForeignKey(d => d.ProizvodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stavkanarudzbe_proizvod_id_fkey");
        });

        modelBuilder.Entity<Stol>(entity =>
        {
            entity.HasKey(e => e.StolId).HasName("stol_pkey");

            entity.ToTable("stol");

            entity.HasIndex(e => e.BrojStola, "stol_broj_stola_key").IsUnique();

            entity.Property(e => e.StolId).HasColumnName("stol_id");
            entity.Property(e => e.BrojStola).HasColumnName("broj_stola");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
