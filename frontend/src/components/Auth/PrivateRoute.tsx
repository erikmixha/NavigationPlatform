import { ReactNode } from 'react';
import { useAuth } from '../../contexts/AuthContext';

/**
 * Props for the PrivateRoute component.
 */
interface PrivateRouteProps {
  children: ReactNode;
}

/**
 * Route component that protects routes requiring authentication.
 * Redirects to login if user is not authenticated.
 */
const PrivateRoute = ({ children }: PrivateRouteProps) => {
  const { user, loading } = useAuth();

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    window.location.href = '/api/auth/login';
    return null;
  }

  return <>{children}</>;
};

export default PrivateRoute;
