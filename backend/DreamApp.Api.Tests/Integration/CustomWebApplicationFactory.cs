using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DreamApp.Api.Tests.Integration;

/// <summary>
/// Spins up the real ASP.NET Core pipeline (real JWT bearer authentication,
/// real EF Core + SQLite) against a throwaway database file, so these tests
/// exercise exactly what runs in production. Fast, isolated logic tests
/// live in Unit/ instead, against FakeAppDbContext.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public readonly string DatabasePath = Path.Combine(Path.GetTempPath(), $"dreamapp-tests-{Guid.NewGuid():N}.db");
    private readonly string _imagesPath = Path.Combine(Path.GetTempPath(), $"dreamapp-tests-uploads-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-only-signing-key-never-used-in-production-0123456789",
                ["Database:Path"] = DatabasePath,
                ["Storage:ImagesPath"] = _imagesPath
            });
        });
    }

    public new void Dispose()
    {
        base.Dispose();
        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }
        if (Directory.Exists(_imagesPath))
        {
            Directory.Delete(_imagesPath, recursive: true);
        }
        GC.SuppressFinalize(this);
    }
}
