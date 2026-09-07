import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Play } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Button, ErrorState, Skeleton } from '@/shared/ui';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { PUBLIC_SPACE_SLUG } from '@/shared/config/publicSpace';
import { AppError } from '@/shared/api/AppError';
import { adoptSession } from '@/shared/api/sessionToken';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import type { StartSessionResponse } from '@/shared/api/types';
import { MY_PROFILE_QUERY_KEY, useMyProfile } from '../api/useMyProfile';
import { useStartOwnSession } from '../api/useStartOwnSession';
import { PublicRegistrationForm } from '../components/PublicRegistrationForm';
import { SavedProfileCard } from '../components/SavedProfileCard';
import { buildReadyPayload, resolveProfileState, type ProfileFormState } from '../lib/profileState';
import { mapStartSessionErrorCode } from '../lib/startSessionErrors';

/** Sarlavha/kirish matni holatga qarab — `new`: anketa, `ready`: boshlash, `edit`: tahrir. */
const HEADING_KEYS: Record<ProfileFormState, { title: string; lead: string }> = {
  new: { title: 'account.register.title', lead: 'account.register.lead' },
  ready: { title: 'account.register.readyTitle', lead: 'account.register.readyLead' },
  edit: { title: 'account.register.editTitle', lead: 'account.register.editLead' },
};

/**
 * `/kabinet/test` — MAKTABSIZ test boshlash (`POST /api/me/sessions`, `docs/07` §5.4).
 *
 * Anketa QAYTA SO'RALMAYDI (2026-09-07): sahifa avval `GET /api/me/profile` ni o'qiydi va
 * uch holatdan birini ko'rsatadi (`lib/profileState.ts`):
 * - **A `new`** — profil yo'q: to'liq forma, F.I.Sh. Telegram ismidan taklif bilan;
 * - **B `ready`** — profil to'liq, rozilik joriy: forma YO'Q, "Sizning ma'lumotlaringiz"
 *   kartasi + "Testni boshlash" (`{}` yuboriladi — shaxsiy ma'lumot ketmaydi) + "O'zgartirish";
 * - **C `edit`** — profil bor, lekin rozilik eskirgan / foydalanuvchi "O'zgartirish" bosdi
 *   (`?edit=1` ham): forma to'ldirilgan holda, rozilik bloki faqat eskirgan bo'lsa.
 *
 * Maktab anketasidan (`features/public-assessment/pages/RegistrationPage`) farqi:
 * `slug`/`accessToken`/`accessCode`/`classLetter`/`parentPhone` YO'Q, yosh 6–99, `grade`
 * ixtiyoriy, 18 yoshgacha ota-ona roziligi majburiy. Maktab anketasi O'ZGARMAGAN.
 *
 * Sessiya ochilgach foydalanuvchi **mavjud** test oqimida davom etadi
 * (`/t/ommaviy/test/:testCode`, `X-Session-Token`) — parallel oqim yo'q.
 */
export default function PublicRegistrationPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const profileQuery = useMyProfile();
  const quickStart = useStartOwnSession();

  const [editing, setEditing] = useState(searchParams.get('edit') === '1');
  const [quickStartError, setQuickStartError] = useState<string | null>(null);

  const state: ProfileFormState | null = profileQuery.isSuccess
    ? resolveProfileState(profileQuery.data, editing)
    : null;
  const heading = HEADING_KEYS[state ?? 'new'];

  usePageTitle(t(heading.title));

  function handleStarted(result: StartSessionResponse): void {
    // Profil yaratildi/tahrirlandi — kabinet va keyingi kirish yangi qiymatni ko'rsin.
    void queryClient.invalidateQueries({ queryKey: MY_PROFILE_QUERY_KEY });

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
  }

  async function handleQuickStart(): Promise<void> {
    setQuickStartError(null);
    try {
      const result = await quickStart.mutateAsync(buildReadyPayload());
      handleStarted(result);
    } catch (error) {
      if (error instanceof AppError && error.code === 'VALIDATION_ERROR') {
        // Server profilni yetarli deb topmadi (masalan rozilik shu orada eskirgan) —
        // profilni yangilab formaga o'tamiz, foydalanuvchi faqat yetishmaganini to'ldiradi.
        void profileQuery.refetch();
        setEditing(true);
        return;
      }
      setQuickStartError(
        error instanceof AppError
          ? mapStartSessionErrorCode(error, t)
          : t('account.register.errors.generic'),
      );
    }
  }

  // "Bekor qilish" faqat tahrir ixtiyoriy bo'lganda (profil o'z holicha `ready`) — rozilik
  // eskirgan bo'lsa formadan qochib bo'lmaydi.
  const canCancelEdit =
    profileQuery.isSuccess && editing && resolveProfileState(profileQuery.data, false) === 'ready';

  return (
    <div className="wrap flex max-w-2xl flex-col gap-6 py-12 sm:py-16">
      <Link to={ROUTES.account.home} className="btn btn-md btn-ghost self-start">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('account.register.back')}
      </Link>

      <header className="flex flex-col gap-2 text-center">
        <p className="eyebrow text-firuza-700">{t('account.register.eyebrow')}</p>
        <h1 className="font-display balance text-3xl font-extrabold tracking-tight text-ink">
          {t(heading.title)}
        </h1>
        <p className="lead text-[15px]">{t(heading.lead)}</p>
      </header>

      {profileQuery.isPending && (
        <Skeleton className="h-72 w-full rounded-4xl bg-line/70" aria-hidden="true" />
      )}

      {profileQuery.isError && (
        <ErrorState
          description={t('account.register.errors.profileLoad')}
          onRetry={() => void profileQuery.refetch()}
        />
      )}

      {profileQuery.isSuccess && state === 'ready' && (
        <SavedProfileCard
          profile={profileQuery.data}
          onEdit={() => {
            setEditing(true);
          }}
        >
          {quickStartError && (
            <p
              role="alert"
              className="rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
            >
              {quickStartError}
            </p>
          )}
          <Button
            type="button"
            size="lg"
            className="w-full"
            isLoading={quickStart.isPending}
            onClick={() => void handleQuickStart()}
          >
            <Play className="size-4" aria-hidden="true" />
            {t('account.register.submitCta')}
          </Button>
        </SavedProfileCard>
      )}

      {profileQuery.isSuccess && state !== null && state !== 'ready' && (
        <PublicRegistrationForm
          // `ready` → `edit` o'tishida forma qayta mount bo'lib profil qiymatlarini oladi
          // (`useForm` `defaultValues` ni faqat mount'da o'qiydi).
          key={state}
          profile={profileQuery.data}
          mode={state}
          onStarted={handleStarted}
          onCancel={
            canCancelEdit
              ? () => {
                  setEditing(false);
                }
              : undefined
          }
        />
      )}
    </div>
  );
}
