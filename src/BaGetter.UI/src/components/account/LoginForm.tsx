import { useState } from "react";
import { useNavigate, useSearchParams, Link } from "react-router";
import { useLogin } from "@/api/account";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { OAuthButtons } from "./OAuthButtons";
import { AlertCircle, Mail, Lock, Eye, EyeOff } from "lucide-react";

export function LoginForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const login = useLogin();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnUrl = searchParams.get("returnUrl") ?? "/";
  const oauthError = searchParams.get("error");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    login.mutate(
      { email, password },
      {
        onSuccess: () => navigate(returnUrl),
        onError: (err) => {
          const body = (err as { body?: { error?: string } }).body;
          setError(body?.error ?? "Login failed. Please try again.");
        },
      },
    );
  };

  const displayError = error ?? oauthError;

  return (
    <div>
      <div className="mb-6 text-center">
        <h2 className="text-xl font-semibold text-[var(--fg)]">Welcome back</h2>
        <p className="mt-1 text-sm text-[var(--fg-muted)]">
          Sign in to your feed
        </p>
      </div>

      {displayError && (
        <div className="mb-5 flex items-start gap-2.5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800/60 dark:bg-red-950/50 dark:text-red-300">
          <AlertCircle size={16} className="mt-0.5 shrink-0" />
          <span>{displayError}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-[var(--fg)]">
            Email
          </label>
          <div className="relative">
            <Mail size={16} className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-[var(--fg-muted)]" />
            <Input
              id="email"
              type="email"
              required
              autoComplete="email"
              autoFocus
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              className="pl-9"
            />
          </div>
        </div>

        <div>
          <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-[var(--fg)]">
            Password
          </label>
          <div className="relative">
            <Lock size={16} className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-[var(--fg-muted)]" />
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="pr-9 pl-9"
            />
            <button
              type="button"
              tabIndex={-1}
              onClick={() => setShowPassword(!showPassword)}
              className="absolute top-1/2 right-3 -translate-y-1/2 text-[var(--fg-muted)] transition-colors hover:text-[var(--fg)]"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <label className="flex cursor-pointer items-center gap-2 text-sm text-[var(--fg-muted)] select-none">
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              className="h-4 w-4 rounded border-[var(--border)] accent-primary-600"
            />{" "}
            Remember me
          </label>
          <Link
            to="/account/forgot-password"
            className="text-sm font-medium text-primary-600 transition-colors hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
          >
            Forgot password?
          </Link>
        </div>

        <Button
          type="submit"
          className="mt-2 h-10 w-full text-sm font-medium"
          disabled={login.isPending}
        >
          {login.isPending ? (
            <span className="inline-flex items-center gap-2">
              <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              Signing in...
            </span>
          ) : (
            "Sign in"
          )}
        </Button>
      </form>

      <OAuthButtons returnUrl={returnUrl} />
    </div>
  );
}
