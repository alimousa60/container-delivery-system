import React, { Suspense, lazy } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import { useTheme } from './context/ThemeContext';
import { Layout } from './components/layout/Layout';
import { LoadingSpinner } from './components/ui/LoadingSpinner';

const LoginPage = lazy(() => import('./pages/auth/LoginPage').then(m => ({ default: m.LoginPage })));
const MfaVerifyPage = lazy(() => import('./pages/auth/MfaVerifyPage').then(m => ({ default: m.MfaVerifyPage })));
const DashboardPage = lazy(() => import('./pages/dashboard/DashboardPage').then(m => ({ default: m.DashboardPage })));
const ContainersPage = lazy(() => import('./pages/containers/ContainersPage').then(m => ({ default: m.ContainersPage })));
const ContainerDetailPage = lazy(() => import('./pages/containers/ContainerDetailPage').then(m => ({ default: m.ContainerDetailPage })));
const VehicleDeliveryPage = lazy(() => import('./pages/vehicles/VehicleDeliveryPage'));
const ReportsPage = lazy(() => import('./pages/reports/ReportsPage').then(m => ({ default: m.ReportsPage })));
const UsersPage = lazy(() => import('./pages/users/UsersPage').then(m => ({ default: m.UsersPage })));
const SettingsPage = lazy(() => import('./pages/settings/SettingsPage').then(m => ({ default: m.SettingsPage })));
const ArchivePage = lazy(() => import('./pages/containers/ArchivePage').then(m => ({ default: m.ArchivePage })));
const ImportPage = lazy(() => import('./pages/ImportPage').then(m => ({ default: m.ImportPage })));
const AuditLogsPage = lazy(() => import('./pages/AuditLogsPage').then(m => ({ default: m.AuditLogsPage })));
const VehiclesPage = lazy(() => import('./pages/VehiclesPage').then(m => ({ default: m.VehiclesPage })));

const PageSkeleton = () => (
  <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
    <LoadingSpinner size="lg" />
  </div>
);

const PrivateRoute = ({ children, allowedRoles }: { children: React.ReactNode; allowedRoles?: string[] }) => {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return <PageSkeleton />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && user && !allowedRoles.some(role => user.roles.includes(role))) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

const PublicRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <PageSkeleton />;
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

const AppRoutes = () => {
  const { resolvedTheme } = useTheme();

  return (
    <div className={resolvedTheme === 'dark' ? 'dark' : ''}>
      <Suspense fallback={<PageSkeleton />}>
        <Routes>
          <Route path="/login" element={
            <PublicRoute>
              <LoginPage />
            </PublicRoute>
          } />
          <Route path="/mfa-verify" element={
            <PublicRoute>
              <MfaVerifyPage />
            </PublicRoute>
          } />
          <Route element={
            <PrivateRoute>
              <Layout />
            </PrivateRoute>
          }>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/containers" element={<ContainersPage />} />
            <Route path="/containers/:id" element={<ContainerDetailPage />} />
            <Route path="/archive" element={<ArchivePage />} />
            <Route path="/vehicles" element={<VehiclesPage />} />
            <Route path="/vehicles/deliver" element={<VehicleDeliveryPage />} />
            <Route path="/import" element={<ImportPage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/audit-logs" element={
              <PrivateRoute allowedRoles={['Admin']}>
                <AuditLogsPage />
              </PrivateRoute>
            } />
            <Route path="/users" element={
              <PrivateRoute allowedRoles={['Admin']}>
                <UsersPage />
              </PrivateRoute>
            } />
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Suspense>
    </div>
  );
};

const App = () => {
  return <AppRoutes />;
};

export default App;