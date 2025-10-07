import { createContext } from 'react';
import type { OpsConsoleLoginRequest } from '../api/postOpsConsoleLogin.ts';
import type { ConsoleRole, ConsoleUser } from '../types/console.ts';

export interface AuthContextValue {
  user: ConsoleUser;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasRole: (role: ConsoleRole) => boolean;
  hasAnyRole: (roles: ConsoleRole[]) => boolean;
  signIn: (credentials: OpsConsoleLoginRequest) => Promise<void>;
  signOut: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
