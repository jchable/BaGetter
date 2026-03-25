import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Card, CardContent } from "@/components/ui/card";
import { Package, Layers, Server, Info } from "lucide-react";

interface StatsData {
  appVersion: string;
  packagesTotal: number;
  versionsTotal: number;
  services: string[];
}

function useStatistics() {
  return useQuery({
    queryKey: ["statistics"],
    queryFn: () => api.get<StatsData>("/api/ui/statistics"),
  });
}

interface StatCardProps {
  readonly icon: React.ReactNode;
  readonly label: string;
  readonly value: string | number;
}

function StatCard({ icon, label, value }: StatCardProps) {
  return (
    <Card>
      <CardContent className="flex items-center gap-4 p-6">
        <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary-50 text-primary-600 dark:bg-primary-950 dark:text-primary-400">
          {icon}
        </div>
        <div>
          <p className="text-sm text-[var(--fg-muted)]">{label}</p>
          <p className="text-2xl font-bold">{typeof value === "number" ? value.toLocaleString() : value}</p>
        </div>
      </CardContent>
    </Card>
  );
}

export function StatisticsPage() {
  const { data, isLoading, error } = useStatistics();

  if (isLoading) return <LoadingSpinner className="py-20" />;

  if (error) {
    return (
      <div className="py-20 text-center">
        <h1 className="text-2xl font-bold">Statistics</h1>
        <p className="mt-2 text-[var(--fg-muted)]">
          Statistics are not available.
        </p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold">Statistics</h1>
      <p className="mt-2 text-[var(--fg-muted)]">Server statistics and usage information.</p>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <StatCard
          icon={<Info size={24} />}
          label="Current version"
          value={data?.appVersion ?? "—"}
        />
        <StatCard
          icon={<Package size={24} />}
          label="Total packages"
          value={data?.packagesTotal ?? 0}
        />
        <StatCard
          icon={<Layers size={24} />}
          label="Total package versions"
          value={data?.versionsTotal ?? 0}
        />
        {data?.services && data.services.length > 0 && (
          <StatCard
            icon={<Server size={24} />}
            label="Configured services"
            value={data.services.join(", ")}
          />
        )}
      </div>
    </div>
  );
}
