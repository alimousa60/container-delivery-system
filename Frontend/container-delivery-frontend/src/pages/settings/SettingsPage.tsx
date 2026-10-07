import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/context/ThemeContext';
import { useLanguage } from '@/context/LanguageContext';
import { userService, authService } from '@/services/api';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { useToast } from '@/components/ui/Toast';
import { useSettings } from '@/hooks/useSettings';
import { User, Lock, Bell, Globe, Monitor, Sun, Moon, Shield, Building2, Image, Upload, Save, X, CheckCircle } from 'lucide-react';
import { clsx } from 'clsx';

const passwordSchema = {
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
};

const getApiErrorMessage = (err: unknown): string => {
  const e = err as {
    response?: {
      data?: {
        error?: string;
        message?: string;
        detail?: string;
        title?: string;
        errors?: Record<string, string[]>;
      };
    };
    message?: string;
  };
  const data = e?.response?.data;
  if (data?.error) return data.error;
  if (data?.message) return data.message;
  if (data?.errors) return Object.values(data.errors).flat().join(' ');
  if (data?.detail) return data.detail;
  if (data?.title) return data.title;
  return e?.message || 'Something went wrong';
};

export const SettingsPage: React.FC = () => {
  const { t } = useTranslation();
  const { user, updateUser } = useAuth();
  const { theme, setTheme } = useTheme();
  const { language, setLanguage } = useLanguage();
  const { settings, isLoading: settingsLoading, refetch: refetchSettings } = useSettings();
  const { success: showSuccess, error: showError } = useToast();
  const [activeTab, setActiveTab] = useState<'profile' | 'security' | 'appearance' | 'notifications' | 'company'>('profile');
  const [profileData, setProfileData] = useState({
    fullName: user?.fullName || '',
    phoneNumber: user?.phoneNumber || '',
  });
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [companyData, setCompanyData] = useState({
    companyName: '',
    logoUrl: '',
    address: '',
    phone: '',
    email: '',
    reportFooter: '',
    website: '',
    isActive: true,
  });
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [logoUploading, setLogoUploading] = useState(false);
  const [notifPrefs, setNotifPrefs] = useState<Record<string, boolean>>(() => {
    try {
      return JSON.parse(localStorage.getItem('notification_prefs') || '{}');
    } catch {
      return {};
    }
  });

  const { updateSettings, uploadLogo } = useSettings();

  useEffect(() => {
    if (settings) {
      setCompanyData({
        companyName: settings.companyName,
        logoUrl: settings.logoUrl || '',
        address: settings.address || '',
        phone: settings.phone || '',
        email: settings.email || '',
        reportFooter: settings.reportFooter || '',
        website: settings.website || '',
        isActive: settings.isActive,
      });
      if (settings.logoUrl) {
        setLogoPreview(settings.logoUrl);
      }
    }
  }, [settings]);

  const profileMutation = useMutation({
    mutationFn: async (data: typeof profileData) => {
      if (!user) throw new Error('Not authenticated');
      return userService.update(user.id, {
        fullName: data.fullName,
        phoneNumber: data.phoneNumber || undefined,
        isActive: user.isActive ?? true,
      });
    },
    onSuccess: (updated) => {
      updateUser({ fullName: updated.fullName, phoneNumber: updated.phoneNumber ?? undefined });
      setProfileData({ fullName: updated.fullName, phoneNumber: updated.phoneNumber || '' });
      showSuccess(t('settings.updateSuccess'));
    },
    onError: (err: Error) => showError(getApiErrorMessage(err)),
    onSettled: () => setIsSaving(false),
  });

  const passwordMutation = useMutation({
    mutationFn: async (data: typeof passwordData) => {
      await authService.changePassword(data.currentPassword, data.newPassword);
    },
    onSuccess: () => {
      setPasswordData({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
      showSuccess(t('auth.passwordChanged'));
    },
    onError: (err: Error) => showError(getApiErrorMessage(err)),
    onSettled: () => setIsSaving(false),
  });

  const companyMutation = useMutation({
    mutationFn: async (data: typeof companyData) => {
      await updateSettings(data);
      return data;
    },
    onSuccess: (data) => {
      setCompanyData(data);
      refetchSettings();
      showSuccess(t('settings.updateSuccess'));
    },
    onError: (err: Error) => showError(getApiErrorMessage(err)),
    onSettled: () => setIsSaving(false),
  });

  const logoUploadMutation = useMutation({
    mutationFn: async (file: File) => {
      return await uploadLogo(file);
    },
    onSuccess: (data) => {
      setCompanyData(prev => ({ ...prev, logoUrl: data.logoUrl }));
      setLogoPreview(data.logoUrl || '');
      refetchSettings();
      showSuccess(t('settings.logoUploaded'));
    },
    onError: (err: Error) => showError(getApiErrorMessage(err)),
    onSettled: () => setLogoUploading(false),
  });

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    profileMutation.mutate(profileData);
  };

  const handlePasswordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      showError(t('settings.passwordsNoMatch'));
      return;
    }
    if (passwordData.newPassword.length < 8) {
      showError(t('settings.passwordTooShort'));
      return;
    }
    setIsSaving(true);
    passwordMutation.mutate(passwordData);
  };

  const handleCompanySubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    companyMutation.mutate(companyData);
  };

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setLogoFile(file);
      setLogoPreview(URL.createObjectURL(file));
    }
  };

  const handleLogoUpload = () => {
    if (logoFile) {
      setLogoUploading(true);
      logoUploadMutation.mutate(logoFile);
    }
  };

  const handleLogoRemove = () => {
    setLogoFile(null);
    setLogoPreview(null);
    setCompanyData(prev => ({ ...prev, logoUrl: '' }));
  };

  const handleThemeChange = (newTheme: 'light' | 'dark' | 'system') => {
    setTheme(newTheme);
    showSuccess(`${t('settings.theme')} updated`);
  };

  const toggleNotifPref = (id: string) => {
    setNotifPrefs(prev => {
      const next = { ...prev, [id]: !(prev[id] ?? true) };
      localStorage.setItem('notification_prefs', JSON.stringify(next));
      return next;
    });
  };

  const tabs = [
    { id: 'profile', label: t('settings.profile'), icon: User },
    { id: 'security', label: t('settings.security'), icon: Shield },
    { id: 'appearance', label: t('settings.theme'), icon: Monitor },
    { id: 'notifications', label: t('settings.notifications'), icon: Bell },
  ];

  const allTabs = user?.roles.includes('Admin') 
    ? [...tabs, { id: 'company', label: t('settings.company'), icon: Building2 }]
    : tabs;

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('settings.title')}</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">{t('settings.subtitle')}</p>
      </div>

      <Card>
        <CardBody className="p-0">
          <nav className="flex border-b border-gray-200 dark:border-gray-700" role="tablist">
            {allTabs.map((tab) => {
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

      <Card>
        <CardBody className="p-6">
          {activeTab === 'profile' && (
            <form onSubmit={handleProfileSubmit} className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{t('settings.profile')}</h2>
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
                <Button type="submit" variant="primary" loading={isSaving} leftIcon={<User className="w-4 h-4" />}>
                  {t('common.save')}
                </Button>
              </div>
            </form>
          )}

          {activeTab === 'security' && (
            <form onSubmit={handlePasswordSubmit} className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{t('settings.security')}</h2>
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
                  helperText={t('auth.passwordHint')}
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
                <Button type="submit" variant="primary" loading={isSaving} leftIcon={<Lock className="w-4 h-4" />}>
                  {t('auth.changePassword')}
                </Button>
              </div>
            </form>
          )}

          {activeTab === 'appearance' && (
            <div className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{t('settings.theme')}</h2>
              <p className="text-gray-500 dark:text-gray-400">{t('settings.chooseTheme')}</p>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {[
                  { value: 'light', label: t('settings.lightMode'), icon: Sun, description: t('settings.lightModeDesc') },
                  { value: 'dark', label: t('settings.darkMode'), icon: Moon, description: t('settings.darkModeDesc') },
                  { value: 'system', label: t('settings.systemDefault'), icon: Monitor, description: t('settings.systemDefaultDesc') },
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
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">{t('settings.language')}</h3>
                <div className="flex gap-4">
                  {[
                    { code: 'en', label: t('settings.english'), flag: '🇺🇸' },
                    { code: 'ar', label: t('settings.arabic'), flag: '🇸🇦' },
                  ].map((lang) => (
                    <button
                      key={lang.code}
                      onClick={() => setLanguage(lang.code as 'en' | 'ar')}
                      className={clsx(
                        'flex items-center gap-2 px-4 py-2 rounded-lg border-2 transition-colors',
                        language === lang.code
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
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{t('settings.notifications')}</h2>
              <p className="text-gray-500 dark:text-gray-400">{t('settings.notificationsDesc')}</p>
              
              <div className="space-y-4">
                {[
                  { id: 'email_delivery', label: t('settings.notifDelivery'), description: t('settings.notifDeliveryDesc') },
                  { id: 'email_reports', label: t('settings.notifReports'), description: t('settings.notifReportsDesc') },
                  { id: 'email_imports', label: t('settings.notifImports'), description: t('settings.notifImportsDesc') },
                  { id: 'push_delivery', label: t('settings.notifPush'), description: t('settings.notifPushDesc') },
                ].map((notification) => (
                  <label key={notification.id} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
                    <div>
                      <p className="font-medium text-gray-900 dark:text-white">{notification.label}</p>
                      <p className="text-sm text-gray-500 dark:text-gray-400">{notification.description}</p>
                    </div>
                    <input
                      type="checkbox"
                      className="w-5 h-5 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      checked={notifPrefs[notification.id] ?? true}
                      onChange={() => toggleNotifPref(notification.id)}
                    />
                  </label>
                ))}
              </div>
            </div>
          )}

          {activeTab === 'company' && (
            <form onSubmit={handleCompanySubmit} className="space-y-6">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-white">{t('settings.company')}</h2>
                <div className="flex items-center gap-2">
                  {logoPreview && (
                    <div className="relative">
                      <img 
                        src={logoPreview} 
                        alt="Company Logo" 
                        className="w-16 h-16 rounded-lg object-cover border border-gray-200 dark:border-gray-700"
                      />
                      <button
                        type="button"
                        onClick={handleLogoRemove}
                        className="absolute -top-2 -right-2 w-6 h-6 rounded-full bg-danger-500 text-white flex items-center justify-center hover:bg-danger-600"
                        aria-label="Remove logo"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  )}
                  {logoUploading ? (
                    <Button variant="outline" disabled leftIcon={<div className="animate-spin h-4 w-4 border-2 border-primary-500 border-t-transparent rounded-full"></div>}>
                      {t('common.uploading')}
                    </Button>
                  ) : (
                    <Button 
                      type="button" 
                      variant="outline" 
                      onClick={() => document.getElementById('logo-upload')?.click()}
                      leftIcon={<Upload className="w-4 h-4" />}
                    >
                      {logoPreview ? t('settings.changeLogo') : t('settings.uploadLogo')}
                    </Button>
                  )}
                  <input
                    id="logo-upload"
                    type="file"
                    accept="image/jpeg,image/png,image/svg+xml,image/webp"
                    onChange={handleLogoChange}
                    className="hidden"
                    disabled={logoUploading}
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Input
                  label={t('settings.companyName')}
                  value={companyData.companyName}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, companyName: e.target.value }))}
                  required
                />
                <Input
                  label={t('settings.website')}
                  value={companyData.website}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, website: e.target.value }))}
                  placeholder="https://example.com"
                />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Input
                  label={t('settings.address')}
                  value={companyData.address}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, address: e.target.value }))}
                  placeholder="123 Shipping Lane, Port City"
                />
                <Input
                  label={t('settings.phone')}
                  value={companyData.phone}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, phone: e.target.value }))}
                  type="tel"
                  placeholder="+1 (555) 123-4567"
                />
              </div>
              <Input
                label={t('settings.email')}
                value={companyData.email}
                onChange={(e) => setCompanyData(prev => ({ ...prev, email: e.target.value }))}
                type="email"
                placeholder="info@company.com"
              />
              <div>
                <label className="label">{t('settings.reportFooter')}</label>
                <textarea
                  className="input min-h-[100px] resize-y"
                  placeholder="This document is confidential and intended solely for the use of the individual or entity to whom it is addressed."
                  value={companyData.reportFooter}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, reportFooter: e.target.value }))}
                  rows={4}
                />
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={companyData.isActive}
                  onChange={(e) => setCompanyData(prev => ({ ...prev, isActive: e.target.checked }))}
                  className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                />
                <label htmlFor="isActive" className="text-sm text-gray-700 dark:text-gray-300 cursor-pointer">{t('settings.isActive')}</label>
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <Button type="button" variant="secondary" onClick={() => {
                  refetchSettings();
                  setCompanyData({
                    companyName: settings?.companyName || '',
                    logoUrl: settings?.logoUrl || '',
                    address: settings?.address || '',
                    phone: settings?.phone || '',
                    email: settings?.email || '',
                    reportFooter: settings?.reportFooter || '',
                    website: settings?.website || '',
                    isActive: settings?.isActive ?? true,
                  });
                  setLogoPreview(settings?.logoUrl || null);
                  setLogoFile(null);
                }} disabled={isSaving || settingsLoading}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" variant="primary" loading={isSaving} leftIcon={<Save className="w-4 h-4" />}>
                  {t('common.save')}
                </Button>
              </div>
            </form>
          )}
        </CardBody>
      </Card>
    </div>
  );
};