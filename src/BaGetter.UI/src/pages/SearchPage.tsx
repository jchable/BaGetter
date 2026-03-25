import { useSearchParams } from "react-router";
import { useSearch } from "@/api/search";
import { PackageList } from "@/components/package/PackageList";
import { SearchFilters } from "@/components/package/SearchFilters";
import { Pagination } from "@/components/package/Pagination";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";

const RESULTS_PER_PAGE = 20;

export function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const query = searchParams.get("q") ?? "";
  const page = Math.max(1, Number(searchParams.get("p") ?? "1"));
  const packageType = searchParams.get("packageType") ?? "any";
  const framework = searchParams.get("framework") ?? "any";
  const prerelease = searchParams.get("prerelease") !== "false";

  const { data, isLoading, error } = useSearch({
    query,
    skip: (page - 1) * RESULTS_PER_PAGE,
    take: RESULTS_PER_PAGE,
    prerelease,
    packageType,
    framework,
  });

  const updateParam = (key: string, value: string) => {
    const params = new URLSearchParams(searchParams);
    if (value && value !== "any" && value !== "true" && value !== "1") {
      params.set(key, value);
    } else {
      params.delete(key);
    }
    // Reset to page 1 on filter change
    if (key !== "p") {
      params.delete("p");
    }
    setSearchParams(params, { replace: true });
  };

  const totalHits = data?.totalHits ?? 0;
  const hasMore = page * RESULTS_PER_PAGE < totalHits;

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-4">
        <div>
          {query && totalHits > 0 && (
            <h2 className="text-lg font-semibold">
              {`${totalHits.toLocaleString()} result${totalHits === 1 ? "" : "s"} for "${query}"`}
            </h2>
          )}
          {query && totalHits === 0 && (
            <h2 className="text-lg font-semibold">
              {`No results for "${query}"`}
            </h2>
          )}
          {!query && !isLoading && (
            <h2 className="text-lg font-semibold">
              {totalHits.toLocaleString()} package{totalHits === 1 ? "" : "s"} available
            </h2>
          )}
        </div>
        <SearchFilters
          packageType={packageType}
          framework={framework}
          prerelease={prerelease}
          onPackageTypeChange={(v) => updateParam("packageType", v)}
          onFrameworkChange={(v) => updateParam("framework", v)}
          onPrereleaseChange={(v) => updateParam("prerelease", v ? "true" : "false")}
        />
      </div>

      {isLoading && <LoadingSpinner className="py-20" />}

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-300">
          Failed to load packages. Please try again.
        </div>
      )}

      {data && (
        <>
          <PackageList packages={data.data} />
          <Pagination
            page={page}
            hasMore={hasMore}
            onPageChange={(p) => updateParam("p", String(p))}
          />
        </>
      )}
    </div>
  );
}
