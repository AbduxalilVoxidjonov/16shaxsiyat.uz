import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { EmptyState, ErrorState, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { usePublicSession, usePublicLogout } from '../api/usePublicSession';
import { useMyAssessments } from '../api/useMyAssessments';
import { useMyProfile } from '../api/useMyProfile';
import { useDeleteMyAccount } from '../api/useDeleteMyAccount';
import { useResumeOwnSession } from '../hooks/useResumeOwnSession';
import { usePublicUserStore } from '../store/publicUserStore';
import { findUnfinishedAssessment } from '../lib/assessmentStatus';
import { ProfileCard } from '../components/ProfileCard';
import { ResumeAssessmentCard } from '../components/ResumeAssessmentCard';
import { SavedProfileCard } from '../components/SavedProfileCard';
import { AssessmentHistoryList } from '../components/AssessmentHistoryList';
import { DeleteAccountDialog } from '../components/DeleteAccountDialog';

function HistorySkeleton() {
  return (
    <div className="flex flex-col gap-3" aria-hidden="true">
      {[0, 1, 2].map((key) => (
        <Skeleton key={key} className="h-24 w-full rounded-4xl bg-line/70" />
      ))}
    </div>
  );
}

/**
 * `/kabinet` — ommaviy foydalanuvchining shaxsiy kabineti (`docs/07` §5).
 *
 * Profil (§5.1) + saqlangan anketa (§5.1a, bo'lsa) + test tarixi (§5.2) + akkauntni o'chirish
 * (§5.5).
 *
 * **Tugallanmagan sessiya** (`Draft`/`InProgress`/`Abandoned` — `lib/assessmentStatus.ts`) bo'lsa
 * tepada "Tugallanmagan test" kartasi chiqadi va "Davom ettirish" tugmasi FAQAT shu kartada
 * (egasining qarori, 2026-09-07: ilgari tugma uch joyda — karta, sarlavha va tarix qatori —
 * takrorlanib sahifani chalkashtirardi). Tarix qatori o'sha sessiyani faqat holati bilan
 * ("Davom etmoqda") ko'rsatadi. "Yangi test boshlash" o'z holicha qoladi: server bir vaqtda
 * ikkinchi sessiya ochmaydi (`docs/07` §5.4), shu sabab u yo'l ham xavfsiz — anketa sahifasi
 * eskisini qaytaradi va "davom ettirildi" deb bildiradi.
 * Davom ettirish — `hooks/useResumeOwnSession.ts`.
 *
 * Guard (`PublicUserRoute`)
 * bu sahifaga faqat kirgan foydalanuvchini kiritadi, shu sabab bu yerda `user` bor deb
 * hisoblanadi — `null` bo'lsa ham sahifa yiqilmaydi (yuklanish holati ko'rsatiladi).
 */
export default function AccountPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();

  const { user } = usePublicSession();
  const logout = usePublicLogout();
  const clearSession = usePublicUserStore((state) => state.clear);
  const assessmentsQuery = useMyAssessments();
  const profileQuery = useMyProfile();
  const deleteAccount = useDeleteMyAccount();
  const resumeSession = useResumeOwnSession();

  const unfinished = assessmentsQuery.isSuccess
    ? findUnfinishedAssessment(assessmentsQuery.data.items)
    : null;

  const [isDeleteOpen, setDeleteOpen] = useState(false);
  const [isLoggingOut, setLoggingOut] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);

  usePageTitle(t('account.home.title'));

  async function handleLogout() {
    setLoggingOut(true);
    await logout();
    navigate(ROUTES.marketing.home, { replace: true });
  }

  function handleDelete() {
    setDeleteError(undefined);
    deleteAccount.mutate(undefined, {
      onSuccess: () => {
        setDeleteOpen(false);
        // Server tokenlarni bekor qildi — lokal holat ham darhol tozalanadi.
        clearSession();
        toast.show({ variant: 'success', title: t('account.delete.doneToast') });
        navigate(ROUTES.marketing.home, { replace: true });
      },
      onError: (error) => {
        setDeleteError(
          error instanceof AppError ? error.message : t('account.delete.errorFallback'),
        );
      },
    });
  }

  return (
    <div className="wrap flex flex-col gap-8 py-12 sm:py-16">
      {user ? (
        <ProfileCard
          user={user}
          onLogout={() => void handleLogout()}
          onDeleteRequest={() => {
            setDeleteOpen(true);
          }}
          isLoggingOut={isLoggingOut}
        />
      ) : (
        <Skeleton className="h-32 w-full rounded-4xl bg-line/70" />
      )}

      {unfinished && (
        <ResumeAssessmentCard
          assessment={unfinished}
          onResume={() => void resumeSession.resume()}
          isResuming={resumeSession.isPending}
          error={resumeSession.error}
        />
      )}

      {/*
        Saqlangan anketa (`GET /api/me/profile`) — Telegram kartasidan ALOHIDA: u akkaunt,
        bu test uchun berilgan rasmiy ma'lumot. Profil yo'q bo'lsa karta ko'rsatilmaydi —
        bo'sh tarix holati o'zi "birinchi testni boshlang" deb taklif qiladi. Yuklanish/xato
        holati jimgina o'tkaziladi: bu bo'lim ixtiyoriy, kabinetning asosiy mazmuni tarix.
      */}
      {profileQuery.isSuccess && profileQuery.data.hasProfile && (
        <SavedProfileCard
          profile={profileQuery.data}
          editHref={`${ROUTES.account.startTest}?edit=1`}
        />
      )}

      <section className="flex flex-col gap-5" aria-labelledby="account-history-heading">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2
              id="account-history-heading"
              className="font-display text-xl font-extrabold tracking-tight text-ink"
            >
              {t('account.history.heading')}
            </h2>
            <p className="mt-1 text-sm text-ink-soft">{t('account.history.lead')}</p>
          </div>
          <Link to={ROUTES.account.startTest} className="btn btn-md btn-primary shrink-0">
            <Plus className="size-4" aria-hidden="true" />
            {t('account.history.startCta')}
          </Link>
        </div>

        {assessmentsQuery.isPending && <HistorySkeleton />}

        {assessmentsQuery.isError && <ErrorState onRetry={() => void assessmentsQuery.refetch()} />}

        {assessmentsQuery.isSuccess &&
          (assessmentsQuery.data.items.length === 0 ? (
            <EmptyState
              title={t('account.history.emptyTitle')}
              description={t('account.history.emptyDescription')}
              action={
                <Link to={ROUTES.account.startTest} className="btn btn-md btn-primary mt-3">
                  {t('account.history.startCta')}
                </Link>
              }
            />
          ) : (
            <AssessmentHistoryList items={assessmentsQuery.data.items} />
          ))}
      </section>

      <DeleteAccountDialog
        open={isDeleteOpen}
        onClose={() => {
          setDeleteOpen(false);
        }}
        onConfirm={handleDelete}
        isDeleting={deleteAccount.isPending}
        error={deleteError}
      />
    </div>
  );
}
