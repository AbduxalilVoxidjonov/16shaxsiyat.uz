import type { Page } from '@playwright/test';

/**
 * Sahifadagi BIR MARTALIK animatsiyalar tugaguncha kutadi.
 *
 * NEGA KERAK (P45 reskin topilmasi): yangi dizayn deyarli har ekranga kirish
 * animatsiyasi qo'shdi (`animate-fade-up`, `animate-fade-in`). `axe` kontrastni
 * animatsiya YARMIDA o'lchasa, matnni yarim shaffof holatda ko'radi va butun formani
 * "kontrast yetarli emas" deb belgilaydi (E-2 anketada 25 ta element: `text-ink-soft`
 * #4a423b o'rniga blend natijasi #afaaa5 — 2.23:1). Animatsiya tugagach o'sha ekranda
 * bitta ham buzilish qolmaydi. Ya'ni bu O'LCHOV nuqsoni, dizayn nuqsoni emas: WCAG
 * kontrast talabi o'tkinchi animatsiya kadriga emas, barqaror holatga tegishli.
 *
 * CHEKSIZ takrorlanadigan animatsiyalar (girih yulduzining sekin aylanishi,
 * `animate-pulse` skeletlar) ATAYLAB hisobga olinmaydi — ular hech qachon tugamaydi.
 */
export async function waitForAnimationsToSettle(page: Page): Promise<void> {
  await page.waitForFunction(
    () =>
      document.getAnimations().every((animation) => {
        const iterations = animation.effect?.getTiming().iterations ?? 1;
        return iterations === Infinity || animation.playState === 'finished';
      }),
    undefined,
    { timeout: 10_000 },
  );
}
