import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApiKeys } from "@/api/admin";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { CopyButton } from "@/components/common/CopyButton";
import { Trash2, KeyRound } from "lucide-react";

const ROLES = ["Reader", "Publisher", "Admin", "Owner"];

export function AdminApiKeysPage() {
  const { data: keys, isLoading } = useApiKeys();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [role, setRole] = useState("Publisher");
  const [createdKey, setCreatedKey] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: (data: { name: string; role: string }) =>
      api.post<{ key: string; id: number; name: string }>("/api/ui/admin/api-keys", data),
    onSuccess: (data) => {
      setCreatedKey(data.key);
      setName("");
      queryClient.invalidateQueries({ queryKey: ["admin", "api-keys"] });
    },
  });

  const revoke = useMutation({
    mutationFn: (id: number) => api.delete(`/api/ui/admin/api-keys/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "api-keys"] }),
  });

  if (isLoading) return <LoadingSpinner className="py-20" />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">API Keys</h1>

      {/* Create key */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) create.mutate({ name: name.trim(), role });
        }}
        className="mb-6 flex flex-wrap gap-2"
      >
        <Input
          placeholder="Key name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={256}
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
          <KeyRound size={14} />
          Create
        </Button>
      </form>

      {/* Newly created key */}
      {createdKey && (
        <div className="mb-6 rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
          <p className="mb-1 text-sm font-medium text-amber-800 dark:text-amber-200">
            Copy your API key now — it won&apos;t be shown again.
          </p>
          <div className="flex items-center gap-2 rounded bg-white px-2 py-1 font-mono text-sm dark:bg-surface-800">
            <span className="flex-1 overflow-x-auto">{createdKey}</span>
            <CopyButton text={createdKey} />
          </div>
        </div>
      )}

      {/* Keys table */}
      <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
        <table className="w-full text-sm">
          <thead className="border-b border-[var(--border)] bg-[var(--bg-muted)]">
            <tr>
              <th className="px-4 py-3 text-left font-medium">Name</th>
              <th className="px-4 py-3 text-left font-medium">Prefix</th>
              <th className="px-4 py-3 text-left font-medium">Role</th>
              <th className="px-4 py-3 text-left font-medium">Created</th>
              <th className="px-4 py-3 text-left font-medium">Status</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border)]">
            {keys?.map((key) => (
              <tr key={key.id} className="hover:bg-[var(--bg-muted)]">
                <td className="px-4 py-3 font-medium">{key.name}</td>
                <td className="px-4 py-3 font-mono text-xs text-[var(--fg-muted)]">{key.keyPrefix}...</td>
                <td className="px-4 py-3"><Badge variant="secondary">{key.role}</Badge></td>
                <td className="px-4 py-3 text-[var(--fg-muted)]">
                  {new Date(key.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  {key.isRevoked
                    ? <Badge variant="destructive">Revoked</Badge>
                    : <Badge variant="success">Active</Badge>}
                </td>
                <td className="px-4 py-3 text-right">
                  {!key.isRevoked && (
                    <Button variant="ghost" size="icon" onClick={() => revoke.mutate(key.id)} title="Revoke">
                      <Trash2 size={14} className="text-red-500" />
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
