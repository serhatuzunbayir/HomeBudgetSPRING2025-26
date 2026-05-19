using SharedData;

namespace MAUIdesktop;

public static class AppDatabase
{
    private static DatabaseRepository? _database;

    public static DatabaseRepository Instance
    {
        get
        {
            if (_database is not null)
            {
                return _database;
            }

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "AppDb.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            if (!File.Exists(databasePath))
            {
                TryCopySeedDatabase(databasePath);
            }

            _database = new DatabaseRepository(databasePath);

            return _database;
        }
    }

    private static void TryCopySeedDatabase(string databasePath)
    {
        try
        {
            var seedPath = SharedDatabasePath.GetPath();
            if (File.Exists(seedPath))
            {
                File.Copy(seedPath, databasePath, overwrite: false);
            }
        }
        catch
        {
            // MacCatalyst sandboxing can block source-tree access; the repository creates a fresh DB below.
        }
    }
}
