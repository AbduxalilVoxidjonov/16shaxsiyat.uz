import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';

/**
 * Ommaviy oqim uchun sodda, brendsiz, mobil-birinchi qatlam (docs/10, 2-bo'lim).
 * Sticky header/footer kabi elementlar test sahifasida (`E-3`) o'ziga xos bo'lgani uchun
 * bu yerga qo'shilmaydi — har sahifa o'zi kerakli chrome'ni quradi.
 */
export function PublicLayout() {
  const { t } = useTranslation();
  return (
    <div className="min-h-dvh bg-neutral-50">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:shadow"
      >
        {t('common.skipToContent')}
      </a>
      <main id="main-content" className="mx-auto w-full max-w-xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  );
}
