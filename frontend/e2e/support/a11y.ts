import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';
import { isKnownA11yIssue } from './known-issues';

/**
 * Qulaylik (a11y) tekshiruvi — `docs/12` 7-bo'lim. Faqat JIDDIY buzilishlar testni
 * yiqitadi (`serious`/`critical`): `minor`/`moderate` topilmalar odatda kontrast yoki
 * "best practice" tavsiyalari bo'lib, ular alohida ish sifatida qilinadi va E2E'ni
 * shovqinga aylantirmasligi kerak.
 */
export async function expectNoSeriousA11yViolations(page: Page, context: string): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  const serious = results.violations.filter(
    (violation) =>
      (violation.impact === 'serious' || violation.impact === 'critical') &&
      (process.env.E2E_A11Y_REPORT === '1' || !isKnownA11yIssue(context, violation.id)),
  );

  const summary = serious.map(
    (violation) =>
      `${violation.id} (${violation.impact ?? '?'}) — ${violation.help}; ` +
      `${String(violation.nodes.length)} ta element: ${violation.nodes
        .slice(0, 3)
        .map((node) => node.target.join(' '))
        .join(' | ')}`,
  );

  if (process.env.E2E_A11Y_REPORT === '1') {
    // Audit rejimi: testni yiqitmasdan barcha ekranlardagi topilmalarni yig'ish uchun.
    for (const line of summary) console.log(`[axe] ${context} :: ${line}`);
    return;
  }

  expect(summary, `axe: ${context}`).toEqual([]);
}
