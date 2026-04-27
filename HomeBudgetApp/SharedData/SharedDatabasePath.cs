namespace SharedData;

public static class SharedDatabasePath
{
    private const string DatabaseFileName = "AppDb.db";

    public static string GetPath()
    {
        string? sharedDataDirectory = FindSharedDataDirectory(AppContext.BaseDirectory)
            ?? FindSharedDataDirectory(Directory.GetCurrentDirectory());

        if (sharedDataDirectory is not null)
        {
            return Path.Combine(sharedDataDirectory, DatabaseFileName);
        }

        string fallbackDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeBudgetApp");

        Directory.CreateDirectory(fallbackDirectory);
        return Path.Combine(fallbackDirectory, DatabaseFileName);
    }

    private static string? FindSharedDataDirectory(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "SharedData");
            string projectFile = Path.Combine(candidate, "SharedData.csproj");

            if (File.Exists(projectFile))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
