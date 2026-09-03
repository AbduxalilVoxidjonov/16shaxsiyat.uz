import { expect, type Page } from '@playwright/test';
import { isKnownConsoleError, isKnownLayoutIssue } from './known-issues';

/**
 * Gorizontal scroll YO'Qligi — `docs/11` 4-bo'lim (mobil-birinchi). 390px da bitta juda
 * keng element butun sahifani yon tomonga siljitib yuboradi va bu jimgina o'tib ketadi.
 * 1px zaxira — sub-piksel yaxlitlashlari uchun.
 */
export async function expectNoHorizontalScroll(page: Page, context: string): Promise<void> {
  const overflow = await page.evaluate(() => {
    const element = document.documentElement;
    return {
      scrollWidth: element.scrollWidth,
      clientWidth: element.clientWidth,
      widest: Array.from(document.querySelectorAll<HTMLElement>('body *'))
        .filter((node) => node.getBoundingClientRect().right > element.clientWidth + 1)
        .slice(0, 3)
        .map((node) => `${node.tagName.toLowerCase()}.${node.className.toString().slice(0, 60)}`),
    };
  });

  if (isKnownLayoutIssue(context, page.viewportSize()?.width ?? 0)) return;

  if (process.env.E2E_A11Y_REPORT === '1' && overflow.scrollWidth > overflow.clientWidth + 1) {
    console.log(
      `[layout] ${context} :: scrollWidth ${String(overflow.scrollWidth)} > ` +
        `clientWidth ${String(overflow.clientWidth)} — ${overflow.widest.join(' | ')}`,
    );
    return;
  }

  expect(
    overflow.scrollWidth,
    `${context}: gorizontal scroll bor (scrollWidth ${String(overflow.scrollWidth)} > ` +
      `clientWidth ${String(overflow.clientWidth)}); chegaradan chiqqan elementlar: ` +
      `${overflow.widest.join(' | ') || 'aniqlanmadi'}`,
  ).toBeLessThanOrEqual(overflow.clientWidth + 1);
}

/** Sahifada `console.error`/ushlanmagan istisno yo'qligi. */
export function expectNoConsoleErrors(errors: readonly string[], context: string): void {
  if (process.env.E2E_A11Y_REPORT === '1') {
    for (const line of errors) console.log(`[console] ${context} :: ${line}`);
    return;
  }

  expect(
    errors.filter((message) => !isKnownConsoleError(message)),
    `${context}: konsolda xato`,
  ).toEqual([]);
}
