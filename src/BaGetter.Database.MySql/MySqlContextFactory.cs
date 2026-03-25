using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaGetter.Database.MySql;

/// <summary>Design-time factory used by EF Core migrations tooling only (never runs in production).</summary>
public class MySqlContextFactory : IDesignTimeDbContextFactory<MySqlContext>
{
    public MySqlContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BAGETTER_MYSQL_DESIGN_CONNECTION")
            ?? "Server=localhost;Database=bagetter_design;User=root;Password=;";
        var builder = new DbContextOptionsBuilder<MySqlContext>();
        builder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
        return new MySqlContext(builder.Options);
    }
}
