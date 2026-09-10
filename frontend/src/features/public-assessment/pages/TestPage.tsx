import { useEffect, useMemo, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useOnline } from '@/shared/hooks/useOnline';
import { Button, ErrorState, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';
import { useSessionState } from '../api/useSessionState';
import { useStartTest } from '../api/useStartTest';
import { useTestQuestions } from '../api/useTestQuestions';
import { useCompleteTest } from '../api/useCompleteTest';
import { useAutosave } from '../hooks/useAutosave';
import { useQuestionVisibility } from '../hooks/useQuestionVisibility';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';
import { useSessionStore } from '../store/sessionStore';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import { answersForTest, readAnswerStore, type AnswerPayload } from '../lib/answerQueue';
import {
  effectiveAnswer,
  isQuestionAnswered,
  orderedVisibleSections,
  questionsInSection,
  resolveVisibleQuestionIds,
} from '../lib/branchingFlow';
import { QuestionRenderer } from '../components/QuestionRenderer';
import { SectionIntro } from '../components/SectionIntro';
import { TestProgressHeader } from '../components/TestProgressHeader';
import { SaveStatusIndicator } from '../components/SaveStatusIndicator';
import { OfflineBanner } from '../components/OfflineBanner';
import { publicButtonClass } from '../components/publicStyles';

function prefersReducedMotion(): boolean {
  return (
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  );
}

/** Yuklanish holati skeleti (docs/10, 7-bo'lim). */
function TestPageSkeleton() {
  return (
    <div className="flex flex-col gap-4" aria-hidden="true">
      <Skeleton className="h-16 w-full rounded-2xl" />
      {[0, 1, 2, 3].map((key) => (
        <Skeleton key={key} className="h-44 w-full rounded-4xl" />
      ))}
    </div>
  );
}

/**
 * E-3 Test sahifasi (`/t/:slug/test/:testCode`) — docs/11 E-3, docs/07 1.4–1.6-bo'lim,
 * docs/10 §4.3, `docs/18` §6.2 (tarmoqlanuvchi so'rovnoma — bo'lim-qadam rejimi),
 * CLAUDE.md MAXSUS DIQQAT 1–5-band.
 *
 * Ikki rejim, BIR XIL savol render/o'zaro ta'sir mantig'i bilan (`QuestionRenderer`,
 * `handleAnswer`/`goToNextQuestion`/validatsiya — `displayedQuestions` ustida ishlaydi):
 *
 * - **Bo'limsiz** (`sections` javobda yo'q/bo'sh) — MAVJUD sahifalash oqimi (`page`/`pageSize`,
 *   `docs/18` §1 "hech narsa buzilmaydi" — bu 4 ta tizim metodikasining yagona yo'li,
 *   regressiya testi bilan qulflangan).
 * - **Bo'limli** (`sections` bor) — bitta so'rovda KELGAN barcha faol savol orasidan
 *   `resolveVisibleQuestionIds` (`shared/lib/visibility.ts`ning TS egizagi) bilan HOZIR
 *   ko'rinadigan bo'lim/savollarni hisoblab, bir ekranda BITTA bo'limni ko'rsatadi.
 *
 * Test nomi bu sahifaning o'z endpoint'laridan kelmaydi to'g'ridan-to'g'ri sahifa parametridan
 * emas — `GET /sessions/me` (`PublicTestSummaryDto`, docs/07 1.3-bo'lim) qaytaradigan
 * `tests[].name` ishlatiladi (P36: backend endi shu maydonni ham qaytaradi). Bu — sessiyaning
 * HAQIQIY dasturi bilan bog'liq ro'yxat (bir nechta dastur bo'lganda maktabning BARCHA
 * testlari emas), shu sabab alohida "katalog" saqlash kerak emas. `sessionStateQuery.data`
 * hali yuklanmagan bo'lsa `testCode`ning o'zi ko'rsatiladi (fallback).
 */
export default function TestPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const isOnline = useOnline();
  const { slug = '', testCode = '' } = useParams<{ slug: string; testCode: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;

  const handleSessionExpired = useSessionExpiredGuard(slug);

  const sessionStateQuery = useSessionState(hasSession);
  const startTest = useStartTest();
  const completeTest = useCompleteTest();
  const testInfo = sessionStateQuery.data?.tests.find((test) => test.code === testCode);
  const testName = testInfo?.name ?? testCode;

  const [page, setPage] = useState(1);
  const [invalidIds, setInvalidIds] = useState<ReadonlySet<string>>(new Set());
  /** Oxirgi sahifada "Keyingi" bosilgach navbatdagi javoblar yuborilmoqda (`complete`dan OLDIN). */
  const [isSavingBeforeComplete, setIsSavingBeforeComplete] = useState(false);
  const startedTestCodeRef = useRef<string | null>(null);
  const initializedPageRef = useRef(false);
  const fieldsetRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const pageLoadTimeRef = useRef(0);
  const sectionHeadingRef = useRef<HTMLHeadingElement | null>(null);
  const isFirstSectionFocusRef = useRef(true);
  /**
   * TestPage'ning O'ZI ushlab turadigan mahalliy javoblar ko'zgusi — `autosave.localAnswers`
   * bilan bir xil mazmunda (har `handleAnswer` HAR IKKALASINI sinxron yangilaydi), lekin
   * AYRICHA `useState` sifatida saqlanadi: `visibility` (pastda) shundan hisoblanadi va
   * NATIJASI (`visibleQuestionIds`) xuddi shu renderda `useAutosave`ga uzatiladi. Agar o'rniga
   * `autosave.localAnswers`ning o'zi ishlatilganda edi, teskari bog'liqlik chiqardi — o'sha
   * qiymat FAQAT `useAutosave` chaqirilgandan KEYIN mavjud bo'ladi, `useAutosave`ning o'ziga
   * esa `visibleQuestionIds` CHAQIRISH vaqtida kerak (`docs/18` §6.2).
   */
  const [draftAnswers, setDraftAnswers] = useState<Record<string, AnswerPayload>>(() =>
    answersForTest(readAnswerStore(), testCode),
  );

  const { registerNode, getDurationSince, reset: resetVisibility } = useQuestionVisibility();

  usePageTitle(testName);

  // Yangi test blokiga o'tilganda (route element qayta ishlatiladi, faqat `:testCode` o'zgaradi)
  // ref'lar tozalanadi — bu faqat effektda xavfsiz (render vaqtida ref yozish taqiqlangan).
  useEffect(() => {
    startedTestCodeRef.current = null;
    initializedPageRef.current = false;
    isFirstSectionFocusRef.current = true;
  }, [testCode]);

  // `invalidIds`/`draftAnswers`ni yangi test blokiga o'tilganda tozalash — React
  // hujjatlaridagi "render vaqtida holatni moslashtirish" naqshi (effekt ichida sinxron
  // `setState` o'rniga — `react-hooks/set-state-in-effect`):
  // https://react.dev/learn/you-might-not-need-an-effect
  const [invalidResetKey, setInvalidResetKey] = useState(testCode);
  if (invalidResetKey !== testCode) {
    setInvalidResetKey(testCode);
    setInvalidIds(new Set());
    setDraftAnswers(answersForTest(readAnswerStore(), testCode));
  }

  // Bo'lim-qadam holati ham xuddi shunday — yangi test blokiga o'tilganda tozalanadi.
  const [sectionResetKey, setSectionResetKey] = useState(testCode);
  const [currentSectionId, setCurrentSectionId] = useState<string | null>(null);
  const [sectionInitialized, setSectionInitialized] = useState(false);
  if (sectionResetKey !== testCode) {
    setSectionResetKey(testCode);
    setCurrentSectionId(null);
    setSectionInitialized(false);
  }

  // Testni boshlash — aralashtirish tartibini qat'iylashtiradi (docs/07 1.4-bo'lim).
  useEffect(() => {
    if (!hasSession || startedTestCodeRef.current === testCode) return;
    startedTestCodeRef.current = testCode;
    startTest.mutate(testCode, {
      onError: (error) => {
        if (error instanceof AppError && error.status === 410) {
          handleSessionExpired();
        }
      },
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `startTest`/`handleSessionExpired` barqaror emas, `testCode` bo'yicha ref bilan qo'lda himoyalangan
  }, [hasSession, testCode]);

  const started = startTest.data?.testCode === testCode ? startTest.data : undefined;

  // Sahifa yangilanganda o'sha joydan davom etish — `answered` soni bo'yicha resume sahifasini hisoblaydi.
  // Bo'limli anketada `totalPages` doim 1 (docs/18 §4.1) — bu effekt zararsiz ravishda `page=1`ni tasdiqlaydi.
  useEffect(() => {
    if (initializedPageRef.current || !started || !testInfo) return;
    const resumePage = Math.min(
      Math.max(Math.floor(testInfo.answered / started.pageSize) + 1, 1),
      Math.max(started.totalPages, 1),
    );
    setPage(resumePage);
    initializedPageRef.current = true;
  }, [started, testInfo]);

  const questionsQuery = useTestQuestions(testCode, page, Boolean(started));

  const rawQuestions: BranchingQuestion[] = useMemo(
    () => questionsQuery.data?.questions ?? [],
    [questionsQuery.data?.questions],
  );
  const sections = useMemo(() => {
    const list = questionsQuery.data?.sections;
    return list && list.length > 0 ? list : null;
  }, [questionsQuery.data?.sections]);

  useEffect(() => {
    resetVisibility();
    pageLoadTimeRef.current = performance.now();
    // `currentSectionId` — bo'lim almashganda ham "ko'rinish vaqti" (durationMs o'lchovi)
    // qayta boshlanishi kerak, bo'limsiz oqimda bu doim `null` bo'lib qoladi (zararsiz).
  }, [questionsQuery.data, currentSectionId, resetVisibility]);

  // `docs/18` §2.6/§6.2 — HOZIR ko'rinadigan bo'lim/savollar, HAR javob o'zgarishida (`draftAnswers`
  // orqali) DARHOL qayta hisoblanadi — server javobini kutmaydi. `draftAnswers` ishlatiladi
  // (`autosave.localAnswers` EMAS): natija (`visibleQuestionIds`) pastda AYNAN shu renderda
  // `useAutosave`ga uzatiladi, `autosave.localAnswers` esa faqat o'sha chaqiruvdan KEYIN
  // mavjud bo'lardi (yuqoridagi `draftAnswers` izohiga qarang).
  const visibility = useMemo(
    () => (sections ? resolveVisibleQuestionIds(sections, rawQuestions, draftAnswers) : null),
    [sections, rawQuestions, draftAnswers],
  );

  const autosave = useAutosave({
    testCode,
    onSessionExpired: handleSessionExpired,
    visibleQuestionIds: visibility?.visibleQuestionIds,
  });

  const visibleSections = useMemo(
    () => (visibility && sections ? orderedVisibleSections(sections, visibility.visibleSectionIds) : []),
    [visibility, sections],
  );

  // Joriy bo'lim endi ko'rinadigan bo'limlar orasida bo'lmasa — moslashtiriladi (React
  // hujjatlaridagi "render vaqtida holatni moslashtirish" naqshi, effekt EMAS —
  // `react-hooks/set-state-in-effect` yuqoridagi `invalidResetKey`ga o'xshash sabab bilan
  // taqiqlaydi): boshlang'ich tanlovda — hali to'ldirilmagan majburiy savoli bor BIRINCHI
  // ko'rinadigan bo'lim (resume), aks holda (allaqachon boshlangan bo'lsa, masalan oldinroqdagi
  // javob o'zgartirilib joriy bo'lim yashiringanda) eng yaqin (oxirgi) ko'rinadigan bo'lim.
  if (
    visibility &&
    visibleSections.length > 0 &&
    !visibleSections.some((section) => section.id === currentSectionId)
  ) {
    const lastSection = visibleSections[visibleSections.length - 1]!;
    const target = sectionInitialized
      ? lastSection
      : (visibleSections.find((section) =>
          questionsInSection(rawQuestions, section.id, visibility.visibleQuestionIds).some(
            (question) => question.isRequired && !isQuestionAnswered(question, draftAnswers[question.id]),
          ),
        ) ?? lastSection);
    setCurrentSectionId(target.id);
    if (!sectionInitialized) setSectionInitialized(true);
  }

  // Bo'lim almashganda fokus yangi sarlavhaga ko'chadi (a11y, docs/18 §6.2) — birinchi
  // ko'rsatishda EMAS (foydalanuvchi hali hech qanday harakat qilmagan).
  useEffect(() => {
    if (!visibility) return;
    if (isFirstSectionFocusRef.current) {
      isFirstSectionFocusRef.current = false;
      return;
    }
    sectionHeadingRef.current?.focus({ preventScroll: true });
  }, [currentSectionId, visibility]);

  // Bu testning statusi allaqachon "Completed" bo'lsa (masalan orqaga qaytilgan) keyingi
  // blokka yoki yakuniy ekranga yo'naltiriladi.
  useEffect(() => {
    if (!sessionStateQuery.data) return;
    const info = sessionStateQuery.data.tests.find((test) => test.code === testCode);
    if (info?.status === 'Completed') {
      const next = pickNextTestCode(sessionStateQuery.data.tests);
      navigate(next ? ROUTES.public.test(slug, next) : ROUTES.public.finish(slug), {
        replace: true,
      });
    }
  }, [sessionStateQuery.data, testCode, slug, navigate]);

  useEffect(() => {
    if (sessionStateQuery.error instanceof AppError && sessionStateQuery.error.status === 410) {
      handleSessionExpired();
    }
  }, [sessionStateQuery.error, handleSessionExpired]);

  const currentSectionIndex = visibleSections.findIndex((section) => section.id === currentSectionId);
  const currentSection = currentSectionIndex >= 0 ? visibleSections[currentSectionIndex] : null;
  const currentSectionQuestions =
    visibility && currentSection
      ? questionsInSection(rawQuestions, currentSection.id, visibility.visibleQuestionIds)
      : [];
  /** Ekranda HOZIR ko'rsatiladigan savollar — bo'limli rejimda joriy bo'lim, aks holda joriy sahifa. */
  const displayedQuestions = sections ? currentSectionQuestions : rawQuestions;

  function scrollToQuestion(questionId: string) {
    const node = fieldsetRefs.current.get(questionId);
    node?.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'center' });
    node?.focus({ preventScroll: true });
  }

  function goToNextQuestion(questionId: string) {
    const index = displayedQuestions.findIndex((q) => q.id === questionId);
    const nextQuestion = index >= 0 ? displayedQuestions[index + 1] : undefined;
    if (nextQuestion) scrollToQuestion(nextQuestion.id);
  }

  function handleAnswer(question: BranchingQuestion, payload: AnswerPayload) {
    const durationMs = getDurationSince(question.id, pageLoadTimeRef.current);
    autosave.setAnswer(question.id, payload, durationMs);
    // `draftAnswers` — `autosave.localAnswers`ning ko'zgusi, `visibility` shundan hisoblanadi
    // (yuqoridagi izohga qarang) — ikkalasi HAR doim shu yerda BIRGA yangilanadi.
    setDraftAnswers((prev) => ({ ...prev, [question.id]: payload }));
    setInvalidIds((prev) => {
      if (!prev.has(question.id)) return prev;
      const next = new Set(prev);
      next.delete(question.id);
      return next;
    });
    // Faqat BITTA-MARTA tanlov (Likert/Binary/SingleChoice/ForcedChoice — `value`) tanlangach
    // avtomatik keyingi savolga o'tadi. Matn/ko'p-tanlov `onAnswer` HAR harf/bosishda chaqiriladi
    // — avtomatik o'tish foydalanuvchini yozayotganda/bir nechta variant tanlayotganda uzib qo'yardi.
    if (payload.value !== undefined) {
      goToNextQuestion(question.id);
    }
  }

  function scrollToTop() {
    window.scrollTo({ top: 0, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
  }

  function handlePrev() {
    // Sahifa/bo'lim ichida harakat — serverga bog'liq emas, shu sabab kutilmaydi (oflaynda ham ishlaydi).
    if (sections) {
      if (currentSectionIndex <= 0) return;
      void autosave.flush();
      setInvalidIds(new Set());
      setCurrentSectionId(visibleSections[currentSectionIndex - 1]!.id);
      scrollToTop();
      return;
    }
    if (page <= 1) return;
    void autosave.flush();
    setPage((prev) => prev - 1);
    scrollToTop();
  }

  /** `docs/18` §4.3 bilan mos — barcha ko'rinadigan majburiy savol to'ldirilgach chaqiriladi. */
  async function finishTest() {
    // OXIRGI EKRAN (P30-2 poygasi). `POST .../complete` backendda "barcha majburiy (ko'rinadigan)
    // savolga javob berilganmi" deb TEKSHIRADI, ya'ni u navbatdagi javoblarga BOG'LIQ. Ilgari
    // `flush()` natijasi kutilmasdi va tez javob berilganda `complete` autosave paketidan OLDIN
    // yetib borib `400 VALIDATION_ERROR (unansweredCount)` qaytarardi. Endi `complete` faqat
    // navbat serverga YETIB BORGANDAN keyin yuboriladi.
    setIsSavingBeforeComplete(true);
    let saved: boolean;
    try {
      saved = await autosave.flush();
    } finally {
      setIsSavingBeforeComplete(false);
    }

    if (!saved) {
      // Javoblar YO'QOLMAYDI — ular `localStorage` navbatida (`pending: true`) qoladi va
      // keyingi urinishda/ulanish tiklanganda qayta yuboriladi (P21).
      toast.show({
        variant: 'danger',
        title: t('test.saveFailedTitle'),
        description: t('test.saveFailedDescription'),
      });
      return;
    }

    completeTest.mutate(testCode, {
      onSuccess: (result) => {
        if (result.allTestsCompleted) {
          navigate(ROUTES.public.finish(slug));
        } else {
          navigate(ROUTES.public.testDone(slug, testCode), {
            // `tests` — TestCompletePage'ga qo'shimcha `GET /sessions/me` so'rovisiz "qolgan
            // bloklar" ro'yxatini (nom/vaqt) berish uchun (P36, `sessionStore.ts` izohiga qarang).
            state: {
              nextTestCode: result.nextTestCode ?? null,
              tests: sessionStateQuery.data?.tests,
            },
          });
        }
      },
      onError: (error) => {
        if (error instanceof AppError && error.status === 410) {
          handleSessionExpired();
          return;
        }
        toast.show({
          variant: 'danger',
          title: error instanceof AppError ? error.message : t('error.generic'),
        });
      },
    });
  }

  async function handleNext() {
    if (isSavingBeforeComplete || completeTest.isPending) return; // ikki marta bosishdan himoya

    const unanswered = displayedQuestions.filter(
      (q) => q.isRequired && !isQuestionAnswered(q, draftAnswers[q.id]),
    );
    if (unanswered.length > 0) {
      setInvalidIds(new Set(unanswered.map((q) => q.id)));
      scrollToQuestion(unanswered[0]!.id);
      return;
    }

    if (sections) {
      // Oraliq bo'lim — keyingi ko'rinadigan bo'limga o'tadi (yashirilganlar sakrab o'tiladi,
      // chunki `visibleSections` allaqachon faqat ko'rinadiganlarni o'z ichiga oladi).
      if (currentSectionIndex < visibleSections.length - 1) {
        void autosave.flush();
        setInvalidIds(new Set());
        setCurrentSectionId(visibleSections[currentSectionIndex + 1]!.id);
        scrollToTop();
        return;
      }
    } else {
      const totalPages = started?.totalPages ?? questionsQuery.data?.totalPages ?? page;
      if (page < totalPages) {
        // Oraliq sahifa — keyingi sahifa serverdagi javoblarga BOG'LIQ EMAS (qiymatlar mahalliy
        // navbatdan ko'rsatiladi), shu sabab yuborish kutilmaydi: oflayn o'quvchi ham testni
        // davom ettira oladi (E2E-3).
        void autosave.flush();
        setPage((prev) => prev + 1);
        scrollToTop();
        return;
      }
    }

    // OXIRGI BO'LIM/SAHIFA — testni yakunlash.
    await finishTest();
  }

  if (!hasSession) {
    return <Navigate to={ROUTES.public.landing(slug)} replace />;
  }

  if (sessionStateQuery.isPending || startTest.isPending || startTest.isIdle) {
    return <TestPageSkeleton />;
  }

  if (sessionStateQuery.isError) {
    return <ErrorState onRetry={() => void sessionStateQuery.refetch()} />;
  }

  if (!testInfo) {
    return (
      <ErrorState
        title={t('test.invalidTestTitle')}
        description={t('test.invalidTestDescription')}
        onRetry={() => navigate(ROUTES.public.landing(slug))}
      />
    );
  }

  if (startTest.isError) {
    const error = startTest.error;
    if (error instanceof AppError && error.status === 409) {
      const currentTestCode = sessionStateQuery.data?.currentTestCode;
      return (
        <Navigate
          to={
            currentTestCode ? ROUTES.public.test(slug, currentTestCode) : ROUTES.public.finish(slug)
          }
          replace
        />
      );
    }
    return (
      <ErrorState
        onRetry={() => {
          startedTestCodeRef.current = null;
          startTest.mutate(testCode);
        }}
      />
    );
  }

  if (questionsQuery.isPending) {
    return <TestPageSkeleton />;
  }

  if (questionsQuery.isError) {
    const error = questionsQuery.error;
    if (error instanceof AppError && error.status === 410) {
      return <TestPageSkeleton />; // `handleSessionExpired` effekt orqali navigatsiya qiladi
    }
    return <ErrorState onRetry={() => void questionsQuery.refetch()} />;
  }

  const questionsData = questionsQuery.data;
  if (!questionsData) {
    return <TestPageSkeleton />;
  }
  // Bo'limli rejimda joriy bo'lim hali tanlanmagan bo'lishi mumkin (birinchi render, effekt
  // hali ishlamagan) — bitta qadam kutiladi.
  if (sections && !currentSection) {
    return <TestPageSkeleton />;
  }

  const scaleLabels = questionsData.scaleLabels ?? [];
  const pageSize = started?.pageSize ?? questionsData.pageSize;
  const totalBlocks = sessionStateQuery.data?.tests.length ?? 1;

  const totalQuestions = visibility
    ? rawQuestions.filter((q) => visibility.visibleQuestionIds.has(q.id)).length
    : testInfo.total || questionsData.totalQuestions;
  const answeredCount = visibility
    ? rawQuestions.filter(
        (q) => visibility.visibleQuestionIds.has(q.id) && isQuestionAnswered(q, draftAnswers[q.id]),
      ).length
    : Math.min(
        totalQuestions,
        (page - 1) * pageSize +
          questionsData.questions.filter((q) => isQuestionAnswered(q, draftAnswers[q.id])).length,
      );

  return (
    <div className="flex flex-col gap-4 pb-28">
      <TestProgressHeader
        testName={testName}
        blockIndex={testInfo.order}
        totalBlocks={totalBlocks}
        answered={answeredCount}
        total={totalQuestions}
      />

      <div className="flex items-center justify-end">
        <SaveStatusIndicator status={autosave.status} />
      </div>

      {!isOnline && <OfflineBanner />}

      <div className="flex flex-col gap-4">
        {currentSection && (
          <SectionIntro
            title={currentSection.title}
            description={currentSection.description}
            headingRef={(node) => {
              sectionHeadingRef.current = node;
            }}
          />
        )}
        {displayedQuestions.map((question) => {
          const answer = effectiveAnswer(question, draftAnswers[question.id]);
          return (
            <QuestionRenderer
              key={question.id}
              question={question}
              scaleLabels={scaleLabels}
              currentValue={answer.value}
              currentText={answer.text}
              currentSelectedValues={answer.selectedValues}
              invalid={invalidIds.has(question.id)}
              onAnswer={(payload) => {
                handleAnswer(question, payload);
              }}
              onAdvance={() => {
                goToNextQuestion(question.id);
              }}
              questionRef={(node) => {
                registerNode(node);
                if (node) {
                  fieldsetRefs.current.set(question.id, node);
                } else {
                  fieldsetRefs.current.delete(question.id);
                }
              }}
            />
          );
        })}
      </div>

      <div className="fixed inset-x-0 bottom-0 z-10 border-t border-line bg-paper/95 px-5 py-3 backdrop-blur-xl sm:px-8">
        <div className="mx-auto flex w-full max-w-prose items-center justify-between gap-3">
          <Button
            variant="outline"
            className={publicButtonClass('ghost', 'md')}
            onClick={handlePrev}
            disabled={sections ? currentSectionIndex <= 0 : page <= 1}
          >
            {t('common.back')}
          </Button>
          <Button
            className={publicButtonClass('primary', 'lg')}
            onClick={() => {
              void handleNext();
            }}
            // `Button` `disabled={disabled || isLoading}` qiladi — yuklanish paytida ikkinchi
            // bosish mumkin emas, foydalanuvchi esa kutayotganini ko'radi.
            isLoading={isSavingBeforeComplete || completeTest.isPending}
          >
            {t('common.next')}
          </Button>
        </div>
      </div>
    </div>
  );
}
