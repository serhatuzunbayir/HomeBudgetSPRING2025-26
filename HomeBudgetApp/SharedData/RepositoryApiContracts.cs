using System.Text.Json;

namespace SharedData;

public sealed class RepositoryCallRequest
{
    public JsonElement[] Arguments { get; set; } = [];
}

public sealed class RepositoryErrorResponse
{
    public string Message { get; set; } = "";
}
