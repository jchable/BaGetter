using System;
using BaGetter.Core;
using BaGetter.Web.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BaGetter.Web;

[ApiController]
[Route("api/ui")]
public class UiConfigController : ControllerBase
{
    private readonly IUrlGenerator _urlGenerator;
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ApplicationVersion _version;

    public UiConfigController(
        IUrlGenerator urlGenerator,
        IOptions<BaGetterOptions> options,
        ApplicationVersion version)
    {
        ArgumentNullException.ThrowIfNull(urlGenerator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(version);
        _urlGenerator = urlGenerator;
        _options = options;
        _version = version;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig()
    {
        var opts = _options.Value;
        return Ok(new
        {
            serviceIndexUrl = _urlGenerator.GetServiceIndexUrl(),
            publishUrl = _urlGenerator.GetPackagePublishResourceUrl(),
            symbolPublishUrl = _urlGenerator.GetSymbolPublishResourceUrl(),
            appVersion = _version.Version,
            statisticsEnabled = opts.Statistics?.EnableStatisticsPage ?? true,
        });
    }
}
