import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';

/**
 * Ommaviy oqim uchun sodda, markazlashtirilgan, mobil-birinchi qatlam (`docs/10`, 2-bo'lim;
 * `docs/11` E-1/E-2, `prompts/20`): brend nomi yuqorida, minimal footer pastda. Sticky
 * header/footer kabi elementlar test sahifasida (`E-3`) o'ziga xos bo'lgani uchun bu yerga
 * qo'shilmaydi — har sahifa o'zi kerakli chrome'ni quradi.
 */
export function PublicLayout() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-dvh flex-col bg-neutral-50">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:shadow"
      >
        {t('common.skipToContent')}
      </a>
      <header className="border-b border-neutral-200 bg-white">
        <div className="mx-auto flex w-full max-w-xl items-center px-4 py-3">
          <span className="text-lg font-semibold text-primary-700">{t('app.name')}</span>
        </div>
      </header>
      <main id="main-content" className="mx-auto w-full max-w-xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      {/*
        Tag-line rangi ATAYLAB `neutral-600`: `neutral-400` (#94a3b8) `neutral-50` (#f8fafc)
        fonida 2.45:1 beradi — WCAG AA (4.5:1) dan past edi (P30-3). `neutral-500` 4.55:1
        bilan chegaraga arang yetadi, `neutral-600` esa 7.24:1 — palitradan chiqmagan holda
        yetarli zaxira. Matn baribir ikkilamchi ko'rinadi (`text-xs`, markazda).
      */}
      <footer className="px-4 py-4 text-center text-xs text-neutral-600">
        <p>{t('app.tagline')}</p>
      </footer>
    </div>
  );
}
