import { createContext } from 'react';
import type { ConsoleRole, ConsoleUser } from '../types/console.ts';

export interface AuthContextValue {
  user: ConsoleUser;
  hasRole: (role: ConsoleRole) => boolean;
  hasAnyRole: (roles: ConsoleRole[]) => boolean;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
