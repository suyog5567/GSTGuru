using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GSTGuru.Web.Data;

/// <summary>
/// Lets the EF Core CLI tooling create the <see cref="GstDbContext"/> without
/// booting the whole web host, which keeps migrations fast and predictable.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GstDbContext>
{
    public GstDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=GstGuru;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<GstDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new GstDbContext(options);
    }
}
