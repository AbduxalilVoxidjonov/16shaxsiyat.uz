import { useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { BirthDateSelect, Button, Checkbox, ConsentBlock, Input, PhoneField, Select } from '@/shared/ui';
import { AppError } from '@/shared/api/AppError';
import { cn } from '@/shared/lib/cn';
import type { StartPublicSessionRequestBody, StartSessionResponse } from '@/shared/api/types';
import { useStartOwnSession } from '../api/useStartOwnSession';
import type { MyStudentProfile } from '../model/types';
import {
  MAX_AGE,
  MIN_AGE,
  PARENTAL_CONSENT_AGE,
  PUBLIC_REGISTRATION_DEFAULT_VALUES,
  ageFromFormValue,
  createPublicRegistrationSchema,
  type PublicRegistrationFormValues,
} from '../schemas/publicRegistrationSchema';
import { buildStartSessionPayload, needsConsent, profileToFormValues } from '../lib/profileState';
import { mapStartSessionErrorCode } from '../lib/startSessionErrors';

/** Sinf tanlovi — bo'sh qiymat "maktabda o'qimayman" degani. */
const GRADE_OPTIONS = Array.from({ length: 11 }, (_, index) => ({
  value: String(index + 1),
  label: String(index + 1),
}));

const FIELD_KEYS = Object.keys(PUBLIC_REGISTRATION_DEFAULT_VALUES) as Array<
  keyof PublicRegistrationFormValues
>;

function isFieldKey(key: string): key is keyof PublicRegistrationFormValues {
  return (FIELD_KEYS as string[]).includes(key);
}

export interface PublicRegistrationFormProps {
  profile: MyStudentProfile;
  /** `new` — birinchi anketa (to'liq to'plam), `edit` — mavjud profil tahriri (`lib/profileState.ts`). */
  mode: 'new' | 'edit';
  programCode?: string;
  onStarted: (result: StartSessionResponse) => void;
  onCancel?: () => void;
}

/**
 * Anketa formasi — `new` va `edit` rejimlarida. Boshlang'ich qiymatlar profildan
 * (`profileToFormValues`): `new` da F.I.Sh. Telegram taklifi, `edit` da hamma maydon
 * to'ldirilgan. Rozilik bloki faqat `needsConsent(profile)` bo'lganda (yangi profil yoki
 * eskirgan versiya) — sxema ham shunga mos (`createPublicRegistrationSchema`).
 *
 * Komponent alohida: `useForm` `defaultValues` ni faqat mount'da o'qiydi, profil esa
 * asinxron keladi — sahifa formani profil yuklangach mount qiladi.
 */
export function PublicRegistrationForm({ profile, mode, programCode, onStarted, onCancel }: PublicRegistrationFormProps) {
  const { t } = useTranslation();
  const startSession = useStartOwnSession();
  const [formError, setFormError] = useState<string | null>(null);

  const consentRequired = needsConsent(profile);
  const schema = useMemo(
    () => createPublicRegistrationSchema({ requireConsent: consentRequired }),
    [consentRequired],
  );

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<PublicRegistrationFormValues>({
    resolver: zodResolver(schema),
    defaultValues: profileToFormValues(profile),
  });

  const consentAccepted = useWatch({ control, name: 'consentAccepted' });
  const genderValue = useWatch({ control, name: 'gender' });
  const birthDateValue = useWatch({ control, name: 'birthDate' });

  // Ota-ona roziligi maydoni faqat sana to'ldirilgan VA yosh 18 dan kichik bo'lganda
  // ko'rsatiladi — kattalarga keraksiz savol berilmaydi (`docs/07` §5.4).
  const age = ageFromFormValue(birthDateValue);
  const needsParentalConsent = age !== null && age < PARENTAL_CONSENT_AGE;

  const showSuggestedNameHint = mode === 'new' && Boolean(profile.suggestedFullName);

  function applyServerError(error: AppError): void {
    if (error.code === 'VALIDATION_ERROR' && error.errors) {
      let mappedAny = false;
      for (const [key, messages] of Object.entries(error.errors)) {
        const message = messages[0];
        if (!message) continue;
        if (key === 'birthDate') {
          setError('birthDate.year', { type: 'server', message });
          mappedAny = true;
        } else if (isFieldKey(key)) {
          setError(key, { type: 'server', message });
          mappedAny = true;
        }
      }
      if (!mappedAny) {
        setFormError(error.message || t('account.register.errors.generic'));
      }
      return;
    }

    setFormError(mapStartSessionErrorCode(error, t));
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const payload: StartPublicSessionRequestBody = buildStartSessionPayload(values, mode, {
      needsConsent: consentRequired,
      programCode,
    });

    try {
      const result = await startSession.mutateAsync(payload);
      onStarted(result);
    } catch (error) {
      if (error instanceof AppError) {
        applyServerError(error);
      } else {
        setFormError(t('account.register.errors.generic'));
      }
    }
  });

  // Rozilik ko'rsatilmasa tugma rozilikka bog'liq emas.
  const submitDisabled = consentRequired && !consentAccepted;

  return (
    <form
      onSubmit={(event) => void onSubmit(event)}
      noValidate
      className="card flex flex-col gap-5 p-5 sm:p-7"
    >
      {mode === 'edit' && !profile.consentCurrent && (
        <p
          role="status"
          className="rounded-2xl border border-zarhal-200 bg-zarhal-50 px-4 py-3 text-sm text-ink-soft"
        >
          {t('account.register.consentOutdated')}
        </p>
      )}

      <Input
        label={t('register.fields.fullName')}
        autoComplete="name"
        hint={errors.fullName || !showSuggestedNameHint ? undefined : t('account.register.suggestedNameHint')}
        error={errors.fullName?.message}
        {...register('fullName')}
      />

      <Controller
        control={control}
        name="birthDate"
        render={({ field }) => (
          <BirthDateSelect
            value={field.value}
            onChange={field.onChange}
            onBlur={field.onBlur}
            minAge={MIN_AGE}
            maxAge={MAX_AGE}
            errors={{
              day: errors.birthDate?.day?.message,
              month: errors.birthDate?.month?.message,
              year: errors.birthDate?.year?.message,
            }}
          />
        )}
      />

      <fieldset className="flex flex-col gap-1.5">
        <legend className="mb-1.5 text-sm font-medium text-ink-soft">
          {t('register.fields.gender')}
        </legend>
        <div className="flex gap-3">
          {(['Male', 'Female'] as const).map((option) => (
            <label
              key={option}
              className={cn(
                'flex h-11 flex-1 cursor-pointer items-center justify-center rounded-xl border text-sm font-semibold transition-colors',
                // Radio `sr-only` — fokus halqasi o'rovchi yorliqda ko'rsatiladi.
                'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-500',
                genderValue === option
                  ? 'border-firuza-500 bg-firuza-50 text-firuza-800'
                  : 'border-line bg-paper-card text-ink-soft hover:border-firuza-300',
              )}
            >
              <input type="radio" value={option} className="sr-only" {...register('gender')} />
              {/*
                Yorliqlar maktab anketasidan FARQ QILADI: u yerda "O'g'il bola"/"Qiz bola"
                (o'quvchilar uchun), bu yerda esa foydalanuvchi 99 yoshgacha bo'lishi
                mumkin — "Erkak"/"Ayol".
              */}
              {t(`account.register.genderOptions.${option === 'Male' ? 'male' : 'female'}`)}
            </label>
          ))}
        </div>
        {errors.gender && (
          <p role="alert" className="text-sm text-terakota-700">
            {errors.gender.message}
          </p>
        )}
      </fieldset>

      <Select
        label={t('account.register.fields.grade')}
        hint={errors.grade ? undefined : t('account.register.fields.gradeHint')}
        options={[{ value: '', label: t('account.register.fields.gradeNone') }, ...GRADE_OPTIONS]}
        error={errors.grade?.message}
        {...register('grade')}
      />

      <Controller
        control={control}
        name="phone"
        render={({ field }) => (
          <PhoneField
            value={field.value}
            onValueChange={field.onChange}
            onBlur={field.onBlur}
            error={errors.phone?.message}
            label={t('register.fields.phone')}
          />
        )}
      />

      <Input
        label={t('register.fields.email')}
        type="email"
        autoComplete="email"
        hint={errors.email ? undefined : t('register.fields.emailHint')}
        error={errors.email?.message}
        {...register('email')}
      />

      {consentRequired && (
        <Controller
          control={control}
          name="consentAccepted"
          render={({ field }) => (
            <ConsentBlock
              consentText={t('account.register.consentText')}
              checked={field.value}
              onChange={field.onChange}
              onBlur={field.onBlur}
              error={errors.consentAccepted?.message}
            />
          )}
        />
      )}

      {needsParentalConsent && (
        <Controller
          control={control}
          name="parentalConsent"
          render={({ field }) => (
            <div className="rounded-3xl border border-zarhal-200 bg-zarhal-50 p-5">
              <p className="mb-3 text-sm leading-relaxed text-ink-soft">
                {t('account.register.parentalConsentText')}
              </p>
              <Checkbox
                label={t('account.register.parentalConsentLabel')}
                checked={field.value}
                onChange={(event) => {
                  field.onChange(event.target.checked);
                }}
                onBlur={field.onBlur}
                error={errors.parentalConsent?.message}
              />
            </div>
          )}
        />
      )}

      {formError && (
        <p
          role="alert"
          className="rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
        >
          {formError}
        </p>
      )}

      <div className="flex flex-col gap-3 sm:flex-row-reverse">
        <Button
          type="submit"
          size="lg"
          className="w-full sm:flex-1"
          isLoading={isSubmitting}
          disabled={submitDisabled}
        >
          {t('account.register.submitCta')}
        </Button>
        {onCancel && (
          <Button type="button" size="lg" variant="ghost" className="w-full sm:w-auto" onClick={onCancel}>
            {t('account.register.cancelEdit')}
          </Button>
        )}
      </div>
    </form>
  );
}
