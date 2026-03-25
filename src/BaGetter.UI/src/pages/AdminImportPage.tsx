import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { api } from "@/api/client";
import { useSSE } from "@/hooks/useSSE";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Import, X, AlertCircle } from "lucide-react";

interface ImportProgress {
  totalVersions: number;
  imported: number;
  skipped: number;
  failed: number;
  currentPackage: string | null;
  errors: string[];
  isComplete: boolean;
}

function getAuthTypeValue(authType: string): number {
  if (authType === "ApiKey") return 1;
  if (authType === "Basic") return 2;
  return 0;
}

export function AdminImportPage() {
  const [feedUrl, setFeedUrl] = useState("");
  const [legacy, setLegacy] = useState(false);
  const [authType, setAuthType] = useState("None");
  const [apiKey, setApiKey] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sseUrl = isRunning ? "/admin/api/import/progress" : null;
  const { data: progress } = useSSE<ImportProgress>(sseUrl);

  const start = useMutation({
    mutationFn: () =>
      api.post("/admin/api/import/start", {
        feedUrl,
        legacy,
        authType: getAuthTypeValue(authType),
        apiKey: authType === "ApiKey" ? apiKey : undefined,
        username: authType === "Basic" ? username : undefined,
        password: authType === "Basic" ? password : undefined,
      }),
    onSuccess: () => {
      setIsRunning(true);
      setError(null);
    },
    onError: (err) => {
      const body = (err as { body?: { error?: string } }).body;
      setError(body?.error ?? "Failed to start import.");
    },
  });

  const cancel = useMutation({
    mutationFn: () => api.post("/admin/api/import/cancel"),
    onSuccess: () => setIsRunning(false),
  });

  // Auto-stop when complete
  if (progress?.isComplete && isRunning) {
    setIsRunning(false);
  }

  const total = progress?.totalVersions ?? 0;
  const done = (progress?.imported ?? 0) + (progress?.skipped ?? 0) + (progress?.failed ?? 0);
  const pct = total > 0 ? Math.round((done / total) * 100) : 0;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Feed Import</h1>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-base">Import packages from a remote NuGet feed</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              start.mutate();
            }}
            className="space-y-4"
          >
            <div>
              <label htmlFor="feedUrl" className="mb-1 block text-sm font-medium">Feed URL</label>
              <Input
                id="feedUrl"
                type="url"
                placeholder="https://api.nuget.org/v3/index.json"
                value={feedUrl}
                onChange={(e) => setFeedUrl(e.target.value)}
                required
                disabled={isRunning}
              />
            </div>

            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={legacy}
                onChange={(e) => setLegacy(e.target.checked)}
                disabled={isRunning}
                className="rounded border-[var(--border)]"
              />
              Legacy feed (NuGet v2 / OData)
            </label>

            <div>
              <label htmlFor="authType" className="mb-1 block text-sm font-medium">Authentication</label>
              <select
                id="authType"
                value={authType}
                onChange={(e) => setAuthType(e.target.value)}
                className="h-9 w-full rounded-md border border-[var(--border)] bg-[var(--bg-card)] px-3 text-sm"
                disabled={isRunning}
              >
                <option value="None">None</option>
                <option value="ApiKey">API Key</option>
                <option value="Basic">Basic Auth</option>
              </select>
            </div>

            {authType === "ApiKey" && (
              <div>
                <label htmlFor="importApiKey" className="mb-1 block text-sm font-medium">API Key</label>
                <Input
                  id="importApiKey"
                  type="password"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  disabled={isRunning}
                />
              </div>
            )}

            {authType === "Basic" && (
              <div className="flex gap-2">
                <div className="flex-1">
                  <label htmlFor="importUsername" className="mb-1 block text-sm font-medium">Username</label>
                  <Input
                    id="importUsername"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    disabled={isRunning}
                  />
                </div>
                <div className="flex-1">
                  <label htmlFor="importPassword" className="mb-1 block text-sm font-medium">Password</label>
                  <Input
                    id="importPassword"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    disabled={isRunning}
                  />
                </div>
              </div>
            )}

            {error && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <AlertCircle size={14} /> {error}
              </div>
            )}

            <div className="flex gap-2">
              <Button type="submit" disabled={isRunning || start.isPending}>
                <Import size={14} />
                {start.isPending ? "Starting..." : "Start Import"}
              </Button>
              {isRunning && (
                <Button type="button" variant="destructive" onClick={() => cancel.mutate()}>
                  <X size={14} />
                  Cancel
                </Button>
              )}
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Progress */}
      {progress && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              {progress.isComplete ? "Import Complete" : "Importing..."}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {/* Progress bar */}
            <div className="h-2 overflow-hidden rounded-full bg-[var(--bg-muted)]">
              <div
                className="h-full rounded-full bg-primary-500 transition-all"
                style={{ width: `${pct}%` }}
              />
            </div>

            <div className="flex flex-wrap gap-4 text-sm">
              <span>Imported: <strong>{progress.imported}</strong></span>
              <span>Skipped: <strong>{progress.skipped}</strong></span>
              <span>Failed: <strong>{progress.failed}</strong></span>
              <span>Total: <strong>{progress.totalVersions}</strong></span>
            </div>

            {progress.currentPackage && !progress.isComplete && (
              <p className="text-sm text-[var(--fg-muted)]">
                Current: {progress.currentPackage}
              </p>
            )}

            {progress.errors.length > 0 && (
              <div className="max-h-40 overflow-y-auto rounded border border-red-200 bg-red-50 p-2 text-xs text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-300">
                {progress.errors.map((err) => (
                  <div key={err}>{err}</div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
