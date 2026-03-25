import { useQuery } from "@tanstack/react-query";
import { api } from "./client";
import type { SearchResponse } from "./types";

interface SearchParams {
  query?: string;
  skip?: number;
  take?: number;
  prerelease?: boolean;
  packageType?: string;
  framework?: string;
}

function buildSearchUrl(params: SearchParams): string {
  const url = new URL("/v3/search", window.location.origin);
  if (params.query) url.searchParams.set("q", params.query);
  if (params.skip) url.searchParams.set("skip", String(params.skip));
  if (params.take) url.searchParams.set("take", String(params.take));
  if (params.prerelease) url.searchParams.set("prerelease", "true");
  if (params.packageType && params.packageType !== "any")
    url.searchParams.set("packageType", params.packageType);
  if (params.framework && params.framework !== "any")
    url.searchParams.set("framework", params.framework);
  url.searchParams.set("semVerLevel", "2.0.0");
  return url.pathname + url.search;
}

export function useSearch(params: SearchParams) {
  return useQuery({
    queryKey: ["search", params],
    queryFn: () => api.get<SearchResponse>(buildSearchUrl(params)),
  });
}
