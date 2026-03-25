using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaGetter.Database.PostgreSql;

/// <summary>Design-time factory used by EF Core migrations tooling.</summary>
public class PostgreSqlContextFactory : IDesignTimeDbContextFactory<PostgreSqlContext>
{
    public PostgreSqlContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PostgreSqlContext>();
        builder.UseNpgsql("Host=localhost;Database=bagetter_design;Username=postgres");
        return new PostgreSqlContext(builder.Options);
    }
}
