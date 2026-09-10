import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Divider, Logo, PatternBackdrop } from '@/shared/ui/brand';

/**
 * Ommaviy oqim uchun markazlashtirilgan, mobil-birinchi qatlam (`docs/10`, 2-bo'lim;
 * `docs/11` E-1/E-2, `prompts/20`): brend logotipi yuqorida, nafis footer pastda. Sticky
 * header/footer kabi elementlar test sahifasida (`E-3`) o'ziga xos bo'lgani uchun bu yerga
 * qo'shilmaydi — har sahifa o'zi kerakli chrome'ni quradi.
 *
 * P45 dizayni: `body` global uslubi (`bg-neutral-50`, admin panel uchun) O'ZGARMAYDI —
 * ommaviy palitra (`bg-paper text-ink`) faqat shu wrapperda beriladi. Girih naqshli fon
 * dekorativ va `pointer-events-none` (`PatternBackdrop`), shu sabab bosish/skrollga
 * xalaqit bermaydi.
 */
export function PublicLayout() {
  const { t } = useTranslation();
  return (
    <div className="relative flex min-h-dvh flex-col bg-paper font-sans text-ink">
      <PatternBackdrop className="h-96 opacity-45" />

      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-full focus:bg-paper-card focus:px-4 focus:py-2 focus:shadow-lift"
      >
        {t('common.skipToContent')}
      </a>

      <header className="relative z-10 border-b border-line/70">
        <div className="mx-auto flex w-full max-w-prose items-center justify-between gap-3 px-5 py-4 sm:px-8">
          <Logo />
          {/*
            Shior faqat kengroq ekranda — telefonda logotipni siqib qo'ymasligi uchun.
            `eyebrow` klassi rangni `text-ink-muted` qilib beradi, lekin u `paper` fonida
            4.49:1 (pastdagi footer izohiga qarang) — shu sabab `text-ink-soft` bilan
            ustidan yoziladi (utility klass `@layer components` dan kuchliroq).
          */}
          <span className="eyebrow xs:block hidden text-ink-soft">{t('app.tagline')}</span>
        </div>
      </header>

      <main
        id="main-content"
        className="relative z-10 mx-auto w-full max-w-prose flex-1 px-5 py-8 sm:px-8"
      >
        <Outlet />
      </main>

      {/*
        Tag-line rangi ATAYLAB `ink-soft`, `ink-muted` EMAS: `ink-muted` (#7C7168) `paper`
        (#FBF8F3) fonida ≈4.49:1 beradi — WCAG AA (4.5:1) dan sal past (bu P30-3 da
        `neutral-400` bilan topilgan xatoning yangi palitradagi ko'rinishi). `ink-soft`
        (#4A423B) esa ≈9.3:1 — palitradan chiqmagan holda yetarli zaxira. Matn baribir
        ikkilamchi ko'rinadi (`text-xs`, markazda).
      */}
      {/*
        `pointer-events-none` — QA topilmasi (P52): footer sof dekorativ (`Divider`
        `aria-hidden`, tagline matni — hech qanday havola/tugma yo'q), lekin `relative z-10`
        (`<main>` bilan bir xil qatlam) sabab DOM tartibida KEYINROQ keladi. Sahifa JUDA
        QISQA bo'lganda (masalan tarmoqlanuvchi so'rovnomaning bitta savolli bo'limi,
        `docs/18` §6.2) `min-h-dvh`/`flex-1` tuzilmasi footer'ni ekran pastiga "itarib"
        yuboradi — aynan o'sha joyda `TestPage`ning `fixed bottom-0` "Keyingi" paneli ham
        joylashgan. `z-index` teng bo'lgani uchun keyinroq DOM elementi (footer) ustidan
        bosishni to'sib qo'yardi (E2E, `branching-survey.e2e.ts`, haqiqiy brauzerda ushlangan
        — Vitest/jsdom'da ko'rinmaydi, chunki u pointer-event stacking'ni hisoblamaydi).
        Footer'da bosiladigan narsa yo'qligi sabab `pointer-events-none` xavfsiz — hech qanday
        funksionallik yo'qolmaydi.
      */}
      <footer className="relative z-10 px-5 pt-6 pb-8 pointer-events-none sm:px-8">
        <div className="mx-auto w-full max-w-prose">
          <Divider />
          <p className="mt-5 text-center text-xs text-ink-soft">{t('app.tagline')}</p>
        </div>
      </footer>
    </div>
  );
}
