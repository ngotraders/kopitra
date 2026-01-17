namespace Kopitra.Api.Functions.Accounts.Models;

/// <summary>
/// Standard API response model for errors and messages
/// </summary>
public class ApiResponse
{
    public string Message { get; }

    public ApiResponse(string message)
    {
        Message = message;
    }
}
