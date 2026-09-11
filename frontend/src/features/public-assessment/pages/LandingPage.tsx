import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, ConsentBlock, EmptyState, ErrorState, Skeleton } from '@/shared/ui';
import { Divider, GirihStar } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import type { PublicTestCatalogItem } from '@/shared/api/types';
import type { PublicProgramWithRegistration } from '@/shared/api/registrationModeTypes';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import { useSchoolInfo } from '../api/useSchoolInfo';
import { useSessionState } from '../api/useSessionState';
import { useStartSession } from '../api/useStartSession';
import { useSessionStore } from '../store/sessionStore';
import { beginFreshVisit } from '../lib/freshVisit';
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
 * - **Aynan bitta bo'lsa** — pastdagi tarmoq (branch) ESKI holatidek ishlaydi: `programs[0].tests`
 *   asosida test kartalari + (`Full` rejimida) shartsiz "Boshlash" tugmasi, tanlov ekrani YO'Q.
 *   Bu ataylab qilingan REGRESSIYA HIMOYASI — jonli sayt (`16shaxsiyat.uz`) da bitta dastur
 *   bor, u yerdagi oqim bitta baytga ham o'zgarmasligi kerak. **2026-09-11 tuzatildi:** ilgari
 *   bu yerda `school.tests` (dasturdan qat'i nazar BUTUN katalog) ishlatilardi — egasi
 *   dasturni arxivlagandan keyin ham uning testlari kirish ekranida ko'rinishda davom etgani
 *   shu yerdan kelib chiqqan (`docs/07` §1.1 izohi).
 * - **Bir nechtasi bo'lsa** — `ProgramSelectCard` ro'yxati (`programs[]`dan), tanlov
 *   `sessionStore.selectedProgramCode`ga yoziladi (persist — sahifa yangilanganda saqlanadi),
 *   "Boshlash" tanlanmaguncha o'chiq. `RegistrationPage` shu kodni `POST /sessions`ga
 *   `programCode` sifatida uzatadi.
 * - **Hech qanday dastur yo'q bo'lsa** — tushunarli xabar (CLAUDE.md MAXSUS DIQQAT 4-band),
 *   "Boshlash" umuman ko'rsatilmaydi.
 *
 * **Ro'yxatdan o'tishsiz dastur (`registrationMode: "None"`, P52, 2026-09-11, `docs/18` §9):**
 * Tanlangan (yoki yagona) dastur `None` bo'lsa `RegistrationPage` UMUMAN ochilmaydi — bu
 * ekranning o'zida rozilik belgisi (`ConsentBlock`, `school.consentText`) ko'rsatiladi va
 * "Boshlash" bosilganda `POST /sessions` shaxs maydonlarisiz to'g'ridan-to'g'ri shu yerdan
 * yuboriladi (`useStartSession`), keyin `RegistrationPage.onSubmit` bilan BIR XIL navigatsiya
 * (`pickNextTestCode` → test yoki yakun sahifasi). `consentAccepted` HAMON majburiy — huquqiy
 * rozilik rejimdan qat'i nazar. `Full` rejim BUTUNLAY tegilmagan: `registerHref`ga navigatsiya
 * ESKI holatidek ishlaydi (regressiya bilan qulflangan, `LandingPage.test.tsx`).
 *
 * **Havola/kod bilan yangi kirish = toza boshlanish (2026-09-07, egasining qarori):**
 * `?k=` bilan kelish (`/kirish` → maktab kodi → `/t/:slug?k=`, SMS/Telegram'dagi havola) —
 * bu YANGI odam: bir qurilmadan (maktab kompyuteri) ketma-ket bir necha o'quvchi kiradi.
 * Mount'da `beginFreshVisit` oldingi sessiyani (qaysi maktab/Telegram bo'lishidan qat'i
 * nazar), javob navbatini va sessiya keshini tozalaydi — anketa va test BO'SH ochiladi.
 * Ilgari store tozalanmasdi: shu maktabning oldingi o'quvchisi uchun "Davom ettirish"
 * chiqardi, javob navbati (`questionId` bo'yicha, sessiyaga bog'lanmagan) esa keyingi
 * o'quvchida "belgilangan savollar" bo'lib ko'rinardi.
 *
 * "Davom ettirish" IXTIYORIY taklif sifatida qoladi, lekin faqat oldingi sessiya xuddi shu
 * maktab va xuddi shu havola (`k`) ostida ochilgan bo'lsa (`sessionStore.resumable`) va server
 * uni hali tirik desa (`GET /sessions/me` aniq token bilan). `?k=` BO'LMAGAN kelish (test
 * ichidan "orqaga") hech narsani tozalamaydi — eski xatti-harakat: o'z sessiyasi bo'lsa
 * "Davom ettirish" ko'rsatiladi.
 */
export default function LandingPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { slug = '' } = useParams<{ slug: string }>();
  const [searchParams] = useSearchParams();
  const accessToken = searchParams.get('k') ?? '';
  const isLinkArrival = accessToken !== '';

  const schoolInfoQuery = useSchoolInfo(slug, accessToken);

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const resumable = useSessionStore((state) => state.resumable);
  const restoreResumable = useSessionStore((state) => state.restoreResumable);
  const clearSession = useSessionStore((state) => state.clear);
  const storedSelectedProgramSlug = useSessionStore((state) => state.selectedProgramSlug);
  const storedSelectedProgramCode = useSessionStore((state) => state.selectedProgramCode);
  const setSelectedProgram = useSessionStore((state) => state.setSelectedProgram);
  const setSession = useSessionStore((state) => state.setSession);

  // `registrationMode: "None"` dastur — rozilik shu ekranda, sessiya to'g'ridan-to'g'ri
  // shu yerdan ochiladi (yuqoridagi docstring). `Full` dasturda bu mutatsiya UMUMAN
  // chaqirilmaydi (`handleStart` pastda).
  const startSession = useStartSession();
  const [anonymousConsent, setAnonymousConsent] = useState(false);
  const [anonymousError, setAnonymousError] = useState<string | null>(null);

  // Boshqa maktab havolasi ochilganda eski rozilik belgisi/xatosi sizib qolmasin. Effekt
  // EMAS ("kaskadli render" ogohlantiradi, `react-hooks/set-state-in-effect`) — render
  // vaqtida moslashtirish (React'ning tavsiya qilingan "adjusting state" naqshi).
  const [consentSlug, setConsentSlug] = useState(slug);
  if (consentSlug !== slug) {
    setConsentSlug(slug);
    setAnonymousConsent(false);
    setAnonymousError(null);
  }

  // Toza boshlanish — faqat `k` bilan kelganda (yuqoridagi izoh). Effekt ichida, chunki bu
  // tashqi holatni (store, `localStorage`, query keshi) o'zgartiradi; `startFresh` idempotent,
  // StrictMode'da ikki marta ishlashi xavfsiz.
  useEffect(() => {
    if (!isLinkArrival) return;
    beginFreshVisit(queryClient, slug, accessToken);
  }, [isLinkArrival, queryClient, slug, accessToken]);

  // `k` bilan kelganda faol sessiya BO'SH (tozalangan) — davom ettirish faqat shu havolaga
  // mos `resumable` taklifi orqali va uning tokeni so'rovga aniq uzatiladi. `k`siz kelganda
  // — eski yo'l: store'dagi faol sessiya shu maktabniki bo'lsa.
  const resumeCandidate =
    isLinkArrival && resumable && resumable.slug === slug && resumable.accessToken === accessToken
      ? resumable
      : null;
  const hasStoredSession = !isLinkArrival && Boolean(sessionToken) && storedSlug === slug;
  const sessionStateQuery = useSessionState(
    hasStoredSession || resumeCandidate !== null,
    resumeCandidate ? { sessionToken: resumeCandidate.sessionToken } : {},
  );

  // Bu maktab uchun avval tanlangan dastur bo'lsa (sahifa yangilanishi) tiklanadi — boshqa
  // maktab havolasi ochilgan bo'lsa (`selectedProgramSlug !== slug`) e'tiborga olinmaydi.
  const [selectedProgramCode, setSelectedProgramCodeState] = useState<string | null>(() =>
    storedSelectedProgramSlug === slug ? storedSelectedProgramCode : null,
  );

  usePageTitle(schoolInfoQuery.data?.name ?? t('pages.landing.title'));

  // docs/10, 4.1-bo'lim: "410 kelsa store tozalanadi" — sessiya muddati tugagan bo'lsa.
  // `clear()` "Davom ettirish" taklifini (`resumable`) ham olib tashlaydi.
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

  // "Boshlash" bosilganda haqiqatda ishga tushadigan dastur — bitta bo'lsa avtomatik, bir
  // nechtasi bo'lsa o'quvchi tanlagani (hali tanlanmagan bo'lsa `undefined`, tugma o'chiq).
  const activeProgram: PublicProgramWithRegistration | undefined =
    programs.length === 1 ? programs[0] : programs.find((program) => program.code === selectedProgramCode);
  const isAnonymousStart = activeProgram?.registrationMode === 'None';

  async function startAnonymousSession(program: PublicProgramWithRegistration) {
    setAnonymousError(null);
    try {
      const result = await startSession.mutateAsync({
        slug,
        accessToken,
        consentAccepted: true,
        languageCode: 'uz',
        programCode: programs.length > 1 ? program.code : undefined,
      });
      setSession(result.sessionToken, slug, result.assessmentId, accessToken);
      const nextTestCode = pickNextTestCode(result.tests);
      navigate(nextTestCode ? ROUTES.public.test(slug, nextTestCode) : ROUTES.public.finish(slug));
    } catch (error) {
      if (error instanceof AppError && error.code === 'RATE_LIMITED') {
        setAnonymousError(t('pages.register.rateLimited'));
      } else if (error instanceof AppError && error.message) {
        setAnonymousError(error.message);
      } else {
        setAnonymousError(t('pages.register.genericSubmitError'));
      }
    }
  }

  function handleStart() {
    if (!activeProgram) return;
    if (activeProgram.registrationMode === 'None') {
      void startAnonymousSession(activeProgram);
      return;
    }
    navigate(registerHref);
  }

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
              // `programs.length === 1` yuqorida tekshirilgan — `noUncheckedIndexedAccess`
              // buni indeks turida ko'rmaydi, shu sabab aniq tasdiq (`!`).
              count: programs[0]!.questionCount,
              minutes: programs[0]!.estimatedMinutes,
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
            onClick={() => {
              // `k` bilan kelinganda oldingi sessiya faol o'rindan chetlatilgan — o'quvchi
              // ATAYLAB davom ettirishni tanladi, shunda u yana faol sessiyaga qaytariladi.
              // Javoblar serverdan (`currentValue`) keladi — mahalliy navbat allaqachon tozalangan.
              if (resumeCandidate) restoreResumable();
              navigate(continueHref);
            }}
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
        <>
          <SingleProgramView tests={programs[0]!.tests} />

          {/*
            `registrationMode: "None"` (P52, `docs/18` §9) — rozilik shu yerda, `Full`da
            (ESKI holat) ko'rsatilmaydi va tugma shartsiz yoqilgan.
          */}
          {isAnonymousStart && (
            <>
              <p className="text-sm text-ink-soft">{t('pages.landing.anonymousNotice')}</p>
              <ConsentBlock
                consentText={school.consentText}
                checked={anonymousConsent}
                onChange={setAnonymousConsent}
              />
            </>
          )}

          {anonymousError && (
            <p
              role="alert"
              className="rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
            >
              {anonymousError}
            </p>
          )}

          <Button
            size="lg"
            className={publicButtonClass('primary', 'lg', 'w-full')}
            isLoading={startSession.isPending}
            disabled={isAnonymousStart && !anonymousConsent}
            onClick={handleStart}
          >
            {t('pages.landing.startCta')}
          </Button>
        </>
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

          {isAnonymousStart && (
            <>
              <p className="text-sm text-ink-soft">{t('pages.landing.anonymousNotice')}</p>
              <ConsentBlock
                consentText={school.consentText}
                checked={anonymousConsent}
                onChange={setAnonymousConsent}
              />
            </>
          )}

          {anonymousError && (
            <p
              role="alert"
              className="rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
            >
              {anonymousError}
            </p>
          )}

          <Button
            size="lg"
            className={publicButtonClass('primary', 'lg', 'w-full')}
            isLoading={startSession.isPending}
            disabled={!activeProgram || (isAnonymousStart && !anonymousConsent)}
            onClick={handleStart}
          >
            {t('pages.landing.startCta')}
          </Button>
        </div>
      )}
    </div>
  );
}

/**
 * Bitta dastur bo'lgan holat — test kartalari ro'yxati. REGRESSIYA HIMOYASI: `Full` rejimida
 * markup bugungidek qoladi (`prompts/36` "eng muhim" bandi) — "Boshlash" tugmasi va rozilik
 * bloki endi YUQORIDA, `LandingPage`ning o'zida (ikkala tarmoq — bitta va bir nechta dastur —
 * bir xil joydan boshqariladi, `docs/18` §9).
 *
 * **2026-09-11 tuzatildi:** `tests` endi `school.tests` (butun katalog) EMAS, AYNAN shu
 * dasturning `programs[0].tests`i — arxivlangan boshqa dastur testi bu yerda ko'rinmasin
 * (`docs/07` §1.1 izohi, egasi topgan jonli xato).
 */
function SingleProgramView({ tests }: { tests: PublicTestCatalogItem[] }) {
  const sortedTests = [...tests].sort((a, b) => a.order - b.order);

  return (
    <div className="flex flex-col gap-3">
      {sortedTests.map((test) => (
        <TestIntroCard key={test.code} {...test} />
      ))}
    </div>
  );
}
