import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthorization } from '../contexts';

const LOGIN_PATH = '/login';

export function RequireAuth() {
  const { isAuthenticated, isLoading } = useAuthorization();
  const location = useLocation();

  if (isLoading) {
    return (
      <div role="status" aria-live="polite" className="auth-loading">
        Verifying session...
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to={LOGIN_PATH}
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    );
  }

  return <Outlet />;
}

export default RequireAuth;
