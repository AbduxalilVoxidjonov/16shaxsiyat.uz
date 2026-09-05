import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import {
  BirthDateSelect,
  Button,
  Checkbox,
  ConsentBlock,
  Input,
  PhoneField,
  Select,
} from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { PUBLIC_SPACE_SLUG } from '@/shared/config/publicSpace';
import { AppError } from '@/shared/api/AppError';
import { adoptSession } from '@/shared/api/sessionToken';
import { birthDateToIso } from '@/shared/lib/birthDate';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import { toE164UzPhone } from '@/shared/lib/formatPhone';
import { cn } from '@/shared/lib/cn';
import type { Gender, StartPublicSessionRequestBody } from '@/shared/api/types';
import { useStartOwnSession } from '../api/useStartOwnSession';
import {
  MAX_AGE,
  MIN_AGE,
  PARENTAL_CONSENT_AGE,
  PUBLIC_REGISTRATION_DEFAULT_VALUES,
  ageFromFormValue,
  publicRegistrationSchema,
  type PublicRegistrationFormValues,
} from '../schemas/publicRegistrationSchema';

/** Sinf tanlovi — bo'sh qiymat "maktabda o'qimayman" degani (`grade: null`). */
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

/**
 * `/kabinet/test` — MAKTABSIZ anketa (`POST /api/me/sessions`, `docs/07` §5.4).
 *
 * Maktab anketasidan (`features/public-assessment/pages/RegistrationPage`) farqi:
 * `slug`/`accessToken`/`accessCode`/`classLetter`/`parentPhone` YO'Q, yosh 6–99, `grade`
 * ixtiyoriy, 18 yoshgacha ota-ona roziligi majburiy. Umumiy qismlar (`BirthDateSelect`,
 * `PhoneField`, `ConsentBlock`, sana hisobi, `pickNextTestCode`) `shared/` ga chiqarilgan —
 * maktab anketasi O'ZGARMAGAN.
 *
 * Sessiya ochilgach foydalanuvchi **mavjud** test oqimida davom etadi
 * (`/t/ommaviy/test/:testCode`, `X-Session-Token`) — parallel oqim yo'q.
 */
export default function PublicRegistrationPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const startSession = useStartOwnSession();
  const [formError, setFormError] = useState<string | null>(null);

  usePageTitle(t('account.register.title'));

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<PublicRegistrationFormValues>({
    resolver: zodResolver(publicRegistrationSchema),
    defaultValues: PUBLIC_REGISTRATION_DEFAULT_VALUES,
  });

  const consentAccepted = useWatch({ control, name: 'consentAccepted' });
  const genderValue = useWatch({ control, name: 'gender' });
  const birthDateValue = useWatch({ control, name: 'birthDate' });

  // Ota-ona roziligi maydoni faqat sana to'ldirilgan VA yosh 18 dan kichik bo'lganda
  // ko'rsatiladi — kattalarga keraksiz savol berilmaydi (`docs/07` §5.4).
  const age = ageFromFormValue(birthDateValue);
  const needsParentalConsent = age !== null && age < PARENTAL_CONSENT_AGE;

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

    const codeMessages: Record<string, string> = {
      PROGRAM_REQUIRED: t('account.register.errors.programRequired'),
      NO_PROGRAM_AVAILABLE: t('account.register.errors.noProgram'),
      PUBLIC_SPACE_NOT_CONFIGURED: t('account.register.errors.notConfigured'),
      SCHOOL_INACTIVE: t('account.register.errors.inactive'),
      DUPLICATE_ASSESSMENT: t('account.register.errors.duplicate'),
      RATE_LIMITED: t('account.register.errors.rateLimited'),
      NOT_FOUND: t('account.register.errors.noProgram'),
    };

    setFormError(codeMessages[error.code] ?? error.message ?? t('account.register.errors.generic'));
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const payload: StartPublicSessionRequestBody = {
      fullName: values.fullName,
      birthDate: birthDateToIso(values.birthDate),
      gender: values.gender as Gender,
      phone: toE164UzPhone(values.phone) ?? '',
      consentAccepted: values.consentAccepted,
      parentalConsent: values.parentalConsent,
      // Bo'sh tanlov — "maktabda o'qimayman": `null`, `0` EMAS (`docs/07` §5.4).
      grade: values.grade ? Number(values.grade) : null,
      email: values.email || null,
      languageCode: 'uz',
    };

    try {
      const result = await startSession.mutateAsync(payload);
      // Sessiyani o'quvchi oqimiga uzatamiz (`shared/api/sessionToken.ts` izohiga qarang).
      adoptSession({
        sessionToken: result.sessionToken,
        slug: PUBLIC_SPACE_SLUG,
        assessmentId: result.assessmentId,
      });

      if (result.resumed) {
        toast.show({ variant: 'info', title: t('account.register.resumedNotice') });
      }

      const nextTestCode = pickNextTestCode(result.tests);
      navigate(
        nextTestCode
          ? ROUTES.public.test(PUBLIC_SPACE_SLUG, nextTestCode)
          : ROUTES.public.finish(PUBLIC_SPACE_SLUG),
      );
    } catch (error) {
      if (error instanceof AppError) {
        applyServerError(error);
      } else {
        setFormError(t('account.register.errors.generic'));
      }
    }
  });

  return (
    <div className="wrap flex max-w-2xl flex-col gap-6 py-12 sm:py-16">
      <Link to={ROUTES.account.home} className="btn btn-md btn-ghost self-start">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('account.register.back')}
      </Link>

      <header className="flex flex-col gap-2 text-center">
        <p className="eyebrow text-firuza-700">{t('account.register.eyebrow')}</p>
        <h1 className="font-display balance text-3xl font-extrabold tracking-tight text-ink">
          {t('account.register.title')}
        </h1>
        <p className="lead text-[15px]">{t('account.register.lead')}</p>
      </header>

      <form
        onSubmit={(event) => void onSubmit(event)}
        noValidate
        className="card flex flex-col gap-5 p-5 sm:p-7"
      >
        <Input
          label={t('register.fields.fullName')}
          autoComplete="name"
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

        <Button
          type="submit"
          size="lg"
          className="w-full"
          isLoading={isSubmitting}
          disabled={!consentAccepted}
        >
          {t('account.register.submitCta')}
        </Button>
      </form>
    </div>
  );
}
