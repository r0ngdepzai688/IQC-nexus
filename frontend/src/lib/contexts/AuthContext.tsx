"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

export type Role = 'Team Leader' | 'Group Leader' | 'Part Leader' | 'Cell Leader' | 'Staff';
export type SystemRole = 'Administrator' | 'User';
export type DashboardProfile = 'Auto' | Role | 'Executive';
export type AccountStatus = 'Active' | 'Inactive' | 'Pending' | 'Locked';

interface AuthState {
  employeeId: string;
  name: string;
  position: Role;
  scope: string;
  systemRole: SystemRole;
  dashboardProfile: DashboardProfile;
  accountStatus: AccountStatus;
  avatar: string;
}

interface AuthContextProps {
  user: AuthState;
  loginAs: (user: Partial<AuthState>) => void;
}

const defaultUser: AuthState = {
  employeeId: '10525728',
  name: 'Bùi Thị Thúy',
  position: 'Staff',
  scope: 'Execution',
  systemRole: 'User',
  dashboardProfile: 'Auto',
  accountStatus: 'Active',
  avatar: ''
};

const AuthContext = createContext<AuthContextProps>({
  user: defaultUser,
  loginAs: () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<AuthState>(defaultUser);

  // Load from localStorage on mount
  React.useEffect(() => {
    if (typeof window !== 'undefined') {
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        try {
          const parsed = JSON.parse(storedUser);
          setUser({
            employeeId: parsed.username || defaultUser.employeeId,
            name: parsed.fullName || defaultUser.name,
            position: parsed.position || defaultUser.position,
            scope: parsed.scope || defaultUser.scope,
            systemRole: parsed.systemRole || defaultUser.systemRole,
            dashboardProfile: parsed.dashboardProfile || defaultUser.dashboardProfile,
            accountStatus: parsed.accountStatus || defaultUser.accountStatus,
            avatar: parsed.avatar || ''
          });
        } catch (e) {
          console.error("Failed to parse user from localStorage", e);
        }
      }
    }
  }, []);

  const loginAs = (updates: Partial<AuthState>) => {
    setUser(prev => ({ ...prev, ...updates }));
  };

  return (
    <AuthContext.Provider value={{ user, loginAs }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
