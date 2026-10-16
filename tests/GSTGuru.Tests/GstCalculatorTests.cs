using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;
using Xunit;

namespace GSTGuru.Tests;

public class GstCalculatorTests
{
    private readonly IGstCalculator _calc = new GstCalculator();

    [Fact]
    public void IntraState_Splits_Gst_Equally()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Test", Quantity = 1, UnitPrice = 1000, GstRate = 18, CessRate = 0 }
        };
        var result = _calc.Calculate(lines, SupplyType.IntraState);
        Assert.Equal(1000m, result.TaxableValue);
        Assert.Equal(90m, result.Cgst);
        Assert.Equal(90m, result.Sgst);
        Assert.Equal(0m, result.Igst);
        Assert.Equal(180m, result.TotalTax);
        Assert.Equal(1180m, result.GrandTotal);
    }

    [Fact]
    public void InterState_Charges_Full_Igst()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Test", Quantity = 1, UnitPrice = 5000, GstRate = 18, CessRate = 0 }
        };
        var result = _calc.Calculate(lines, SupplyType.InterState);
        Assert.Equal(5000m, result.TaxableValue);
        Assert.Equal(0m, result.Cgst);
        Assert.Equal(0m, result.Sgst);
        Assert.Equal(900m, result.Igst);
        Assert.Equal(5900m, result.GrandTotal);
    }

    [Fact]
    public void Discount_Reduces_Taxable()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Test", Quantity = 2, UnitPrice = 1000, DiscountPercent = 10, GstRate = 5 }
        };
        // Gross 2000, discount 200, taxable 1800, tax 90 (intra: 45+45)
        var result = _calc.Calculate(lines, SupplyType.IntraState);
        Assert.Equal(1800m, result.TaxableValue);
        Assert.Equal(90m, result.TotalTax);
        Assert.Equal(1890m, result.GrandTotal);
    }

    [Fact]
    public void Cess_Is_Applied_On_Taxable()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Car", Quantity = 1, UnitPrice = 100000, GstRate = 28, CessRate = 15 }
        };
        var result = _calc.Calculate(lines, SupplyType.IntraState);
        Assert.Equal(100000m, result.TaxableValue);
        Assert.Equal(14000m, result.Cgst);
        Assert.Equal(14000m, result.Sgst);
        Assert.Equal(15000m, result.Cess);
        Assert.Equal(143000m, result.GrandTotal);
    }

    [Fact]
    public void ZeroRated_Has_No_Tax()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Export", Quantity = 1, UnitPrice = 10000, GstRate = 18 }
        };
        var result = _calc.Calculate(lines, SupplyType.InterState, SupplyCategory.ZeroRated);
        Assert.Equal(10000m, result.TaxableValue);
        Assert.Equal(0m, result.TotalTax);
        Assert.Equal(10000m, result.GrandTotal);
    }
}
