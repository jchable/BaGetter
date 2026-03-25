import { useState } from "react";
import { Link } from "react-router";
import { Download, ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { VersionInfo } from "@/api/types";

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

interface VersionListProps {
  packageId: string;
  versions: VersionInfo[];
}

const INITIAL_COUNT = 5;

export function VersionList({ packageId, versions }: VersionListProps) {
  const [showAll, setShowAll] = useState(false);
  const visible = showAll ? versions : versions.slice(0, INITIAL_COUNT);

  return (
    <div>
      <div className="space-y-1">
        {visible.map((v) => (
          <Link
            key={v.version}
            to={`/packages/${encodeURIComponent(packageId)}/${encodeURIComponent(v.version)}`}
            className={`flex items-center justify-between rounded px-2 py-1.5 text-sm transition-colors hover:bg-[var(--bg-muted)] ${
              v.selected ? "bg-primary-50 font-medium text-primary-700 dark:bg-primary-950 dark:text-primary-300" : ""
            }`}
          >
            <span>{v.version}</span>
            <span className="flex items-center gap-3 text-xs text-[var(--fg-muted)]">
              <span className="flex items-center gap-1">
                <Download size={10} />
                {formatDownloads(v.downloads)}
              </span>
              <span>{formatDate(v.lastUpdated)}</span>
            </span>
          </Link>
        ))}
      </div>

      {versions.length > INITIAL_COUNT && (
        <Button
          variant="ghost"
          size="sm"
          className="mt-2 w-full"
          onClick={() => setShowAll(!showAll)}
        >
          {showAll ? (
            <>
              <ChevronUp size={14} />
              Show fewer versions
            </>
          ) : (
            <>
              <ChevronDown size={14} />
              Show all {versions.length} versions
            </>
          )}
        </Button>
      )}
    </div>
  );
}
