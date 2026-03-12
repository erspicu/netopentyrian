namespace OpenTyrian.Platform;

public static class TyrianDataDirectoryResolver
{
    public static string Resolve(string? preferredDirectory = null)
    {
        foreach (string candidate in GetCandidates(preferredDirectory))
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            string fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "tyrian1.lvl")))
            {
                return fullPath;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetCandidates(string? preferredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(preferredDirectory))
        {
            yield return preferredDirectory;
        }

        yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tyrian21");
        yield return Path.Combine(AppContext.BaseDirectory, "tyrian21");
        yield return Path.Combine(AppContext.BaseDirectory, "data");
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }
}
