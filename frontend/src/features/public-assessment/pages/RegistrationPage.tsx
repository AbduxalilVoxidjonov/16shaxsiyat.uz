import { useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, ErrorState, Input, Select, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { cn } from '@/shared/lib/cn';
import { toE164UzPhone } from '@/shared/lib/formatPhone';
import type { Gender, StartSessionRequestBody } from '@/shared/api/types';
import { useSchoolInfo } from '../api/useSchoolInfo';
import { useStartSession } from '../api/useStartSession';
import { useSessionStore } from '../store/sessionStore';
import { pickNextTestCode } from '../lib/nextTest';
import { ConsentBlock } from '../components/ConsentBlock';
import { PhoneField } from '../components/PhoneField';
import { BirthDateSelect } from '../components/BirthDateSelect';
import {
  MAX_AGE,
  MIN_AGE,
  REGISTRATION_DEFAULT_VALUES,
  birthDateToIso,
  buildRegistrationSchema,
  type RegistrationFormValues,
} from '../schemas/registrationSchema';

const GRADE_OPTIONS = Array.from({ length: 11 }, (_, index) => ({
  value: String(index + 1),
  label: String(index + 1),
}));

/**
 * Backend `errors` lug'ati kalitlari endi camelCase (`ValidationException.ToCamelCasePropertyPath`,
 * backend tuzatildi) — RHF maydon nomlari bilan to'g'ridan-to'g'ri mos (`fullName`, `phone`,
 * `parentPhone`, `email`, `grade`, `consentAccepted`; maydon nomlash ataylab shunga moslab
 * tanlangan — `registrationSchema.ts`dagi izohga qarang). Shu sabab endi alohida
 * PascalCase→RHF xarita (map) shart emas — faqat qaysi kalitlar bizning forma maydonlarimizga
 * tegishli ekanini bilish uchun kichik ro'yxat (`REGISTRATION_DEFAULT_VALUES` kalitlari) bilan
 * tekshiriladi. Yagona istisno — `birthDate`: bizda 3 ta alohida select (`day`/`month`/`year`)
 * bor, backend esa yagona `BirthDate`ni tekshiradi (yosh oralig'i) — shu xato `birthDate.year`ga
 * bog'lanadi (aynan shu yerda `registrationSchema.ts`ning o'z yosh xatosi ham chiqadi).
 */
const REGISTRATION_FIELD_KEYS = Object.keys(REGISTRATION_DEFAULT_VALUES) as Array<
  keyof RegistrationFormValues
>;

function isRegistrationFieldKey(key: string): key is keyof RegistrationFormValues {
  return (REGISTRATION_FIELD_KEYS as string[]).includes(key);
}

/** Yuklanish holati skeleti (docs/10, 7-bo'lim). */
function RegistrationSkeleton() {
  return (
    <div className="flex flex-col gap-5" aria-hidden="true">
      <Skeleton className="h-7 w-48" />
      {[0, 1, 2, 3, 4, 5].map((key) => (
        <Skeleton key={key} className="h-11 w-full rounded-lg" />
      ))}
      <Skeleton className="h-12 w-full rounded-lg" />
    </div>
  );
}

/** E-2 Anketa (`/t/:slug/register`) — docs/11 E-2, docs/07 1.2-bo'lim, prompts/20. */
export default function RegistrationPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const { slug = '' } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const accessToken = searchParams.get('k') ?? '';

  const schoolInfoQuery = useSchoolInfo(slug, accessToken);
  const startSession = useStartSession();
  const setSession = useSessionStore((state) => state.setSession);

  const [formError, setFormError] = useState<string | null>(null);

  usePageTitle(t('pages.register.title'));

  const requiresAccessCode = schoolInfoQuery.data?.requiresAccessCode ?? false;
  const schema = useMemo(() => buildRegistrationSchema(requiresAccessCode), [requiresAccessCode]);

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegistrationFormValues>({
    resolver: zodResolver(schema),
    defaultValues: REGISTRATION_DEFAULT_VALUES,
  });

  // `useWatch` — `form.watch()` o'rniga: React Compiler `watch()` funksiyasini memoize qila
  // olmaydi (render vaqtida chaqirilganda "incompatible library" ogohlantiradi), `useWatch`
  // esa kontekst orqali obuna bo'lib xavfsiz qayta render qiladi.
  const consentAccepted = useWatch({ control, name: 'consentAccepted' });
  const genderValue = useWatch({ control, name: 'gender' });

  function applyServerError(error: AppError): void {
    if (error.code === 'VALIDATION_ERROR' && error.errors) {
      let mappedAny = false;
      for (const [key, messages] of Object.entries(error.errors)) {
        const message = messages[0];
        if (message && key === 'birthDate') {
          setError('birthDate.year', { type: 'server', message });
          mappedAny = true;
        } else if (message && isRegistrationFieldKey(key)) {
          setError(key, { type: 'server', message });
          mappedAny = true;
        }
      }
      if (!mappedAny) {
        setFormError(error.message || t('pages.register.genericSubmitError'));
      }
      return;
    }

    if (error.code === 'ACCESS_CODE_INVALID') {
      setError('accessCode', { type: 'server', message: error.message });
      return;
    }

    if (error.code === 'DUPLICATE_ASSESSMENT') {
      setFormError(t('pages.register.duplicateAssessment'));
      return;
    }

    if (error.code === 'RATE_LIMITED') {
      setFormError(t('pages.register.rateLimited'));
      return;
    }

    if (error.code === 'NOT_FOUND') {
      setFormError(t('pages.landing.notFoundDescription'));
      return;
    }

    if (error.code === 'SCHOOL_INACTIVE') {
      setFormError(t('pages.landing.inactiveDescription'));
      return;
    }

    setFormError(error.message || t('pages.register.genericSubmitError'));
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);

    const payload: StartSessionRequestBody = {
      slug,
      accessToken,
      accessCode: requiresAccessCode ? values.accessCode : undefined,
      fullName: values.fullName,
      birthDate: birthDateToIso(values.birthDate),
      gender: values.gender as Gender,
      grade: Number(values.grade),
      classLetter: values.classLetter ? values.classLetter.toUpperCase() : undefined,
      phone: toE164UzPhone(values.phone) ?? '',
      parentPhone: values.parentPhone ? (toE164UzPhone(values.parentPhone) ?? undefined) : undefined,
      email: values.email || undefined,
      consentAccepted: values.consentAccepted,
      languageCode: 'uz',
    };

    try {
      const result = await startSession.mutateAsync(payload);
      setSession(result.sessionToken, slug, result.assessmentId);
      if (result.resumed) {
        toast.show({ variant: 'info', title: t('pages.register.resumedNotice') });
      }
      const nextTestCode = pickNextTestCode(result.tests);
      navigate(nextTestCode ? ROUTES.public.test(slug, nextTestCode) : ROUTES.public.finish(slug));
    } catch (error) {
      if (error instanceof AppError) {
        applyServerError(error);
      } else {
        setFormError(t('pages.register.genericSubmitError'));
      }
    }
  });

  if (schoolInfoQuery.isPending) {
    return <RegistrationSkeleton />;
  }

  if (schoolInfoQuery.isError) {
    const error = schoolInfoQuery.error;
    if (error instanceof AppError && error.status === 404) {
      return (
        <ErrorState
          title={t('pages.landing.notFoundTitle')}
          description={t('pages.landing.notFoundDescription')}
        />
      );
    }
    if (error instanceof AppError && error.status === 410) {
      return (
        <ErrorState
          title={t('pages.landing.inactiveTitle')}
          description={t('pages.landing.inactiveDescription')}
        />
      );
    }
    return <ErrorState onRetry={() => void schoolInfoQuery.refetch()} />;
  }

  const school = schoolInfoQuery.data;

  return (
    <form onSubmit={(event) => void onSubmit(event)} noValidate className="flex flex-col gap-5">
      <h1 className="text-xl font-bold text-neutral-900">{t('pages.register.title')}</h1>

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
        <legend className="mb-1.5 text-sm font-medium text-neutral-700">
          {t('register.fields.gender')}
        </legend>
        <div className="flex gap-3">
          {(['Male', 'Female'] as const).map((option) => (
            <label
              key={option}
              className={cn(
                'flex h-11 flex-1 cursor-pointer items-center justify-center rounded-lg border text-sm font-medium',
                // Radio input `sr-only` (vizual jihatdan yashirilgan) — fokus halqasi shu sabab
                // o'rab turgan yorliqda ko'rsatiladi (`has-[:focus-visible]`), aks holda
                // klaviatura bilan navigatsiya qilganda fokus ko'rinmay qolardi (`docs/11`, 4-bo'lim).
                'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-primary-600',
                genderValue === option
                  ? 'border-primary-600 bg-primary-50 text-primary-700'
                  : 'border-neutral-300 text-neutral-700',
              )}
            >
              <input type="radio" value={option} className="sr-only" {...register('gender')} />
              {t(`register.genderOptions.${option === 'Male' ? 'male' : 'female'}`)}
            </label>
          ))}
        </div>
        {errors.gender && (
          <p role="alert" className="text-sm text-danger-600">
            {errors.gender.message}
          </p>
        )}
      </fieldset>

      <div className="grid grid-cols-[2fr_1fr] gap-3">
        <Select
          label={t('register.fields.grade')}
          placeholder={t('register.fields.gradePlaceholder')}
          options={GRADE_OPTIONS}
          error={errors.grade?.message}
          {...register('grade')}
        />
        <Input
          label={t('register.fields.classLetter')}
          maxLength={2}
          error={errors.classLetter?.message}
          {...register('classLetter')}
        />
      </div>

      <Controller
        control={control}
        name="phone"
        render={({ field }) => (
          <PhoneField
            label={t('register.fields.phone')}
            autoComplete="tel-national"
            value={field.value}
            onValueChange={field.onChange}
            onBlur={field.onBlur}
            error={errors.phone?.message}
          />
        )}
      />

      <Controller
        control={control}
        name="parentPhone"
        render={({ field }) => (
          <PhoneField
            label={t('register.fields.parentPhone')}
            value={field.value}
            onValueChange={field.onChange}
            onBlur={field.onBlur}
            error={errors.parentPhone?.message}
            hint={errors.parentPhone ? undefined : t('register.fields.parentPhoneHint')}
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
            consentText={school.consentText}
            checked={field.value}
            onChange={field.onChange}
            onBlur={field.onBlur}
            error={errors.consentAccepted?.message}
          />
        )}
      />

      {requiresAccessCode && (
        <Input
          label={t('register.fields.accessCode')}
          inputMode="numeric"
          maxLength={6}
          error={errors.accessCode?.message}
          {...register('accessCode')}
        />
      )}

      {formError && (
        <p
          role="alert"
          className="rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700"
        >
          {formError}
        </p>
      )}

      <Button type="submit" size="lg" isLoading={isSubmitting} disabled={!consentAccepted}>
        {t('register.submitCta')}
      </Button>
    </form>
  );
}
