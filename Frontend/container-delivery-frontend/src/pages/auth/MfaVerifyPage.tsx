import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/context/AuthContext';
import { useToast } from '@/components/ui/Toast';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { Shield, AlertCircle, CheckCircle, Smartphone, QrCode } from 'lucide-react';
import { clsx } from 'clsx';

const mfaSchema = z.object({
  code: z.string().length(6, 'Code must be 6 digits').regex(/^\d+$/, 'Code must be numeric'),
  rememberDevice: z.boolean().optional(),
});

type MfaForm = z.infer<typeof mfaSchema>;

export const MfaVerifyPage: React.FC = () => {
  const { t } = useTranslation();
  const { verifyMfa } = useAuth();
  const { error: showError } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [timeLeft, setTimeLeft] = useState(30);

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<MfaForm>({
    resolver: zodResolver(mfaSchema),
  });

  useEffect(() => {
    // Auto-focus first input
    const inputs = document.querySelectorAll('input[name="code"]');
    if (inputs.length > 0) {
      (inputs[0] as HTMLInputElement).focus();
    }
  }, []);

  // Auto-advance to next input
  const handleKeyDown = (e: React.KeyboardEvent, index: number) => {
    if (e.key >= '0' && e.key <= '9') {
      const nextInput = document.querySelector(`input[name="code"][data-index="${index + 1}"]`);
      if (nextInput) {
        (nextInput as HTMLInputElement).focus();
      }
    } else if (e.key === 'Backspace' && index > 0) {
      const prevInput = document.querySelector(`input[name="code"][data-index="${index - 1}"]`);
      if (prevInput) {
        (prevInput as HTMLInputElement).focus();
      }
    }
  };

  const onSubmit = async (data: MfaForm) => {
    setIsLoading(true);
    try {
      // This would call the actual MFA verification
      await verifyMfa({
        userId: 1, // Would come from pending auth
        code: data.code,
        rememberDevice: data.rememberDevice,
      });
    } catch (err) {
      showError(err instanceof Error ? err.message : 'MFA verification failed');
      setIsLoading(false);
    }
  };

  // Timer for code expiry
  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft(prev => {
        if (prev <= 1) {
          clearInterval(timer);
          return 30;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const codeInputs = Array.from({ length: 6 }, (_, i) => i);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary-600 mx-auto mb-4">
            <Shield className="w-10 h-10 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            {t('auth.mfaVerify')}
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-2">
            {t('auth.enterCode')}
          </p>
        </div>

        <Card className="shadow-lg">
          <CardHeader className="text-center pb-2">
            <div className="flex items-center justify-center gap-2 mb-4">
              <div className={clsx(
                'w-12 h-12 rounded-full flex items-center justify-center',
                timeLeft > 10 ? 'bg-success-100 dark:bg-success-900/30' : 'bg-warning-100 dark:bg-warning-900/30'
              )}>
                {timeLeft > 10 ? (
                  <CheckCircle className="w-6 h-6 text-success-600" />
                ) : (
                  <AlertCircle className="w-6 h-6 text-warning-600" />
                )}
              </div>
            </div>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Code expires in <span className="font-mono font-bold text-lg">{timeLeft}s</span>
            </p>
          </CardHeader>
          <CardBody className="pt-0">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="flex gap-2 justify-center" role="group" aria-label="MFA Code">
                {codeInputs.map((_, index) => (
                  <input
                    key={index}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    data-index={index}
                    onKeyDown={(e) => handleKeyDown(e, index)}
                    onChange={(e) => {
                      const value = e.target.value;
                      if (value && value.match(/^\d$/)) {
                        setValue(`code`, Array.from({ length: 6 }, (_, i) => 
                          i === index ? value : 
                          ((document.querySelector(`input[data-index="${i}"]`) as HTMLInputElement | null)?.value || '')
                        ).join(''));
                      }
                    }}
                    className={clsx(
                      'w-10 h-12 text-center text-2xl font-mono rounded-lg border-2',
                      'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
                      'bg-white dark:bg-gray-800 text-gray-900 dark:text-white',
                      'border-gray-300 dark:border-gray-600'
                    )}
                    aria-label={`Digit ${index + 1}`}
                    autoComplete="one-time-code"
                  />
                ))}
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="rememberDevice"
                  {...register('rememberDevice')}
                  className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                />
                <label htmlFor="rememberDevice" className="text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
                  {t('auth.rememberDevice', { defaultValue: 'Remember this device for 30 days' })}
                </label>
              </div>

              {errors.code && (
                <p className="text-sm text-danger-500" role="alert">
                  {errors.code.message}
                </p>
              )}

              <Button type="submit" variant="primary" fullWidth loading={isLoading} className="mt-4">
                {t('auth.login')}
              </Button>
            </form>

            <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
              <p className="text-sm text-gray-500 dark:text-gray-400 text-center mb-4">
                Didn't receive the code?
              </p>
              <Button variant="ghost" fullWidth onClick={() => { /* Resend logic */ }} disabled={isLoading}>
                <span className="flex items-center justify-center gap-2">
                  <Smartphone className="w-4 h-4" />
                  Resend via SMS
                </span>
              </Button>
              <Button variant="ghost" fullWidth onClick={() => { /* Show QR */ }} disabled={isLoading} className="mt-2">
                <span className="flex items-center justify-center gap-2">
                  <QrCode className="w-4 h-4" />
                  Show QR Code
                </span>
              </Button>
            </div>
          </CardBody>
        </Card>

        <p className="text-center text-sm text-gray-500 dark:text-gray-400 mt-6">
          Container Vehicle Delivery Management System
        </p>
      </div>
    </div>
  );
};