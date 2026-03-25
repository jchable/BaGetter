using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace BaGetter.Core;

public class MirrorOptions : IValidatableObject
{
    /// <summary>
    /// If true, packages that aren't found locally will be indexed
    /// using the upstream source.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The v3 index that will be mirrored.
    /// </summary>
    public Uri PackageSource { get; set; }

    /// <summary>
    /// Whether or not the package source is a v2 package source feed.
    /// </summary>
    public bool Legacy { get; set; }

    /// <summary>
    /// The time before a download from the package source times out.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PackageDownloadTimeoutSeconds { get; set; } = 600;

    public MirrorAuthenticationOptions Authentication { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (PackageSource == null)
        {
            yield return new ValidationResult(
                $"The {nameof(PackageSource)} configuration is required if mirroring is enabled",
                [nameof(PackageSource)]);
        }
        else if (IsPrivateOrLoopbackUri(PackageSource))
        {
            yield return new ValidationResult(
                $"The {nameof(PackageSource)} must not point to a loopback or private network address",
                [nameof(PackageSource)]);
        }

        if (Authentication != null)
        {
            if (Legacy && Authentication.Type is not (MirrorAuthenticationType.None or MirrorAuthenticationType.Basic))
            {
                yield return new ValidationResult(
                    "Legacy v2 feeds only support basic authentication",
                    [nameof(Legacy), nameof(Authentication)]);
            }

            switch (Authentication.Type)
            {
                case MirrorAuthenticationType.Basic:
                    if (string.IsNullOrEmpty(Authentication.Username))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Username)} configuration is required for basic authentication",
                            [nameof(Authentication.Username)]);
                    }

                    if (string.IsNullOrEmpty(Authentication.Password))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Password)} configuration is required for basic authentication",
                            [nameof(Authentication.Password)]);
                    }
                    break;

                case MirrorAuthenticationType.Bearer:
                    if (string.IsNullOrEmpty(Authentication.Token))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Token)} configuration is required for bearer authentication",
                            [nameof(Authentication.Token)]);
                    }
                    break;

                case MirrorAuthenticationType.Custom:
                    if (Authentication.CustomHeaders == null)
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.CustomHeaders)} configuration is required for custom authentication",
                            [nameof(Authentication.CustomHeaders)]);
                        break;
                    }

                    if (Authentication.CustomHeaders.Count == 0)
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.CustomHeaders)} configuration has no headers defined." +
                            $" Use \"{nameof(Authentication.Type)}\": \"{nameof(MirrorAuthenticationType.None)}\" instead if you intend you use no authentication.",
                            [nameof(Authentication.CustomHeaders)]);
                    }
                    break;
            }
        }
    }

    private static bool IsPrivateOrLoopbackUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            return false;

        var host = uri.Host;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var ip))
            return false;

        if (IPAddress.IsLoopback(ip))
            return true;

        // RFC 1918 / RFC 4193 private ranges
        var bytes = ip.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] switch
        {
            10 => true,                                         // 10.0.0.0/8
            172 => bytes[1] >= 16 && bytes[1] <= 31,            // 172.16.0.0/12
            192 => bytes[1] == 168,                             // 192.168.0.0/16
            169 => bytes[1] == 254,                             // 169.254.0.0/16 (link-local)
            _ => false
        };
    }
}
