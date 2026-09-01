import { NavLink, Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  ClipboardList,
  LayoutDashboard,
  School,
  ScrollText,
  Settings,
  Sparkles,
  Users,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { ROUTES } from '@/shared/config/routes';

interface NavItem {
  to: string;
  key: string;
  icon: typeof LayoutDashboard;
  end?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { to: ROUTES.admin.dashboard, key: 'nav.dashboard', icon: LayoutDashboard, end: true },
  { to: ROUTES.admin.schools, key: 'nav.schools', icon: School },
  { to: ROUTES.admin.students, key: 'nav.students', icon: Users },
  { to: ROUTES.admin.assessments, key: 'nav.assessments', icon: ClipboardList },
  { to: ROUTES.admin.catalog, key: 'nav.catalog', icon: BookOpen },
  { to: ROUTES.admin.ai, key: 'nav.ai', icon: Sparkles },
  { to: ROUTES.admin.audit, key: 'nav.audit', icon: ScrollText },
  { to: ROUTES.admin.settings, key: 'nav.settings', icon: Settings },
];

/** Admin panel qatlami — yon panel + sarlavha (docs/10, 2-bo'lim). To'liq mantiq keyingi promptlarda. */
export function AdminLayout() {
  const { t } = useTranslation();

  return (
    <div className="grid min-h-dvh grid-cols-1 md:grid-cols-[240px_1fr]">
      <a
        href="#admin-main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:shadow"
      >
        {t('common.skipToContent')}
      </a>
      <aside className="hidden border-r border-neutral-200 bg-white md:block">
        <div className="px-4 py-5 text-lg font-semibold text-neutral-900">{t('app.name')}</div>
        <nav aria-label={t('nav.dashboard')} className="flex flex-col gap-0.5 px-2">
          {NAV_ITEMS.map(({ to, key, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-medium text-neutral-600 hover:bg-neutral-100',
                  isActive && 'bg-primary-50 text-primary-700',
                )
              }
            >
              <Icon size={18} aria-hidden="true" />
              {t(key)}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex min-w-0 flex-col">
        <header className="flex h-14 items-center justify-between border-b border-neutral-200 bg-white px-4">
          <span className="text-sm font-medium text-neutral-500">{t('app.name')}</span>
        </header>
        <main id="admin-main-content" className="min-w-0 flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
