using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace BaGetter.Core;

public class FeedImportService : IFeedImportService
{
    private readonly IPackageIndexingService _indexingService;
    private readonly ILogger<FeedImportService> _logger;

    public FeedImportService(
        IPackageIndexingService indexingService,
        ILogger<FeedImportService> logger)
    {
        _indexingService = indexingService ?? throw new ArgumentNullException(nameof(indexingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeedImportResult> ImportAsync(
        FeedImportOptions options,
        IProgress<FeedImportProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(progress);

        var state = new FeedImportProgress();

        try
        {
            using var cacheContext = new SourceCacheContext();
            var repository = CreateRepository(options);

            // Phase 1: Enumerate all package IDs and versions
            _logger.LogInformation("Enumerating packages from {FeedUrl}...", options.FeedUrl);
            state.CurrentPackage = "Enumerating packages...";
            progress.Report(state);

            var allVersions = await EnumerateAllVersionsAsync(repository, cacheContext, cancellationToken);

            state.TotalVersions = allVersions.Count;
            progress.Report(state);

            _logger.LogInformation("Found {Count} total version(s) to import", allVersions.Count);

            // Phase 2: Download and index each version
            var findPackageById = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

            foreach (var (id, version) in allVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                state.CurrentPackage = $"{id} {version}";
                progress.Report(state);

                try
                {
                    using var packageStream = new MemoryStream();
                    var success = await findPackageById.CopyNupkgToStreamAsync(
                        id, version, packageStream, cacheContext, NullLogger.Instance, cancellationToken);

                    if (!success)
                    {
                        state.Failed++;
                        state.Errors.Add($"{id} {version}: download failed");
                        _logger.LogWarning("Failed to download {PackageId} {Version} from remote feed", id, version);
                        progress.Report(state);
                        continue;
                    }

                    packageStream.Position = 0;
                    var result = await _indexingService.IndexAsync(packageStream, cancellationToken);

                    switch (result)
                    {
                        case PackageIndexingResult.Success:
                            state.Imported++;
                            _logger.LogInformation("Imported {PackageId} {Version}", id, version);
                            break;

                        case PackageIndexingResult.PackageAlreadyExists:
                            state.Skipped++;
                            _logger.LogDebug("Skipped {PackageId} {Version} (already exists)", id, version);
                            break;

                        case PackageIndexingResult.InvalidPackage:
                            state.Failed++;
                            state.Errors.Add($"{id} {version}: invalid package");
                            _logger.LogWarning("Invalid package {PackageId} {Version}", id, version);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    state.Failed++;
                    state.Errors.Add($"{id} {version}: {ex.Message}");
                    _logger.LogError(ex, "Error importing {PackageId} {Version}", id, version);
                }

                progress.Report(state);
            }

            state.IsComplete = true;
            state.CurrentPackage = null;
            progress.Report(state);

            _logger.LogInformation(
                "Feed import complete. Imported: {Imported}, Skipped: {Skipped}, Failed: {Failed}",
                state.Imported, state.Skipped, state.Failed);

            return FeedImportResult.Success;
        }
        catch (OperationCanceledException)
        {
            state.IsComplete = true;
            state.CurrentPackage = null;
            progress.Report(state);

            _logger.LogInformation("Feed import cancelled");
            return FeedImportResult.Cancelled;
        }
        catch (Exception ex)
        {
            state.IsComplete = true;
            state.CurrentPackage = null;
            state.Errors.Add($"Fatal: {ex.Message}");
            progress.Report(state);

            _logger.LogError(ex, "Feed import failed");
            return FeedImportResult.Failed;
        }
    }

    private static SourceRepository CreateRepository(FeedImportOptions options)
    {
        var packageSource = new PackageSource(options.FeedUrl.AbsoluteUri);

        // Configure authentication
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            // MyGet accepts API key as basic auth password
            packageSource.Credentials = new PackageSourceCredential(
                packageSource.Source, "api-key", options.ApiKey, isPasswordClearText: true, validAuthenticationTypesText: null);
        }
        else if (options.AuthType == MirrorAuthenticationType.Basic
                 && !string.IsNullOrEmpty(options.Username))
        {
            packageSource.Credentials = new PackageSourceCredential(
                packageSource.Source, options.Username, options.Password, isPasswordClearText: true, validAuthenticationTypesText: null);
        }

        // Both v2 and v3 use the same SourceRepository pattern with credentials
        var providers = Repository.Provider.GetCoreV3();
        return new SourceRepository(packageSource, providers);
    }

    private static async Task<List<(string Id, NuGetVersion Version)>> EnumerateAllVersionsAsync(
        SourceRepository repository,
        SourceCacheContext cache,
        CancellationToken cancellationToken)
    {
        var allVersions = new List<(string Id, NuGetVersion Version)>();

        // Use PackageSearchResource to enumerate all packages
        var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);

        var skip = 0;
        const int take = 100;
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = await searchResource.SearchAsync(
                "", new SearchFilter(includePrerelease: true), skip, take, NullLogger.Instance, cancellationToken);

            var batch = results.ToList();
            if (batch.Count == 0)
                break;

            foreach (var result in batch)
            {
                packageIds.Add(result.Identity.Id);
            }

            skip += batch.Count;

            if (batch.Count < take)
                break;
        }

        // For each unique package, enumerate all versions
        var findByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        foreach (var packageId in packageIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var versions = await findByIdResource.GetAllVersionsAsync(packageId, cache, NullLogger.Instance, cancellationToken);

            foreach (var version in versions)
            {
                allVersions.Add((packageId, version));
            }
        }

        return allVersions;
    }
}
