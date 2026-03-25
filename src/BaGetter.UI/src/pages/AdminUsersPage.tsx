import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAdminUsers } from "@/api/admin";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Trash2 } from "lucide-react";

const ROLES = ["Owner", "Admin", "Publisher", "Reader"];

export function AdminUsersPage() {
  const { data: users, isLoading } = useAdminUsers();
  const queryClient = useQueryClient();

  const changeRole = useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: string }) =>
      api.put(`/api/ui/admin/users/${userId}/role`, { role }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "users"] }),
  });

  const deleteUser = useMutation({
    mutationFn: (userId: string) => api.delete(`/api/ui/admin/users/${userId}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "users"] }),
  });

  if (isLoading) return <LoadingSpinner className="py-20" />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Users</h1>

      <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
        <table className="w-full text-sm">
          <thead className="border-b border-[var(--border)] bg-[var(--bg-muted)]">
            <tr>
              <th className="px-4 py-3 text-left font-medium">User</th>
              <th className="px-4 py-3 text-left font-medium">Role</th>
              <th className="px-4 py-3 text-left font-medium">Created</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border)]">
            {users?.map((user) => (
              <tr key={user.id} className="hover:bg-[var(--bg-muted)]">
                <td className="px-4 py-3">
                  <div className="font-medium">{user.displayName || user.userName}</div>
                  <div className="text-xs text-[var(--fg-muted)]">{user.email}</div>
                </td>
                <td className="px-4 py-3">
                  <select
                    value={user.role}
                    onChange={(e) => changeRole.mutate({ userId: user.id, role: e.target.value })}
                    className="rounded border border-[var(--border)] bg-[var(--bg-card)] px-2 py-1 text-sm"
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                </td>
                <td className="px-4 py-3 text-[var(--fg-muted)]">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3 text-right">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => {
                      if (confirm(`Delete user ${user.userName}?`))
                        deleteUser.mutate(user.id);
                    }}
                    title="Delete user"
                  >
                    <Trash2 size={14} className="text-red-500" />
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
