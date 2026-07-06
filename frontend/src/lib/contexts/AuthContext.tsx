"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

export type Role = 'Team Leader' | 'Group Leader' | 'Part Leader' | 'Cell Leader' | 'Staff';
export type SystemRole = 'Administrator' | 'User';
export type AccountStatus = 'Active' | 'Inactive' | 'Pending' | 'Locked';

interface AuthState {
  employeeId: string;
  name: string;
  position: Role;
  scope: string;
  systemRole: SystemRole;
  accountStatus: AccountStatus;
  avatar: string;
}

interface AuthContextProps {
  user: AuthState;
  activeRoleLens: Role;
  setRoleLens: (lens: Role) => void;
  loginAs: (user: Partial<AuthState>) => void;
}

const defaultUser: AuthState = {
  employeeId: '10545998',
  name: 'Nguyễn Văn A',
  position: 'Team Leader',
  scope: 'Factory',
  systemRole: 'Administrator',
  accountStatus: 'Active',
  avatar: ''
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
            avatar: parsed.avatar || ''
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
          systemRole: newUser.systemRole
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

