import { useState } from 'react';
import Header from './components/Header/Header.tsx';
import Sidebar from './components/Sidebar/Sidebar.tsx';
import { Dashboard } from './features/dashboard/Dashboard.tsx';
import { navigationItems } from './data/dashboard.ts';
import type { NavigationItem } from './types/dashboard.ts';
import './App.css';

export interface AppProps {
  initialNavId?: NavigationItem['id'];
  onSignOut?: () => void;
}

function App({ initialNavId = navigationItems[0].id, onSignOut }: AppProps) {
  const [activeNav, setActiveNav] = useState(initialNavId);

  return (
    <div className="app">
      <Header
        environment={activeNav === 'overview' ? 'Production' : 'Sandbox'}
        userName="Alex Morgan"
        onSignOut={onSignOut ?? (() => console.info('Signing out...'))}
      />
      <div className="app__content">
        <Sidebar
          items={navigationItems}
          activeId={activeNav}
          onSelect={(item) => setActiveNav(item.id)}
        />
        <main className="app__main">
          <Dashboard />
        </main>
      </div>
    </div>
  );
}

export default App;
