import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authApi } from '../services/api';
import { env } from '../config/env';

/**
 * Represents a user in the authentication context.
 */
interface User {
  userId: string;
  email: string;
  name: string;
  roles: string[];
  isAuthenticated: boolean;
}

/**
 * Authentication context type.
 */
interface AuthContextType {
  user: User | null;
  loading: boolean;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

/**
 * Authentication provider component that manages user authentication state.
 */
export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadUser();
  }, []);

  /**
   * Loads the current user from the API.
   */
  const loadUser = async () => {
    try {
      const response = await authApi.getCurrentUser();
      setUser(response.data);
    } catch (error) {
      console.error('Failed to load user', error);
      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Logs out the current user and redirects to Keycloak logout.
   */
  const logout = async () => {
    setUser(null);
    setLoading(false);

    try {
      await authApi.logout();
    } catch (error) {
      console.error('Logout failed', error);
    }

    localStorage.clear();
    sessionStorage.clear();
    document.cookie.split(';').forEach((c) => {
      document.cookie = c
        .replace(/^ +/, '')
        .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
    });

    const keycloakLogoutUrl = `${env.keycloakUrl}/realms/${env.keycloakRealm}/protocol/openid-connect/logout`;
    const redirectUri = encodeURIComponent(`${env.appUrl}/login`);
    const clientId = encodeURIComponent(env.keycloakClientId);
    window.location.href = `${keycloakLogoutUrl}?post_logout_redirect_uri=${redirectUri}&client_id=${clientId}`;
  };

  return <AuthContext.Provider value={{ user, loading, logout }}>{children}</AuthContext.Provider>;
};

/**
 * Hook to access the authentication context.
 * @throws Error if used outside of AuthProvider.
 */
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
