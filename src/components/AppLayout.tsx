import { Link, NavLink, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { hasInternalAccess, hasSettingsAccess, isDoctorRole } from '../utils/validators';
import logoImage from '../assets/logo.png';

interface LayoutProps {
  children: React.ReactNode;
}

export function AppLayout({ children }: LayoutProps) {
  const { session, logout } = useAuth();
  const location = useLocation();

  const isInternalRoute = location.pathname.startsWith('/portal/interno');
  const isInternalLoginRoute = location.pathname === '/portal/interno/login';
  const internalAccess = hasInternalAccess(session?.roles ?? []);
  const settingsAccess = hasSettingsAccess(session?.roles ?? []);
  const doctorAccess = isDoctorRole(session?.roles ?? []);
  const isPatient = session?.roles.includes('Patient') ?? false;

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="topbar-inner">
          <Link to="/" className="brand">
            <img className="brand-mark brand-logo" src={logoImage} alt="Logo de Piedrazul" />
            <div>
              <strong>Piedrazul</strong>
              <small>Centro médico</small>
            </div>
          </Link>

          <nav className="main-nav" aria-label="Navegación principal">
            {!isInternalRoute && (
              <>
                <NavLink to="/">Inicio</NavLink>
                <NavLink to="/reservar">Reservar cita</NavLink>
                {isPatient && <NavLink to="/portal/paciente">Mi portal</NavLink>}
              </>
            )}
            {isInternalRoute && internalAccess && <NavLink to="/portal/interno/citas">Portal interno</NavLink>}
            {isInternalRoute && settingsAccess && <NavLink to="/portal/interno/configuracion">Configuración</NavLink>}
            {isInternalRoute && doctorAccess && <NavLink to="/portal/interno/perfil">Mi perfil</NavLink>}
          </nav>

          <div className="header-actions">
            {session ? (
              <>
                <span className="welcome-chip">
                  <strong>{session.displayName}</strong>
                  <span>{session.roles.includes('Patient') ? 'Paciente' : 'Personal autorizado'}</span>
                </span>
                <button className="button" onClick={() => void logout()}>
                  Cerrar sesión
                </button>
              </>
            ) : !isInternalLoginRoute ? (
              <>
                <Link className="button button-secondary" to="/iniciar-sesion">
                  Iniciar sesión
                </Link>
                <Link className="button" to="/crear-cuenta">
                  Crear cuenta
                </Link>
              </>
            ) : null}
          </div>
        </div>
      </header>

      <main className="page-container">{children}</main>

      <footer className="footer">
        <div>
          <strong>Piedrazul - Centro Médico</strong>
          <p>Agenda tus citas en línea con una experiencia clara, usable y pensada para pacientes.</p>
        </div>
        {!isInternalRoute && (
          <div className="footer-links">
            <Link to="/reservar">Reservar cita</Link>
            {!session && <Link to="/iniciar-sesion">Iniciar sesión</Link>}
            <Link to="/portal/interno/login">Acceso interno</Link>
          </div>
        )}
      </footer>
    </div>
  );
}
