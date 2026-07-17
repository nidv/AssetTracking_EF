using AssetTracking_EF.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetTracking_EF.Data;

public class AssetTrackingDbContext : DbContext
{
    string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=lex2026july;Trusted_Connection=True;Encrypt=False;";

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Level 1 requirement - Inheritance (Table-Per-Hierarchy)
        // One shared Assets table; EF's auto-discriminator column selects the concrete type
        modelBuilder.Entity<Computer>().HasBaseType<Asset>();
        modelBuilder.Entity<Laptop>().HasBaseType<Asset>();
        modelBuilder.Entity<MobilePhone>().HasBaseType<Asset>();
        modelBuilder.Entity<Tablet>().HasBaseType<Asset>();

        modelBuilder.Entity<Asset>(b =>
        {
            b.Property(a => a.AssetType).HasConversion<string>().HasMaxLength(20);
            b.Property(a => a.PurchasePriceUsd).HasColumnType("decimal(18,2)");
            b.Property(a => a.LocalCurrency).HasConversion<string>().HasMaxLength(8);
            b.Property(a => a.Brand).IsRequired().HasMaxLength(60);
            b.Property(a => a.Model).IsRequired().HasMaxLength(60);
            b.Property(a => a.SerialNumber).IsRequired().HasMaxLength(8);

            b.HasOne(a => a.Office)
             .WithMany(o => o.Assets)
             .HasForeignKey(a => a.OfficeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.AssignedTo)
             .WithMany(e => e.AssignedAssets)
             .HasForeignKey(a => a.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(a => a.SerialNumber).IsUnique();
            b.HasIndex(a => a.PurchaseDate);
            b.HasIndex(a => a.OfficeId);
        });


        modelBuilder.Entity<Office>(b =>
        {
            b.Property(o => o.Name).IsRequired().HasMaxLength(60);
            b.Property(o => o.Country).IsRequired().HasMaxLength(60);
            b.Property(o => o.Currency).HasConversion<string>().HasMaxLength(8);
            b.HasIndex(o => o.Name).IsUnique();
        });

        modelBuilder.Entity<Employee>(b =>
        {
            b.Property(e => e.FullName).IsRequired().HasMaxLength(60);
            b.Property(e => e.Department).IsRequired().HasMaxLength(60);
            b.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            b.Property(e => e.Email).IsRequired().HasMaxLength(100);
            b.HasIndex(e => e.Email).IsUnique();
        });


        // Level 3 requirement - Seed Offices (constant data)
        modelBuilder.Entity<Office>().HasData(
            new Office { Id = 1, Name = "Sweden Office", Country = "Sweden", Currency = Currency.SEK },
            new Office { Id = 2, Name = "USA Office", Country = "USA", Currency = Currency.USD },
            new Office { Id = 3, Name = "Germany Office", Country = "Germany", Currency = Currency.EUR },
            new Office { Id = 4, Name = "Turkey Office", Country = "Turkey", Currency = Currency.TRY }
        );
    }
}
