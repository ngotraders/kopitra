import { useMemo, type ReactNode } from 'react';
import { currentUser as defaultUser } from '../data/console.ts';
import type { ConsoleRole, ConsoleUser } from '../types/console.ts';
import { AuthContext, type AuthContextValue } from './auth-context.ts';

interface AuthProviderProps {
  children: ReactNode;
  user?: ConsoleUser;
}

export function AuthProvider({ children, user = defaultUser }: AuthProviderProps) {
  const value = useMemo<AuthContextValue>(() => {
    const normalizedRoles = Array.from(new Set(user.roles));
    const normalizedUser: ConsoleUser = { ...user, roles: normalizedRoles };

    const hasRole = (role: ConsoleRole) => normalizedUser.roles.includes(role);
    const hasAnyRole = (roles: ConsoleRole[]) => roles.some((role) => hasRole(role));

    return {
      user: normalizedUser,
      hasRole,
      hasAnyRole,
    };
  }, [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
