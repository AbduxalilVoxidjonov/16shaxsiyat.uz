import { useEffect, useRef, useState, type MouseEvent } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  ClipboardList,
  LayoutDashboard,
  Layers,
  LogOut,
  Menu,
  School,
  ScrollText,
  Settings,
  Sparkles,
  User,
  Users,
  X,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { Logo } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { useAuthStore } from '@/features/auth/store/authStore';
import { useLogout } from '@/features/auth/api/useLogout';

interface NavItem {
  to: string;
  key: string;
  icon: typeof LayoutDashboard;
  end?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  {
    to: ROUTES.admin.dashboard,
    key: 'nav.dashboard',
    icon: LayoutDashboard,
    end: true,
  },
  { to: ROUTES.admin.schools, key: 'nav.schools', icon: School },
  { to: ROUTES.admin.students, key: 'nav.students', icon: Users },
  { to: ROUTES.admin.assessments, key: 'nav.assessments', icon: ClipboardList },
  { to: ROUTES.admin.catalog, key: 'nav.catalog', icon: BookOpen },
  { to: ROUTES.admin.programs, key: 'nav.programs', icon: Layers },
  { to: ROUTES.admin.ai, key: 'nav.ai', icon: Sparkles },
  { to: ROUTES.admin.audit, key: 'nav.audit', icon: ScrollText },
  { to: ROUTES.admin.settings, key: 'nav.settings', icon: Settings },
];

/** Ikkala (desktop `<aside>` va mobil drawer) joyda ishlatiladigan umumiy navigatsiya ro'yxati. */
function SidebarNav({ ariaLabel, onNavigate }: { ariaLabel: string; onNavigate?: () => void }) {
  const { t } = useTranslation();
  return (
    <nav aria-label={ariaLabel} className="flex flex-col gap-0.5 px-2">
      {NAV_ITEMS.map(({ to, key, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-2.5 rounded-full px-3 py-2.5 text-sm font-medium text-ink-soft transition-colors hover:bg-line',
              // Faol havola to'ldirilgan firuza "tabletka"si: `firuza-50` tint sidebar ning
              // o'z `paper-deep` foniga juda yaqin bo'lib, faol holat ko'rinmay qolardi.
              // Oq matn `firuza-600` (#0B8080) ustida 4.76:1 — WCAG AA o'tadi.
              isActive && 'bg-firuza-600 font-semibold text-white shadow-soft hover:bg-firuza-700',
            )
          }
        >
          <Icon size={18} aria-hidden="true" />
          <span className="flex-1">{t(key)}</span>
        </NavLink>
      ))}
    </nav>
  );
}

/**
 * Mobil sidebar — native `<dialog>.showModal()` asosida (`shared/ui/Dialog.tsx` bilan bir xil
 * naqsh): brauzer o'zi fokus tuzog'ini va `Escape`da yopilishni ta'minlaydi. Fon scroll'i
 * qo'shimcha ravishda `document.body.style.overflow` bilan aniq bloklanadi — `showModal()`ning
 * o'zi buni barcha brauzerlarda bir xilda ta'minlamaydi.
 */
function MobileSidebarDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation();
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const node = ref.current;
    if (!node || typeof node.showModal !== 'function' || typeof node.close !== 'function') return;
    if (open && !node.open) {
      node.showModal();
    } else if (!open && node.open) {
      node.close();
    }
  }, [open]);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    const handleClose = () => {
      onClose();
    };
    node.addEventListener('close', handleClose);
    return () => {
      node.removeEventListener('close', handleClose);
    };
  }, [onClose]);

  useEffect(() => {
    if (!open) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [open]);

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    if (event.target === ref.current) {
      onClose();
    }
  }

  return (
    // Backdrop'ga bosishni aniqlashning yagona yo'li shu klik handleri (`Dialog.tsx`dagi kabi);
    // `Escape` `onCancel` orqali qamrab olingan.
    // eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/click-events-have-key-events
    <dialog
      ref={ref}
      onClick={handleBackdropClick}
      onCancel={onClose}
      // `dialog-drawer-start` — `index.css` dagi nomlangan istisno: umumiy
      // `dialog:modal { margin: auto }` qoidasi (barcha oyna markazda) shu bitta oynada
      // bekor qilinadi va drawer ekranning chap chetiga yopishadi. Tailwind ning `m-0`
      // klassi buni qila olmaydi — spetsifiklik bo'yicha `dialog:modal` dan past (P30-8).
      className="dialog-drawer-start fixed top-0 left-0 h-dvh w-72 max-w-[80vw] border-0 bg-paper-deep p-0 shadow-lift backdrop:bg-ink/40 md:hidden"
    >
      <div className="flex items-center justify-between border-b border-line px-4 py-4">
        <Logo />
        <button
          type="button"
          onClick={onClose}
          aria-label={t('nav.closeMenu')}
          className="rounded-full p-1.5 text-ink-soft hover:bg-line hover:text-ink"
        >
          <X size={18} aria-hidden="true" />
        </button>
      </div>
      <div className="py-3">
        <SidebarNav ariaLabel={t('nav.dashboard')} onNavigate={onClose} />
      </div>
    </dialog>
  );
}

/** Header'dagi foydalanuvchi menyusi — native `<details>` (o'zi klaviatura bilan ochiladi). */
function UserMenu() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const clearSession = useAuthStore((state) => state.clear);
  const logoutMutation = useLogout();
  const detailsRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    function handleClickOutside(event: globalThis.MouseEvent) {
      if (detailsRef.current && !detailsRef.current.contains(event.target as Node)) {
        detailsRef.current.open = false;
      }
    }
    document.addEventListener('click', handleClickOutside);
    return () => {
      document.removeEventListener('click', handleClickOutside);
    };
  }, []);

  async function handleLogout() {
    if (detailsRef.current) {
      detailsRef.current.open = false;
    }
    try {
      await logoutMutation.mutateAsync();
    } catch {
      // Server bilan aloqa uzilgan bo'lsa ham mahalliy sessiya baribir tozalanadi — foydalanuvchi
      // brauzerda har doim chiqib keta olishi kerak.
    } finally {
      clearSession();
      navigate(ROUTES.admin.login, { replace: true });
    }
  }

  return (
    <details ref={detailsRef} className="relative">
      <summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 rounded-full px-3 py-1.5 text-sm font-medium text-ink-soft hover:bg-line hover:text-ink [&::-webkit-details-marker]:hidden">
        <User size={16} aria-hidden="true" />
        {user?.username ?? t('nav.account')}
      </summary>
      <div className="absolute right-0 z-10 mt-2 w-44 rounded-2xl border border-line bg-paper-card p-1 shadow-lift">
        <button
          type="button"
          onClick={() => void handleLogout()}
          className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-left text-sm text-ink-soft hover:bg-paper-deep hover:text-ink"
        >
          <LogOut size={16} aria-hidden="true" />
          {t('nav.logout')}
        </button>
      </div>
    </details>
  );
}

/**
 * Admin panel qatlami — chap sidebar + yuqori header (docs/10, 2-bo'lim; docs/11 A-1
 * yaqinidagi umumiy tuzilma; prompts/22). Mobilda sidebar drawer sifatida ochiladi.
 */
export function AdminLayout() {
  const { t } = useTranslation();
  const [isDrawerOpen, setDrawerOpen] = useState(false);

  return (
    <div className="grid min-h-dvh grid-cols-1 bg-paper text-ink md:grid-cols-[240px_1fr]">
      <a
        href="#admin-main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-full focus:bg-paper-card focus:px-4 focus:py-2 focus:font-semibold focus:text-ink focus:shadow-lift"
      >
        {t('common.skipToContent')}
      </a>
      {/*
        Navigatsiya sahifa bilan birga siljimaydi: `sticky top-0` + `h-dvh` bilan ekranga
        yopishtiriladi, faqat asosiy mazmun siljiydi. `self-start` grid katagining
        cho'zilishini to'xtatadi (usiz `sticky` ishlamaydi). Ro'yxatning o'zi ekrandan
        baland bo'lsa (kichik noutbuk, katta shrift) `overflow-y-auto` bilan ichida siljiydi.
      */}
      <aside className="sticky top-0 hidden h-dvh self-start overflow-y-auto border-r border-line bg-paper-deep md:block print:hidden">
        <div className="px-4 py-5">
          <Logo />
        </div>
        <SidebarNav ariaLabel={t('nav.dashboard')} />
      </aside>
      <MobileSidebarDrawer open={isDrawerOpen} onClose={() => setDrawerOpen(false)} />
      <div className="flex min-w-0 flex-col">
        <header className="flex h-14 items-center justify-between gap-3 border-b border-line bg-paper-deep px-4 print:hidden">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setDrawerOpen(true)}
              aria-label={t('nav.openMenu')}
              className="rounded-full p-1.5 text-ink-soft hover:bg-line hover:text-ink md:hidden"
            >
              <Menu size={20} aria-hidden="true" />
            </button>
            <span className="font-display text-sm font-bold text-ink md:hidden">
              {t('app.name')}
            </span>
          </div>
          <UserMenu />
        </header>
        {/*
          KONTRAST QARORI (P45): mazmun maydoni ATAYLAB `bg-paper-card` (oq), `bg-paper`
          (#FBF8F3) EMAS. Admin sahifalari (`features/**`) hali eski palitrada va ularda
          sahifa fonida to'g'ridan-to'g'ri turadigan `text-neutral-500` (#64748B) matn bor
          (masalan `SchoolDetailPage` — maktab viloyat/tuman satri). U eski `bg-neutral-50`
          fonida 4.55:1 edi — WCAG AA (4.5:1) ni arang o'tardi; `bg-paper` da esa 4.49:1
          bo'lib, axe `color-contrast` (serious) qoidasini YIQITARDI. Oq fonda 4.76:1 —
          regressiya yo'q, aksincha yaxshilanish.
          Iliq "qog'oz" qatlami sidebar/topbar (`bg-paper-deep`) va jadval sarlavhalari
          orqali beriladi. Admin sahifalari `ink-*` palitrasiga ko'chirilgach, bu yerni
          `bg-paper` ga o'zgartirsa bo'ladi (`text-ink-soft` paper fonida 9.29:1).
        */}
        <main id="admin-main-content" className="min-w-0 flex-1 bg-paper-card p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
