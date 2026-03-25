import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTenants } from "@/api/admin";
import { api } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Building2, AlertCircle } from "lucide-react";

export function AdminTenantsPage() {
  const { data: tenants, isLoading } = useTenants();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [error, setError] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: (data: { name: string; slug: string }) =>
      api.post("/api/ui/admin/tenants", data),
    onSuccess: () => {
      setName("");
      setSlug("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["admin", "tenants"] });
    },
    onError: (err) => {
      const body = (err as { body?: { error?: string } }).body;
      setError(body?.error ?? "Failed to create tenant.");
    },
  });

  if (isLoading) return <LoadingSpinner className="py-20" />;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Tenants</h1>

      {/* Create tenant */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim() && slug.trim()) create.mutate({ name: name.trim(), slug: slug.trim() });
        }}
        className="mb-6 flex flex-wrap gap-2"
      >
        <Input
          placeholder="Tenant name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={256}
        />
        <Input
          placeholder="slug (lowercase, hyphens)"
          value={slug}
          onChange={(e) => setSlug(e.target.value.toLowerCase().replaceAll(/[^a-z0-9-]/g, ""))}
          required
          maxLength={128}
          pattern="^[a-z0-9\-]+$"
        />
        <Button type="submit" disabled={create.isPending}>
          <Building2 size={14} />
          Create
        </Button>
      </form>

      {error && (
        <div className="mb-4 flex items-center gap-2 text-sm text-red-600">
          <AlertCircle size={14} /> {error}
        </div>
      )}

      {/* Tenants table */}
      <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
        <table className="w-full text-sm">
          <thead className="border-b border-[var(--border)] bg-[var(--bg-muted)]">
            <tr>
              <th className="px-4 py-3 text-left font-medium">Name</th>
              <th className="px-4 py-3 text-left font-medium">Slug</th>
              <th className="px-4 py-3 text-left font-medium">Users</th>
              <th className="px-4 py-3 text-left font-medium">Packages</th>
              <th className="px-4 py-3 text-left font-medium">Created</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border)]">
            {tenants?.map((t) => (
              <tr key={t.id} className="hover:bg-[var(--bg-muted)]">
                <td className="px-4 py-3 font-medium">{t.name}</td>
                <td className="px-4 py-3 font-mono text-xs text-[var(--fg-muted)]">{t.slug}</td>
                <td className="px-4 py-3">{t.userCount}</td>
                <td className="px-4 py-3">{t.packageCount}</td>
                <td className="px-4 py-3 text-[var(--fg-muted)]">
                  {new Date(t.createdAt).toLocaleDateString()}
                </td>
              </tr>
            ))}
            {tenants?.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-[var(--fg-muted)]">
                  No tenants yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
