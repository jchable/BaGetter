using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Protocol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaGetter.Web;

[Authorize(AuthenticationSchemes = AuthenticationConstants.AllSchemes, Policy = AuthenticationConstants.PolicyCanRead)]
public class SearchController : Controller
{
    private const int MaxTake = 100;
    private const int MaxSkip = 10_000;

    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        _searchService = searchService;
    }

    public async Task<ActionResult<SearchResponse>> SearchAsync(
        [FromQuery(Name = "q")] string query = null,
        [FromQuery]int skip = 0,
        [FromQuery]int take = 20,
        [FromQuery]bool prerelease = false,
        [FromQuery]string semVerLevel = null,

        // These are unofficial parameters
        [FromQuery]string packageType = null,
        [FromQuery]string framework = null,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Clamp(skip, 0, MaxSkip);
        take = Math.Clamp(take, 1, MaxTake);

        var request = new SearchRequest
        {
            Skip = skip,
            Take = take,
            IncludePrerelease = prerelease,
            IncludeSemVer2 = semVerLevel == "2.0.0",
            PackageType = packageType,
            Framework = framework,
            Query = query ?? string.Empty,
        };

        return await _searchService.SearchAsync(request, cancellationToken);
    }

    public async Task<ActionResult<AutocompleteResponse>> AutocompleteAsync(
        [FromQuery(Name = "q")] string autocompleteQuery = null,
        [FromQuery(Name = "id")] string versionsQuery = null,
        [FromQuery]bool prerelease = false,
        [FromQuery]string semVerLevel = null,
        [FromQuery]int skip = 0,
        [FromQuery]int take = 20,

        // These are unofficial parameters
        [FromQuery]string packageType = null,
        CancellationToken cancellationToken = default)
    {
        // If only "id" is provided, find package versions. Otherwise, find package IDs.
        if (versionsQuery != null && autocompleteQuery == null)
        {
            var request = new VersionsRequest
            {
                IncludePrerelease = prerelease,
                IncludeSemVer2 = semVerLevel == "2.0.0",
                PackageId = versionsQuery,
            };

            return await _searchService.ListPackageVersionsAsync(request, cancellationToken);
        }
        else
        {
            skip = Math.Clamp(skip, 0, MaxSkip);
            take = Math.Clamp(take, 1, MaxTake);

            var request = new AutocompleteRequest
            {
                IncludePrerelease = prerelease,
                IncludeSemVer2 = semVerLevel == "2.0.0",
                PackageType = packageType,
                Skip = skip,
                Take = take,
                Query = autocompleteQuery,
            };

            return await _searchService.AutocompleteAsync(request, cancellationToken);
        }
    }

    public async Task<ActionResult<DependentsResponse>> DependentsAsync(
        [FromQuery] string packageId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return BadRequest();
        }

        return await _searchService.FindDependentsAsync(packageId, cancellationToken);
    }
}
