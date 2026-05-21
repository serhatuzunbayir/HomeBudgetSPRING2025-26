using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SharedData;

namespace Web.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseApiController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly IReadOnlyDictionary<string, MethodInfo> RepositoryMethods =
        typeof(IDatabaseRepository)
            .GetMethods()
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

    private readonly DatabaseRepository _database;

    public DatabaseApiController(DatabaseRepository database)
    {
        _database = database;
    }

    [HttpPost("{methodName}")]
    public IActionResult Invoke(string methodName, RepositoryCallRequest request)
    {
        if (!RepositoryMethods.TryGetValue(methodName, out var method))
        {
            return NotFound(new RepositoryErrorResponse { Message = $"Unknown database operation: {methodName}." });
        }

        var parameters = method.GetParameters();
        if (request.Arguments.Length > parameters.Length)
        {
            return BadRequest(new RepositoryErrorResponse { Message = $"Too many arguments for {methodName}." });
        }

        var arguments = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            if (index >= request.Arguments.Length)
            {
                if (!parameters[index].HasDefaultValue)
                {
                    return BadRequest(new RepositoryErrorResponse { Message = $"Missing argument {parameters[index].Name} for {methodName}." });
                }

                arguments[index] = parameters[index].DefaultValue;
                continue;
            }

            arguments[index] = DeserializeArgument(request.Arguments[index], parameters[index].ParameterType);
        }

        try
        {
            var value = method.Invoke(_database, arguments);
            return method.ReturnType == typeof(void)
                ? NoContent()
                : new JsonResult(value, JsonOptions);
        }
        catch (TargetInvocationException ex)
        {
            return BadRequest(new RepositoryErrorResponse { Message = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new RepositoryErrorResponse { Message = ex.Message });
        }
    }

    private static object? DeserializeArgument(JsonElement argument, Type parameterType)
    {
        if (argument.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return argument.Deserialize(parameterType, JsonOptions);
    }
}
