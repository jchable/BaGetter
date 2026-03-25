import { PackageRow } from "./PackageRow";
import type { SearchResult } from "@/api/types";

interface PackageListProps {
  packages: SearchResult[];
}

export function PackageList({ packages }: PackageListProps) {
  if (packages.length === 0) {
    return (
      <div className="py-12 text-center text-[var(--fg-muted)]">
        No packages found.
      </div>
    );
  }

  return (
    <div className="divide-y divide-[var(--border)]">
      {packages.map((pkg) => (
        <PackageRow key={`${pkg.id}-${pkg.version}`} pkg={pkg} />
      ))}
    </div>
  );
}
