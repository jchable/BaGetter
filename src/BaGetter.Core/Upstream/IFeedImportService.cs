using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

/// <summary>
/// Options for importing packages from a remote NuGet feed.
/// </summary>
public class FeedImportOptions
{
    /// <summary>
    /// The URL of the remote NuGet feed (v2 or v3 service index).
    /// </summary>
    public Uri FeedUrl { get; set; }

    /// <summary>
    /// Whether the feed uses the legacy NuGet v2 (OData) protocol.
    /// </summary>
    public bool Legacy { get; set; }

    /// <summary>
    /// Authentication type for the remote feed.
    /// </summary>
    public MirrorAuthenticationType AuthType { get; set; } = MirrorAuthenticationType.None;

    /// <summary>
    /// API key for the remote feed (sent as basic auth password with "api-key" username).
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Username for basic authentication.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password for basic authentication.
    /// </summary>
    public string Password { get; set; }
}

/// <summary>
/// Real-time progress of a feed import operation.
/// </summary>
public class FeedImportProgress
{
    public int TotalVersions { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public string CurrentPackage { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsComplete { get; set; }
}

/// <summary>
/// The result of a completed feed import operation.
/// </summary>
public enum FeedImportResult
{
    Success,
    Cancelled,
    Failed,
}

/// <summary>
/// Service that imports all packages from a remote NuGet feed into BaGetter.
/// </summary>
public interface IFeedImportService
{
    /// <summary>
    /// Import all packages from a remote NuGet feed.
    /// The operation is idempotent: packages that already exist are skipped.
    /// </summary>
    Task<FeedImportResult> ImportAsync(
        FeedImportOptions options,
        IProgress<FeedImportProgress> progress,
        CancellationToken cancellationToken);
}
