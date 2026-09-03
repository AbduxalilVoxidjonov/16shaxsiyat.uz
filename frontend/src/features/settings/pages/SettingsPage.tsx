import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge, Button, Card, Checkbox, Dialog, Input, Skeleton } from '@/shared/ui';
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

/**
 * Zaxira kodlar `.txt` fayl nomi — foydalanuvchi diskiga tushadigan yagona nusxa.
 * Kodlar `localStorage`/konsol/URL'ga hech qachon yozilmaydi (CLAUDE.md 4-qoida ruhi):
 * `blob:` havolasi opaque UUID, kod matni URL'da ko'rinmaydi.
 */
const BACKUP_CODES_FILE_NAME = 'shaxsiyat-zaxira-kodlar.txt';

/** 2FA (TOTP) kartasi — yoqish/o'chirish (P22 qamrovi). */
function TwoFactorCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const accountQuery = useAccountStatusQuery();
  const enableTotp = useEnableTotp();
  const disableTotp = useDisableTotp();

  const [enableResult, setEnableResult] = useState<TotpEnableResponse | null>(null);
  const [codesSaved, setCodesSaved] = useState(false);
  const [dismissBlocked, setDismissBlocked] = useState(false);
  /**
   * Dialog `key`ining bir qismi. Native `<dialog>` `Escape`/backdrop bosilganda o'zini DOM
   * darajasida yopadi, `open` prop esa o'zgarmagani uchun `shared/ui/Dialog` uni qayta ochmaydi.
   * Nonce'ni oshirish Dialog'ni qayta o'rnatadi (remount) → effekt `showModal()`ni qayta chaqiradi.
   * Shu tariqa oyna zaxira kodlar saqlangani tasdiqlanmaguncha yopilmaydi — `shared/ui`ga
   * tegmasdan (u boshqa featurelar bilan bo'lishiladi).
   */
  const [dialogNonce, setDialogNonce] = useState(0);

  const [isDisableDialogOpen, setDisableDialogOpen] = useState(false);
  const [disablePassword, setDisablePassword] = useState('');
  const [disableError, setDisableError] = useState<string | null>(null);

  async function invalidateAccountStatus() {
    await queryClient.invalidateQueries({ queryKey: SETTINGS_QUERY_KEYS.account() });
  }

  async function handleEnable() {
    try {
      const result = await enableTotp.mutateAsync();
      setCodesSaved(false);
      setDismissBlocked(false);
      setEnableResult(result);
      await invalidateAccountStatus();
    } catch {
      toast.show({ variant: 'danger', title: t('settings.twoFactor.genericError') });
    }
  }

  /**
   * `Escape`, backdrop va yopish tugmasi shu yerga tushadi. 2FA server tomonda ALLAQACHON
   * yoqilgan, zaxira kodlar esa boshqa hech qachon ko'rsatilmaydi — shuning uchun tasdiqlashsiz
   * yopishga ruxsat berilmaydi (aks holda foydalanuvchi telefonini yo'qotsa hisobga kira olmaydi).
   */
  function handleBackupDialogClose() {
    if (!codesSaved) {
      setDismissBlocked(true);
      setDialogNonce((nonce) => nonce + 1);
      return;
    }
    closeBackupDialog();
  }

  function closeBackupDialog() {
    setEnableResult(null);
    setCodesSaved(false);
    setDismissBlocked(false);
  }

  async function handleCopyCodes() {
    if (!enableResult) return;
    const clipboard = navigator.clipboard as Clipboard | undefined;
    if (!clipboard?.writeText) {
      toast.show({ variant: 'danger', title: t('settings.twoFactor.copyError') });
      return;
    }
    try {
      await clipboard.writeText(enableResult.backupCodes.join('\n'));
      toast.show({ variant: 'success', title: t('settings.twoFactor.copySuccess') });
    } catch {
      toast.show({ variant: 'danger', title: t('settings.twoFactor.copyError') });
    }
  }

  function handleDownloadCodes() {
    if (!enableResult) return;
    if (typeof URL.createObjectURL !== 'function') {
      toast.show({ variant: 'danger', title: t('settings.twoFactor.downloadError') });
      return;
    }
    const content = `${t('settings.twoFactor.downloadFileHeading')}\n\n${enableResult.backupCodes.join('\n')}\n`;
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const objectUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = BACKUP_CODES_FILE_NAME;
    link.rel = 'noopener';
    document.body.append(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(objectUrl);
  }

  async function handleConfirmDisable() {
    setDisableError(null);
    if (!disablePassword) {
      setDisableError(t('settings.twoFactor.passwordRequired'));
      return;
    }
    try {
      await disableTotp.mutateAsync({ currentPassword: disablePassword });
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
        key={`totp-backup-codes-${dialogNonce}`}
        open={Boolean(enableResult)}
        onClose={handleBackupDialogClose}
        title={t('settings.twoFactor.enableSuccess')}
        description={t('settings.twoFactor.backupCodesHint')}
        footer={
          <Button disabled={!codesSaved} onClick={closeBackupDialog}>
            {t('settings.twoFactor.acknowledgeCta')}
          </Button>
        }
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
              <p id="totp-backup-codes-label" className="font-medium">
                {t('settings.twoFactor.backupCodesLabel')}
              </p>
              <ul
                aria-labelledby="totp-backup-codes-label"
                className="mt-1 grid grid-cols-2 gap-1 font-mono text-xs"
              >
                {enableResult.backupCodes.map((code, index) => (
                  // Kodlar takrorlanishi nazariy jihatdan mumkin (raqamli, tasodifiy) —
                  // ro'yxat statik, shuning uchun kalitda indeks ishlatiladi.
                  <li key={`${index}-${code}`} className="rounded bg-neutral-100 px-2 py-1">
                    {code}
                  </li>
                ))}
              </ul>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button size="sm" variant="outline" onClick={() => void handleCopyCodes()}>
                {t('settings.twoFactor.copyCta')}
              </Button>
              <Button size="sm" variant="outline" onClick={handleDownloadCodes}>
                {t('settings.twoFactor.downloadCta')}
              </Button>
            </div>
            <Checkbox
              label={t('settings.twoFactor.acknowledgeLabel')}
              checked={codesSaved}
              onChange={(event) => {
                setCodesSaved(event.target.checked);
                if (event.target.checked) setDismissBlocked(false);
              }}
            />
            {dismissBlocked && (
              <p role="alert" className="text-sm text-danger-600">
                {t('settings.twoFactor.acknowledgeRequired')}
              </p>
            )}
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
