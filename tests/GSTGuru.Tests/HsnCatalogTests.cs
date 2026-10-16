using GSTGuru.Web.Domain;
using GSTGuru.Web.Gst;
using Xunit;

namespace GSTGuru.Tests;

public class HsnCatalogTests
{
    [Theory]
    [InlineData("laptop", "8471")]
    [InlineData("wireless earbuds", "8518")]
    [InlineData("What is the GST rate for restaurant service?", "9963")]
    [InlineData("packaged drinking water", "2201")]
    [InlineData("passenger car", "8703")]
    public void Guess_Finds_The_Expected_Entry(string description, string expectedCode)
    {
        var entry = HsnCatalog.Guess(description);
        Assert.NotNull(entry);
        Assert.Equal(expectedCode, entry!.HsnCode);
    }

    [Fact]
    public void Guess_Ignores_Filler_Words()
    {
        // "the"/"for"/"rate"/"gst" must not match the word "other" in the dairy entry.
        var entry = HsnCatalog.Guess("what is the gst rate for");
        Assert.Null(entry);
    }
}

public class GstRoundingTests
{
    private readonly IGstCalculator _calc = new GstCalculator();

    [Fact]
    public void Grand_Total_Is_Rounded_To_Nearest_Rupee()
    {
        var lines = new List<GstLineInput>
        {
            new() { Description = "Tea", Quantity = 3, UnitPrice = 210, GstRate = 5 }
        };
        // Taxable 630, tax 5% = 31.50 -> 661.50 -> rounds up to 662.
        var result = _calc.Calculate(lines, SupplyType.IntraState);
        Assert.Equal(630m, result.TaxableValue);
        Assert.Equal(15.75m, result.Cgst);
        Assert.Equal(15.75m, result.Sgst);
        Assert.Equal(662m, result.GrandTotal);
        Assert.Equal(0.50m, result.RoundOff);
    }
}
