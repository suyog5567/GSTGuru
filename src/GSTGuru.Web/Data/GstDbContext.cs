using GSTGuru.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Data;

/// <summary>Entity Framework Core unit of work for GST Guru.</summary>
public class GstDbContext(DbContextOptions<GstDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Party> Parties => Set<Party>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.Property(b => b.LegalName).HasMaxLength(200).IsRequired();
            entity.Property(b => b.TradeName).HasMaxLength(200);
            entity.Property(b => b.Gstin).HasMaxLength(15);
            entity.Property(b => b.StateCode).HasMaxLength(2).IsRequired();
            entity.Property(b => b.StateName).HasMaxLength(100);
            entity.Property(b => b.Address).HasMaxLength(500);
            entity.Property(b => b.Email).HasMaxLength(200);
            entity.Property(b => b.Phone).HasMaxLength(30);
            entity.Property(b => b.InvoicePrefix).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.HsnCode).HasMaxLength(12);
            entity.Property(p => p.Unit).HasMaxLength(10).IsRequired();
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
            entity.Property(p => p.DefaultGstRate).HasPrecision(5, 2);
            entity.Property(p => p.DefaultCessRate).HasPrecision(5, 2);

            // Speeds up catalogue searching and invoice line lookups by code.
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.HsnCode);
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Gstin).HasMaxLength(15);
            entity.Property(p => p.StateCode).HasMaxLength(2).IsRequired();
            entity.Property(p => p.StateName).HasMaxLength(100);
            entity.Property(p => p.Address).HasMaxLength(500);
            entity.Property(p => p.Email).HasMaxLength(200);
            entity.Property(p => p.Phone).HasMaxLength(30);

            // A GSTIN is unique across parties but many B2C parties have none,
            // so use a filtered index that ignores NULLs.
            entity.HasIndex(p => p.Gstin).IsUnique().HasFilter("[Gstin] IS NOT NULL");
            entity.HasIndex(p => p.Name);
            entity.Ignore(p => p.IsRegistered);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(i => i.PlaceOfSupplyStateCode).HasMaxLength(2).IsRequired();
            entity.Property(i => i.SellerStateCode).HasMaxLength(2).IsRequired();
            entity.Property(i => i.Notes).HasMaxLength(1000);

            entity.Property(i => i.TaxableValue).HasPrecision(18, 2);
            entity.Property(i => i.Cgst).HasPrecision(18, 2);
            entity.Property(i => i.Sgst).HasPrecision(18, 2);
            entity.Property(i => i.Igst).HasPrecision(18, 2);
            entity.Property(i => i.Cess).HasPrecision(18, 2);
            entity.Property(i => i.RoundOff).HasPrecision(18, 2);
            entity.Property(i => i.GrandTotal).HasPrecision(18, 2);

            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.HasIndex(i => i.InvoiceDate);
            entity.HasIndex(i => new { i.PartyId, i.InvoiceDate });

            entity.Ignore(i => i.TotalTax);

            entity.HasOne(i => i.Party)
                .WithMany()
                .HasForeignKey(i => i.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(i => i.Lines)
                .WithOne(l => l.Invoice!)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.Property(l => l.Description).HasMaxLength(300).IsRequired();
            entity.Property(l => l.HsnCode).HasMaxLength(12);
            entity.Property(l => l.Unit).HasMaxLength(10).IsRequired();
            entity.Property(l => l.Quantity).HasPrecision(18, 3);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
            entity.Property(l => l.DiscountPercent).HasPrecision(5, 2);
            entity.Property(l => l.TaxableValue).HasPrecision(18, 2);
            entity.Property(l => l.GstRate).HasPrecision(5, 2);
            entity.Property(l => l.CessRate).HasPrecision(5, 2);
            entity.Property(l => l.Cgst).HasPrecision(18, 2);
            entity.Property(l => l.Sgst).HasPrecision(18, 2);
            entity.Property(l => l.Igst).HasPrecision(18, 2);
            entity.Property(l => l.Cess).HasPrecision(18, 2);
            entity.Property(l => l.LineTotal).HasPrecision(18, 2);

            entity.HasIndex(l => l.InvoiceId);
            entity.HasIndex(l => l.HsnCode);

            entity.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
