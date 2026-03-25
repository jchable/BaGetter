namespace BaGetter.Core;

public class ApiKeyValidationResult
{
    public int KeyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Publisher;
    public string TenantId { get; set; } = string.Empty;
}
