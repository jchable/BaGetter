import { BrowserRouter, Routes, Route } from "react-router";
import { useMemo } from "react";
import { useCurrentUser } from "@/api/account";
import { AuthContext } from "@/hooks/useAuth";
import { AppShell } from "@/components/layout/AppShell";
import { AuthLayout } from "@/components/layout/AuthLayout";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { SearchPage } from "@/pages/SearchPage";
import { PackagePage } from "@/pages/PackagePage";
import { UploadPage } from "@/pages/UploadPage";
import { FeedInfoPage } from "@/pages/FeedInfoPage";
import { StatisticsPage } from "@/pages/StatisticsPage";
import { LoginPage } from "@/pages/LoginPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { ForgotPasswordPage } from "@/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "@/pages/ResetPasswordPage";
import { ManagePage } from "@/pages/ManagePage";
import { AccessDeniedPage } from "@/pages/AccessDeniedPage";
import { AdminDashboardPage } from "@/pages/AdminDashboardPage";
import { AdminUsersPage } from "@/pages/AdminUsersPage";
import { AdminInvitationsPage } from "@/pages/AdminInvitationsPage";
import { AdminApiKeysPage } from "@/pages/AdminApiKeysPage";
import { AdminAuditLogPage } from "@/pages/AdminAuditLogPage";
import { AdminTenantsPage } from "@/pages/AdminTenantsPage";
import { AdminImportPage } from "@/pages/AdminImportPage";
import { NotFoundPage } from "@/pages/NotFoundPage";

export function App() {
  const { data: user, isLoading } = useCurrentUser();

  const authValue = useMemo(
    () => ({
      user: user ?? null,
      isLoading,
      isAuthenticated: !!user,
      isAdmin: user?.roles?.some((r) => r === "Admin" || r === "Owner") ?? false,
    }),
    [user, isLoading],
  );

  return (
    <AuthContext.Provider value={authValue}>
      <BrowserRouter>
        <Routes>
          {/* Public pages with AppShell */}
          <Route element={<AppShell />}>
            <Route index element={<SearchPage />} />
            <Route path="packages/:id/:version?" element={<PackagePage />} />
            <Route path="upload" element={<UploadPage />} />
            <Route path="feed-info" element={<FeedInfoPage />} />
            <Route path="stats" element={<StatisticsPage />} />
            <Route path="account/manage" element={<ManagePage />} />
            <Route path="account/access-denied" element={<AccessDeniedPage />} />

            {/* Admin pages */}
            <Route path="admin" element={<AdminLayout />}>
              <Route index element={<AdminDashboardPage />} />
              <Route path="users" element={<AdminUsersPage />} />
              <Route path="invitations" element={<AdminInvitationsPage />} />
              <Route path="api-keys" element={<AdminApiKeysPage />} />
              <Route path="audit-log" element={<AdminAuditLogPage />} />
              <Route path="tenants" element={<AdminTenantsPage />} />
              <Route path="import" element={<AdminImportPage />} />
            </Route>

            <Route path="*" element={<NotFoundPage />} />
          </Route>

          {/* Auth pages with AuthLayout */}
          <Route element={<AuthLayout />}>
            <Route path="account/login" element={<LoginPage />} />
            <Route path="account/register" element={<RegisterPage />} />
            <Route path="account/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="account/reset-password" element={<ResetPasswordPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthContext.Provider>
  );
}
