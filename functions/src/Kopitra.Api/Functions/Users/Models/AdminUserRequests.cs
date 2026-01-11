namespace Kopitra.Api.Functions.Users.Models;

public class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}

public class UpdatePermissionsRequest
{
    public bool? CanProvide { get; set; }
    public bool? CanSubscribe { get; set; }
    public string? Memo { get; set; }
}

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}
