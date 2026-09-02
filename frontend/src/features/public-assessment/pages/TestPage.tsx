import { useEffect, useRef, useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useOnline } from '@/shared/hooks/useOnline';
import { Button, ErrorState, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useSessionState } from '../api/useSessionState';
import { useStartTest } from '../api/useStartTest';
import { useTestQuestions } from '../api/useTestQuestions';
import { useCompleteTest } from '../api/useCompleteTest';
import { useAutosave } from '../hooks/useAutosave';
import { useQuestionVisibility } from '../hooks/useQuestionVisibility';
import { useSessionExpiredGuard } from '../hooks/useSessionExpiredGuard';
import { useSessionStore } from '../store/sessionStore';
import { pickNextTestCode } from '../lib/nextTest';
import { LikertQuestion } from '../components/LikertQuestion';
import { TestProgressHeader } from '../components/TestProgressHeader';
import { SaveStatusIndicator } from '../components/SaveStatusIndicator';
import { OfflineBanner } from '../components/OfflineBanner';
import type { PublicQuestion } from '@/shared/api/types';

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
      <Skeleton className="h-16 w-full rounded-xl" />
      {[0, 1, 2, 3].map((key) => (
        <Skeleton key={key} className="h-32 w-full rounded-xl" />
      ))}
    </div>
  );
}

/**
 * E-3 Test sahifasi (`/t/:slug/test/:testCode`) — docs/11 E-3, docs/07 1.4–1.6-bo'lim,
 * docs/10 4.3-bo'lim, CLAUDE.md MAXSUS DIQQAT 1–5-band.
 *
 * Test nomi bu sahifaning o'z endpoint'laridan kelmaydi (`PublicTestSummaryDto`da faqat
 * `code`/`status`/`answered`/`total`/`order` bor — docs/07 1.3-bo'lim) — `sessionStore.
 * testCatalog` (Landing/Registration `GET /schools/{slug}` javobidan saqlab qo'yadi) orqali
 * olinadi; u yo'q bo'lsa (masalan `localStorage` tozalangan holatda to'g'ridan-to'g'ri havola
 * ochilgan) `testCode`ning o'zi ko'rsatiladi (fallback).
 */
export default function TestPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const isOnline = useOnline();
  const { slug = '', testCode = '' } = useParams<{ slug: string; testCode: string }>();

  const sessionToken = useSessionStore((state) => state.sessionToken);
  const storedSlug = useSessionStore((state) => state.slug);
  const testCatalog = useSessionStore((state) => state.testCatalog);
  const hasSession = Boolean(sessionToken) && storedSlug === slug;
  const testName = testCatalog?.find((item) => item.code === testCode)?.name ?? testCode;

  const handleSessionExpired = useSessionExpiredGuard(slug);

  const sessionStateQuery = useSessionState(hasSession);
  const startTest = useStartTest();
  const completeTest = useCompleteTest();

  const [page, setPage] = useState(1);
  const [invalidIds, setInvalidIds] = useState<ReadonlySet<string>>(new Set());
  const startedTestCodeRef = useRef<string | null>(null);
  const initializedPageRef = useRef(false);
  const fieldsetRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const pageLoadTimeRef = useRef(0);

  const { registerNode, getDurationSince, reset: resetVisibility } = useQuestionVisibility();

  usePageTitle(testName);

  // Yangi test blokiga o'tilganda (route element qayta ishlatiladi, faqat `:testCode` o'zgaradi)
  // ref'lar tozalanadi — bu faqat effektda xavfsiz (render vaqtida ref yozish taqiqlangan).
  useEffect(() => {
    startedTestCodeRef.current = null;
    initializedPageRef.current = false;
  }, [testCode]);

  // `invalidIds`ni yangi test blokiga o'tilganda tozalash — React hujjatlaridagi "render
  // vaqtida holatni moslashtirish" naqshi (effekt ichida sinxron `setState` o'rniga —
  // `react-hooks/set-state-in-effect`): https://react.dev/learn/you-might-not-need-an-effect
  const [invalidResetKey, setInvalidResetKey] = useState(testCode);
  if (invalidResetKey !== testCode) {
    setInvalidResetKey(testCode);
    setInvalidIds(new Set());
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

  const testInfo = sessionStateQuery.data?.tests.find((test) => test.code === testCode);
  const started = startTest.data?.testCode === testCode ? startTest.data : undefined;

  // Sahifa yangilanganda o'sha joydan davom etish — `answered` soni bo'yicha resume sahifasini hisoblaydi.
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

  useEffect(() => {
    resetVisibility();
    pageLoadTimeRef.current = performance.now();
  }, [questionsQuery.data, resetVisibility]);

  const autosave = useAutosave({ testCode, onSessionExpired: handleSessionExpired });

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

  function displayValue(question: PublicQuestion): number | null {
    return autosave.localValues[question.id] ?? question.currentValue ?? null;
  }

  function goToNextQuestion(questionId: string) {
    const questions = questionsQuery.data?.questions ?? [];
    const index = questions.findIndex((q) => q.id === questionId);
    const nextQuestion = index >= 0 ? questions[index + 1] : undefined;
    if (!nextQuestion) return;
    const node = fieldsetRefs.current.get(nextQuestion.id);
    node?.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'center' });
    node?.focus({ preventScroll: true });
  }

  function handleAnswer(question: PublicQuestion, value: number) {
    const durationMs = getDurationSince(question.id, pageLoadTimeRef.current);
    autosave.setAnswer(question.id, value, durationMs);
    setInvalidIds((prev) => {
      if (!prev.has(question.id)) return prev;
      const next = new Set(prev);
      next.delete(question.id);
      return next;
    });
    goToNextQuestion(question.id);
  }

  function scrollToTop() {
    window.scrollTo({ top: 0, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
  }

  function handlePrev() {
    if (page <= 1) return;
    autosave.flush();
    setPage((prev) => prev - 1);
    scrollToTop();
  }

  function handleNext() {
    const questions = questionsQuery.data?.questions ?? [];
    const unanswered = questions.filter((q) => q.isRequired && displayValue(q) === null);
    if (unanswered.length > 0) {
      setInvalidIds(new Set(unanswered.map((q) => q.id)));
      const first = unanswered[0];
      const node = first ? fieldsetRefs.current.get(first.id) : undefined;
      node?.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'center' });
      node?.focus({ preventScroll: true });
      return;
    }

    autosave.flush();
    const totalPages = started?.totalPages ?? questionsQuery.data?.totalPages ?? page;
    if (page < totalPages) {
      setPage((prev) => prev + 1);
      scrollToTop();
      return;
    }

    completeTest.mutate(testCode, {
      onSuccess: (result) => {
        if (result.allTestsCompleted) {
          navigate(ROUTES.public.finish(slug));
        } else {
          navigate(ROUTES.public.testDone(slug, testCode), {
            state: { nextTestCode: result.nextTestCode ?? null },
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
          to={currentTestCode ? ROUTES.public.test(slug, currentTestCode) : ROUTES.public.finish(slug)}
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
  const scaleLabels = questionsData.scaleLabels ?? [];
  const pageSize = started?.pageSize ?? questionsData.pageSize;
  const totalQuestions = testInfo.total || questionsData.totalQuestions;
  const answeredOnPage = questionsData.questions.filter((q) => displayValue(q) !== null).length;
  const answeredCount = Math.min(totalQuestions, (page - 1) * pageSize + answeredOnPage);
  const totalBlocks = sessionStateQuery.data?.tests.length ?? 1;

  return (
    <div className="flex flex-col gap-4 pb-24">
      <TestProgressHeader
        testName={testName}
        blockIndex={testInfo.order}
        totalBlocks={totalBlocks}
        answered={answeredCount}
        total={totalQuestions}
      />

      <div className="flex items-center justify-between">
        <SaveStatusIndicator status={autosave.status} />
      </div>

      {!isOnline && <OfflineBanner />}

      <div className="flex flex-col gap-4">
        {questionsData.questions.map((question) => (
          <LikertQuestion
            key={question.id}
            question={question}
            scaleLabels={scaleLabels}
            value={displayValue(question)}
            invalid={invalidIds.has(question.id)}
            onAnswer={(value) => {
              handleAnswer(question, value);
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
        ))}
      </div>

      <div className="fixed inset-x-0 bottom-0 z-10 border-t border-neutral-200 bg-white px-4 py-3">
        <div className="mx-auto flex w-full max-w-xl items-center justify-between gap-3">
          <Button variant="outline" onClick={handlePrev} disabled={page <= 1}>
            {t('common.back')}
          </Button>
          <Button onClick={handleNext} isLoading={completeTest.isPending}>
            {t('common.next')}
          </Button>
        </div>
      </div>
    </div>
  );
}
