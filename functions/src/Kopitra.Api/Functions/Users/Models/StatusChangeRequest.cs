namespace Kopitra.Api.Functions.Users.Models;

public class StatusChangeRequest
{
 public bool IsActive { get; set; }
 public string? Reason { get; set; }
 public string? Memo { get; set; }
}
