namespace Tiketin.Tests;

/// <summary>TimeProvider frozen at a fixed instant for deterministic assertions.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
/// Skips integration tests on machines without Docker (Testcontainers needs it).
/// CI always has Docker, so these tests are never skipped there.
/// </summary>
public sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!DockerDetector.IsAvailable)
        {
            Skip = "Docker is not available on this machine.";
        }
    }
}

public static class DockerDetector
{
    public static bool IsAvailable { get; } = Detect();

    private static bool Detect()
    {
        if (Environment.GetEnvironmentVariable("DOCKER_HOST") is not null)
        {
            return true;
        }

        return OperatingSystem.IsWindows()
            ? File.Exists(@"\\.\pipe\docker_engine")
            : File.Exists("/var/run/docker.sock");
    }
}
