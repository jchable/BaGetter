import { createContext, useContext } from "react";
import type { User } from "@/api/types";

interface AuthContextValue {
  user: User | null | undefined;
  isLoading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
}

export const AuthContext = createContext<AuthContextValue>({
  user: undefined,
  isLoading: true,
  isAuthenticated: false,
  isAdmin: false,
});

export function useAuth() {
  return useContext(AuthContext);
}
