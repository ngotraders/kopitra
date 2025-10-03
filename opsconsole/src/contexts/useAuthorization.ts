import { useContext } from 'react';
import { AuthContext } from './auth-context.ts';

export function useAuthorization() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuthorization must be used within an AuthProvider');
  }

  return context;
}
