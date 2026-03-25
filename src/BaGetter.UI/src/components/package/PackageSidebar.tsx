import { Download, Calendar, ExternalLink, GitFork } from "lucide-react";
import type { PackageDetail } from "@/api/types";

function formatDownloads(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return String(n);
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

interface PackageSidebarProps {
  readonly pkg: PackageDetail;
}

export function PackageSidebar({ pkg }: PackageSidebarProps) {
  return (
    <div className="space-y-5">
      {/* Stats */}
      <div className="space-y-2">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
          Info
        </h4>
        <div className="space-y-1.5 text-sm">
          <div className="flex items-center gap-2">
            <Download size={14} className="text-[var(--fg-muted)]" />
            <span>{formatDownloads(pkg.totalDownloads)} total downloads</span>
          </div>
          <div className="flex items-center gap-2">
            <Download size={14} className="text-[var(--fg-muted)]" />
            <span>{formatDownloads(pkg.downloads)} version downloads</span>
          </div>
          <div className="flex items-center gap-2">
            <Calendar size={14} className="text-[var(--fg-muted)]" />
            <span>Published {formatDate(pkg.published)}</span>
          </div>
          <div className="flex items-center gap-2">
            <Calendar size={14} className="text-[var(--fg-muted)]" />
            <span>Last updated {formatDate(pkg.lastUpdated)}</span>
          </div>
        </div>
      </div>

      {/* Links */}
      {(pkg.projectUrl ?? pkg.repositoryUrl ?? pkg.licenseUrl) && (
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
            Links
          </h4>
          <div className="space-y-1.5 text-sm">
            {pkg.projectUrl && (
              <a
                href={pkg.projectUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 text-primary-600 hover:underline dark:text-primary-400"
              >
                <ExternalLink size={14} />
                Project page
              </a>
            )}
            {pkg.repositoryUrl && (
              <a
                href={pkg.repositoryUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 text-primary-600 hover:underline dark:text-primary-400"
              >
                <GitFork size={14} />
                Source repository
              </a>
            )}
            {pkg.licenseUrl && (
              <a
                href={pkg.licenseUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 text-primary-600 hover:underline dark:text-primary-400"
              >
                <ExternalLink size={14} />
                License
              </a>
            )}
          </div>
        </div>
      )}

      {/* Authors */}
      {pkg.authors.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
            Authors
          </h4>
          <p className="text-sm">{pkg.authors.join(", ")}</p>
        </div>
      )}

      {/* Tags */}
      {pkg.tags.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
            Tags
          </h4>
          <div className="flex flex-wrap gap-1">
            {pkg.tags.map((tag) => (
              <span
                key={tag}
                className="rounded bg-[var(--bg-muted)] px-2 py-0.5 text-xs"
              >
                {tag}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Download link */}
      <a
        href={pkg.packageDownloadUrl}
        className="inline-flex items-center gap-2 text-sm text-primary-600 hover:underline dark:text-primary-400"
      >
        <Download size={14} />
        Download .nupkg
      </a>
    </div>
  );
}
