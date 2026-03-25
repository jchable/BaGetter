namespace BaGetter.Core;

public static class AuditAction
{
    public const string PackagePushed = "PackagePushed";
    public const string PackageDeleted = "PackageDeleted";
    public const string PackageUnlisted = "PackageUnlisted";
    public const string PackageRelisted = "PackageRelisted";
    public const string LoginSuccess = "LoginSuccess";
    public const string LoginFailure = "LoginFailure";
    public const string RoleChanged = "RoleChanged";
    public const string UserDeleted = "UserDeleted";
    public const string InvitationCreated = "InvitationCreated";
    public const string InvitationAccepted = "InvitationAccepted";
    public const string InvitationRevoked = "InvitationRevoked";
    public const string ApiKeyCreated = "ApiKeyCreated";
    public const string ApiKeyRevoked = "ApiKeyRevoked";
}
