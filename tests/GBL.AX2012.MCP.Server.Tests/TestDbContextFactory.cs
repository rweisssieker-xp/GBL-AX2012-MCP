using Microsoft.EntityFrameworkCore;
using GBL.AX2012.MCP.Audit.Data;

namespace GBL.AX2012.MCP.Server.Tests;

public class TestDbContextFactory : IDbContextFactory<WebhookDbContext>
{
    private readonly DbContextOptions<WebhookDbContext> _options;
    
    public TestDbContextFactory(WebhookDbContext sharedContext, string databaseName)
    {
        // Create new options with the same database name to share the in-memory database
        _options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }
    
    public WebhookDbContext CreateDbContext()
    {
        return new WebhookDbContext(_options);
    }
    
    public Task<WebhookDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
