import { useDashboardStats } from "@/api/admin";
import { Card, CardContent } from "@/components/ui/card";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Users, Package, Mail } from "lucide-react";

export function AdminDashboardPage() {
  const { data, isLoading } = useDashboardStats();

  if (isLoading) return <LoadingSpinner className="py-20" />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Dashboard</h1>
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary-50 text-primary-600 dark:bg-primary-950 dark:text-primary-400">
              <Users size={24} />
            </div>
            <div>
              <p className="text-sm text-[var(--fg-muted)]">Users</p>
              <p className="text-2xl font-bold">{data?.userCount ?? 0}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary-50 text-primary-600 dark:bg-primary-950 dark:text-primary-400">
              <Package size={24} />
            </div>
            <div>
              <p className="text-sm text-[var(--fg-muted)]">Packages</p>
              <p className="text-2xl font-bold">{data?.packageCount ?? 0}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-accent-50 text-accent-600 dark:bg-accent-950 dark:text-accent-400">
              <Mail size={24} />
            </div>
            <div>
              <p className="text-sm text-[var(--fg-muted)]">Pending Invitations</p>
              <p className="text-2xl font-bold">{data?.pendingInvitationCount ?? 0}</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
