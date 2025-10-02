import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthorization } from '../contexts';
import type { ConsoleRole } from '../types/console.ts';

export interface RequireRolesProps {
  roles: ConsoleRole[];
  fallbackPath?: string;
}

const DEFAULT_FALLBACK = '/operations/overview';

export function RequireRoles({ roles, fallbackPath = DEFAULT_FALLBACK }: RequireRolesProps) {
  const { hasAnyRole } = useAuthorization();
  const location = useLocation();

  if (hasAnyRole(roles)) {
    return <Outlet />;
  }

  return (
    <Navigate
      to={fallbackPath}
      replace
      state={{
        toast: {
          intent: 'error' as const,
          title: 'Access denied',
          description: 'You do not have permission to view that section.',
        },
        deniedFrom: `${location.pathname}${location.search}`,
      }}
    />
  );
}

export default RequireRoles;
