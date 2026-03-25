import { useQuery } from "@tanstack/react-query";
import { api } from "./client";
import type { PackageDetail } from "./types";

export function usePackageDetail(id: string, version?: string) {
  const url = version
    ? `/api/ui/packages/${encodeURIComponent(id)}/versions/${encodeURIComponent(version)}`
    : `/api/ui/packages/${encodeURIComponent(id)}`;

  return useQuery({
    queryKey: ["package", id, version],
    queryFn: () => api.get<PackageDetail>(url),
    enabled: !!id,
  });
}
