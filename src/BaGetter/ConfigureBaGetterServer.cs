using System.Linq;
using System.Net;
using BaGetter.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace BaGetter;

public class ConfigureBaGetterServer
    : IConfigureOptions<CorsOptions>
    , IConfigureOptions<FormOptions>
    , IConfigureOptions<ForwardedHeadersOptions>
    , IConfigureOptions<IISServerOptions>
{
    public const string CorsPolicy = "AllowAll";
    private readonly BaGetterOptions _baGetterOptions;

    public ConfigureBaGetterServer(IOptions<BaGetterOptions> baGetterOptions)
    {
        _baGetterOptions = baGetterOptions.Value;
    }


    public void Configure(CorsOptions options)
    {
        options.AddPolicy(
            CorsPolicy,
            builder =>
            {
                var origins = _baGetterOptions.AllowedCorsOrigins;
                if (origins is { Length: > 0 })
                {
                    builder
                        .WithOrigins(origins)
                        .WithMethods("GET", "HEAD", "OPTIONS")
                        .WithHeaders("Accept", "Accept-Language", "Content-Language", "Content-Type");
                }
                // If no origins are configured, no CORS policy is applied.
                // NuGet CLI and dotnet restore are not browser requests and are unaffected.
            });
    }

    public void Configure(FormOptions options)
    {
        // Allow packages up to ~8GiB in size
        options.MultipartBodyLengthLimit = (long) _baGetterOptions.MaxPackageSizeGiB * int.MaxValue / 2;
    }

    public void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

        var trustedProxies = _baGetterOptions.TrustedProxies;
        if (trustedProxies is { Length: > 0 })
        {
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in trustedProxies.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        }
        // If no trusted proxies are configured, ASP.NET Core defaults (loopback only)
        // are preserved — do not clear KnownNetworks/KnownProxies here.
        // Configure TrustedProxies in appsettings.json to match your reverse proxy IP.
    }

    public void Configure(IISServerOptions options)
    {
        options.MaxRequestBodySize = (long)_baGetterOptions.MaxPackageSizeGiB * int.MaxValue / 2;
    }
}
