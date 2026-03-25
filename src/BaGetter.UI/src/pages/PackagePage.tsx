import { useParams } from "react-router";
import { usePackageDetail } from "@/api/packages";
import { useAppConfig } from "@/api/account";
import { PackageIcon } from "@/components/package/PackageIcon";
import { InstallTabs } from "@/components/package/InstallTabs";
import { ReadmeSection } from "@/components/package/ReadmeSection";
import { DependencyGroups } from "@/components/package/DependencyGroups";
import { VersionList } from "@/components/package/VersionList";
import { DeprecationBanner } from "@/components/package/DeprecationBanner";
import { PackageSidebar } from "@/components/package/PackageSidebar";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Badge } from "@/components/ui/badge";

export function PackagePage() {
  const { id = "", version } = useParams();
  const { data: pkg, isLoading, error } = usePackageDetail(id, version);
  const { data: config } = useAppConfig();

  if (isLoading) {
    return <LoadingSpinner className="py-20" />;
  }

  if (error || !pkg) {
    return (
      <div className="py-20 text-center">
        <h1 className="text-2xl font-bold">Package not found</h1>
        <p className="mt-2 text-[var(--fg-muted)]">
          The package &quot;{id}&quot; could not be found.
        </p>
      </div>
    );
  }

  const isDotnetTool = pkg.packageTypes?.some((t) => t.name.toLowerCase() === "dotnettool") ?? false;
  const isDotnetTemplate = pkg.packageTypes?.some((t) => t.name.toLowerCase() === "template") ?? false;

  return (
    <div>
      {/* Deprecation banner */}
      {pkg.deprecation && <DeprecationBanner deprecation={pkg.deprecation} />}

      {/* Header */}
      <div className="flex items-start gap-4">
        <PackageIcon url={pkg.iconUrl} alt={pkg.id} size={64} className="hidden shrink-0 sm:block" />
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-2xl font-bold">{pkg.id}</h1>
            <Badge variant="secondary">{pkg.version}</Badge>
            {!pkg.listed && <Badge variant="warning">Unlisted</Badge>}
          </div>
          {pkg.description && (
            <p className="mt-1 text-[var(--fg-muted)]">{pkg.description}</p>
          )}
        </div>
      </div>

      {/* Install tabs */}
      <div className="mt-6">
        <InstallTabs
          packageId={pkg.id}
          version={pkg.version}
          isDotnetTool={isDotnetTool}
          isDotnetTemplate={isDotnetTemplate}
          serviceIndexUrl={config?.serviceIndexUrl}
        />
      </div>

      {/* Main content + sidebar */}
      <div className="mt-6 flex flex-col gap-6 lg:flex-row">
        {/* Left column */}
        <div className="min-w-0 flex-1">
          {/* Readme */}
          {pkg.readme && <ReadmeSection markdown={pkg.readme} />}

          {/* Release notes */}
          {pkg.releaseNotes && (
            <div className="mt-6">
              <h3 className="mb-3 text-lg font-semibold">Release Notes</h3>
              <div className="rounded-lg border border-[var(--border)] bg-[var(--bg-card)] p-4 text-sm">
                {pkg.releaseNotes}
              </div>
            </div>
          )}

          {/* Dependencies */}
          <div className="mt-6">
            <h3 className="mb-3 text-lg font-semibold">Dependencies</h3>
            <DependencyGroups groups={pkg.dependencyGroups} />
          </div>

          {/* Used by */}
          {pkg.usedBy && pkg.usedBy.length > 0 && (
            <div className="mt-6">
              <h3 className="mb-3 text-lg font-semibold">Used By</h3>
              <div className="space-y-2">
                {pkg.usedBy.map((dep) => (
                  <a
                    key={dep.id}
                    href={`/packages/${encodeURIComponent(dep.id)}`}
                    className="block rounded-lg border border-[var(--border)] p-3 text-sm transition-colors hover:bg-[var(--bg-muted)]"
                  >
                    <span className="font-medium text-primary-600 dark:text-primary-400">
                      {dep.id}
                    </span>
                    {dep.description && (
                      <p className="mt-1 line-clamp-1 text-[var(--fg-muted)]">
                        {dep.description}
                      </p>
                    )}
                  </a>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Right sidebar */}
        <aside className="w-full shrink-0 lg:w-64">
          <PackageSidebar pkg={pkg} />

          {/* Versions */}
          <div className="mt-6">
            <h4 className="mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
              Versions
            </h4>
            <VersionList packageId={pkg.id} versions={pkg.versions} />
          </div>
        </aside>
      </div>
    </div>
  );
}
