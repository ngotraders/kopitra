import { Outlet, useLocation } from 'react-router-dom';
import Header from '../components/Header/Header.tsx';
import Sidebar from '../components/Sidebar/Sidebar.tsx';
import { navigationItems } from '../data/console.ts';
import type { Environment } from '../types/console.ts';
import '../App.css';

export interface AppLayoutProps {
  onSignOut?: () => void;
}

function resolveEnvironment(search: string): Environment {
  const params = new URLSearchParams(search);
  return params.get('environment')?.toLowerCase() === 'sandbox' ? 'Sandbox' : 'Production';
}

export function AppLayout({ onSignOut }: AppLayoutProps) {
  const location = useLocation();
  const environment = resolveEnvironment(location.search);

  return (
    <div className="app">
      <Header
        environment={environment}
        userName="Alex Morgan"
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
