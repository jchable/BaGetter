import { useState } from "react";
import { Link, Outlet, useNavigate, useSearchParams } from "react-router";
import {
  Package,
  Upload,
  BarChart3,
  Menu,
  X,
  User,
  LogOut,
  Settings,
  ChevronDown,
  Search,
  LayoutDashboard,
  Users,
  Mail,
  KeyRound,
  ScrollText,
  Building2,
  Import,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/hooks/useAuth";
import { useAppConfig, useLogout } from "@/api/account";

export function AppShell() {
  const { user, isAuthenticated, isAdmin } = useAuth();
  const { data: config } = useAppConfig();
  const [menuOpen, setMenuOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [searchQuery, setSearchQuery] = useState(searchParams.get("q") ?? "");
  const logout = useLogout();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    navigate(`/?q=${encodeURIComponent(searchQuery)}`);
  };

  const handleLogout = () => {
    logout.mutate(undefined, {
      onSuccess: () => navigate("/account/login"),
    });
  };

  return (
    <div className="flex min-h-screen flex-col">
      {/* Navbar */}
      <header className="border-b border-[var(--border)] bg-[var(--bg-card)]">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4">
          {/* Logo + nav */}
          <div className="flex items-center gap-6">
            <Link to="/" className="flex items-center gap-2 font-bold text-primary-600 dark:text-primary-400">
              <Package size={24} />
              <span className="text-lg">BaGetter</span>
            </Link>

            <nav className="hidden items-center gap-1 md:flex">
              <Link to="/">
                <Button variant="ghost" size="sm">
                  <Package size={16} />
                  Packages
                </Button>
              </Link>
              <Link to="/upload">
                <Button variant="ghost" size="sm">
                  <Upload size={16} />
                  Upload
                </Button>
              </Link>
              {config?.statisticsEnabled && (
                <Link to="/stats">
                  <Button variant="ghost" size="sm">
                    <BarChart3 size={16} />
                    Statistics
                  </Button>
                </Link>
              )}
            </nav>
          </div>

          {/* Right side */}
          <div className="flex items-center gap-2">
            {isAuthenticated && user ? (
              <div className="relative">
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1"
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                >
                  <User size={16} />
                  <span className="hidden sm:inline">{user.displayName || user.userName}</span>
                  <ChevronDown size={14} />
                </Button>

                {userMenuOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-40"
                      onClick={() => setUserMenuOpen(false)}
                    />
                    <div className="absolute right-0 z-50 mt-1 w-48 rounded-md border border-[var(--border)] bg-[var(--bg-card)] py-1 shadow-lg">
                      <Link
                        to="/account/manage"
                        className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]"
                        onClick={() => setUserMenuOpen(false)}
                      >
                        <Settings size={14} />
                        My Account
                      </Link>
                      {isAdmin && (
                        <>
                          <hr className="my-1 border-[var(--border)]" />
                          <div className="px-3 py-1 text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
                            Administration
                          </div>
                          <Link to="/admin" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <LayoutDashboard size={14} /> Dashboard
                          </Link>
                          <Link to="/admin/users" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <Users size={14} /> Users
                          </Link>
                          <Link to="/admin/invitations" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <Mail size={14} /> Invitations
                          </Link>
                          <Link to="/admin/api-keys" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <KeyRound size={14} /> API Keys
                          </Link>
                          <Link to="/admin/audit-log" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <ScrollText size={14} /> Audit Log
                          </Link>
                          <Link to="/admin/tenants" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <Building2 size={14} /> Tenants
                          </Link>
                          <Link to="/admin/import" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--bg-muted)]" onClick={() => setUserMenuOpen(false)}>
                            <Import size={14} /> Import
                          </Link>
                        </>
                      )}
                      <hr className="my-1 border-[var(--border)]" />
                      <button
                        className="flex w-full items-center gap-2 px-3 py-2 text-sm text-red-600 hover:bg-[var(--bg-muted)]"
                        onClick={handleLogout}
                      >
                        <LogOut size={14} />
                        Sign Out
                      </button>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <Link to="/account/login">
                <Button variant="outline" size="sm">
                  Sign In
                </Button>
              </Link>
            )}

            {/* Mobile menu toggle */}
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              onClick={() => setMenuOpen(!menuOpen)}
            >
              {menuOpen ? <X size={20} /> : <Menu size={20} />}
            </Button>
          </div>
        </div>

        {/* Mobile nav */}
        {menuOpen && (
          <nav className="border-t border-[var(--border)] px-4 py-2 md:hidden">
            <Link to="/" className="block py-2 text-sm" onClick={() => setMenuOpen(false)}>
              Packages
            </Link>
            <Link to="/upload" className="block py-2 text-sm" onClick={() => setMenuOpen(false)}>
              Upload
            </Link>
            {config?.statisticsEnabled && (
              <Link to="/stats" className="block py-2 text-sm" onClick={() => setMenuOpen(false)}>
                Statistics
              </Link>
            )}
          </nav>
        )}
      </header>

      {/* Search bar */}
      <div className="border-b border-[var(--border)] bg-primary-600 dark:bg-primary-900">
        <div className="mx-auto max-w-7xl px-4 py-4">
          <form onSubmit={handleSearch} className="flex gap-2">
            <div className="relative flex-1">
              <Search
                size={18}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-surface-400"
              />
              <Input
                type="search"
                placeholder="Search packages..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="border-primary-500 bg-white/95 pl-10 dark:bg-surface-800"
              />
            </div>
            <Button type="submit" variant="accent">
              Search
            </Button>
          </form>
        </div>
      </div>

      {/* Main content */}
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
        <Outlet />
      </main>

      {/* Footer */}
      <footer className="border-t border-[var(--border)] bg-[var(--bg-card)]">
        <div className="mx-auto max-w-7xl px-4 py-4 text-center text-sm text-[var(--fg-muted)]">
          Powered by{" "}
          <a
            href="https://github.com/bagetter/BaGetter"
            className="text-primary-500 hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            BaGetter
          </a>
        </div>
      </footer>
    </div>
  );
}
