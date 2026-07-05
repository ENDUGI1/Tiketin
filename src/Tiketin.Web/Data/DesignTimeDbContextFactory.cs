using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tiketin.Web.Data;

/// <summary>Used by `dotnet ef` at design time; never at runtime.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=tiketin;Username=tiketin;Password=tiketin")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
