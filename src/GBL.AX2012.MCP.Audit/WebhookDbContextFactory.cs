using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GBL.AX2012.MCP.Audit;

/// <summary>
/// Design-time factory for WebhookDbContext migrations
/// </summary>
public class WebhookDbContextFactory : IDesignTimeDbContextFactory<Data.WebhookDbContext>
{
    public Data.WebhookDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "GBL.AX2012.MCP.Server"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("AuditDb")
            ?? "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<Data.WebhookDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new Data.WebhookDbContext(optionsBuilder.Options);
    }
}

