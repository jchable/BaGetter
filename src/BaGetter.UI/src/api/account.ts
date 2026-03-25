import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "./client";
import type { AppConfig, User } from "./types";

export function useCurrentUser() {
  return useQuery({
    queryKey: ["currentUser"],
    queryFn: () => api.get<User>("/api/ui/account/me", { skipAuthRedirect: true }),
    retry: false,
    staleTime: 60_000,
  });
}

export function useAppConfig() {
  return useQuery({
    queryKey: ["appConfig"],
    queryFn: () => api.get<AppConfig>("/api/ui/config"),
    staleTime: 300_000,
  });
}

export function useLogin() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (credentials: { email: string; password: string }) =>
      api.post("/api/ui/account/login", credentials),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => api.post("/api/ui/account/logout"),
    onSuccess: () => {
      queryClient.setQueryData(["currentUser"], null);
      queryClient.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });
}
