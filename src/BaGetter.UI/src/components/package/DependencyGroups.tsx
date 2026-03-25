import { Link } from "react-router";
import type { DependencyGroup } from "@/api/types";

interface DependencyGroupsProps {
  groups: DependencyGroup[];
}

export function DependencyGroups({ groups }: DependencyGroupsProps) {
  if (groups.length === 0) {
    return (
      <p className="text-sm text-[var(--fg-muted)]">This package has no dependencies.</p>
    );
  }

  return (
    <div className="space-y-4">
      {groups.map((group) => (
        <div key={group.name}>
          <h4 className="mb-2 text-sm font-semibold">{group.name}</h4>
          {group.dependencies.length === 0 ? (
            <p className="text-sm text-[var(--fg-muted)]">No dependencies</p>
          ) : (
            <ul className="space-y-1">
              {group.dependencies.map((dep) => (
                <li key={dep.packageId} className="flex items-baseline gap-2 text-sm">
                  <Link
                    to={`/packages/${encodeURIComponent(dep.packageId)}`}
                    className="text-primary-600 hover:underline dark:text-primary-400"
                  >
                    {dep.packageId}
                  </Link>
                  {dep.versionSpec && (
                    <span className="text-[var(--fg-muted)]">{dep.versionSpec}</span>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      ))}
    </div>
  );
}
