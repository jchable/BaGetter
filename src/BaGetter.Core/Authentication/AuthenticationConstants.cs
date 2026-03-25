namespace BaGetter.Authentication;

public static class AuthenticationConstants
{
    // Schemes
    public const string ApiKeyScheme = "ApiKey";
    public const string BasicScheme = "Basic";
    public const string CookieScheme = "Identity.Application"; // Identity default cookie name

    // Combined scheme list for controller attributes
    public const string AllSchemes = ApiKeyScheme + "," + BasicScheme + "," + CookieScheme;

    // Policies
    public const string PolicyCanRead = "CanRead";
    public const string PolicyCanPublish = "CanPublish";
    public const string PolicyCanAdmin = "CanAdmin";
}
