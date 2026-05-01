import { NavLink } from 'react-router-dom';

interface PortalTabsProps {
  items: Array<{ to: string; label: string }>;
}

export function PortalTabs({ items }: PortalTabsProps) {
  return (
    <nav className="portal-tabs" aria-label="Secciones del portal">
      {items.map((item) => (
        <NavLink key={item.to} to={item.to} className={({ isActive }) => `portal-tab ${isActive ? 'active' : ''}`}>
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}
