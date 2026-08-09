namespace SupportRoom.Domain.Configuration;

/// <summary>
/// Minimal .env loader for Development (and for test projects loading the same real credentials
/// `dotnet run` uses locally). Deliberately tiny (no external package): reads KEY=VALUE lines and
/// sets them as process environment variables, so the existing
/// Environment.GetEnvironmentVariable(...) readers in ExternalServiceEnv/ProviderSelectionReader
/// keep working unchanged. A variable already present in the environment (set by the shell or
/// launchSettings.json) is never overridden - the .env is only a fallback.
/// </summary>
public static class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // Strip one layer of matching surrounding quotes. Escaped sequences inside (e.g. the
            // \n in a single-line PEM private key) are left intact - the consumer un-escapes them.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
