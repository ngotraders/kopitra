import { NavLink } from 'react-router-dom';
import type { NavigationItem } from '../../types/console.ts';
import './Sidebar.css';

export interface SidebarProps {
  items: NavigationItem[];
}

export function Sidebar({ items }: SidebarProps) {
  return (
    <nav className="sidebar" aria-label="Primary">
      <ul className="sidebar__list">
        {items.map((item) => {
          return (
            <li key={item.id}>
              <NavLink
                to={item.to}
                className={({ isActive }) =>
                  `sidebar__item ${isActive ? 'sidebar__item--active' : ''}`
                }
                end={item.id === 'dashboard'}
              >
                <span>{item.label}</span>
                {item.badge ? <span className="sidebar__badge">{item.badge}</span> : null}
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

export default Sidebar;
