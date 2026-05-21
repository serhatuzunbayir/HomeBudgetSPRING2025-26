using SharedData;

namespace MAUIdesktop;

public static class AppDatabase
{
    private const string DefaultApiBaseAddress = "http://localhost:5225";
    private static IDatabaseRepository? _database;

    public static IDatabaseRepository Instance
    {
        get
        {
            if (_database is not null)
            {
                return _database;
            }

            var apiBaseAddress = Environment.GetEnvironmentVariable("HOMEBUDGET_API_BASE_URL");
            if (string.IsNullOrWhiteSpace(apiBaseAddress))
            {
                apiBaseAddress = DefaultApiBaseAddress;
            }

            _database = new HttpDatabaseRepository(new HttpClient
            {
                BaseAddress = new Uri(NormalizeBaseAddress(apiBaseAddress))
            });
            return _database;
        }
    }

    private static string NormalizeBaseAddress(string baseAddress)
    {
        return baseAddress.EndsWith('/') ? baseAddress : $"{baseAddress}/";
    }
}
