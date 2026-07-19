"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

export type Role = 'Group Leader' | 'Part Leader' | 'Cell Leader' | 'Staff';
export type SystemRole = 'Administrator' | 'User';
export type AccountStatus = 'Active' | 'Inactive' | 'Pending' | 'Locked';

export interface AuthState {
  employeeId: string;
  name: string;
  position: Role;
  scope: string;
  systemRole: SystemRole;
  accountStatus: AccountStatus;
  avatar: string;
  organization: string;
  part: string;
  email: string;
  roleProfile: string;
}

interface AuthContextProps {
  user: AuthState;
  activeRoleLens: Role;
  setRoleLens: (lens: Role) => void;
  loginAs: (user: Partial<AuthState>) => void;
}

const defaultUser: AuthState = {
  employeeId: '',
  name: 'Authenticated User',
  position: 'Staff',
  scope: '',
  systemRole: 'User',
  accountStatus: 'Active',
  avatar: '',
  organization: '',
  part: '',
  email: '',
  roleProfile: ''
};

const AuthContext = createContext<AuthContextProps>({
  user: defaultUser,
  activeRoleLens: 'Staff',
  setRoleLens: () => {},
  loginAs: () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<AuthState>(defaultUser);
  const [activeRoleLens, setActiveRoleLens] = useState<Role>(defaultUser.position);

  // Load from localStorage on mount
  React.useEffect(() => {
    if (typeof window !== 'undefined') {
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        try {
          const parsed = JSON.parse(storedUser);
          const loadedUser: AuthState = {
            employeeId: parsed.username || defaultUser.employeeId,
            name: parsed.fullName || defaultUser.name,
            position: parsed.position || defaultUser.position,
            scope: parsed.scope || defaultUser.scope,
            systemRole: parsed.systemRole || defaultUser.systemRole,
            accountStatus: parsed.accountStatus || defaultUser.accountStatus,
            avatar: parsed.avatar || '',
            organization: parsed.organization || defaultUser.organization,
            part: parsed.part || defaultUser.part,
            email: parsed.email || defaultUser.email,
            roleProfile: parsed.roleProfile || defaultUser.roleProfile
          };
          setUser(loadedUser); // eslint-disable-line react-hooks/set-state-in-effect
          setActiveRoleLens(loadedUser.position);
        } catch (e) {
          console.error("Failed to parse user from localStorage", e);
        }
      }
    }
  }, []);

  const loginAs = (updates: Partial<AuthState>) => {
    setUser(prev => {
      const newUser = { ...prev, ...updates };
      // If position changes and we haven't overridden the lens, update lens too
      if (updates.position) {
        setActiveRoleLens(updates.position);
      }
      
      // Persist to localStorage so it survives reloads
      if (typeof window !== 'undefined') {
        const stored = localStorage.getItem('user');
        let parsed = stored ? JSON.parse(stored) : {};
        parsed = {
          ...parsed,
          username: newUser.employeeId,
          fullName: newUser.name,
          position: newUser.position,
          scope: newUser.scope,
          systemRole: newUser.systemRole,
          organization: newUser.organization,
          part: newUser.part,
          email: newUser.email,
          roleProfile: newUser.roleProfile
        };
        localStorage.setItem('user', JSON.stringify(parsed));
      }
      
      return newUser;
    });
  };

  const setRoleLens = (lens: Role) => {
    setActiveRoleLens(lens);
  };

  return (
    <AuthContext.Provider value={{ user, activeRoleLens, setRoleLens, loginAs }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);

