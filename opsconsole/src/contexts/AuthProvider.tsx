import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchCurrentUser } from '../api/fetchCurrentUser.ts';
import { getStoredAccessToken, storeAccessToken } from '../api/authStorage.ts';
import { resetOpsConsoleSnapshotCache } from '../api/opsConsoleSnapshot.ts';
import { postOpsConsoleLogin, type OpsConsoleLoginRequest } from '../api/postOpsConsoleLogin.ts';
import { setIntegrationBearerToken } from '../api/integration/config.ts';
import type { ConsoleRole, ConsoleUser } from '../types/console.ts';
import { AuthContext, type AuthContextValue } from './auth-context.ts';

interface AuthProviderProps {
  children: ReactNode;
  user?: ConsoleUser;
}

export function AuthProvider({ children, user }: AuthProviderProps) {
  const queryClient = useQueryClient();
  const fallbackUser: ConsoleUser = { id: '', name: '', email: '', roles: [] };
  const [token, setToken] = useState<string | null>(() => {
    if (user) {
      return null;
    }

    const stored = getStoredAccessToken();
    if (stored) {
      setIntegrationBearerToken(stored);
    }
    return stored;
  });

  useEffect(() => {
    if (user) {
      setIntegrationBearerToken(null);
      return;
    }

    setIntegrationBearerToken(token);
  }, [token, user]);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['currentUser'],
    queryFn: fetchCurrentUser,
    enabled: Boolean(token) && !user,
    initialData: user,
    staleTime: 5 * 60 * 1000,
  });

  const resolvedUser = user ?? data ?? fallbackUser;
  const normalizedRoles = useMemo(() => Array.from(new Set(resolvedUser.roles)), [resolvedUser]);
  const normalizedUser: ConsoleUser = useMemo(
    () => ({ ...resolvedUser, roles: normalizedRoles }),
    [resolvedUser, normalizedRoles],
  );

  const isAuthenticated = Boolean(user || (token && data));
  const authLoading = Boolean(!user && token && (isLoading || isFetching));

  const hasRole = useCallback(
    (role: ConsoleRole) => normalizedRoles.includes(role),
    [normalizedRoles],
  );
  const hasAnyRole = useCallback(
    (roles: ConsoleRole[]) => roles.some((role) => normalizedRoles.includes(role)),
    [normalizedRoles],
  );

  const signIn = useCallback(
    async (credentials: OpsConsoleLoginRequest) => {
      const result = await postOpsConsoleLogin(credentials);
      storeAccessToken(result.token);
      setIntegrationBearerToken(result.token);
      setToken(result.token);
      resetOpsConsoleSnapshotCache();
      queryClient.setQueryData(['currentUser'], result.user);
    },
    [queryClient],
  );

  const signOut = useCallback(() => {
    storeAccessToken(null);
    setIntegrationBearerToken(null);
    setToken(null);
    resetOpsConsoleSnapshotCache();
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: normalizedUser,
      isAuthenticated,
      isLoading: authLoading,
      hasRole,
      hasAnyRole,
      signIn,
      signOut,
    }),
    [normalizedUser, isAuthenticated, authLoading, hasRole, hasAnyRole, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
