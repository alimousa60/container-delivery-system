import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/context/ThemeContext';
import { useMutation } from '@tanstack/react-query';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { useToast } from '@/components/ui/Toast';
import { User, Lock, Bell, Globe, Monitor, Sun, Moon, Shield } from 'lucide-react';
import { clsx } from 'clsx';

const passwordSchema = {
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
};

export const SettingsPage: React.FC = () => {
  const { t } = useTranslation();
  const { user, updateUser } = useAuth();
  const { theme, resolvedTheme, setTheme } = useTheme();
  const { success: showSuccess, error: showError } = useToast();
  const [activeTab, setActiveTab] = useState<'profile' | 'security' | 'appearance' | 'notifications'>('profile');
  const [profileData, setProfileData] = useState({
    fullName: user?.fullName || '',
    phoneNumber: user?.phoneNumber || '',
  });
  const [passwordData, setPasswordData] = useState(passwordSchema);
  const [isSaving, setIsSaving] = useState(false);

  const profileMutation = useMutation({
    mutationFn: async (data: typeof profileData) => {
      // In real app, call API to update profile
      await new Promise(resolve => setTimeout(resolve, 500));
      return data;
    },
    onSuccess: (data) => {
      updateUser(data);
      setProfileData(data);
      showSuccess('Profile updated successfully');
    },
    onError: (err: Error) => showError(err.message),
    onSettled: () => setIsSaving(false),
  });

  const passwordMutation = useMutation({
    mutationFn: async (data: typeof passwordData) => {
      // In real app, call API to change password
      await new Promise(resolve => setTimeout(resolve, 500));
      return data;
    },
    onSuccess: () => {
      setPasswordData(passwordSchema);
      showSuccess('Password changed successfully');
    },
    onError: (err: Error) => showError(err.message),
    onSettled: () => setIsSaving(false),
  });

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    profileMutation.mutate(profileData);
  };

  const handlePasswordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      showError('Passwords do not match');
      return;
    }
    if (passwordData.newPassword.length < 8) {
      showError('Password must be at least 8 characters');
      return;
    }
    setIsSaving(true);
    passwordMutation.mutate(passwordData);
  };

  const handleThemeChange = (newTheme: 'light' | 'dark' | 'system') => {
    setTheme(newTheme);
    showSuccess(`${t('settings.theme')} updated`);
  };

  const tabs = [
    { id: 'profile', label: t('settings.profile'), icon: User },
    { id: 'security', label: t('settings.security'), icon: Shield },
    { id: 'appearance', label: t('settings.theme'), icon: Monitor },
    { id: 'notifications', label: t('settings.notifications'), icon: Bell },
  ];

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('settings.title')}</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">Manage your account settings and preferences</p>
      </div>

      {/* Tab Navigation */}
      <Card>
        <CardBody className="p-0">
          <nav className="flex border-b border-gray-200 dark:border-gray-700" role="tablist">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  role="tab"
                  aria-selected={isActive}
                  onClick={() => setActiveTab(tab.id as typeof activeTab)}
                  className={clsx(
                    'flex items-center gap-2 px-4 py-4 text-sm font-medium border-b-2 transition-colors',
                    isActive
                      ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                      : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
                  )}
                >
                  <Icon className="w-4 h-4" />
                  {tab.label}
                </button>
              );
            })}
          </nav>
        </CardBody>
      </Card>

      {/* Tab Content */}
      <Card>
        <CardBody className="p-6">
          {activeTab === 'profile' && (
            <form onSubmit={handleProfileSubmit} className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Profile Information</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Input
                  label={t('users.fullName')}
                  value={profileData.fullName}
                  onChange={(e) => setProfileData(prev => ({ ...prev, fullName: e.target.value }))}
                  required
                />
                <Input
                  label={t('users.phoneNumber')}
                  value={profileData.phoneNumber}
                  onChange={(e) => setProfileData(prev => ({ ...prev, phoneNumber: e.target.value }))}
                  type="tel"
                />
              </div>
              <div className="flex justify-end">
                <Button type="submit" variant="primary" loading={isSaving} leftIcon={<User className="w-4 h-4" }}>
                  Save Changes
                </Button>
              </div>
            </form>
          )}

          {activeTab === 'security' && (
            <form onSubmit={handlePasswordSubmit} className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Change Password</h2>
              <div className="space-y-4">
                <Input
                  label={t('auth.currentPassword')}
                  type="password"
                  value={passwordData.currentPassword}
                  onChange={(e) => setPasswordData(prev => ({ ...prev, currentPassword: e.target.value }))}
                  required
                  autoComplete="current-password"
                />
                <Input
                  label={t('auth.newPassword')}
                  type="password"
                  value={passwordData.newPassword}
                  onChange={(e) => setPasswordData(prev => ({ ...prev, newPassword: e.target.value }))}
                  required
                  autoComplete="new-password"
                  helperText="At least 8 characters"
                />
                <Input
                  label={t('auth.confirmPassword')}
                  type="password"
                  value={passwordData.confirmPassword}
                  onChange={(e) => setPasswordData(prev => ({ ...prev, confirmPassword: e.target.value }))}
                  required
                  autoComplete="new-password"
                />
              </div>
              <div className="flex justify-end">
                <Button type="submit" variant="primary" loading={isSaving} leftIcon={<Lock className="w-4 h-4" }}>
                  {t('auth.changePassword')}
                </Button>
              </div>
            </form>
          )}

          {activeTab === 'appearance' && (
            <div className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Theme</h2>
              <p className="text-gray-500 dark:text-gray-400">Choose your preferred color theme</p>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {[
                  { value: 'light', label: t('settings.lightMode'), icon: Sun, description: 'Always use light mode' },
                  { value: 'dark', label: t('settings.darkMode'), icon: Moon, description: 'Always use dark mode' },
                  { value: 'system', label: t('settings.systemDefault'), icon: Monitor, description: 'Match system setting' },
                ].map((option) => {
                  const Icon = option.icon;
                  const isActive = theme === option.value;
                  return (
                    <button
                      key={option.value}
                      onClick={() => handleThemeChange(option.value as 'light' | 'dark' | 'system')}
                      className={clsx(
                        'relative p-6 rounded-xl border-2 text-left transition-all',
                        isActive
                          ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                      )}
                    >
                      {isActive && (
                        <div className="absolute top-2 right-2 w-5 h-5 bg-primary-500 rounded-full flex items-center justify-center">
                          <CheckCircle className="w-3 h-3 text-white" />
                        </div>
                      )}
                      <div className="flex items-center gap-3">
                        <div className={clsx(
                          'w-12 h-12 rounded-lg flex items-center justify-center',
                          isActive ? 'bg-primary-100 dark:bg-primary-900/30' : 'bg-gray-100 dark:bg-gray-800'
                        )}>
                          <Icon className={clsx('w-6 h-6', isActive ? 'text-primary-600' : 'text-gray-500')} />
                        </div>
                        <div>
                          <p className="font-medium text-gray-900 dark:text-white">{option.label}</p>
                          <p className="text-sm text-gray-500 dark:text-gray-400">{option.description}</p>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>

              <div className="pt-6 border-t border-gray-200 dark:border-gray-700">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Language</h3>
                <div className="flex gap-4">
                  {[
                    { code: 'en', label: t('settings.english'), flag: '🇺🇸' },
                    { code: 'ar', label: t('settings.arabic'), flag: '🇸🇦' },
                  ].map((lang) => (
                    <button
                      key={lang.code}
                      onClick={() => document.documentElement.lang = lang.code}
                      className={clsx(
                        'flex items-center gap-2 px-4 py-2 rounded-lg border-2 transition-colors',
                        document.documentElement.lang === lang.code
                          ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                      )}
                    >
                      <span className="text-2xl">{lang.flag}</span>
                      <span className="font-medium text-gray-900 dark:text-white">{lang.label}</span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'notifications' && (
            <div className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Notification Preferences</h2>
              <p className="text-gray-500 dark:text-gray-400">Configure how you receive notifications</p>
              
              <div className="space-y-4">
                {[
                  { id: 'email_delivery', label: 'Delivery Confirmations', description: 'Receive email when vehicles are delivered' },
                  { id: 'email_reports', label: 'Report Generation', description: 'Get notified when reports are ready' },
                  { id: 'email_imports', label: 'Import Completion', description: 'Receive email when imports finish' },
                  { id: 'push_delivery', label: 'Push Notifications', description: 'Get browser notifications for deliveries' },
                ].map((notification) => (
                  <label key={notification.id} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
                    <div>
                      <p className="font-medium text-gray-900 dark:text-white">{notification.label}</p>
                      <p className="text-sm text-gray-500 dark:text-gray-400">{notification.description}</p>
                    </div>
                    <input
                      type="checkbox"
                      className="w-5 h-5 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      defaultChecked
                    />
                  </label>
                ))}
              </div>
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
};