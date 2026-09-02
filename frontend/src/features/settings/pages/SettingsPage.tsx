import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge, Button, Card, Dialog, Input, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useChangePassword } from '../api/useChangePassword';
import { useAccountStatusQuery } from '../api/useAccountStatus';
import { useDisableTotp, useEnableTotp } from '../api/useTotp';
import { SETTINGS_QUERY_KEYS } from '../api/settingsKeys';
import { SETTINGS_ERROR_CODES, type TotpEnableResponse } from '../model/types';
import {
  changePasswordSchema,
  CHANGE_PASSWORD_DEFAULT_VALUES,
  type ChangePasswordFormValues,
} from '../model/changePasswordSchema';

/** Parolni o'zgartirish kartasi — docs/10 route jadvali: "/admin/settings — Parol, 2FA...". */
function ChangePasswordCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const changePassword = useChangePassword();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: CHANGE_PASSWORD_DEFAULT_VALUES,
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await changePassword.mutateAsync({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      reset(CHANGE_PASSWORD_DEFAULT_VALUES);
      toast.show({ variant: 'success', title: t('settings.changePassword.success') });
    } catch (error) {
      if (error instanceof AppError && error.code === SETTINGS_ERROR_CODES.currentPasswordInvalid) {
        setError('currentPassword', {
          type: 'server',
          message: t('settings.changePassword.currentPasswordInvalid'),
        });
        return;
      }
      setFormError(t('settings.changePassword.genericError'));
    }
  });

  return (
    <Card title={t('settings.changePassword.heading')}>
      <form
        onSubmit={(event) => void onSubmit(event)}
        noValidate
        className="flex max-w-sm flex-col gap-4"
      >
        <Input
          label={t('settings.changePassword.currentPasswordLabel')}
          type="password"
          autoComplete="current-password"
          error={errors.currentPassword?.message}
          {...register('currentPassword')}
        />
        <Input
          label={t('settings.changePassword.newPasswordLabel')}
          type="password"
          autoComplete="new-password"
          error={errors.newPassword?.message}
          {...register('newPassword')}
        />
        <Input
          label={t('settings.changePassword.confirmPasswordLabel')}
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        {formError && (
          <p role="alert" className="text-sm text-danger-600">
            {formError}
          </p>
        )}
        <Button type="submit" isLoading={isSubmitting} className="self-start">
          {t('settings.changePassword.submitCta')}
        </Button>
      </form>
    </Card>
  );
}

/** 2FA (TOTP) kartasi — yoqish/o'chirish skeleti (P22 qamrovi). */
function TwoFactorCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const accountQuery = useAccountStatusQuery();
  const enableTotp = useEnableTotp();
  const disableTotp = useDisableTotp();

  const [enableResult, setEnableResult] = useState<TotpEnableResponse | null>(null);
  const [isDisableDialogOpen, setDisableDialogOpen] = useState(false);
  const [disablePassword, setDisablePassword] = useState('');
  const [disableError, setDisableError] = useState<string | null>(null);

  async function invalidateAccountStatus() {
    await queryClient.invalidateQueries({ queryKey: SETTINGS_QUERY_KEYS.account() });
  }

  async function handleEnable() {
    try {
      const result = await enableTotp.mutateAsync();
      setEnableResult(result);
      await invalidateAccountStatus();
    } catch {
      toast.show({ variant: 'danger', title: t('settings.twoFactor.genericError') });
    }
  }

  async function handleConfirmDisable() {
    setDisableError(null);
    if (!disablePassword) {
      setDisableError(t('settings.twoFactor.passwordRequired'));
      return;
    }
    try {
      await disableTotp.mutateAsync({ password: disablePassword });
      setDisableDialogOpen(false);
      setDisablePassword('');
      await invalidateAccountStatus();
      toast.show({ variant: 'success', title: t('settings.twoFactor.disableSuccess') });
    } catch (error) {
      setDisableError(
        error instanceof AppError ? error.message : t('settings.twoFactor.genericError'),
      );
    }
  }

  const totpEnabled = accountQuery.data?.totpEnabled ?? false;

  return (
    <Card title={t('settings.twoFactor.heading')}>
      <div className="flex max-w-sm flex-col gap-4">
        <p className="text-sm text-neutral-600">{t('settings.twoFactor.description')}</p>

        {accountQuery.isPending ? (
          <Skeleton className="h-6 w-24" />
        ) : (
          <div className="flex items-center gap-2">
            <Badge variant={totpEnabled ? 'success' : 'neutral'}>
              {totpEnabled
                ? t('settings.twoFactor.enabledStatus')
                : t('settings.twoFactor.disabledStatus')}
            </Badge>
          </div>
        )}

        {!accountQuery.isPending &&
          (totpEnabled ? (
            <Button
              variant="danger"
              className="self-start"
              onClick={() => setDisableDialogOpen(true)}
            >
              {t('settings.twoFactor.disableCta')}
            </Button>
          ) : (
            <Button
              variant="outline"
              className="self-start"
              isLoading={enableTotp.isPending}
              onClick={() => void handleEnable()}
            >
              {t('settings.twoFactor.enableCta')}
            </Button>
          ))}
      </div>

      <Dialog
        open={Boolean(enableResult)}
        onClose={() => setEnableResult(null)}
        title={t('settings.twoFactor.enableSuccess')}
        footer={<Button onClick={() => setEnableResult(null)}>{t('common.close')}</Button>}
      >
        {enableResult && (
          <div className="flex flex-col gap-3 text-sm text-neutral-700">
            <div>
              <p className="font-medium">{t('settings.twoFactor.secretLabel')}</p>
              <code className="mt-1 block rounded-md bg-neutral-100 px-2 py-1 break-all">
                {enableResult.secret}
              </code>
            </div>
            <div>
              <p className="font-medium">{t('settings.twoFactor.recoveryCodesLabel')}</p>
              <ul className="mt-1 grid grid-cols-2 gap-1 font-mono text-xs">
                {enableResult.recoveryCodes.map((code) => (
                  <li key={code} className="rounded bg-neutral-100 px-2 py-1">
                    {code}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        )}
      </Dialog>

      <Dialog
        open={isDisableDialogOpen}
        onClose={() => {
          setDisableDialogOpen(false);
          setDisablePassword('');
          setDisableError(null);
        }}
        title={t('settings.twoFactor.confirmDisableTitle')}
        description={t('settings.twoFactor.confirmDisableDescription')}
        footer={
          <>
            <Button variant="outline" onClick={() => setDisableDialogOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              variant="danger"
              isLoading={disableTotp.isPending}
              onClick={() => void handleConfirmDisable()}
            >
              {t('settings.twoFactor.disableCta')}
            </Button>
          </>
        }
      >
        <Input
          label={t('settings.twoFactor.passwordLabel')}
          type="password"
          autoComplete="current-password"
          value={disablePassword}
          onChange={(event) => setDisablePassword(event.target.value)}
          error={disableError ?? undefined}
        />
      </Dialog>
    </Card>
  );
}

export default function SettingsPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.settings.title'));

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-neutral-900">{t('pages.settings.title')}</h1>
      <ChangePasswordCard />
      <TwoFactorCard />
    </div>
  );
}
