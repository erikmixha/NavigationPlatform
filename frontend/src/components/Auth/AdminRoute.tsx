import { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

/**
 * Props for the AdminRoute component.
 */
interface AdminRouteProps {
  children: ReactNode;
}

/**
 * Route component that protects routes requiring admin role.
 * Redirects to login if not authenticated, or to dashboard if not admin.
 */
const AdminRoute = ({ children }: AdminRouteProps) => {
  const { user, loading } = useAuth();

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (!user.roles.includes('Admin')) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

export default AdminRoute;
