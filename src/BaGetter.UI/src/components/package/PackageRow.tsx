import { Link } from "react-router";
import { Download } from "lucide-react";
import { PackageIcon } from "./PackageIcon";
import { Badge } from "@/components/ui/badge";
import type { SearchResult } from "@/api/types";

function formatDownloads(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return String(n);
}

interface PackageRowProps {
  pkg: SearchResult;
}

export function PackageRow({ pkg }: PackageRowProps) {
  return (
    <Link
      to={`/packages/${encodeURIComponent(pkg.id)}`}
      className="flex gap-4 rounded-lg border border-transparent p-4 transition-colors hover:border-[var(--border)] hover:bg-[var(--bg-muted)]"
    >
      <PackageIcon url={pkg.iconUrl} alt={pkg.id} size={48} className="hidden shrink-0 sm:block" />

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <h3 className="truncate font-semibold text-primary-600 dark:text-primary-400">
            {pkg.id}
          </h3>
          <Badge variant="secondary">{pkg.version}</Badge>
        </div>

        {pkg.description && (
          <p className="mt-1 line-clamp-2 text-sm text-[var(--fg-muted)]">
            {pkg.description}
          </p>
        )}

        <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-[var(--fg-muted)]">
          <span className="flex items-center gap-1">
            <Download size={12} />
            {formatDownloads(pkg.totalDownloads)} downloads
          </span>
          {pkg.authors?.length > 0 && (
            <span>by {pkg.authors.join(", ")}</span>
          )}
          {pkg.tags?.length > 0 && (
            <span className="hidden sm:inline">
              {pkg.tags.slice(0, 5).map((tag) => (
                <span
                  key={tag}
                  className="mr-1 inline-block rounded bg-[var(--bg-muted)] px-1.5 py-0.5"
                >
                  {tag}
                </span>
              ))}
            </span>
          )}
        </div>
      </div>
    </Link>
  );
}
