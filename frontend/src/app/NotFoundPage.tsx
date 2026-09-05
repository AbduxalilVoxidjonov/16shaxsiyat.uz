import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ROUTES } from '@/shared/config/routes';
import { GirihStar, Logo, PatternBackdrop } from '@/shared/ui/brand';

/**
 * 404 — hech qanday marshrutga tushmagan manzil (`router.tsx` dagi `path: '*'`).
 *
 * Ikkita chiqish yo'li beriladi: tanishtiruv bosh sahifasi (`ROUTES.marketing.home`) va
 * brauzer tarixi bo'yicha orqaga qaytish. Orqaga qaytish ham kerak, chunki o'quvchi bu
 * yerga ko'pincha noto'g'ri terilgan MAKTAB havolasi (`/t/:slug`) bilan tushadi — unga
 * bosh sahifa emas, oldingi sahifa foydaliroq.
 */
export default function NotFoundPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const title = t('pages.notFound.title');
  usePageTitle(title);

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden bg-paper px-5 py-16 text-center">
      <PatternBackdrop className="opacity-40" />

      <div className="relative flex max-w-md flex-col items-center animate-fade-up">
        <Logo />

        <div className="relative mt-10 grid size-32 place-items-center">
          <GirihStar
            className="absolute inset-0 animate-spin-slow text-line-strong"
            strokeWidth={2}
          />
          <span className="relative font-display text-3xl font-extrabold tracking-tight text-ink-faint">
            404
          </span>
        </div>

        <h1 className="mt-8 font-display text-3xl font-extrabold tracking-tight text-ink balance sm:text-4xl">
          {title}
        </h1>
        <p className="lead mt-4 text-base sm:text-lg">{t('pages.notFoundDescription')}</p>

        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <Link to={ROUTES.marketing.home} className="btn btn-lg btn-primary">
            {t('marketing.nav.home')}
          </Link>
          <button type="button" className="btn btn-lg btn-ghost" onClick={() => void navigate(-1)}>
            {t('common.back')}
          </button>
        </div>
      </div>
    </div>
  );
}
