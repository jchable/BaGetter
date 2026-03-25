import { useQuery } from "@tanstack/react-query";
import { api } from "./client";
import type { DashboardStats, AdminUser, Invitation, ApiKey, AuditLogEntry, Tenant } from "./types";

export function useDashboardStats() {
  return useQuery({
    queryKey: ["admin", "dashboard"],
    queryFn: () => api.get<DashboardStats>("/api/ui/admin/dashboard"),
  });
}

export function useAdminUsers() {
  return useQuery({
    queryKey: ["admin", "users"],
    queryFn: () => api.get<AdminUser[]>("/api/ui/admin/users"),
  });
}

export function useInvitations() {
  return useQuery({
    queryKey: ["admin", "invitations"],
    queryFn: () => api.get<Invitation[]>("/api/ui/admin/invitations"),
  });
}

export function useApiKeys() {
  return useQuery({
    queryKey: ["admin", "api-keys"],
    queryFn: () => api.get<ApiKey[]>("/api/ui/admin/api-keys"),
  });
}

export function useAuditLog(params?: { action?: string; userId?: string; page?: number }) {
  return useQuery({
    queryKey: ["admin", "audit-log", params],
    queryFn: () => {
      const url = new URL("/api/ui/admin/audit-log", window.location.origin);
      if (params?.action) url.searchParams.set("action", params.action);
      if (params?.userId) url.searchParams.set("userId", params.userId);
      if (params?.page) url.searchParams.set("page", String(params.page));
      return api.get<AuditLogEntry[]>(url.pathname + url.search);
    },
  });
}

export function useTenants() {
  return useQuery({
    queryKey: ["admin", "tenants"],
    queryFn: () => api.get<Tenant[]>("/api/ui/admin/tenants"),
  });
}
