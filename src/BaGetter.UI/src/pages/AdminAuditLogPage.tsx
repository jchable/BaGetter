import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Pagination } from "@/components/package/Pagination";
import { Search } from "lucide-react";

interface AuditLogResponse {
  logs: Array<{
    id: number;
    timestamp: string;
    userId: string | null;
    userName: string | null;
    action: string;
    resourceType: string | null;
    resourceId: string | null;
    details: string | null;
    ipAddress: string | null;
  }>;
  currentPage: number;
  totalPages: number;
  total: number;
}

export function AdminAuditLogPage() {
  const [action, setAction] = useState("");
  const [userId, setUserId] = useState("");
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["admin", "audit-log", { action, userId, page }],
    queryFn: () => {
      const params = new URLSearchParams();
      if (action) params.set("action", action);
      if (userId) params.set("userId", userId);
      params.set("page", String(page));
      return api.get<AuditLogResponse>(`/api/ui/admin/audit-log?${params}`);
    },
  });

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Audit Log</h1>

      {/* Filters */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          setPage(1);
        }}
        className="mb-4 flex flex-wrap gap-2"
      >
        <Input
          placeholder="Filter by action..."
          value={action}
          onChange={(e) => setAction(e.target.value)}
          className="w-48"
        />
        <Input
          placeholder="Filter by user ID..."
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
          className="w-48"
        />
        <Button type="submit" variant="secondary" size="sm">
          <Search size={14} />
          Filter
        </Button>
      </form>

      {isLoading && <LoadingSpinner className="py-12" />}

      {data && (
        <>
          <p className="mb-2 text-sm text-[var(--fg-muted)]">
            {data.total.toLocaleString()} entries
          </p>

          <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
            <table className="w-full text-sm">
              <thead className="border-b border-[var(--border)] bg-[var(--bg-muted)]">
                <tr>
                  <th className="px-4 py-3 text-left font-medium">Time</th>
                  <th className="px-4 py-3 text-left font-medium">Action</th>
                  <th className="px-4 py-3 text-left font-medium">User</th>
                  <th className="px-4 py-3 text-left font-medium">Resource</th>
                  <th className="px-4 py-3 text-left font-medium">IP</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border)]">
                {data.logs.map((log) => (
                  <tr key={log.id} className="hover:bg-[var(--bg-muted)]">
                    <td className="whitespace-nowrap px-4 py-3 text-[var(--fg-muted)]">
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant="secondary">{log.action}</Badge>
                    </td>
                    <td className="px-4 py-3">{log.userName ?? "—"}</td>
                    <td className="px-4 py-3 text-[var(--fg-muted)]">
                      {log.resourceType && `${log.resourceType}`}
                      {log.resourceId && `: ${log.resourceId}`}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-[var(--fg-muted)]">
                      {log.ipAddress ?? "—"}
                    </td>
                  </tr>
                ))}
                {data.logs.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-[var(--fg-muted)]">
                      No audit log entries.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <Pagination
            page={data.currentPage}
            hasMore={data.currentPage < data.totalPages}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  );
}
