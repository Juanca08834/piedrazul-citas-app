import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
  redirectTo?: string;
}

export function ProtectedRoute({ children, roles = [], redirectTo = '/' }: ProtectedRouteProps) {
  const { ready, session } = useAuth();
  const location = useLocation();

  if (!ready) {
    return <div className="loading-card">Cargando portal...</div>;
  }

  if (!session) {
    return <Navigate to={redirectTo} replace state={{ from: location }} />;
  }

  if (roles.length > 0 && !roles.some((role) => session.roles.includes(role))) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
