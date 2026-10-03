import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook_form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/context/AuthContext';
import { useToast } from '@/components/ui/Toast';
import { useSettings } from '@/hooks/useSettings';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { Box, Mail, Lock, Eye, EyeOff, AlertCircle } from 'lucide-react';
import { clsx } from 'clsx';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional(),
});

type LoginForm = z.infer<typeof loginSchema>;

export const LoginPage: React.FC = () => {
  const { t } = useTranslation();
  const { login } = useAuth();
  const { error: showError } = useToast();
  const { settings } = useSettings();
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [showMfa, setShowMfa] = useState(false);
  const [mfaCode, setMfaCode] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { rememberMe: false },
  });

  const onSubmit = async (data: LoginForm) => {
    setIsLoading(true);
    try {
      await login(data);
    } catch (err) {
      if (err instanceof Error && err.message === 'MFA_REQUIRED') {
        setShowMfa(true);
      } else {
        showError(err instanceof Error ? err.message : t('auth.loginFailed'));
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleMfaSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      // This would call the actual MFA verification
      // await verifyMfa({ userId: pendingUser.id, code: mfaCode });
      showError('MFA verification not implemented in demo');
    } catch (err) {
      showError(err instanceof Error ? err.message : 'MFA verification failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary-600 mx-auto mb-4">
            {settings?.logoUrl ? (
              <img src={settings.logoUrl} alt={settings.companyName || "Logo"} className="w-10 h-10" />
            ) : (
              <Box className="w-10 h-10 text-white" />
            )}
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            {settings?.companyName?.split(' ')[0] || t('navigation.dashboard').split(' ')[0]} Delivery
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-2">
            {t('auth.login')} to your account
          </p>
        </div>

        <Card className="shadow-lg">
          <CardHeader className="text-center pb-2">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {showMfa ? t('auth.mfaVerify') : t('auth.login')}
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              {showMfa 
                ? t('auth.enterCode') 
                : 'Enter your credentials to access the system'}
            </p>
          </CardHeader>
          <CardBody className="pt-0">
            {showMfa ? (
              <form onSubmit={handleMfaSubmit} className="space-y-4">
                <div className="flex justify-center mb-4">
                  <div className="w-16 h-16 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
                    <svg className="w-8 h-8 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                    </svg>
                  </div>
                </div>
                <Input
                  label={t('auth.mfaCode')}
                  type="text"
                  placeholder="123456"
                  value={mfaCode}
                  onChange={(e) => setMfaCode(e.target.value)}
                  error={errors.mfaCode?.message}
                  autoComplete="one-time-code"
                  inputMode="numeric"
                  maxLength={6}
                  required
                  {...register('mfaCode', { required: true, minLength: 6 })}
                />
                <Button type="submit" variant="primary" fullWidth loading={isLoading}>
                  {t('auth.login')}
                </Button>
                <Button 
                  type="button" 
                  variant="ghost" 
                  fullWidth 
                  onClick={() => setShowMfa(false)}
                  disabled={isLoading}
                >
                  {t('common.back')}
                </Button>
              </form>
            ) : (
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <Input
                  label={t('auth.email')}
                  type="email"
                  placeholder="user@company.com"
                  leftIcon={<Mail className="w-5 h-5" />}
                  error={errors.email?.message}
                  autoComplete="email"
                  {...register('email')}
                />
                <div className="relative">
                  <Input
                    label={t('auth.password')}
                    type={showPassword ? 'text' : 'password'}
                    placeholder="••••••••"
                    leftIcon={<Lock className="w-5 h-5" />}
                    rightIcon={
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                      >
                        {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                      </button>
                    }
                    error={errors.password?.message}
                    autoComplete="current-password"
                    {...register('password')}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      className="w-4 h-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                      {...register('rememberMe')}
                    />
                    <span className="text-sm text-gray-600 dark:text-gray-400">{t('auth.rememberMe')}</span>
                  </label>
                  <a href="#" className="text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400">
                    {t('auth.forgotPassword')}
                  </a>
                </div>
                <Button type="submit" variant="primary" fullWidth loading={isLoading}>
                  {t('auth.login')}
                </Button>
              </form>
            )}
          </CardBody>
        </Card>

        {/* Demo credentials */}
        <div className="mt-6 p-4 bg-gray-100 dark:bg-gray-800 rounded-lg">
          <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Demo Credentials</h3>
          <div className="text-sm text-gray-600 dark:text-gray-400 space-y-1 font-mono">
            <p>Admin: admin@container-delivery.com / Admin123!</p>
            <p>Delivery: delivery@container-delivery.com / Delivery123!</p>
          </div>
        </div>

        <p className="text-center text-sm text-gray-500 dark:text-gray-400 mt-6">
          Container Vehicle Delivery Management System v1.0
        </p>
      </div>
    </div>
  );
};