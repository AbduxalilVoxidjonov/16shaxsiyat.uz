import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, Card, Input } from '@/shared/ui';
import { AppError } from '@/shared/api/AppError';
import { cn } from '@/shared/lib/cn';
import { ROUTES } from '@/shared/config/routes';
import { useLogin } from '../api/useLogin';
import { useAuthStore } from '../store/authStore';
import { AUTH_ERROR_CODES } from '../model/types';
import { buildLoginSchema, LOGIN_DEFAULT_VALUES, type LoginFormValues } from '../model/loginSchema';

/**
 * A-1 Kirish (`/admin/login`) — docs/11, A-1; docs/07, 2-bo'lim; docs/08, 2-bo'lim.
 *
 * TOTP maydoni faqat backend `TOTP_REQUIRED` qaytarganda ko'rinadi (birinchi urinishda
 * username+parol to'g'ri, lekin 2FA yoqilgan bo'lganda). Xato xabari qaysi maydon
 * (login/parol) noto'g'ri ekanini oshkor qilmaydi (CLAUDE.md, MUHIM SHARTNOMA NUANSLARI 5).
 */
export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const setSession = useAuthStore((state) => state.setSession);
  const loginMutation = useLogin();

  const [requiresTotp, setRequiresTotp] = useState(false);
  const [isLocked, setIsLocked] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  usePageTitle(t('pages.login.title'));

  const schema = useMemo(() => buildLoginSchema(requiresTotp), [requiresTotp]);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(schema),
    defaultValues: LOGIN_DEFAULT_VALUES,
  });

  useEffect(() => {
    if (requiresTotp) {
      setFocus('totpCode');
    }
  }, [requiresTotp, setFocus]);

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      const result = await loginMutation.mutateAsync({
        username: values.username,
        password: values.password,
        totpCode: requiresTotp ? values.totpCode : undefined,
      });

      setIsLocked(false);
      setSession(result.accessToken, result.user);

      const returnUrl = searchParams.get('returnUrl');
      navigate(returnUrl ? decodeURIComponent(returnUrl) : ROUTES.admin.dashboard, {
        replace: true,
      });
    } catch (error) {
      if (!(error instanceof AppError)) {
        setFormError(t('auth.login.genericError'));
        return;
      }

      switch (error.code) {
        case AUTH_ERROR_CODES.totpRequired:
          setRequiresTotp(true);
          setFormError(t('auth.login.totpRequired'));
          break;
        case AUTH_ERROR_CODES.totpInvalid:
          setError('totpCode', { type: 'server', message: t('auth.login.totpInvalid') });
          break;
        case AUTH_ERROR_CODES.accountLocked:
          setIsLocked(true);
          setFormError(error.message || t('auth.login.accountLocked'));
          break;
        case AUTH_ERROR_CODES.invalidCredentials:
        case 'UNAUTHORIZED':
          setFormError(t('auth.login.invalidCredentials'));
          break;
        default:
          // Tarmoq xatosi (`NETWORK_ERROR_CODE`) yoki kutilmagan kod — xom `error.message`
          // (masalan `fetch`ning inglizcha "Failed to fetch") ko'rsatilmaydi, faqat
          // tarjima qilingan umumiy xabar.
          setFormError(t('auth.login.genericError'));
      }
    }
  });

  return (
    <div className="flex min-h-dvh items-center justify-center bg-neutral-50 p-4">
      <Card className="w-full max-w-sm">
        <div className="mb-5 flex flex-col items-center gap-1 text-center">
          <p className="text-xl font-bold text-primary-700">{t('app.name')}</p>
          <h1 className="text-sm font-medium text-neutral-500">{t('auth.login.heading')}</h1>
        </div>
        <form onSubmit={(event) => void onSubmit(event)} noValidate className="flex flex-col gap-4">
          <Input
            label={t('auth.login.usernameLabel')}
            autoComplete="username"
            disabled={isLocked}
            error={errors.username?.message}
            {...register('username')}
          />
          <Input
            label={t('auth.login.passwordLabel')}
            type="password"
            autoComplete="current-password"
            disabled={isLocked}
            error={errors.password?.message}
            {...register('password')}
          />

          {requiresTotp && (
            <Input
              label={t('auth.login.totpLabel')}
              inputMode="numeric"
              maxLength={6}
              autoComplete="one-time-code"
              disabled={isLocked}
              hint={errors.totpCode ? undefined : t('auth.login.totpHint')}
              error={errors.totpCode?.message}
              {...register('totpCode')}
            />
          )}

          {formError && (
            <p
              role="alert"
              className={cn(
                'rounded-lg border px-3 py-2 text-sm',
                isLocked
                  ? 'border-warning-300 bg-warning-50 text-warning-700'
                  : 'border-danger-200 bg-danger-50 text-danger-700',
              )}
            >
              {formError}
            </p>
          )}

          <Button type="submit" size="lg" isLoading={isSubmitting} disabled={isLocked}>
            {t('auth.login.submitCta')}
          </Button>
        </form>
      </Card>
    </div>
  );
}
