import { useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import {
  BirthDateSelect,
  Button,
  Checkbox,
  ConsentBlock,
  Input,
  PhoneField,
  RegistrationCustomFieldInput,
  Select,
} from '@/shared/ui';
import { AppError } from '@/shared/api/AppError';
import { cn } from '@/shared/lib/cn';
import type { StartSessionResponse } from '@/shared/api/types';
import { orderedRegistrationFields } from '@/shared/lib/registrationFormFields';
import { useStartOwnSession } from '../api/useStartOwnSession';
import { useUpdateMyProfile } from '../api/useUpdateMyProfile';
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
import {
  buildProfilePayload,
  buildStartSessionPayload,
  needsConsent,
  profileToFormValues,
  submitActionFor,
  type ProfileFormMode,
} from '../lib/profileState';
import { mapStartSessionErrorCode } from '../lib/startSessionErrors';

/** Sinf tanlovi — bo'sh qiymat "maktabda o'qimayman" degani. */
const GRADE_OPTIONS = Array.from({ length: 11 }, (_, index) => ({
  value: String(index + 1),
  label: String(index + 1),
}));

const FIELD_KEYS = (Object.keys(PUBLIC_REGISTRATION_DEFAULT_VALUES) as Array<
  keyof PublicRegistrationFormValues
>).filter((key) => key !== 'customFields');

function isFieldKey(key: string): key is keyof PublicRegistrationFormValues {
  return (FIELD_KEYS as string[]).includes(key);
}

export interface PublicRegistrationFormProps {
  profile: MyStudentProfile;
  /**
   * `new` — birinchi anketa (to'liq to'plam, test boshlanadi), `consent` — profil bor, rozilik/
   * maydon yetishmaydi (test boshlanadi), `edit` — foydalanuvchi "O'zgartirish" bosdi (FAQAT
   * saqlanadi). Qoida — `submitActionFor` (`lib/profileState.ts`).
   */
  mode: ProfileFormMode;
  programCode?: string;
  /** Sessiya ochildi (`new`/`consent`). */
  onStarted: (result: StartSessionResponse) => void;
  /** Profil saqlandi, sessiya OCHILMADI (`edit`) — yangilangan profil bilan. */
  onSaved: (profile: MyStudentProfile) => void;
  onCancel?: () => void;
}

/**
 * Anketa formasi — `new`, `consent` va `edit` rejimlarida. Boshlang'ich qiymatlar profildan
 * (`profileToFormValues`): `new` da F.I.Sh. Telegram taklifi, qolganida hamma maydon
 * to'ldirilgan. Rozilik bloki faqat `needsConsent(profile)` bo'lganda (yangi profil yoki
 * eskirgan versiya) — sxema ham shunga mos (`createPublicRegistrationSchema`).
 *
 * Submit `submitActionFor(mode)` ga bog'liq: `edit` → `PUT /api/me/profile` (test
 * boshlanmaydi), aks holda `POST /api/me/sessions`. Tugma matni ham shu amalga mos —
 * "Saqlash" yoki "Testni boshlash".
 *
 * Komponent alohida: `useForm` `defaultValues` ni faqat mount'da o'qiydi, profil esa
 * asinxron keladi — sahifa formani profil yuklangach mount qiladi.
 */
export function PublicRegistrationForm({
  profile,
  mode,
  programCode,
  onStarted,
  onSaved,
  onCancel,
}: PublicRegistrationFormProps) {
  const { t } = useTranslation();
  const startSession = useStartOwnSession();
  const updateProfile = useUpdateMyProfile();
  const [formError, setFormError] = useState<string | null>(null);

  const submitAction = submitActionFor(mode);
  const genericErrorMessage = t(
    submitAction === 'saveProfile'
      ? 'account.register.errors.saveGeneric'
      : 'account.register.errors.generic',
  );

  const consentRequired = needsConsent(profile);
  // `profile.registrationForm` — GLOBAL ro'yxatdan o'tish formasi ta'rifi (P52 2-to'lqin,
  // 2026-09-12, `docs/18` §9.6.2). `useMyProfile()` (TanStack Query) keshi barqaror obyekt
  // qaytaradi — memo bog'liqligi to'g'ridan-to'g'ri ishlatiladi (`RegistrationPage.tsx`dagi
  // bilan bir xil naqsh).
  const registrationForm = profile.registrationForm;
  const genderHidden = registrationForm.coreFields.gender.requirement === 'Hidden';
  const schema = useMemo(
    () =>
      createPublicRegistrationSchema({
        requireConsent: consentRequired,
        genderRequirement: registrationForm.coreFields.gender.requirement,
        customFields: registrationForm.customFields,
        requireCustomFields: mode === 'new',
      }),
    [consentRequired, registrationForm, mode],
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
        } else if (registrationForm.customFields.some((field) => field.code === key)) {
          setError(`customFields.${key}`, { type: 'server', message });
          mappedAny = true;
        }
      }
      if (!mappedAny) {
        setFormError(error.message || genericErrorMessage);
      }
      return;
    }

    setFormError(mapStartSessionErrorCode(error, t, genericErrorMessage));
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    try {
      if (submitAction === 'saveProfile') {
        // `edit`: FAQAT saqlash — `POST /api/me/sessions` bu shoxda hech qachon chaqirilmaydi.
        const saved = await updateProfile.mutateAsync(
          buildProfilePayload(values, mode, {
            needsConsent: consentRequired,
            customFields: registrationForm.customFields,
          }),
        );
        onSaved(saved);
        return;
      }

      const result = await startSession.mutateAsync(
        buildStartSessionPayload(values, mode, {
          needsConsent: consentRequired,
          programCode,
          customFields: registrationForm.customFields,
        }),
      );
      onStarted(result);
    } catch (error) {
      if (error instanceof AppError) {
        applyServerError(error);
      } else {
        setFormError(genericErrorMessage);
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
      {mode !== 'new' && !profile.consentCurrent && (
        <p
          role="status"
          className="rounded-2xl border border-zarhal-200 bg-zarhal-50 px-4 py-3 text-sm text-ink-soft"
        >
          {t('account.register.consentOutdated')}
        </p>
      )}

      <Input
        label={registrationForm.coreFields.fullName.labelUz || t('register.fields.fullName')}
        placeholder={registrationForm.coreFields.fullName.placeholderUz ?? undefined}
        autoComplete="name"
        hint={errors.fullName || !showSuggestedNameHint ? undefined : t('account.register.suggestedNameHint')}
        error={errors.fullName?.message}
        {...register('fullName')}
      />

      {/*
        `birthDate`/`phone` bu oqimda GLOBAL sozlamadan qat'i nazar HAMISHA majburiy va
        ko'rsatiladi (`docs/07` §5.1b/§5.4, `publicRegistrationSchema.ts`dagi izohga qarang) —
        faqat yorliq/joy egallovchi matn sozlamadan olinadi.
      */}
      <Controller
        control={control}
        name="birthDate"
        render={({ field }) => (
          <BirthDateSelect
            label={registrationForm.coreFields.birthDate.labelUz || t('register.fields.birthDate')}
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

      {/* `gender` — YAGONA maydon bu oqimda GLOBAL sozlamaga ergashadi (Hidden bo'lsa ko'rsatilmaydi). */}
      {!genderHidden && (
        <fieldset className="flex flex-col gap-1.5">
          <legend className="mb-1.5 text-sm font-medium text-ink-soft">
            {registrationForm.coreFields.gender.labelUz || t('register.fields.gender')}
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
      )}

      <Select
        label={registrationForm.coreFields.grade.labelUz || t('account.register.fields.grade')}
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
            label={registrationForm.coreFields.phone.labelUz || t('register.fields.phone')}
          />
        )}
      />

      <Input
        label={registrationForm.coreFields.email.labelUz || t('register.fields.email')}
        placeholder={registrationForm.coreFields.email.placeholderUz ?? undefined}
        type="email"
        autoComplete="email"
        hint={errors.email ? undefined : t('register.fields.emailHint')}
        error={errors.email?.message}
        {...register('email')}
      />

      {/*
        Superadmin qo'shgan o'z maydonlari — FAQAT `new` rejimida (`docs/07` §5.1b/§5.4:
        "faqat profil YARATILAYOTGANDA so'raladi"). `edit`/`consent`da profil allaqachon bor —
        `GET /api/me/profile` oldin to'ldirilgan `customFields` QIYMATLARINI qaytarmaydi
        (faqat forma ta'rifi), shu sabab bo'sh maydonlarni qayta ko'rsatish "tozalab
        qo'yildimi" degan noto'g'ri taassurot qoldirardi.
      */}
      {mode === 'new' &&
        orderedRegistrationFields(registrationForm)
          .filter((item): item is typeof item & { kind: 'custom' } => item.kind === 'custom')
          .map((item) => {
            if (item.field.requirement === 'Hidden') return null;
            const code = item.field.code;
            return (
              <RegistrationCustomFieldInput
                key={code}
                field={item.field}
                control={control}
                name={`customFields.${code}`}
                error={errors.customFields?.[code]?.message}
              />
            );
          })}

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

      {submitAction === 'startSession' && (
        // Foydalanuvchi bosishdan oldin bilsin: bu tugma anketani saqlaydi VA testni boshlaydi.
        <p className="text-center text-sm text-ink-muted">{t('account.register.startNote')}</p>
      )}

      <div className="flex flex-col gap-3 sm:flex-row-reverse">
        <Button
          type="submit"
          size="lg"
          className="w-full sm:flex-1"
          isLoading={isSubmitting}
          disabled={submitDisabled}
        >
          {t(
            submitAction === 'saveProfile'
              ? 'account.register.saveProfile'
              : 'account.register.submitCta',
          )}
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
