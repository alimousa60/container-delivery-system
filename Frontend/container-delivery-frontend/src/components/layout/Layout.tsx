import React, { useState, useEffect } from 'react';
import { Outlet, NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/context/ThemeContext';
import { useSettings } from '@/hooks/useSettings';
import { Button } from '@/components/ui/Button';
import { Dropdown, DropdownItem } from '@/components/ui/Dropdown';
import { 
  LayoutDashboard, 
  Box, 
  Truck, 
  FileText, 
  Upload, 
  Users, 
  Activity, 
  Settings, 
  LogOut, 
  User, 
  Sun, 
  Moon, 
  Monitor,
  Globe,
  ChevronDown,
  Menu,
  X
} from 'lucide-react';
import { clsx } from 'clsx';

const navigation = [
  { name: 'dashboard', href: '/dashboard', icon: LayoutDashboard },
  { name: 'containers', href: '/containers', icon: Box },
  { name: 'vehicles', href: '/vehicles/deliver', icon: Truck },
  { name: 'reports', href: '/reports', icon: FileText },
  { name: 'import', href: '/import', icon: Upload },
  { name: 'settings', href: '/settings', icon: Settings },
];

const adminNavigation = [
  { name: 'users', href: '/users', icon: Users },
  { name: 'auditLogs', href: '/audit-logs', icon: Activity },
];

export const Layout: React.FC = () => {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const { theme, resolvedTheme, setTheme, toggleTheme } = useTheme();
  const { settings } = useSettings();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const isAdmin = user?.roles.includes('Admin') || false;

  const handleLogout = async () => {
    await logout();
    setUserMenuOpen(false);
  };

  const themeOptions: DropdownItem[] = [
    { label: t('settings.lightMode'), icon: <Sun className="w-4 h-4" />, onClick: () => setTheme('light') },
    { label: t('settings.darkMode'), icon: <Moon className="w-4 h-4" />, onClick: () => setTheme('dark') },
    { label: t('settings.systemDefault'), icon: <Monitor className="w-4 h-4" />, onClick: () => setTheme('system') },
  ];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div 
          className="fixed inset-0 z-40 bg-black/50 lg:hidden" 
          onClick={() => setSidebarOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside 
        className={clsx(
          'fixed inset-y-0 left-0 z-50 w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 transform transition-transform duration-300 ease-in-out lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
        aria-label="Sidebar navigation"
      >
        <div className="flex flex-col h-full">
          {/* Logo */}
          <div className="flex items-center justify-between h-16 px-4 border-b border-gray-200 dark:border-gray-700">
            <NavLink to="/dashboard" className="flex items-center gap-2">
              {settings?.logoUrl ? (
                <img 
                  src={settings.logoUrl} 
                  alt={settings.companyName || "Logo"} 
                  className="w-8 h-8 rounded-lg object-cover" 
                />
              ) : (
                <Box className="w-8 h-8 text-primary-600" />
              )}
              <span className="text-xl font-bold text-gray-900 dark:text-white">
                {settings?.companyName?.split(' ')[0] || t('navigation.dashboard').split(' ')[0]}
              </span>
            </NavLink>
            <button
              onClick={() => setSidebarOpen(false)}
              className="lg:hidden p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
              aria-label="Close sidebar"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Navigation */}
          <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto" role="navigation" aria-label="Main navigation">
            {navigation.map((item) => {
              const isActive = location.pathname.startsWith(item.href);
              return (
                <NavLink
                  key={item.name}
                  to={item.href}
                  className={({ isActive }) => clsx(
                    'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
                    isActive 
                      ? 'bg-primary-50 text-primary-700 dark:bg-primary-900/20 dark:text-primary-400' 
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900 dark:text-gray-400 dark:hover:bg-gray-700'
                  )}
                  aria-current={isActive ? 'page' : undefined}
                >
                  <item.icon className="w-5 h-5 flex-shrink-0" aria-hidden="true" />
                  {t(`navigation.${item.name}`)}
                </NavLink>
              );
            })}

            {isAdmin && (
              <>
                <div className="pt-4 pb-2 px-3">
                  <h3 className="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider">
                    {t('common.admin', { defaultValue: 'Administration' })}
                  </h3>
                </div>
                {adminNavigation.map((item) => {
                  const isActive = location.pathname.startsWith(item.href);
                  return (
                    <NavLink
                      key={item.name}
                      to={item.href}
                      className={({ isActive }) => clsx(
                        'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
                        isActive 
                          ? 'bg-primary-50 text-primary-700 dark:bg-primary-900/20 dark:text-primary-400' 
                          : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900 dark:text-gray-400 dark:hover:bg-gray-700'
                      )}
                      aria-current={isActive ? 'page' : undefined}
                    >
                      <item.icon className="w-5 h-5 flex-shrink-0" aria-hidden="true" />
                      {t(`navigation.${item.name}`)}
                    </NavLink>
                  );
                })}
              </>
            )}
          </nav>

          {/* User section */}
          <div className="p-3 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-3 px-3 py-2">
              <div className="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
                <User className="w-5 h-5 text-primary-600 dark:text-primary-400" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{user?.fullName}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{user?.email}</p>
              </div>
            </div>
          </div>
        </div>
      </aside>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top bar */}
        <header className="sticky top-0 z-30 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between h-16 px-4 sm:px-6">
            {/* Mobile menu button */}
            <button
              onClick={() => setSidebarOpen(true)}
              className="lg:hidden p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
              aria-label="Open sidebar"
            >
              <Menu className="w-6 h-6" />
            </button>

            {/* Page title */}
            <div className="flex-1 lg:pl-8">
              <h1 className="text-lg font-semibold text-gray-900 dark:text-white">
                {getPageTitle(location.pathname, t)}
              </h1>
            </div>

            {/* Right side actions */}
            <div className="flex items-center gap-3">
              {/* Theme toggle */}
              <Dropdown
                trigger={
                  <button className="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" aria-label="Theme">
                    {resolvedTheme === 'dark' ? <Moon className="w-5 h-5" /> : <Sun className="w-5 h-5" />}
                  </button>
                }
                items={themeOptions.map(opt => ({
                  label: opt.label,
                  icon: <opt.icon className="w-4 h-4" />,
                  onClick: () => setTheme(opt.value as 'light' | 'dark' | 'system'),
                }))}
                align="right"
              />

              {/* Language toggle */}
              <Dropdown
                trigger={
                  <button className="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" aria-label="Language">
                    <Globe className="w-5 h-5" />
                  </button>
                }
                items={[
                  { label: t('settings.english'), onClick: () => document.documentElement.lang = 'en' },
                  { label: t('settings.arabic'), onClick: () => document.documentElement.lang = 'ar' },
                ]}
                align="right"
              />

              {/* User menu */}
              <Dropdown
                trigger={
                  <button 
                    onClick={() => setUserMenuOpen(!userMenuOpen)}
                    className="flex items-center gap-2 p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                    aria-label="User menu"
                    aria-expanded={userMenuOpen}
                    aria-haspopup="true"
                  >
                    <div className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
                      <User className="w-4 h-4 text-primary-600 dark:text-primary-400" />
                    </div>
                    <span className="hidden sm:block text-sm font-medium text-gray-700 dark:text-gray-300">{user?.fullName}</span>
                    <ChevronDown className="w-4 h-4 text-gray-500" />
                  </button>
                }
                items={[
                  { label: t('navigation.profile'), icon: <User className="w-4 h-4" />, onClick: () => setUserMenuOpen(false) },
                  { divider: true },
                  { label: t('navigation.settings'), icon: <Settings className="w-4 h-4" />, onClick: () => setUserMenuOpen(false) },
                  { divider: true },
                  { label: t('navigation.logout'), icon: <LogOut className="w-4 h-4" />, onClick: handleLogout, danger: true },
                ]}
                align="right"
              />
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="p-4 sm:p-6 lg:p-8" role="main">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

function getPageTitle(pathname: string, t: (key: string) => string): string {
  const titles: Record<string, string> = {
    '/dashboard': t('dashboard.title'),
    '/containers': t('containers.title'),
    '/vehicles/deliver': t('vehicles.title'),
    '/reports': t('reports.title'),
    '/import': t('import.title'),
    '/users': t('users.title'),
    '/audit-logs': t('audit.title'),
    '/settings': t('settings.title'),
  };

  if (pathname.startsWith('/containers/')) {
    return t('containers.viewDetails');
  }

  return titles[pathname] || t('dashboard.title');
}