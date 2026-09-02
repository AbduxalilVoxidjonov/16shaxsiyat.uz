import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, Card, EmptyState, ErrorState, Skeleton } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useSchoolInfo } from '../api/useSchoolInfo';
import { useSessionState } from '../api/useSessionState';
import { useSessionStore } from '../store/sessionStore';
import { TestIntroCard } from '../components/TestIntroCard';
import { ProgramSelectCard } from '../components/ProgramSelectCard';

/** Yuklanish holati skeleti (docs/10, 7-bo'lim). */
function LandingSkeleton() {
  return (
    <div className="flex flex-col gap-6" aria-hidden="true">
      <div className="flex flex-col items-center gap-2">
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-7 w-56" />
        <Skeleton className="h-4 w-64" />
      </div>
      <div className="flex flex-col gap-3">
        {[0, 1, 2, 3].map((key) => (
          <Skeleton key={key} className="h-20 w-full rounded-xl" />
        ))}
      </div>
      <Skeleton className="h-12 w-full rounded-lg" />
    </div>
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
    return <ErrorState onRetry={() => void schoolInfoQuery.refetch()} />;
  }

  const school = schoolInfoQuery.data;
  const programs = school.programs;

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col items-center gap-2 text-center">
        <p className="text-sm font-medium text-primary-600">{school.name}</p>
        <h1 className="text-2xl font-bold text-neutral-900">{t('pages.landing.heading')}</h1>
        {programs.length === 1 && (
          <p className="text-sm text-neutral-600">
            {t('pages.landing.summary', {
              count: school.tests.reduce((sum, test) => sum + test.questionCount, 0),
              minutes: school.totalEstimatedMinutes,
            })}
          </p>
        )}
        <p className="text-sm text-neutral-600">{t('pages.landing.noRightWrong')}</p>
        <p className="text-sm text-neutral-600">{t('pages.landing.notAGrade')}</p>
      </section>

      {continueHref && (
        <Card className="border-primary-200 bg-primary-50">
          <p className="mb-3 text-sm text-primary-800">{t('pages.landing.resumeNotice')}</p>
          <Button className="w-full" onClick={() => navigate(continueHref)}>
            {t('pages.landing.resumeCta')}
          </Button>
        </Card>
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
          <p className="text-sm text-neutral-600">
            {t('publicAssessment.programSelect.summary', { count: programs.length })}
          </p>
          <div role="radiogroup" aria-label={t('publicAssessment.programSelect.heading')} className="flex flex-col gap-3">
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
            className="w-full"
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

      <Button size="lg" className="w-full" onClick={onStart}>
        {t('pages.landing.startCta')}
      </Button>
    </>
  );
}
