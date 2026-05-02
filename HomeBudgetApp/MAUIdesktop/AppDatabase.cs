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

            _database = new DatabaseRepository(SharedDatabasePath.GetPath());
         
            return _database;
        }
    }
}
