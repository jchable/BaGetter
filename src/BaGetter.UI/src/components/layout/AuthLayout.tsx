import { Link, Outlet } from "react-router";
import { Package } from "lucide-react";

export function AuthLayout() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--bg-muted)] px-4">
      {/* Subtle background decoration */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-primary-500/5 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-accent-500/5 blur-3xl" />
      </div>

      <div className="relative w-full max-w-sm">
        {/* Logo */}
        <div className="mb-8 text-center">
          <Link to="/" className="group inline-flex flex-col items-center gap-3">
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary-600 shadow-lg shadow-primary-500/25 transition-transform group-hover:scale-105 dark:bg-primary-500">
              <Package size={28} className="text-white" />
            </div>
            <span className="text-2xl font-bold tracking-tight text-[var(--fg)]">
              BaGetter
            </span>
          </Link>
        </div>

        {/* Card */}
        <div className="rounded-2xl border border-[var(--border)] bg-[var(--bg-card)] p-8 shadow-xl shadow-black/5 dark:shadow-black/20">
          <Outlet />
        </div>

        {/* Footer */}
        <p className="mt-6 text-center text-xs text-[var(--fg-muted)]">
          A lightweight NuGet and symbol server
        </p>
      </div>
    </div>
  );
}
