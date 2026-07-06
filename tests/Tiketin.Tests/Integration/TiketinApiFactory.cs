using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Tiketin.Tests.Integration;

/// <summary>
/// Boots the real app against a disposable Postgres 16 container. The app runs
/// its own migrations and seeds roles, demo users, and categories on startup.
/// </summary>
public sealed class TiketinApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tiketin_test")
        .WithUsername("tiketin")
        .WithPassword("tiketin")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["SkipDemoSeed"] = "true",
                ["Jwt:SigningKey"] = "integration-test-signing-key-0123456789abcdef"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>HttpClient authenticated as the given seeded user via the JWT login endpoint.</summary>
    public async Task<HttpClient> ClientForAsync(string email, string password = "Tiketin123!")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginEnvelope>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Data.AccessToken);
        return client;
    }

    private sealed record LoginEnvelope(LoginData Data);

    private sealed record LoginData(string AccessToken, int ExpiresInSeconds, string RefreshToken);
}
