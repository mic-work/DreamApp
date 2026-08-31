using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DreamApp.Api.Data;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` build an
/// AppDbContext directly, without running the rest of Program.cs (JWT
/// validation, CORS setup, etc.). Design-time tooling only needs a
/// connection string, never the app's runtime secrets — coupling the two
/// made `dotnet ef` fail whenever Jwt:Key wasn't configured yet.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? "App_Data/dreamapp.db";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new AppDbContext(optionsBuilder.Options);
    }
}
