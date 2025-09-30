import type { NavigationItem } from '../../types/dashboard.ts';
import './Sidebar.css';

export interface SidebarProps {
  items: NavigationItem[];
  activeId: string;
  onSelect?: (item: NavigationItem) => void;
}

export function Sidebar({ items, activeId, onSelect }: SidebarProps) {
  return (
    <nav className="sidebar" aria-label="Primary">
      <ul className="sidebar__list">
        {items.map((item) => {
          const isActive = item.id === activeId;
          return (
            <li key={item.id}>
              <button
                type="button"
                className={`sidebar__item ${isActive ? 'sidebar__item--active' : ''}`}
                onClick={() => onSelect?.(item)}
              >
                <span>{item.label}</span>
                {item.badge ? <span className="sidebar__badge">{item.badge}</span> : null}
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

export default Sidebar;
