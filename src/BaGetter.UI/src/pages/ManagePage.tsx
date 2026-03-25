import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/api/client";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { CopyButton } from "@/components/common/CopyButton";
import { AlertCircle, Check, KeyRound, Trash2 } from "lucide-react";

interface ApiKeyResponse {
  id: number;
  name: string;
  keyPrefix: string;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
  isRevoked: boolean;
}

export function ManagePage() {
  const { user, isLoading: authLoading } = useAuth();

  if (authLoading) return <LoadingSpinner className="py-20" />;
  if (!user) return null;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">My Account</h1>
      <ProfileSection displayName={user.displayName} email={user.email} />
      <PasswordSection />
      <ApiKeysSection />
    </div>
  );
}

function ProfileSection({ displayName: initial, email }: { readonly displayName: string; readonly email: string }) {
  const [displayName, setDisplayName] = useState(initial);
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const update = useMutation({
    mutationFn: (data: { displayName: string }) => api.put("/api/ui/account/profile", data),
    onSuccess: () => {
      setMessage("Profile updated.");
      queryClient.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Profile</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            update.mutate({ displayName });
          }}
          className="space-y-3"
        >
          <div>
            <label htmlFor="email" className="mb-1 block text-sm font-medium">Email</label>
            <Input id="email" value={email} disabled />
          </div>
          <div>
            <label htmlFor="displayName" className="mb-1 block text-sm font-medium">
              Display name
            </label>
            <Input
              id="displayName"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              required
              maxLength={100}
            />
          </div>
          {message && (
            <div className="flex items-center gap-1 text-sm text-green-600">
              <Check size={14} /> {message}
            </div>
          )}
          <Button type="submit" disabled={update.isPending}>
            {update.isPending ? "Saving..." : "Save"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function PasswordSection() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const change = useMutation({
    mutationFn: (data: { currentPassword: string; newPassword: string }) =>
      api.put("/api/ui/account/password", data),
    onSuccess: () => {
      setMessage("Password changed.");
      setError(null);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    },
    onError: (err) => {
      const body = (err as { body?: { error?: string } }).body;
      setError(body?.error ?? "Failed to change password.");
      setMessage(null);
    },
  });

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setMessage(null);
    if (newPassword !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    change.mutate({ currentPassword, newPassword });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Change Password</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label htmlFor="currentPassword" className="mb-1 block text-sm font-medium">
              Current password
            </label>
            <Input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
            />
          </div>
          <div>
            <label htmlFor="newPassword" className="mb-1 block text-sm font-medium">
              New password
            </label>
            <Input
              id="newPassword"
              type="password"
              required
              minLength={8}
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </div>
          <div>
            <label htmlFor="confirmNewPassword" className="mb-1 block text-sm font-medium">
              Confirm new password
            </label>
            <Input
              id="confirmNewPassword"
              type="password"
              required
              minLength={8}
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
          </div>
          {error && (
            <div className="flex items-center gap-1 text-sm text-red-600">
              <AlertCircle size={14} /> {error}
            </div>
          )}
          {message && (
            <div className="flex items-center gap-1 text-sm text-green-600">
              <Check size={14} /> {message}
            </div>
          )}
          <Button type="submit" disabled={change.isPending}>
            {change.isPending ? "Changing..." : "Change Password"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function ApiKeysSection() {
  const [newKeyName, setNewKeyName] = useState("");
  const [createdKey, setCreatedKey] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const { data: keys, isLoading } = useQuery({
    queryKey: ["my-api-keys"],
    queryFn: () => api.get<ApiKeyResponse[]>("/api/ui/account/api-keys"),
  });

  const create = useMutation({
    mutationFn: (name: string) => api.post<{ key: string; id: number; name: string }>("/api/ui/account/api-keys", { name }),
    onSuccess: (data) => {
      setCreatedKey(data.key);
      setNewKeyName("");
      queryClient.invalidateQueries({ queryKey: ["my-api-keys"] });
    },
  });

  const revoke = useMutation({
    mutationFn: (id: number) => api.delete(`/api/ui/account/api-keys/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["my-api-keys"] }),
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">API Keys</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Create new key */}
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (newKeyName.trim()) create.mutate(newKeyName.trim());
          }}
          className="flex gap-2"
        >
          <Input
            placeholder="Key name"
            value={newKeyName}
            onChange={(e) => setNewKeyName(e.target.value)}
            required
            maxLength={256}
          />
          <Button type="submit" disabled={create.isPending}>
            <KeyRound size={14} />
            Create
          </Button>
        </form>

        {/* Newly created key warning */}
        {createdKey && (
          <div className="rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
            <p className="mb-1 text-sm font-medium text-amber-800 dark:text-amber-200">
              Copy your API key now — it won&apos;t be shown again.
            </p>
            <div className="flex items-center gap-2 rounded bg-white px-2 py-1 font-mono text-sm dark:bg-surface-800">
              <span className="flex-1 overflow-x-auto">{createdKey}</span>
              <CopyButton text={createdKey} />
            </div>
          </div>
        )}

        {/* Keys list */}
        {isLoading && <LoadingSpinner />}
        {keys?.length === 0 && (
          <p className="text-sm text-[var(--fg-muted)]">No API keys.</p>
        )}
        {keys?.map((key) => (
          <div
            key={key.id}
            className="flex items-center justify-between rounded-md border border-[var(--border)] px-3 py-2"
          >
            <div>
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium">{key.name}</span>
                <code className="text-xs text-[var(--fg-muted)]">{key.keyPrefix}...</code>
                {key.isRevoked && <Badge variant="destructive">Revoked</Badge>}
              </div>
              <p className="text-xs text-[var(--fg-muted)]">
                Created {new Date(key.createdAt).toLocaleDateString()}
                {key.lastUsedAt && ` · Last used ${new Date(key.lastUsedAt).toLocaleDateString()}`}
              </p>
            </div>
            {!key.isRevoked && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => revoke.mutate(key.id)}
                title="Revoke key"
              >
                <Trash2 size={14} className="text-red-500" />
              </Button>
            )}
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
