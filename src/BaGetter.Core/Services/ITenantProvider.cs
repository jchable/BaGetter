namespace BaGetter.Core;

/// <summary>Resolves the current tenant from the request context.</summary>
public interface ITenantProvider
{
    string? GetCurrentTenantId();
}
