"use client";

import { AuthUser, authClient, normalizeAuthUser, PlatformPermission } from "@/lib/auth/client";
import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from "react";

export type Role = "Group Leader" | "Part Leader" | "Cell Leader" | "Staff";
export type AuthState = AuthUser;

export interface AuthContextValue {
  user: AuthUser;
  status: "loading" | "authenticated" | "anonymous" | "error";
  error: string | null;
  can: (permission: PlatformPermission) => boolean;
  hasRole: (role: string) => boolean;
  login: (username: string, password: string) => Promise<AuthUser>;
  signOut: () => Promise<void>;
  activeRoleLens: Role;
  setRoleLens: (role: Role) => void;
  loginAs: (updates: Partial<AuthUser>) => void;
}

const emptyUser = normalizeAuthUser(null);

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser>(() => {
    if (authClient.hasToken()) {
      const cached = authClient.getStoredUser();
      if (cached) return cached;
    }
    return emptyUser;
  });

  const [status, setStatus] = useState<AuthContextValue["status"]>(() =>
    authClient.hasToken() ? "loading" : "anonymous"
  );
  const [error, setError] = useState<string | null>(null);

  const [activeRoleLens, setRoleLens] = useState<Role>(() => {
    if (authClient.hasToken()) {
      const cached = authClient.getStoredUser();
      if (cached && ["Group Leader", "Part Leader", "Cell Leader", "Staff"].includes(cached.position)) {
        return cached.position as Role;
      }
    }
    return "Staff";
  });

  useEffect(() => {
    if (!authClient.hasToken()) {
      return;
    }

    authClient.currentUser()
      .then((currentUser) => {
        setUser(currentUser);
        if (["Group Leader", "Part Leader", "Cell Leader", "Staff"].includes(currentUser.position)) {
          setRoleLens(currentUser.position as Role);
        }
        setStatus("authenticated");
        setError(null);
      })
      .catch((err: unknown) => {
        const authErr = err as { status?: number; message?: string };
        if (authErr?.status === 401) {
          setUser(emptyUser);
          setStatus("anonymous");
        } else {
          // If network error but we have cached user from initial state, stay authenticated
          if (authClient.getStoredUser()) {
            setStatus("authenticated");
          } else {
            setStatus("error");
            setError(authErr?.message || "Failed to authenticate session.");
          }
        }
      });
  }, []);

  const login = async (username: string, password: string): Promise<AuthUser> => {
    try {
      setStatus("loading");
      setError(null);
      const authenticatedUser = await authClient.login(username, password);
      setUser(authenticatedUser);
      if (["Group Leader", "Part Leader", "Cell Leader", "Staff"].includes(authenticatedUser.position)) {
        setRoleLens(authenticatedUser.position as Role);
      }
      setStatus("authenticated");
      return authenticatedUser;
    } catch (err: unknown) {
      setStatus("anonymous");
      const authErr = err as { message?: string };
      const msg = authErr?.message || "Invalid credentials or network error.";
      setError(msg);
      throw err;
    }
  };

  const signOut = async () => {
    try {
      await authClient.logout();
    } finally {
      setUser(emptyUser);
      setStatus("anonymous");
      setError(null);
    }
  };

  const loginAs = (updates: Partial<AuthUser>) => {
    setUser((current) => normalizeAuthUser({ ...current, ...updates }));
  };

  const can = useCallback(
    (permission: PlatformPermission): boolean => {
      if (user.systemRole === "Administrator") return true;
      return user.permissions.includes(permission);
    },
    [user.systemRole, user.permissions]
  );

  const hasRole = useCallback(
    (role: string): boolean => {
      if (user.systemRole === "Administrator") return true;
      return user.roles.some((r) => r.toLowerCase() === role.toLowerCase());
    },
    [user.systemRole, user.roles]
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      status,
      error,
      can,
      hasRole,
      login,
      signOut,
      activeRoleLens,
      setRoleLens,
      loginAs,
    }),
    [user, status, error, can, hasRole, activeRoleLens]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return value;
}

export function usePermission(permission: PlatformPermission): boolean {
  const { can } = useAuth();
  return can(permission);
}

export function useAnyPermission(permissions: PlatformPermission[]): boolean {
  const { can } = useAuth();
  return permissions.some((permission) => can(permission));
}