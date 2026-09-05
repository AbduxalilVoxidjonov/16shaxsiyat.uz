import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';
import { GirihStar, Logo } from '@/shared/ui/brand';
import { usePublicSession } from '@/features/public-account/api/usePublicSession';
import { CONTACT } from '@/shared/config/contact';
import { ROUTES } from '@/shared/config/routes';
import { cn } from '@/shared/lib/cn';

/**
 * Ommaviy tanishtiruv qatlamining qatlam-komponenti (P45) — sticky shaffof header, mobil
 * menyu va katta footer.
 *
 * Nega `PublicLayout` dan alohida: `PublicLayout` (docs/10, 2-bo'lim) test oqimi uchun
 * ataylab minimal — u yerda o'quvchini chalg'itadigan navigatsiya BO'LMASLIGI kerak. Bu
 * qatlam esa aksincha, tanishtiruv sahifalari o'rtasida yurish uchun to'liq navigatsiya
 * beradi. Ikkalasi bir-biriga tegmaydi.
 *
 * `body` global uslubi admin panel uchun `bg-neutral-50 text-neutral-900` bo'lib qoladi
 * (`src/index.css`), shu sabab qog'oz palitrasi shu yerdagi wrapper'da beriladi.
 */

/**
 * Header va footer navigatsiyasi — matn kalitlari `marketing.nav.*` dan olinadi.
 * "Asosiy" ataylab ro'yxatning boshida: logotip ham bosh sahifaga olib boradi, lekin
 * foydalanuvchi uchun ochiq havola bo'lishi kerak (logotipning bosiladiganligi hammaga
 * ravshan emas). `isActivePath` bosh sahifani aniq moslik bo'yicha tekshiradi.
 */
const NAV_ITEMS = [
  { key: 'home', to: ROUTES.marketing.home },
  { key: 'methodology', to: ROUTES.marketing.methodology },
  { key: 'about', to: ROUTES.marketing.about },
  { key: 'contact', to: ROUTES.marketing.contact },
] as const;

/** Footer'dagi "Sayt" ustuni header bilan bir xil ro'yxatdan foydalanadi. */
const FOOTER_LINKS = NAV_ITEMS;

const MOBILE_MENU_ID = 'marketing-mobile-menu';

function isActivePath(pathname: string, to: string): boolean {
  return to === ROUTES.marketing.home ? pathname === to : pathname.startsWith(to);
}

function MarketingHeader() {
  const { t } = useTranslation();
  const { pathname } = useLocation();
  /*
    Navigatsiya foydalanuvchi kirgan-kirmaganiga qarab ikki xil ko'rinadi (P47):
    anonim — "Kirish" + "Testni boshlash" (`/kirish`), kirgan — "Kabinet" + "Testni
    boshlash" (`/kabinet/test`). Sessiya holati `usePublicSession` orqali keladi va u
    faqat `localStorage` dagi BELGI bo'lganda server so'rovini yuboradi.
  */
  const { isAuthenticated } = usePublicSession();
  const accountLink = isAuthenticated ? ROUTES.account.home : ROUTES.account.login;
  const accountLabel = isAuthenticated ? t('marketing.nav.account') : t('marketing.nav.login');
  const startTestLink = isAuthenticated ? ROUTES.account.startTest : ROUTES.account.login;
  /*
    Mobil menyu holati QAYSI sahifada ochilgani bilan birga saqlanadi. Shu sabab boshqa
    sahifaga o'tilishi bilan (havola bosildimi yoki brauzerning "orqaga" tugmasimi — farqi
    yo'q) menyu o'z-o'zidan yopiladi: `open` render paytida hisoblanadi, `useEffect` ichida
    `setState` chaqirilmaydi (react-hooks/set-state-in-effect).
  */
  const [openedAtPath, setOpenedAtPath] = useState<string | null>(null);
  const open = openedAtPath === pathname;
  const [scrolled, setScrolled] = useState(false);

  // Sahifa yuqorisida header shaffof, pastga siljiganda qog'oz fon + blur bilan ajraladi.
  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  // Menyu ochiq turganda ostidagi sahifa skroll bo'lmasin (fokus tuzog'i o'rniga sodda yechim:
  // menyu paneli `fixed` va butun ekranni egallaydi).
  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open]);

  return (
    <header
      className={cn(
        'sticky top-0 z-50 border-b transition-all duration-300',
        scrolled ? 'border-line bg-paper/85 backdrop-blur-xl' : 'border-transparent bg-transparent',
      )}
    >
      <div className="wrap flex h-[72px] items-center justify-between gap-6">
        <Logo asLink />

        <nav
          className="hidden items-center gap-1 lg:flex"
          aria-label={t('marketing.nav.primaryLabel')}
        >
          {NAV_ITEMS.map((item) => {
            const active = isActivePath(pathname, item.to);
            return (
              <Link
                key={item.key}
                to={item.to}
                aria-current={active ? 'page' : undefined}
                className={cn(
                  'rounded-full px-4 py-2 text-sm font-semibold transition-colors',
                  active
                    ? 'bg-firuza-50 text-firuza-700'
                    : 'text-ink-soft hover:bg-paper-deep hover:text-ink',
                )}
              >
                {t(`marketing.nav.${item.key}`)}
              </Link>
            );
          })}
        </nav>

        <div className="flex items-center gap-2">
          <Link to={accountLink} className="btn btn-md btn-ghost hidden sm:inline-flex">
            {accountLabel}
          </Link>
          <Link to={startTestLink} className="btn btn-md btn-primary hidden sm:inline-flex">
            {t('marketing.nav.startTest')}
          </Link>
          <button
            type="button"
            onClick={() => setOpenedAtPath(open ? null : pathname)}
            aria-label={open ? t('marketing.nav.closeMenu') : t('marketing.nav.openMenu')}
            aria-expanded={open}
            aria-controls={MOBILE_MENU_ID}
            className="grid size-11 place-items-center rounded-full border border-line-strong bg-paper-card lg:hidden"
          >
            {/* Uchta chiziq ochiq holatda krestga aylanadi — sof dekorativ, matn tugma nomida. */}
            <span className="relative block h-3.5 w-5" aria-hidden="true">
              <span
                className={cn(
                  'absolute left-0 block h-0.5 w-5 rounded-full bg-ink transition-all duration-300',
                  open ? 'top-1.5 rotate-45' : 'top-0',
                )}
              />
              <span
                className={cn(
                  'absolute top-1.5 left-0 block h-0.5 w-5 rounded-full bg-ink transition-opacity duration-200',
                  open ? 'opacity-0' : 'opacity-100',
                )}
              />
              <span
                className={cn(
                  'absolute left-0 block h-0.5 w-5 rounded-full bg-ink transition-all duration-300',
                  open ? 'top-1.5 -rotate-45' : 'top-3',
                )}
              />
            </span>
          </button>
        </div>
      </div>

      <div
        id={MOBILE_MENU_ID}
        hidden={!open}
        className="animate-fade-in fixed inset-x-0 top-[72px] bottom-0 z-40 overflow-y-auto border-t border-line bg-paper lg:hidden"
      >
        <nav className="wrap flex flex-col gap-1 py-6" aria-label={t('marketing.nav.mobileLabel')}>
          {FOOTER_LINKS.map((item) => (
            <Link
              key={item.key}
              to={item.to}
              aria-current={isActivePath(pathname, item.to) ? 'page' : undefined}
              className="rounded-2xl px-4 py-4 text-lg font-semibold text-ink hover:bg-paper-deep"
            >
              {t(`marketing.nav.${item.key}`)}
            </Link>
          ))}
          <Link to={accountLink} className="btn btn-lg btn-ghost mt-4 w-full">
            {accountLabel}
          </Link>
          <Link to={startTestLink} className="btn btn-lg btn-primary mt-2 w-full">
            {t('marketing.nav.startTest')}
          </Link>
        </nav>
      </div>
    </header>
  );
}

function MarketingFooter() {
  const { t } = useTranslation();

  return (
    <footer className="relative mt-24 overflow-hidden border-t border-line bg-paper-deep">
      <div
        className="bg-girih pointer-events-none absolute inset-0 opacity-50"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute -top-24 -right-24 size-72 text-line-strong opacity-40"
        aria-hidden="true"
      >
        <GirihStar className="animate-spin-slow size-full" strokeWidth={0.6} />
      </div>

      <div className="wrap relative py-16">
        <div className="grid gap-12 lg:grid-cols-[1.6fr_1fr_1fr]">
          <div>
            <Logo />
            <p className="mt-5 max-w-sm text-[15px] leading-relaxed text-ink-soft">
              {t('marketing.footer.description')}
            </p>
          </div>

          <div>
            <h2 className="font-display text-sm font-bold text-ink">
              {t('marketing.footer.sitemapHeading')}
            </h2>
            <ul className="mt-4 space-y-2.5">
              {FOOTER_LINKS.map((item) => (
                <li key={item.key}>
                  <Link
                    to={item.to}
                    className="text-[15px] text-ink-soft transition-colors hover:text-firuza-700"
                  >
                    {t(`marketing.nav.${item.key}`)}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h2 className="font-display text-sm font-bold text-ink">
              {t('marketing.footer.contactHeading')}
            </h2>
            <ul className="mt-4 space-y-2.5 text-[15px] text-ink-soft">
              <li>
                <a
                  href={CONTACT.email.href}
                  className="transition-colors hover:text-firuza-700 break-words"
                >
                  {CONTACT.email.display}
                </a>
              </li>
              <li>
                <a href={CONTACT.phone.href} className="transition-colors hover:text-firuza-700">
                  {CONTACT.phone.display}
                </a>
              </li>
              <li>
                <a
                  href={CONTACT.telegram.href}
                  rel="noreferrer"
                  className="transition-colors hover:text-firuza-700"
                >
                  {CONTACT.telegram.display}
                </a>
              </li>
            </ul>
            <Link to={ROUTES.marketing.contact} className="btn btn-md btn-dark mt-6 w-full">
              {t('marketing.footer.ctaLabel')}
            </Link>
          </div>
        </div>

        <div className="mt-14 border-t border-line pt-8">
          {/*
            Eslatma ATAYLAB har sahifada ko'rinadi (CLAUDE.md 6-qoida: "AI tashxis qo'ymaydi") —
            platforma nima QILMASLIGI birinchi ekrandayoq aytiladi.
          */}
          <p className="text-[13px] leading-relaxed text-ink-soft">
            <strong className="font-semibold text-ink">
              {t('marketing.footer.disclaimerLabel')}
            </strong>{' '}
            {t('marketing.footer.disclaimer')}
          </p>
          <p className="mt-4 text-[13px] text-ink-soft">
            {t('marketing.footer.copyright', { year: new Date().getFullYear() })}
          </p>
        </div>
      </div>
    </footer>
  );
}

export function MarketingLayout() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-dvh flex-col bg-paper text-ink">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-paper-card focus:px-3 focus:py-2 focus:shadow"
      >
        {t('common.skipToContent')}
      </a>
      <MarketingHeader />
      <main id="main-content" className="flex-1">
        <Outlet />
      </main>
      <MarketingFooter />
    </div>
  );
}
