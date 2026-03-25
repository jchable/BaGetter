import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useInvitations } from "@/api/admin";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { CopyButton } from "@/components/common/CopyButton";
import { Trash2, Send } from "lucide-react";

const ROLES = ["Reader", "Publisher", "Admin", "Owner"];

export function AdminInvitationsPage() {
  const { data: invitations, isLoading } = useInvitations();
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Reader");
  const [lastUrl, setLastUrl] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: (data: { email: string; role: string }) =>
      api.post<{ id: number; registerUrl: string }>("/api/ui/admin/invitations", data),
    onSuccess: (data) => {
      setLastUrl(data.registerUrl);
      setEmail("");
      queryClient.invalidateQueries({ queryKey: ["admin", "invitations"] });
    },
  });

  const revoke = useMutation({
    mutationFn: (id: number) => api.delete(`/api/ui/admin/invitations/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "invitations"] }),
  });

  if (isLoading) return <LoadingSpinner className="py-20" />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Invitations</h1>

      {/* Create invitation */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (email.trim()) create.mutate({ email: email.trim(), role });
        }}
        className="mb-6 flex flex-wrap gap-2"
      >
        <Input
          type="email"
          placeholder="Email address"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="flex-1"
        />
        <select
          value={role}
          onChange={(e) => setRole(e.target.value)}
          className="h-9 rounded-md border border-[var(--border)] bg-[var(--bg-card)] px-3 text-sm"
        >
          {ROLES.map((r) => (
            <option key={r} value={r}>{r}</option>
          ))}
        </select>
        <Button type="submit" disabled={create.isPending}>
          <Send size={14} />
          Invite
        </Button>
      </form>

      {/* Last created URL */}
      {lastUrl && (
        <div className="mb-6 rounded-md border border-green-200 bg-green-50 p-3 dark:border-green-800 dark:bg-green-950">
          <p className="mb-1 text-sm font-medium text-green-800 dark:text-green-200">
            Invitation created! Share this registration link:
          </p>
          <div className="flex items-center gap-2 rounded bg-white px-2 py-1 font-mono text-xs dark:bg-surface-800">
            <span className="flex-1 overflow-x-auto">{lastUrl}</span>
            <CopyButton text={lastUrl} />
          </div>
        </div>
      )}

      {/* Invitation list */}
      <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
        <table className="w-full text-sm">
          <thead className="border-b border-[var(--border)] bg-[var(--bg-muted)]">
            <tr>
              <th className="px-4 py-3 text-left font-medium">Email</th>
              <th className="px-4 py-3 text-left font-medium">Role</th>
              <th className="px-4 py-3 text-left font-medium">Status</th>
              <th className="px-4 py-3 text-left font-medium">Expires</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border)]">
            {invitations?.map((inv) => {
              const expired = new Date(inv.expiresAt) < new Date();
              const accepted = !!inv.acceptedAt;
              return (
                <tr key={inv.id} className="hover:bg-[var(--bg-muted)]">
                  <td className="px-4 py-3">{inv.email}</td>
                  <td className="px-4 py-3"><Badge variant="secondary">{inv.role}</Badge></td>
                  <td className="px-4 py-3">
                    {accepted && <Badge variant="success">Accepted</Badge>}
                    {!accepted && expired && <Badge variant="warning">Expired</Badge>}
                    {!accepted && !expired && <Badge variant="default">Pending</Badge>}
                  </td>
                  <td className="px-4 py-3 text-[var(--fg-muted)]">
                    {new Date(inv.expiresAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    {!accepted && (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => revoke.mutate(inv.id)}
                        title="Revoke"
                      >
                        <Trash2 size={14} className="text-red-500" />
                      </Button>
                    )}
                  </td>
                </tr>
              );
            })}
            {invitations?.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-[var(--fg-muted)]">
                  No invitations yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
