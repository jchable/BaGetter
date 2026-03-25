using System.Security.Claims;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;

namespace BaGetter.Web.Services;

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(AuthenticationConstants.TenantIdClaim);
    }
}
