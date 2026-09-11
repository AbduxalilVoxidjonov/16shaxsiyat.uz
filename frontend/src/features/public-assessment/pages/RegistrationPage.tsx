import { useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Navigate, useNavigate, useParams, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import {
  BirthDateSelect,
  Button,
  ConsentBlock,
  ErrorState,
  Input,
  PhoneField,
  Select,
  Skeleton,
} from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { cn } from '@/shared/lib/cn';
import { toE164UzPhone } from '@/shared/lib/formatPhone';
import type { Gender } from '@/shared/api/types';
import type { StartSessionRegistrationRequestBody } from '@/shared/api/registrationModeTypes';
import { resolveRegistrationFields } from '@/shared/api/registrationModeTypes';
import { useSchoolInfo } from '../api/useSchoolInfo';
import { useStartSession } from '../api/useStartSession';
import { useSessionStore } from '../store/sessionStore';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import { publicButtonClass } from '../components/publicStyles';
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
      <Skeleton className="mx-auto h-8 w-48" />
      {[0, 1, 2, 3, 4, 5].map((key) => (
        <Skeleton key={key} className="h-11 w-full rounded-xl" />
      ))}
      <Skeleton className="h-14 w-full rounded-full" />
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
  const storedSelectedProgramSlug = useSessionStore((state) => state.selectedProgramSlug);
  const storedSelectedProgramCode = useSessionStore((state) => state.selectedProgramCode);

  const [formError, setFormError] = useState<string | null>(null);

  usePageTitle(t('pages.register.title'));

  const requiresAccessCode = schoolInfoQuery.data?.requiresAccessCode ?? false;

  // `prompts/36` — Landing (E-1) bir nechta dastur bo'lganda tanlovni talab qiladi. Bitta
  // dastur bo'lsa (ORQAGA MOSLIK — hozirgi oqim) `programCode` umuman yuborilmaydi (backend
  // yagona mavjud dasturni o'zi tanlaydi, `prompts/34` C9-band).
  const programs = schoolInfoQuery.data?.programs ?? [];
  const requiresProgramSelection = programs.length > 1;
  const selectedProgramCode = storedSelectedProgramSlug === slug ? storedSelectedProgramCode : null;

  // `activeProgram` — shu render uchun "amaldagi" dastur (bitta bo'lsa yagonasi, ko'p bo'lsa
  // tanlangani). Pastdagi shart bilan bir xil hisoblash — pastda `registrationMode === 'None'`
  // tekshiruvi uchun ham qayta ishlatiladi. Hook EMAS, shu sabab shartli qaytishlardan oldin
  // hisoblash xavfsiz (faqat hook chaqiruvlari tartibi muhim).
  const activeProgram =
    programs.length === 1 ? programs[0] : programs.find((program) => program.code === selectedProgramCode);

  // P52 (`docs/18` §9) — dasturning har bir shaxs maydoni uchun "Yashirin"/"Ixtiyoriy"/
  // "Majburiy" sozlamasi. Javobda kelmasa (eski fixture/hali yangilanmagan backend) standart
  // qiymatlar bilan to'ldiriladi — 20 ta mavjud test shu sabab o'zgarmasdan yashil qoladi.
  const registrationFields = resolveRegistrationFields(activeProgram?.registrationFields);
  const schema = useMemo(
    () => buildRegistrationSchema(requiresAccessCode, registrationFields),
    [requiresAccessCode, registrationFields],
  );

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
    if (error.code === 'PROGRAM_REQUIRED') {
      // Maktabda bir nechta dastur bor, lekin `programCode` yubormadik/mos kelmadi (masalan
      // sessionStore boshqa oynada tozalangan) — o'quvchi tanlov ekraniga qaytariladi
      // (CLAUDE.md MAXSUS DIQQAT 4-band).
      toast.show({ variant: 'info', title: t('publicAssessment.programRequired.message') });
      navigate(`${ROUTES.public.landing(slug)}?k=${encodeURIComponent(accessToken)}`, {
        replace: true,
      });
      return;
    }

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

    // Har bir shaxs maydoni `registrationFields`ga qarab yuboriladi/tashlab ketiladi (`Hidden`
    // — umuman yuborilmaydi, `docs/18` §9). Bo'sh qoldirilgan `Optional` maydon ham
    // yuborilmaydi (backend `undefined`ni "berilmagan" deb o'qiydi) — faqat haqiqiy qiymat
    // borida jo'natiladi.
    const hasBirthDate = Boolean(
      values.birthDate.day && values.birthDate.month && values.birthDate.year,
    );
    const payload: StartSessionRegistrationRequestBody = {
      slug,
      accessToken,
      accessCode: requiresAccessCode ? values.accessCode : undefined,
      fullName: values.fullName,
      birthDate:
        registrationFields.birthDate !== 'Hidden' && hasBirthDate
          ? birthDateToIso(values.birthDate)
          : undefined,
      gender:
        registrationFields.gender !== 'Hidden' && values.gender
          ? (values.gender as Gender)
          : undefined,
      grade:
        registrationFields.grade !== 'Hidden' && values.grade ? Number(values.grade) : undefined,
      classLetter:
        registrationFields.classLetter !== 'Hidden' && values.classLetter
          ? values.classLetter.toUpperCase()
          : undefined,
      phone:
        registrationFields.phone !== 'Hidden' && values.phone
          ? (toE164UzPhone(values.phone) ?? undefined)
          : undefined,
      parentPhone:
        registrationFields.parentPhone !== 'Hidden' && values.parentPhone
          ? (toE164UzPhone(values.parentPhone) ?? undefined)
          : undefined,
      email: registrationFields.email !== 'Hidden' && values.email ? values.email : undefined,
      consentAccepted: values.consentAccepted,
      languageCode: 'uz',
      programCode: requiresProgramSelection ? (selectedProgramCode ?? undefined) : undefined,
    };

    try {
      const result = await startSession.mutateAsync(payload);
      // `accessToken` (`?k=`) ham saqlanadi — havola bilan qayta kelganda `LandingPage` shu
      // sessiya "xuddi shu havolaniki"mi deb ajrata olishi uchun (`sessionStore.startFresh`).
      // Yangi token bo'lsa `setSession` eski javob navbatini o'zi tozalaydi.
      setSession(result.sessionToken, slug, result.assessmentId, accessToken);
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

  // Bir nechta dastur bor, lekin tanlov yo'q (masalan to'g'ridan-to'g'ri havola ochilgan yoki
  // boshqa oynada `sessionStore` tozalangan) — tanlov ekraniga qaytariladi, forma
  // ko'rsatilmaydi (server `400 PROGRAM_REQUIRED` bilan javob berishini kutish shart emas).
  if (requiresProgramSelection && !selectedProgramCode) {
    return (
      <Navigate
        to={`${ROUTES.public.landing(slug)}?k=${encodeURIComponent(accessToken)}`}
        replace
      />
    );
  }

  // `registrationMode: "None"` dastur — bu ekran UMUMAN ochilmaydi (`docs/18` §9,
  // `LandingPage` shu dastur uchun sessiyani o'zi to'g'ridan-to'g'ri ochadi). Oddiy oqimda
  // bu yerga hech qachon kelinmaydi (`LandingPage` navigatsiya qilmaydi), lekin to'g'ridan-
  // to'g'ri havola/"orqaga" bilan kelish ehtimoliga qarshi himoya (`activeProgram` yuqorida,
  // sxema qurish uchun hisoblangan).
  if (activeProgram?.registrationMode === 'None') {
    return (
      <Navigate
        to={`${ROUTES.public.landing(slug)}?k=${encodeURIComponent(accessToken)}`}
        replace
      />
    );
  }

  return (
    <div className="flex animate-fade-up flex-col gap-6 motion-reduce:animate-none">
      <header className="flex flex-col items-center gap-2 text-center">
        <p className="eyebrow text-firuza-700">{school.name}</p>
        <h1 className="font-display text-2xl font-extrabold tracking-tight text-balance text-ink">
          {t('pages.register.title')}
        </h1>
        {/*
          Havola bilan kelganda anketa har doim BO'SH ochiladi (`LandingPage` toza boshlanish),
          davom ettirish esa SERVERDA: xuddi shu F.I.Sh. + tug'ilgan sana → `POST /sessions`
          `resumed: true` va yarim qolgan sessiya javoblari bilan qaytadi (`docs/07` 1.2).
        */}
        <p className="text-sm text-ink-soft text-balance">{t('pages.register.resumeHint')}</p>
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

        {registrationFields.birthDate !== 'Hidden' && (
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
        )}

        {/*
          Ro'yxatdan o'tishni sozlagan superadmin tug'ilgan sanani `Optional`/`Hidden` qilgan
          bo'lsa ham ko'rinadi (maydon ko'rsatilmasa ham) — takror topshirishni aniqlash
          (F.I.Sh. + tug'ilgan sana) buzilishi mumkinligi haqida ogohlantiradi (P52, egasining
          talabi, "ma'lumot buzilishining oldini olish" — taqiq emas, faqat eslatma).
        */}
        {registrationFields.birthDate !== 'Required' && (
          <p role="note" className="rounded-2xl bg-zarhal-50 p-3 text-sm text-zarhal-800">
            {t('pages.register.birthDateOptionalWarning')}
          </p>
        )}

        {registrationFields.gender !== 'Hidden' && (
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
                    // Radio input `sr-only` (vizual jihatdan yashirilgan) — fokus halqasi shu sabab
                    // o'rab turgan yorliqda ko'rsatiladi (`has-[:focus-visible]`), aks holda
                    // klaviatura bilan navigatsiya qilganda fokus ko'rinmay qolardi (`docs/11`, 4-bo'lim).
                    'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-500',
                    genderValue === option
                      ? 'border-firuza-500 bg-firuza-50 text-firuza-800'
                      : 'border-line bg-paper-card text-ink-soft hover:border-firuza-300',
                  )}
                >
                  <input type="radio" value={option} className="sr-only" {...register('gender')} />
                  {t(`register.genderOptions.${option === 'Male' ? 'male' : 'female'}`)}
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

        {/*
          `minmax(0,…)` MAJBURIY: sof `1fr` ning eng kichik o'lchami `auto`, ya'ni
          "Sinf harfi" inputining brauzer standarti bo'yicha juda keng min-content
          o'lchami. 390px da shu tufayli nisbat teskarisiga aylanib ketardi — "Sinf"
          ustuni siqilib, "Tanlang" matni "Tanl…" bo'lib kesilardi. Ikkala maydon ham
          ko'ringanda shu 2 ustunli grid ishlatiladi (standart holat, regressiya qulfi);
          faqat bittasi ko'rinsa — to'liq kenglikda, bittasi ham ko'rinmasa — umuman yo'q.
        */}
        {registrationFields.grade !== 'Hidden' && registrationFields.classLetter !== 'Hidden' && (
          <div className="grid grid-cols-[minmax(0,2fr)_minmax(0,1fr)] gap-3">
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
        )}
        {registrationFields.grade !== 'Hidden' && registrationFields.classLetter === 'Hidden' && (
          <Select
            label={t('register.fields.grade')}
            placeholder={t('register.fields.gradePlaceholder')}
            options={GRADE_OPTIONS}
            error={errors.grade?.message}
            {...register('grade')}
          />
        )}
        {registrationFields.grade === 'Hidden' && registrationFields.classLetter !== 'Hidden' && (
          <Input
            label={t('register.fields.classLetter')}
            maxLength={2}
            error={errors.classLetter?.message}
            {...register('classLetter')}
          />
        )}

        {registrationFields.phone !== 'Hidden' && (
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
        )}

        {registrationFields.parentPhone !== 'Hidden' && (
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
        )}

        {registrationFields.email !== 'Hidden' && (
          <Input
            label={t('register.fields.email')}
            type="email"
            autoComplete="email"
            hint={errors.email ? undefined : t('register.fields.emailHint')}
            error={errors.email?.message}
            {...register('email')}
          />
        )}

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
            className="rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
          >
            {formError}
          </p>
        )}

        <Button
          type="submit"
          size="lg"
          className={publicButtonClass('primary', 'lg', 'w-full')}
          isLoading={isSubmitting}
          disabled={!consentAccepted}
        >
          {t('register.submitCta')}
        </Button>
      </form>
    </div>
  );
}
