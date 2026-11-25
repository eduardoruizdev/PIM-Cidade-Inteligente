namespace SantosConecta.API;

/// <summary>Loads the ignored root .env for the backend only. Existing process variables win.</summary>
public static class RootDotEnv
{
    public static void Load()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (!File.Exists(path)) continue;
                foreach (var raw in File.ReadLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith('#')) continue;
                    var split = line.IndexOf('=');
                    if (split <= 0) continue;
                    var name = line[..split].Trim();
                    var value = line[(split + 1)..].Trim();
                    if (name.Length == 0 || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_')) continue;
                    if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))) value = value[1..^1];
                    if (value.Length == 0) continue;
                    if (Environment.GetEnvironmentVariable(name) is null) Environment.SetEnvironmentVariable(name, value);
                }
                return;
            }
        }
    }
}
