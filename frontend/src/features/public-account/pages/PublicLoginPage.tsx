import { useCallback, useState } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ShieldCheck, Sparkles, UserRound } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Spinner } from '@/shared/ui';
import { GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import type { TelegramLoginRequestBody } from '@/shared/api/types';
import { TelegramLoginButton } from '../components/TelegramLoginButton';
import { useTelegramLogin } from '../api/useTelegramLogin';
import { usePublicSession } from '../api/usePublicSession';
import { usePublicUserStore } from '../store/publicUserStore';

/** Kirishdan keyin qayerga qaytishni aniqlaydi — faqat ichki (nisbiy) yo'lga ruxsat. */
function safeReturnUrl(raw: string | null): string {
  // Ochiq redirect himoyasi: `//evil.com` ham brauzer uchun MUTLAQ manzil, shu sabab
  // ikkinchi belgi ham tekshiriladi.
  if (raw && raw.startsWith('/') && !raw.startsWith('//')) {
    return raw;
  }
  return ROUTES.account.home;
}

/** Kirish sahifasidagi uchta qisqa va'da — matn i18n kalitlaridan. */
const PROMISE_ITEMS = [
  { key: 'own', Icon: UserRound },
  { key: 'privacy', Icon: ShieldCheck },
  { key: 'free', Icon: Sparkles },
] as const;

/**
 * `/kirish` — ommaviy foydalanuvchining Telegram orqali kirishi (`docs/07` §2a.1).
 *
 * Maktab oqimiga (`/t/:slug`) BU SAHIFA umuman aloqador emas: u yerda kirish ham,
 * ro'yxatdan o'tish ham talab qilinmaydi.
 */
export default function PublicLoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnUrl = safeReturnUrl(searchParams.get('returnUrl'));

  const { isRestoring, isAuthenticated } = usePublicSession();
  const setSession = usePublicUserStore((state) => state.setSession);
  const login = useTelegramLogin();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  usePageTitle(t('account.login.title'));

  const handleAuth = useCallback(
    (user: TelegramLoginRequestBody) => {
      setErrorMessage(null);
      login.mutate(user, {
        onSuccess: (result) => {
          // `setSession` tokenni HTTP qatlamiga (`publicUserClient`) va React qatlamiga
          // (store) BIRGA qo'yadi — ikkalasi hech qachon bir-biridan ajralmaydi.
          setSession(result.accessToken, result.user);
          navigate(returnUrl, { replace: true });
        },
        onError: (error) => {
          setErrorMessage(
            error instanceof AppError ? loginErrorKey(error) : 'account.login.errors.generic',
          );
        },
      });
    },
    [login, navigate, returnUrl, setSession],
  );

  if (isRestoring) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size={28} />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to={returnUrl} replace />;
  }

  return (
    <section className="wrap py-14 sm:py-20">
      <div className="relative mx-auto max-w-xl overflow-hidden rounded-5xl border border-line bg-paper-card p-7 shadow-soft sm:p-10">
        <PatternBackdrop className="opacity-25" />
        <GirihStar
          className="animate-spin-slow pointer-events-none absolute -top-24 -right-24 size-64 text-firuza-100"
          strokeWidth={1}
          aria-hidden="true"
        />

        <div className="relative flex flex-col items-center gap-6 text-center">
          <div>
            <p className="eyebrow text-firuza-700">{t('account.login.eyebrow')}</p>
            <h1 className="font-display balance mt-3 text-3xl font-extrabold tracking-tight text-ink sm:text-4xl">
              {t('account.login.heading')}
            </h1>
            <p className="lead mt-4 text-[15px]">{t('account.login.lead')}</p>
          </div>

          {login.isPending ? (
            <p className="flex items-center gap-2 text-sm text-ink-soft">
              <Spinner size={18} />
              {t('account.login.pending')}
            </p>
          ) : (
            <TelegramLoginButton onAuth={handleAuth} disabled={login.isPending} />
          )}

          {errorMessage && (
            <p
              role="alert"
              className="w-full rounded-3xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
            >
              {t(errorMessage)}
            </p>
          )}

          <ul className="grid w-full gap-3 text-left sm:grid-cols-3">
            {PROMISE_ITEMS.map(({ key, Icon }) => (
              <li key={key} className="rounded-3xl border border-line bg-paper-deep/50 p-4">
                <Icon className="size-4 text-firuza-700" aria-hidden="true" focusable="false" />
                <p className="font-display mt-2 text-[13px] leading-snug font-bold text-ink">
                  {t(`account.login.promises.${key}.title`)}
                </p>
                <p className="mt-1 text-[13px] leading-relaxed text-ink-soft">
                  {t(`account.login.promises.${key}.text`)}
                </p>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

/** Backend xato kodini foydalanuvchiga tushunarli i18n kalitiga o'giradi (`docs/07` §2a.1). */
function loginErrorKey(error: AppError): string {
  switch (error.code) {
    case 'TELEGRAM_AUTH_NOT_CONFIGURED':
      return 'account.login.errors.notConfigured';
    case 'TELEGRAM_AUTH_EXPIRED':
      return 'account.login.errors.expired';
    case 'TELEGRAM_AUTH_INVALID':
      return 'account.login.errors.invalid';
    case 'RATE_LIMITED':
      return 'account.login.errors.rateLimited';
    case 'VALIDATION_ERROR':
      return 'account.login.errors.invalid';
    default:
      return 'account.login.errors.generic';
  }
}
