using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using BaGetter.Protocol.Models;
using System.Threading.Tasks;
using BaGetter.Protocol;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core;

public class FeedImportService : IFeedImportService
{
    private const int SearchPageSize = 20;

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
            var client = CreateNuGetClient(options);

            // Phase 1: Enumerate all package IDs via search with pagination
            _logger.LogInformation("Enumerating packages from {FeedUrl}...", options.FeedUrl);
            state.CurrentPackage = "Enumerating packages...";
            progress.Report(state);

            var packageIds = await EnumeratePackageIdsAsync(client, cancellationToken);

            _logger.LogInformation("Found {Count} unique package(s) on remote feed", packageIds.Count);

            // Phase 2: For each package, list all versions and download+index each
            var allVersions = new List<(string Id, NuGetVersion Version)>();

            foreach (var packageId in packageIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var versions = await client.ListPackageVersionsAsync(packageId, includeUnlisted: true, cancellationToken);
                foreach (var version in versions)
                {
                    allVersions.Add((packageId, version));
                }
            }

            state.TotalVersions = allVersions.Count;
            progress.Report(state);

            _logger.LogInformation("Found {Count} total version(s) to import", allVersions.Count);

            // Phase 3: Download and index each version
            foreach (var (id, version) in allVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                state.CurrentPackage = $"{id} {version}";
                progress.Report(state);

                try
                {
                    using var packageStream = await DownloadPackageAsync(client, id, version, cancellationToken);
                    if (packageStream == null)
                    {
                        state.Failed++;
                        state.Errors.Add($"{id} {version}: download returned null");
                        _logger.LogWarning("Failed to download {PackageId} {Version} from remote feed", id, version);
                        continue;
                    }

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

    private NuGetClient CreateNuGetClient(FeedImportOptions options)
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName().Name ?? "BaGetter";
        var assemblyVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(600),
        };

        httpClient.DefaultRequestHeaders.Add("User-Agent", $"{assemblyName}/{assemblyVersion}");

        // Configure authentication
        switch (options.AuthType)
        {
            case MirrorAuthenticationType.Basic:
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
                break;

            case MirrorAuthenticationType.None when !string.IsNullOrEmpty(options.ApiKey):
                // MyGet and many feeds accept API key as basic auth password
                var apiKeyCredentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"api-key:{options.ApiKey}"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", apiKeyCredentials);
                break;
        }

        var clientFactory = new NuGetClientFactory(httpClient, options.FeedUrl.ToString());
        return new NuGetClient(clientFactory);
    }

    private static async Task<List<string>> EnumeratePackageIdsAsync(
        NuGetClient client,
        CancellationToken cancellationToken)
    {
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skip = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = await client.SearchAsync(
                query: "",
                skip: skip,
                take: SearchPageSize,
                includePrerelease: true,
                cancellationToken: cancellationToken);

            if (results.Count == 0)
                break;

            foreach (var result in results)
            {
                packageIds.Add(result.PackageId);
            }

            skip += results.Count;

            // If we got fewer results than requested, we've reached the end
            if (results.Count < SearchPageSize)
                break;
        }

        return packageIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task<Stream> DownloadPackageAsync(
        NuGetClient client,
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        try
        {
            var stream = await client.DownloadPackageAsync(id, version, cancellationToken);

            // Ensure the stream is seekable (required by PackageIndexingService)
            if (!stream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);
                await stream.DisposeAsync();
                memoryStream.Position = 0;
                return memoryStream;
            }

            return stream;
        }
        catch (PackageNotFoundException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
