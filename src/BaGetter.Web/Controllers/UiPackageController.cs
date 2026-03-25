using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Core.Statistics;
using BaGetter.Web.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace BaGetter.Web;

[ApiController]
[Route("api/ui")]
[Authorize(AuthenticationSchemes = AuthenticationConstants.AllSchemes, Policy = AuthenticationConstants.PolicyCanRead)]
public class UiPackageController : ControllerBase
{
    private readonly IPackageService _packages;
    private readonly IPackageContentService _content;
    private readonly ISearchService _search;
    private readonly IUrlGenerator _url;
    private readonly IStatisticsService _statistics;
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ApplicationVersion _version;

    public UiPackageController(
        IPackageService packages,
        IPackageContentService content,
        ISearchService search,
        IUrlGenerator url,
        IStatisticsService statistics,
        IOptions<BaGetterOptions> options,
        ApplicationVersion version)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(version);
        _packages = packages;
        _content = content;
        _search = search;
        _url = url;
        _statistics = statistics;
        _options = options;
        _version = version;
    }

    [HttpGet("packages/{id}")]
    public Task<IActionResult> GetPackage(string id, CancellationToken cancellationToken)
    {
        return GetPackageDetail(id, null, cancellationToken);
    }

    [HttpGet("packages/{id}/versions/{version}")]
    public Task<IActionResult> GetPackageVersion(string id, string version, CancellationToken cancellationToken)
    {
        return GetPackageDetail(id, version, cancellationToken);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var opts = _options.Value;
        if (!(opts.Statistics?.EnableStatisticsPage ?? true))
        {
            return NotFound();
        }

        var packagesTotal = await _statistics.GetPackagesTotalAmount();
        var versionsTotal = await _statistics.GetVersionsTotalAmount();
        var services = (opts.Statistics?.ListConfiguredServices ?? false)
            ? _statistics.GetKnownServices().ToArray()
            : Array.Empty<string>();

        return Ok(new
        {
            appVersion = _version.InformationalVersion ?? _version.Version,
            packagesTotal,
            versionsTotal,
            services,
        });
    }

    private async Task<IActionResult> GetPackageDetail(string id, string versionString, CancellationToken cancellationToken)
    {
        var packages = await _packages.FindPackagesAsync(id, cancellationToken);
        var listedPackages = packages.Where(p => p.Listed).ToList();

        Package package = null;

        if (NuGetVersion.TryParse(versionString, out var requestedVersion))
        {
            package = packages.SingleOrDefault(p => p.Version == requestedVersion);
        }

        package ??= listedPackages.OrderByDescending(p => p.Version).FirstOrDefault();

        if (package == null)
        {
            return NotFound(new { error = $"Package '{id}' not found." });
        }

        var packageVersion = package.Version;
        var dependents = await _search.FindDependentsAsync(package.Id, cancellationToken);

        string readme = null;
        if (package.HasReadme)
        {
            readme = await GetReadmeMarkdownOrNull(package.Id, packageVersion, cancellationToken);
        }

        var iconUrl = package.HasEmbeddedIcon
            ? _url.GetPackageIconDownloadUrl(package.Id, packageVersion)
            : package.IconUrlString;

        return Ok(new
        {
            id = package.Id,
            version = packageVersion.ToNormalizedString(),
            description = package.Description,
            authors = package.Authors ?? Array.Empty<string>(),
            iconUrl = string.IsNullOrEmpty(iconUrl) ? null : iconUrl,
            licenseUrl = string.IsNullOrEmpty(package.LicenseUrlString) ? null : package.LicenseUrlString,
            projectUrl = string.IsNullOrEmpty(package.ProjectUrlString) ? null : package.ProjectUrlString,
            repositoryUrl = string.IsNullOrEmpty(package.RepositoryUrlString) ? null : package.RepositoryUrlString,
            repositoryType = package.RepositoryType,
            tags = package.Tags ?? Array.Empty<string>(),
            totalDownloads = packages.Sum(p => p.Downloads),
            downloads = package.Downloads,
            published = package.Published,
            lastUpdated = packages.Max(p => p.Published),
            listed = package.Listed,
            hasReadme = package.HasReadme,
            readme,
            releaseNotes = package.ReleaseNotes,
            packageDownloadUrl = _url.GetPackageDownloadUrl(package.Id, packageVersion),
            deprecation = MapDeprecation(package.Deprecation),
            dependencyGroups = ToDependencyGroups(package),
            versions = ToVersions(listedPackages, packageVersion),
            packageTypes = package.PackageTypes?.Select(t => new { name = t.Name }).ToArray(),
            usedBy = dependents.Data?.Select(d => new
            {
                id = d.Id,
                description = d.Description,
                totalDownloads = d.TotalDownloads,
            }).ToArray(),
        });
    }

    private async Task<string> GetReadmeMarkdownOrNull(string packageId, NuGetVersion packageVersion, CancellationToken cancellationToken)
    {
        await using var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(packageId, packageVersion, cancellationToken);
        if (readmeStream == null) return null;

        using var reader = new StreamReader(readmeStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static object MapDeprecation(Protocol.Models.PackageDeprecation deprecation)
    {
        if (deprecation == null) return null;
        return new
        {
            reasons = deprecation.Reasons ?? new List<string>(),
            message = deprecation.Message,
            alternatePackage = deprecation.AlternatePackage != null
                ? new { id = deprecation.AlternatePackage.Id, range = deprecation.AlternatePackage.Range }
                : null,
        };
    }

    private static List<object> ToDependencyGroups(Package package)
    {
        return package.Dependencies
            .GroupBy(d => d.TargetFramework)
            .Select(group => (object)new
            {
                name = PrettifyTargetFramework(group.Key),
                dependencies = group
                    .Where(d => d.Id != null)
                    .Select(d => new
                    {
                        packageId = d.Id,
                        versionSpec = d.VersionRange != null
                            ? VersionRange.Parse(d.VersionRange).PrettyPrint()
                            : string.Empty,
                    })
                    .ToList(),
            })
            .ToList();
    }

    private static string PrettifyTargetFramework(string targetFramework)
    {
        if (targetFramework == null) return "All Frameworks";

        NuGetFramework framework;
        try
        {
            framework = NuGetFramework.Parse(targetFramework);
        }
        catch (Exception)
        {
            return targetFramework;
        }

        string frameworkName;
        if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp, StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = framework.Version.Major >= 5 ? ".NET" : ".NET Core";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetStandard, StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Standard";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.Net, StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Framework";
        }
        else
        {
            frameworkName = framework.Framework;
        }

        var frameworkVersion = framework.Version.Build == 0
            ? framework.Version.ToString(2)
            : framework.Version.ToString(3);

        return $"{frameworkName} {frameworkVersion}";
    }

    private static List<object> ToVersions(IReadOnlyList<Package> packages, NuGetVersion selectedVersion)
    {
        return packages
            .OrderByDescending(p => p.Version)
            .Select(p => (object)new
            {
                version = p.Version.ToNormalizedString(),
                downloads = p.Downloads,
                selected = p.Version == selectedVersion,
                lastUpdated = p.Published,
            })
            .ToList();
    }
}
