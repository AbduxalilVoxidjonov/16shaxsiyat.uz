import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, EmptyState, ErrorState, Skeleton } from '@/shared/ui';
import { Divider, GirihStar } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useSchoolInfo } from '../api/useSchoolInfo';
import { useSessionState } from '../api/useSessionState';
import { useSessionStore } from '../store/sessionStore';
import { TestIntroCard } from '../components/TestIntroCard';
import { ProgramSelectCard } from '../components/ProgramSelectCard';
import { publicButtonClass } from '../components/publicStyles';

/** Yuklanish holati skeleti (docs/10, 7-bo'lim). */
function LandingSkeleton() {
  return (
    <div className="flex flex-col gap-6" aria-hidden="true">
      <div className="flex flex-col items-center gap-3">
        <Skeleton className="size-16 rounded-[28%]" />
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-4 w-64" />
      </div>
      <div className="flex flex-col gap-3">
        {[0, 1, 2, 3].map((key) => (
          <Skeleton key={key} className="h-20 w-full rounded-3xl" />
        ))}
      </div>
      <Skeleton className="h-14 w-full rounded-full" />
    </div>
  );
}

/**
 * Oqim bosqichlari (anketa → savollar → yakun) — o'quvchi oldinda nima turganini bir qarashda
 * ko'radi. Matnlar MAVJUD sahifa sarlavhalari kalitlaridan olinadi (yangi i18n kaliti
 * qo'shilmagan — `prompts/45` qoidasi).
 */
function FlowSteps() {
  const { t } = useTranslation();
  const steps = [t('pages.register.title'), t('pages.test.title'), t('pages.finish.title')];

  return (
    <ol className="grid grid-cols-3 gap-2">
      {steps.map((step, index) => (
        <li key={step} className="flex flex-col items-center gap-2 text-center">
          <span
            aria-hidden="true"
            className="grid size-8 place-items-center rounded-full border border-line-strong bg-paper-card font-display text-[13px] font-bold text-firuza-700"
          >
            {index + 1}
          </span>
          <span className="text-xs font-semibold text-ink-soft">{step}</span>
        </li>
      ))}
    </ol>
  );
}

/**
 * E-1 Landing (`/t/:slug`) — docs/11 E-1, docs/07 1.1-bo'lim, prompts/20, prompts/36.
 *
 * **Dastur tanlovi (`docs/06` 8-bo'lim, 2026-09-02 qarorlari, `prompts/36`):**
 * `GetSchoolInfoResult.programs[]` — maktab uchun mavjud dastur(lar).
 * - **Aynan bitta bo'lsa** — pastdagi tarmoq (branch) ESKI holatidek ishlaydi: `school.tests`
 *   (ORQAGA MOSLIK maydoni) asosida test kartalari + shartsiz "Boshlash" tugmasi, tanlov
 *   ekrani YO'Q. Bu ataylab qilingan REGRESSIYA HIMOYASI — jonli sayt (`16shaxsiyat.uz`) da
 *   bitta dastur bor, u yerdagi oqim bitta baytga ham o'zgarmasligi kerak.
 * - **Bir nechtasi bo'lsa** — `ProgramSelectCard` ro'yxati (`programs[]`dan), tanlov
 *   `sessionStore.selectedProgramCode`ga yoziladi (persist — sahifa yangilanganda saqlanadi),
 *   "Boshlash" tanlanmaguncha o'chiq. `RegistrationPage` shu kodni `POST /sessions`ga
 *   `programCode` sifatida uzatadi.
 * - **Hech qanday dastur yo'q bo'lsa** — tushunarli xabar (CLAUDE.md MAXSUS DIQQAT 4-band),
 *   "Boshlash" umuman ko'rsatilmaydi.
 */
export default function LandingPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { slug = '' } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const accessToken = searchParams.get('k') ?? '';

  const schoolInfoQuery = useSchoolInfo(slug, accessToken);

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const clearSession = useSessionStore((state) => state.clear);
  const storedSelectedProgramSlug = useSessionStore((state) => state.selectedProgramSlug);
  const storedSelectedProgramCode = useSessionStore((state) => state.selectedProgramCode);
  const setSelectedProgram = useSessionStore((state) => state.setSelectedProgram);
  const hasResumableSession = Boolean(sessionToken) && storedSlug === slug;
  const sessionStateQuery = useSessionState(hasResumableSession);

  // Bu maktab uchun avval tanlangan dastur bo'lsa (sahifa yangilanishi) tiklanadi — boshqa
  // maktab havolasi ochilgan bo'lsa (`selectedProgramSlug !== slug`) e'tiborga olinmaydi.
  const [selectedProgramCode, setSelectedProgramCodeState] = useState<string | null>(() =>
    storedSelectedProgramSlug === slug ? storedSelectedProgramCode : null,
  );

  usePageTitle(schoolInfoQuery.data?.name ?? t('pages.landing.title'));

  // docs/10, 4.1-bo'lim: "410 kelsa store tozalanadi" — sessiya muddati tugagan bo'lsa.
  useEffect(() => {
    if (sessionStateQuery.error instanceof AppError && sessionStateQuery.error.status === 410) {
      clearSession();
    }
  }, [sessionStateQuery.error, clearSession]);

  function selectProgram(code: string) {
    setSelectedProgramCodeState(code);
    setSelectedProgram(slug, code);
  }

  const registerHref = `${ROUTES.public.register(slug)}?k=${encodeURIComponent(accessToken)}`;

  const continueHref = useMemo(() => {
    const state = sessionStateQuery.data;
    if (!state) {
      return null;
    }
    return state.currentTestCode
      ? ROUTES.public.test(slug, state.currentTestCode)
      : ROUTES.public.finish(slug);
  }, [sessionStateQuery.data, slug]);

  if (schoolInfoQuery.isPending) {
    return <LandingSkeleton />;
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
    // `409 NO_PROGRAM_AVAILABLE` (2026-09-03): maktab va havola TO'G'RI, faqat hozircha
    // mavjud dastur yo'q. Umumiy xato ekrani ("Nimadir noto'g'ri ketdi" + "Qayta urinish")
    // bu yerda chalg'ituvchi: o'quvchi havolani buzuq deb o'ylab maktabga behuda murojaat
    // qiladi va qayta urinish hech narsani o'zgartirmaydi.
    if (error instanceof AppError && error.status === 409) {
      return (
        <EmptyState
          title={t('publicAssessment.noPrograms.title')}
          description={t('publicAssessment.noPrograms.description')}
        />
      );
    }
    return <ErrorState onRetry={() => void schoolInfoQuery.refetch()} />;
  }

  const school = schoolInfoQuery.data;
  const programs = school.programs;

  return (
    <div className="flex animate-fade-up flex-col gap-8 motion-reduce:animate-none">
      <section className="flex flex-col items-center gap-4 text-center">
        {/* Brend emblemasi — ikki qatlamli girih naqshi (sof dekor, `aria-hidden`). */}
        <span className="relative grid size-16 place-items-center rounded-[28%] bg-linear-to-br from-firuza-400 to-firuza-700 shadow-glow">
          <GirihStar className="absolute inset-[16%] text-white/40" strokeWidth={2} />
          <GirihStar
            className="absolute inset-[34%] text-white/25"
            strokeWidth={1.5}
            withCircle={false}
          />
        </span>
        <p className="eyebrow text-firuza-700">{school.name}</p>
        <h1 className="font-display text-3xl font-extrabold tracking-tight text-balance text-ink sm:text-4xl">
          {t('pages.landing.heading')}
        </h1>
        {programs.length === 1 && (
          <p className="lead text-balance">
            {t('pages.landing.summary', {
              count: school.tests.reduce((sum, test) => sum + test.questionCount, 0),
              minutes: school.totalEstimatedMinutes,
            })}
          </p>
        )}
        <div className="flex flex-col gap-1 text-sm leading-relaxed text-ink-soft">
          <p>{t('pages.landing.noRightWrong')}</p>
          <p>{t('pages.landing.notAGrade')}</p>
        </div>
      </section>

      <Divider />

      <FlowSteps />

      {continueHref && (
        <div className="card rounded-3xl border-firuza-200 bg-firuza-50 p-5">
          <p className="mb-4 text-sm font-medium text-firuza-900">
            {t('pages.landing.resumeNotice')}
          </p>
          <Button
            className={publicButtonClass('primary', 'md', 'w-full')}
            onClick={() => navigate(continueHref)}
          >
            {t('pages.landing.resumeCta')}
          </Button>
        </div>
      )}

      {programs.length === 0 ? (
        <EmptyState
          title={t('publicAssessment.noPrograms.title')}
          description={t('publicAssessment.noPrograms.description')}
        />
      ) : programs.length === 1 ? (
        <SingleProgramView school={school} onStart={() => navigate(registerHref)} />
      ) : (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-ink-soft">
            {t('publicAssessment.programSelect.summary', { count: programs.length })}
          </p>
          <div
            role="radiogroup"
            aria-label={t('publicAssessment.programSelect.heading')}
            className="flex flex-col gap-3"
          >
            {programs.map((program) => (
              <ProgramSelectCard
                key={program.code}
                {...program}
                selected={selectedProgramCode === program.code}
                onSelect={() => selectProgram(program.code)}
              />
            ))}
          </div>
          <Button
            size="lg"
            className={publicButtonClass('primary', 'lg', 'w-full')}
            disabled={!selectedProgramCode}
            onClick={() => navigate(registerHref)}
          >
            {t('pages.landing.startCta')}
          </Button>
        </div>
      )}
    </div>
  );
}

/**
 * Bitta dastur bo'lgan holat — REGRESSIYA HIMOYASI: bugungi (`school.tests`ga tayanuvchi)
 * markup/mantiq bitta baytga ham o'zgarmagan (`prompts/36` "eng muhim" bandi).
 */
function SingleProgramView({
  school,
  onStart,
}: {
  school: NonNullable<ReturnType<typeof useSchoolInfo>['data']>;
  onStart: () => void;
}) {
  const { t } = useTranslation();
  const sortedTests = [...school.tests].sort((a, b) => a.order - b.order);

  return (
    <>
      <div className="flex flex-col gap-3">
        {sortedTests.map((test) => (
          <TestIntroCard key={test.code} {...test} />
        ))}
      </div>

      <Button size="lg" className={publicButtonClass('primary', 'lg', 'w-full')} onClick={onStart}>
        {t('pages.landing.startCta')}
      </Button>
    </>
  );
}
