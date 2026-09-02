import { useEffect, useRef, useState, type MouseEvent } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  ClipboardList,
  LayoutDashboard,
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
import { ROUTES } from '@/shared/config/routes';
import { useAuthStore } from '@/features/auth/store/authStore';
import { useLogout } from '@/features/auth/api/useLogout';

interface NavItem {
  to: string;
  key: string;
  icon: typeof LayoutDashboard;
  end?: boolean;
  /** `false` — bo'lim hali qurilmoqda, sidebarda "tez orada" belgisi bilan ko'rsatiladi. */
  ready: boolean;
}

const NAV_ITEMS: NavItem[] = [
  {
    to: ROUTES.admin.dashboard,
    key: 'nav.dashboard',
    icon: LayoutDashboard,
    end: true,
    ready: false,
  },
  { to: ROUTES.admin.schools, key: 'nav.schools', icon: School, ready: false },
  { to: ROUTES.admin.students, key: 'nav.students', icon: Users, ready: false },
  { to: ROUTES.admin.assessments, key: 'nav.assessments', icon: ClipboardList, ready: false },
  { to: ROUTES.admin.catalog, key: 'nav.catalog', icon: BookOpen, ready: false },
  { to: ROUTES.admin.ai, key: 'nav.ai', icon: Sparkles, ready: false },
  { to: ROUTES.admin.audit, key: 'nav.audit', icon: ScrollText, ready: false },
  { to: ROUTES.admin.settings, key: 'nav.settings', icon: Settings, ready: true },
];

/** Ikkala (desktop `<aside>` va mobil drawer) joyda ishlatiladigan umumiy navigatsiya ro'yxati. */
function SidebarNav({ ariaLabel, onNavigate }: { ariaLabel: string; onNavigate?: () => void }) {
  const { t } = useTranslation();
  return (
    <nav aria-label={ariaLabel} className="flex flex-col gap-0.5 px-2">
      {NAV_ITEMS.map(({ to, key, icon: Icon, end, ready }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-medium text-neutral-600 hover:bg-neutral-100',
              isActive && 'bg-primary-50 text-primary-700',
            )
          }
        >
          <Icon size={18} aria-hidden="true" />
          <span className="flex-1">{t(key)}</span>
          {!ready && (
            <span className="rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-500">
              {t('nav.comingSoon')}
            </span>
          )}
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
      className="fixed top-0 left-0 m-0 h-dvh w-72 max-w-[80vw] border-0 bg-white p-0 shadow-lg backdrop:bg-neutral-900/40 md:hidden"
    >
      <div className="flex items-center justify-between px-4 py-4">
        <span className="text-lg font-semibold text-neutral-900">{t('app.name')}</span>
        <button
          type="button"
          onClick={onClose}
          aria-label={t('nav.closeMenu')}
          className="rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900"
        >
          <X size={18} aria-hidden="true" />
        </button>
      </div>
      <SidebarNav ariaLabel={t('nav.dashboard')} onNavigate={onClose} />
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
      <summary className="flex cursor-pointer list-none items-center gap-2 rounded-lg px-2 py-1.5 text-sm font-medium text-neutral-700 hover:bg-neutral-100 [&::-webkit-details-marker]:hidden">
        <User size={16} aria-hidden="true" />
        {user?.username ?? t('nav.account')}
      </summary>
      <div className="absolute right-0 z-10 mt-2 w-44 rounded-lg border border-neutral-200 bg-white p-1 shadow-md">
        <button
          type="button"
          onClick={() => void handleLogout()}
          className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm text-neutral-700 hover:bg-neutral-100"
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
    <div className="grid min-h-dvh grid-cols-1 md:grid-cols-[240px_1fr]">
      <a
        href="#admin-main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:shadow"
      >
        {t('common.skipToContent')}
      </a>
      <aside className="hidden border-r border-neutral-200 bg-white md:block print:hidden">
        <div className="px-4 py-5 text-lg font-semibold text-neutral-900">{t('app.name')}</div>
        <SidebarNav ariaLabel={t('nav.dashboard')} />
      </aside>
      <MobileSidebarDrawer open={isDrawerOpen} onClose={() => setDrawerOpen(false)} />
      <div className="flex min-w-0 flex-col">
        <header className="flex h-14 items-center justify-between gap-3 border-b border-neutral-200 bg-white px-4 print:hidden">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setDrawerOpen(true)}
              aria-label={t('nav.openMenu')}
              className="rounded-md p-1.5 text-neutral-600 hover:bg-neutral-100 md:hidden"
            >
              <Menu size={20} aria-hidden="true" />
            </button>
            <span className="text-sm font-medium text-neutral-500 md:hidden">{t('app.name')}</span>
          </div>
          <UserMenu />
        </header>
        <main id="admin-main-content" className="min-w-0 flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
