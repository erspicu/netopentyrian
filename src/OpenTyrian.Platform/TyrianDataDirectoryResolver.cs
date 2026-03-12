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
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        if (!string.IsNullOrWhiteSpace(preferredDirectory))
        {
            yield return preferredDirectory ?? string.Empty;
        }

        yield return Path.Combine(baseDirectory, "..", "..", "..", "..", "tyrian21");
        yield return Path.Combine(baseDirectory, "tyrian21");
        yield return Path.Combine(baseDirectory, "data");
        yield return baseDirectory;
        yield return Directory.GetCurrentDirectory();
    }
}
