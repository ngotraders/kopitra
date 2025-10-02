import { useEffect, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import Header from '../components/Header/Header.tsx';
import Sidebar from '../components/Sidebar/Sidebar.tsx';
import { navigationItems } from '../data/console.ts';
import type { Environment } from '../types/console.ts';
import { Toast, type ToastProps } from '../components/Toast/Toast.tsx';
import { useAuthorization } from '../contexts';
import '../App.css';

export interface AppLayoutProps {
  onSignOut?: () => void;
}

type LayoutToast = Omit<ToastProps, 'onDismiss'>;

interface LayoutLocationState {
  toast?: LayoutToast;
  deniedFrom?: string;
}

function resolveEnvironment(search: string): Environment {
  const params = new URLSearchParams(search);
  return params.get('environment')?.toLowerCase() === 'sandbox' ? 'Sandbox' : 'Production';
}

export function AppLayout({ onSignOut }: AppLayoutProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuthorization();
  const environment = resolveEnvironment(location.search);
  const [toast, setToast] = useState<LayoutToast | null>(null);

  useEffect(() => {
    const state = location.state as LayoutLocationState | null;
    if (state?.toast) {
      const { toast: toastPayload, ...rest } = state;
      setToast(toastPayload);
      navigate(`${location.pathname}${location.search}`, {
        replace: true,
        state: Object.keys(rest).length ? rest : undefined,
      });
    }
  }, [location, navigate]);

  useEffect(() => {
    if (!toast) {
      return undefined;
    }

    const timeout = window.setTimeout(() => setToast(null), 5000);
    return () => window.clearTimeout(timeout);
  }, [toast]);

  return (
    <div className="app">
      {toast ? (
        <div className="app__toast">
          <Toast {...toast} onDismiss={() => setToast(null)} />
        </div>
      ) : null}
      <Header
        environment={environment}
        userName={user.name}
        onSignOut={onSignOut ?? (() => console.info('Signing out...'))}
      />
      <div className="app__content">
        <Sidebar items={navigationItems} />
        <main className="app__main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default AppLayout;
