import { NavLink, Outlet } from "react-router";
import {
  LayoutDashboard,
  Users,
  Mail,
  KeyRound,
  ScrollText,
  Building2,
  Import,
} from "lucide-react";
import { cn } from "@/components/ui/utils";

const adminLinks = [
  { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/users", label: "Users", icon: Users },
  { to: "/admin/invitations", label: "Invitations", icon: Mail },
  { to: "/admin/api-keys", label: "API Keys", icon: KeyRound },
  { to: "/admin/audit-log", label: "Audit Log", icon: ScrollText },
  { to: "/admin/tenants", label: "Tenants", icon: Building2 },
  { to: "/admin/import", label: "Import", icon: Import },
];

export function AdminLayout() {
  return (
    <div className="flex gap-6">
      {/* Sidebar */}
      <aside className="hidden w-56 shrink-0 md:block">
        <nav className="sticky top-6 space-y-1">
          <h2 className="mb-3 px-3 text-xs font-semibold uppercase tracking-wider text-[var(--fg-muted)]">
            Administration
          </h2>
          {adminLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors",
                  isActive
                    ? "bg-primary-50 text-primary-700 dark:bg-primary-950 dark:text-primary-300"
                    : "text-[var(--fg-muted)] hover:bg-[var(--bg-muted)] hover:text-[var(--fg)]",
                )
              }
            >
              <link.icon size={16} />
              {link.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <Outlet />
      </div>
    </div>
  );
}
