using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaGetter.Database.Sqlite;

/// <summary>Design-time factory used by EF Core migrations tooling.</summary>
public class SqliteContextFactory : IDesignTimeDbContextFactory<SqliteContext>
{
    public SqliteContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SqliteContext>();
        builder.UseSqlite("Data Source=bagetter_design.db");
        return new SqliteContext(builder.Options);
    }
}
