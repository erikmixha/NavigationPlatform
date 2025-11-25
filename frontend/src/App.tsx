import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from 'react-query';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { NotificationProvider } from './contexts/NotificationContext';
import { ConfirmDialogProvider } from './contexts/ConfirmDialogContext';
import Layout from './components/Layout/Layout';
import Dashboard from './pages/Dashboard';
import Journeys from './pages/Journeys';
import JourneyDetails from './pages/JourneyDetails';
import CreateJourney from './pages/CreateJourney';
import EditJourney from './pages/EditJourney';
import Statistics from './pages/Statistics';
import Notifications from './pages/Notifications';
import Login from './pages/Login';
import AdminPanel from './pages/AdminPanel';
import AdminJourneyDetails from './pages/AdminJourneyDetails';
import PublicJourney from './pages/PublicJourney';
import PrivateRoute from './components/Auth/PrivateRoute';
import AdminRoute from './components/Auth/AdminRoute';

/**
 * Component that redirects users based on their roles.
 * Admin-only users go to admin panel, others go to dashboard.
 */
const AdminRedirect = () => {
  const { user } = useAuth();
  const hasAdminRole = user?.roles.includes('Admin');
  const hasUserRole = user?.roles.includes('User');

  if (hasAdminRole && !hasUserRole) {
    return <Navigate to="/admin" replace />;
  }

  return <Navigate to="/dashboard" replace />;
};

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

/**
 * Main application component that sets up routing and providers.
 */
function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <Router>
              <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/journeys/public/:token" element={<PublicJourney />} />
                <Route
                  path="/"
                  element={
                    <PrivateRoute>
                      <Layout />
                    </PrivateRoute>
                  }
                >
                  <Route index element={<AdminRedirect />} />
                  <Route path="dashboard" element={<Dashboard />} />
                  <Route path="journeys" element={<Journeys />} />
                  <Route path="journeys/:id" element={<JourneyDetails />} />
                  <Route path="journeys/:id/edit" element={<EditJourney />} />
                  <Route path="journeys/new" element={<CreateJourney />} />
                  <Route path="statistics" element={<Statistics />} />
                  <Route path="notifications" element={<Notifications />} />
                  <Route
                    path="admin"
                    element={
                      <AdminRoute>
                        <AdminPanel />
                      </AdminRoute>
                    }
                  />
                  <Route
                    path="admin/journeys/:id"
                    element={
                      <AdminRoute>
                        <AdminJourneyDetails />
                      </AdminRoute>
                    }
                  />
                </Route>
              </Routes>
            </Router>
            <Toaster
              position="top-right"
              toastOptions={{
                duration: 4000,
                style: {
                  background: 'var(--background)',
                  color: 'var(--text)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius-lg)',
                  boxShadow: 'var(--shadow-lg)',
                  transition: 'opacity 0.3s ease, filter 0.3s ease',
                },
                success: {
                  iconTheme: {
                    primary: 'var(--success)',
                    secondary: '#fff',
                  },
                },
                error: {
                  iconTheme: {
                    primary: 'var(--error)',
                    secondary: '#fff',
                  },
                },
              }}
            />
          </ConfirmDialogProvider>
        </NotificationProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;
