namespace Tiketin.Web.Infrastructure;

/// <summary>
/// Accepts both keyword connection strings (Npgsql native) and postgres:// URIs
/// (the format Neon, Railway, Render, and Heroku hand out by default), so a
/// pasted URI never crashes the app at startup.
/// </summary>
public static class ConnectionStringNormalizer
{
    public static string? Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) ||
            (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
             !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var parts = new List<string>
        {
            $"Host={uri.Host}",
            $"Port={(uri.Port > 0 ? uri.Port : 5432)}",
            $"Database={Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))}",
            $"Username={Uri.UnescapeDataString(userInfo[0])}"
        };

        if (userInfo.Length > 1)
        {
            parts.Add($"Password={Uri.UnescapeDataString(userInfo[1])}");
        }

        // Carry over query options (?sslmode=require etc.) in keyword form.
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
            parts.Add(Uri.UnescapeDataString(kv[0]).ToLowerInvariant() switch
            {
                "sslmode" => $"SSL Mode={value}",
                "channel_binding" => $"Channel Binding={value}",
                var key => $"{key}={value}"
            });
        }

        return string.Join(";", parts);
    }
}
