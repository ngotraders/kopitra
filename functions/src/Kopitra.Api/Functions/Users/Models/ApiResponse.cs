namespace Kopitra.Api.Functions.Users.Models;

public class ApiResponse
{
    public string Message { get; }
    public ApiResponse(string message)
    {
        Message = message;
    }
}
