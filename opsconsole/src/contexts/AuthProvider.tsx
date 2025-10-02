import { useMemo, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchCurrentUser } from '../api/fetchCurrentUser.ts';
import { currentUser as defaultUser } from '../data/console.ts';
import type { ConsoleRole, ConsoleUser } from '../types/console.ts';
import { AuthContext, type AuthContextValue } from './auth-context.ts';

interface AuthProviderProps {
  children: ReactNode;
  user?: ConsoleUser;
}

export function AuthProvider({ children, user }: AuthProviderProps) {
  const { data } = useQuery({
    queryKey: ['currentUser'],
    queryFn: fetchCurrentUser,
    enabled: !user,
    initialData: user ?? defaultUser,
    staleTime: 5 * 60 * 1000,
  });

  const resolvedUser = user ?? data ?? defaultUser;

  const value = useMemo<AuthContextValue>(() => {
    const normalizedRoles = Array.from(new Set(resolvedUser.roles));
    const normalizedUser: ConsoleUser = { ...resolvedUser, roles: normalizedRoles };

    const hasRole = (role: ConsoleRole) => normalizedUser.roles.includes(role);
    const hasAnyRole = (roles: ConsoleRole[]) => roles.some((role) => hasRole(role));

    return {
      user: normalizedUser,
      hasRole,
      hasAnyRole,
    };
  }, [resolvedUser]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
