using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;
using Microsoft.EntityFrameworkCore;

namespace GSTGuru.Web.Data;

/// <summary>
/// Creates demo data on first run so every screen (dashboard, reports, AI
/// assistant) has something meaningful to show. Safe to call repeatedly.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(GstDbContext db, IGstCalculator calculator, CancellationToken ct = default)
    {
        await SeedBusinessProfileAsync(db, ct);

        var products = await SeedProductsAsync(db, ct);
        var parties = await SeedPartiesAsync(db, ct);

        if (!await db.Invoices.AnyAsync(ct))
        {
            await SeedInvoicesAsync(db, calculator, products, parties, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedBusinessProfileAsync(GstDbContext db, CancellationToken ct)
    {
        if (await db.BusinessProfiles.AnyAsync(ct))
        {
            return;
        }

        db.BusinessProfiles.Add(new BusinessProfile
        {
            LegalName = "GST Guru Trading Private Limited",
            TradeName = "GST Guru",
            Gstin = "27ABCDE1234F1Z5",
            StateCode = "27",
            StateName = "Maharashtra",
            Address = "402, Trade Centre, Andheri East, Mumbai, Maharashtra 400069",
            Email = "billing@gstguru.example",
            Phone = "+91 22 4000 1234",
            InvoicePrefix = "INV",
            NextInvoiceNumber = 1,
        });
    }

    private static async Task<Dictionary<string, Product>> SeedProductsAsync(GstDbContext db, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(ct))
        {
            return await db.Products.ToDictionaryAsync(p => p.Name, ct);
        }

        var products = new List<Product>
        {
            New("Full Cream Milk 1L", "0401", "LTR", 62m, 0m),
            New("Assam Tea 500g", "0902", "PKT", 210m, 5m),
            New("Basmati Rice 5kg", "1006", "BAG", 640m, 5m),
            New("Glucose Biscuits 200g", "1905", "PKT", 40m, 18m),
            New("Toilet Soap 125g", "3401", "NOS", 55m, 18m),
            New("Paracetamol Tablets 10s", "3004", "STRP", 32m, 12m),
            New("Cotton T-Shirt", "6109", "NOS", 399m, 5m),
            New("Office Laptop 14\"", "8471", "NOS", 54990m, 18m),
            New("Smartphone 128GB", "8517", "NOS", 18999m, 18m),
            New("Ergonomic Office Chair", "9403", "NOS", 7499m, 18m),
            New("Portland Cement 50kg", "2523", "BAG", 420m, 28m),
            New("Compact Hatchback Car", "8703", "NOS", 650000m, 28m, 15m),
            New("Printed Textbook", "4901", "NOS", 350m, 0m),
            New("Detergent Powder 1kg", "3402", "PKT", 120m, 18m),
            New("Wireless Earbuds", "8517", "NOS", 2499m, 18m),
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);
        return products.ToDictionary(p => p.Name);

        static Product New(string name, string hsn, string unit, decimal price, decimal rate, decimal cess = 0m)
            => new()
            {
                Name = name,
                HsnCode = hsn,
                Unit = unit,
                UnitPrice = price,
                DefaultGstRate = rate,
                DefaultCessRate = cess,
            };
    }

    private static async Task<List<Party>> SeedPartiesAsync(GstDbContext db, CancellationToken ct)
    {
        if (await db.Parties.AnyAsync(ct))
        {
            return await db.Parties.ToListAsync(ct);
        }

        var parties = new List<Party>
        {
            NewParty("Aarav Enterprises", "27AACCA1234B1Z5", PartyType.Customer, "27", "Maharashtra", true),
            NewParty("Bengaluru Traders", "29AACCB5678C1Z4", PartyType.Customer, "29", "Karnataka", true),
            NewParty("Delhi Distributors", "07AACCD9012D1Z3", PartyType.Customer, "07", "Delhi", true),
            NewParty("Chennai Supplies", "33AACCE3456E1Z2", PartyType.Customer, "33", "Tamil Nadu", true),
            NewParty("Sharma Retail Store", null, PartyType.Customer, "27", "Maharashtra", false),
            NewParty("Kiran General Stores", null, PartyType.Customer, "29", "Karnataka", false),
            NewParty("Gujarat Packaging Co", "24AACCF7890G1Z1", PartyType.Supplier, "24", "Gujarat", true),
        };

        db.Parties.AddRange(parties);
        await db.SaveChangesAsync(ct);
        return parties;

        static Party NewParty(string name, string? gstin, PartyType type, string code, string state, bool active)
            => new()
            {
                Name = name,
                Gstin = gstin,
                PartyType = type,
                StateCode = code,
                StateName = state,
                IsActive = active,
                Address = $"{state}, India",
            };
    }

    private static async Task SeedInvoicesAsync(
        GstDbContext db,
        IGstCalculator calculator,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyList<Party> parties,
        CancellationToken ct)
    {
        var profile = await db.BusinessProfiles.FirstAsync(ct);
        var customers = parties.Where(p => p.PartyType == PartyType.Customer).ToList();

        // A compact script of lines per invoice keeps the seed deterministic.
        var scripts = new (string Product, decimal Qty)[][]
        {
            new[] { ("Full Cream Milk 1L", 40m), ("Assam Tea 500g", 10m) },
            new[] { ("Office Laptop 14\"", 2m), ("Wireless Earbuds", 5m) },
            new[] { ("Portland Cement 50kg", 80m) },
            new[] { ("Basmati Rice 5kg", 25m), ("Paracetamol Tablets 10s", 30m) },
            new[] { ("Smartphone 128GB", 3m) },
            new[] { ("Ergonomic Office Chair", 6m) },
            new[] { ("Compact Hatchback Car", 1m) },
            new[] { ("Cotton T-Shirt", 60m), ("Detergent Powder 1kg", 20m) },
            new[] { ("Printed Textbook", 100m), ("Glucose Biscuits 200g", 50m) },
            new[] { ("Toilet Soap 125g", 120m), ("Assam Tea 500g", 15m) },
            new[] { ("Office Laptop 14\"", 4m) },
            new[] { ("Basmati Rice 5kg", 40m), ("Full Cream Milk 1L", 60m) },
        };

        var startMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-4);
        var number = 1;

        for (var i = 0; i < scripts.Length; i++)
        {
            var party = customers[i % customers.Count];
            var lineInputs = scripts[i]
                .Select(s =>
                {
                    var product = products[s.Product];
                    return new GstLineInput
                    {
                        Description = product.Name,
                        HsnCode = product.HsnCode,
                        Unit = product.Unit,
                        Quantity = s.Qty,
                        UnitPrice = product.UnitPrice,
                        DiscountPercent = 0m,
                        GstRate = product.DefaultGstRate,
                        CessRate = product.DefaultCessRate,
                    };
                })
                .ToList();

            // Spread invoices across the last five calendar months.
            var monthOffset = i % 5;
            var date = startMonth.AddMonths(monthOffset).AddDays(3 + ((i * 4) % 24));
            if (date > DateTime.UtcNow)
            {
                date = DateTime.UtcNow.AddDays(-((i % 10) + 1));
            }

            var supplyType = calculator.ResolveSupplyType(profile.StateCode, party.StateCode);
            var breakdown = calculator.Calculate(lineInputs, supplyType, SupplyCategory.Taxable);

            var invoice = new Invoice
            {
                InvoiceNumber = $"{profile.InvoicePrefix}-{number:D4}",
                InvoiceDate = date,
                PartyId = party.Id,
                PlaceOfSupplyStateCode = party.StateCode,
                SellerStateCode = profile.StateCode,
                SupplyType = supplyType,
                SupplyCategory = SupplyCategory.Taxable,
                ReverseCharge = false,
                Status = InvoiceStatus.Issued,
                IssuedAt = date,
                Notes = "Seeded demo invoice",
                TaxableValue = breakdown.TaxableValue,
                Cgst = breakdown.Cgst,
                Sgst = breakdown.Sgst,
                Igst = breakdown.Igst,
                Cess = breakdown.Cess,
                RoundOff = breakdown.RoundOff,
                GrandTotal = breakdown.GrandTotal,
                CreatedAt = date,
                Lines = lineInputs.Select((input, index) =>
                {
                    var result = breakdown.Lines[index];
                    return new InvoiceLine
                    {
                        ProductId = products[input.Description].Id,
                        Description = result.Description,
                        HsnCode = result.HsnCode,
                        Unit = result.Unit,
                        Quantity = result.Quantity,
                        UnitPrice = result.UnitPrice,
                        DiscountPercent = result.DiscountPercent,
                        TaxableValue = result.TaxableValue,
                        GstRate = result.GstRate,
                        CessRate = result.CessRate,
                        Cgst = result.Cgst,
                        Sgst = result.Sgst,
                        Igst = result.Igst,
                        Cess = result.Cess,
                        LineTotal = result.LineTotal,
                    };
                }).ToList(),
            };

            db.Invoices.Add(invoice);
            number++;
        }

        profile.NextInvoiceNumber = number;
    }
}
