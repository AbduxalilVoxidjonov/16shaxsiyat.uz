import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
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
import { SchoolCodeCard } from '../components/SchoolCodeCard';
import { readTelegramCallbackParams, stripTelegramCallbackParams } from '../lib/telegramCallback';
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

/**
 * Telegram foydalanuvchini QAYTARADIGAN manzil (`data-auth-url`).
 *
 * **Callback sifatida `/kirish` ning O'ZI tanlandi**, alohida yo'l emas. Sabablari:
 *   1. Xato holatlari (`TELEGRAM_AUTH_INVALID`, `…_EXPIRED`, `…_NOT_CONFIGURED`, `429`),
 *      kutish indikatori va "qaytadan urinish" tugmasi allaqachon shu sahifada. Alohida
 *      `/kirish/telegram` sahifasi ularning hammasini takrorlashga majbur qilardi.
 *   2. `returnUrl` shu manzilda saqlanadi va Telegram uni o'z parametrlari bilan birga
 *      qaytaradi — qo'shimcha holat saqlash (`sessionStorage`/`state`) kerak emas.
 *   3. Xato bo'lsa foydalanuvchi allaqachon kerakli sahifada turadi — hech qayerga
 *      ko'chirilmaydi.
 *
 * Manzil **MUTLAQ** bo'lishi shart: Telegram nisbiy yo'lni qabul qilmaydi. U ayni
 * ochilgan origin'dan quriladi, ya'ni dev (`localhost:5173`) va prod (`16shaxsiyat.uz`)
 * uchun alohida sozlama kerak emas.
 */
function buildTelegramAuthUrl(returnUrl: string): string {
  const url = new URL(ROUTES.account.login, window.location.origin);
  if (returnUrl !== ROUTES.account.home) {
    url.searchParams.set('returnUrl', returnUrl);
  }
  return url.toString();
}

/** Kirish sahifasidagi uchta qisqa va'da — matn i18n kalitlaridan. */
const PROMISE_ITEMS = [
  { key: 'own', Icon: UserRound },
  { key: 'privacy', Icon: ShieldCheck },
  { key: 'free', Icon: Sparkles },
] as const;

/**
 * `/kirish` — IKKI TENG yo'l (`docs/07` §2a.1, §1.1a; `docs/08` §2a, §3a):
 *   1. **Shaxsiy kabinet** — Telegram orqali kirish (mavjud oqim, o'zgarmagan);
 *   2. **Maktab uchun** — maktab bergan 8 belgili kod (`SchoolCodeCard`): akkaunt ochilmaydi,
 *      kod `{ slug, accessToken }` ga aylanadi va foydalanuvchi MAVJUD maktab oqimiga
 *      (`/t/:slug?k=`) o'tadi.
 *
 * Telegram qismi IKKI holatda ochiladi:
 *   - oddiy tashrif — Telegram widget tugmasi ko'rsatiladi;
 *   - Telegram redirect'i — manzilda `id`, `auth_date`, `hash`… parametrlari bor; ular
 *     O'ZGARTIRILMASDAN `POST /api/auth/telegram` ga yuboriladi, so'ng manzil satridan
 *     darhol tozalanadi (shaxsiy ma'lumot brauzer tarixida qolmasin).
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

  /**
   * Telegram callback'i FAQAT sahifa ochilgandagi manzildan o'qiladi (`useState`
   * initializer bir marta ishlaydi). Keyin manzil `replaceState` bilan tozalanadi, ya'ni
   * qayta o'qishga urinish baribir bo'sh natija berardi.
   */
  const [callbackPayload] = useState(() => readTelegramCallbackParams(searchParams));
  const isSubmittedRef = useRef(false);

  const authUrl = useMemo(() => buildTelegramAuthUrl(returnUrl), [returnUrl]);

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

  useEffect(() => {
    if (!callbackPayload || isSubmittedRef.current) {
      return;
    }
    // Bir martalik: React StrictMode effektni ikki marta chaqiradi, `ref` esa qayta
    // o'rnatilmaydi — ya'ni bitta `hash` ikki marta yuborilmaydi.
    isSubmittedRef.current = true;
    handleAuth(callbackPayload);
    // So'rov yuborilgandan keyin DARHOL: ism, Telegram id va avatar havolasi manzil
    // satrida ham, brauzer tarixida ham qolmaydi. Javobni kutmaymiz — kutish davomida
    // foydalanuvchi manzilni ko'rib turgan bo'lardi.
    stripTelegramCallbackParams();
  }, [callbackPayload, handleAuth]);

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

  // Telegram'dan qaytilganda tugma emas, kutish holati ko'rsatiladi — so'rov hali
  // ketmagan bo'lsa ham (effekt renderdan keyin ishlaydi) ekran "sakramaydi".
  const isPending = login.isPending || (callbackPayload !== null && errorMessage === null);

  return (
    <section className="wrap py-14 sm:py-20">
      <div className="relative mx-auto max-w-4xl overflow-hidden rounded-5xl border border-line bg-paper-card p-7 shadow-soft sm:p-10">
        <PatternBackdrop className="opacity-25" />
        <GirihStar
          className="animate-spin-slow pointer-events-none absolute -top-24 -right-24 size-64 text-firuza-100"
          strokeWidth={1}
          aria-hidden="true"
        />

        <div className="relative flex flex-col gap-8">
          <div className="text-center">
            <p className="eyebrow text-firuza-700">{t('account.login.eyebrow')}</p>
            <h1 className="font-display balance mt-3 text-3xl font-extrabold tracking-tight text-ink sm:text-4xl">
              {t('account.login.pageHeading')}
            </h1>
            <p className="lead mt-4 text-[15px]">{t('account.login.pageLead')}</p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            {/* 1-karta: shaxsiy kabinet (Telegram) — mavjud mantiq o'zgarmagan. */}
            <section
              aria-labelledby="telegram-login-heading"
              className="flex flex-col items-center gap-4 rounded-4xl border border-line bg-paper-deep/50 p-6 text-center"
            >
              <div>
                <p className="eyebrow text-firuza-700">{t('account.login.personal.title')}</p>
                <h2
                  id="telegram-login-heading"
                  className="font-display mt-2 text-xl font-extrabold text-ink"
                >
                  {t('account.login.heading')}
                </h2>
                <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">{t('account.login.lead')}</p>
              </div>

              {isPending ? (
                <p className="flex items-center gap-2 text-sm text-ink-soft">
                  <Spinner size={18} />
                  {t('account.login.pending')}
                </p>
              ) : (
                <TelegramLoginButton authUrl={authUrl} disabled={login.isPending} />
              )}

              {errorMessage && (
                <p
                  role="alert"
                  className="w-full rounded-3xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
                >
                  {t(errorMessage)}
                </p>
              )}
            </section>

            {/* 2-karta: maktab kodi — akkauntsiz, mavjud `/t/:slug` oqimiga eshik. */}
            <SchoolCodeCard />
          </div>

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
