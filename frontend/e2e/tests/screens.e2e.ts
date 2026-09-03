import { expect, test } from '../support/fixtures';
import { expectNoSeriousA11yViolations } from '../support/a11y';
import { expectNoConsoleErrors, expectNoHorizontalScroll } from '../support/layout';
import { readAdminCredentials } from '../support/config';
import { gotoAdminSection } from '../support/adminNav';
import { UI, loginAsAdmin, openSchoolLink, registerStudent, uniqueStudentName } from '../support/flow';
import type { Page } from '@playwright/test';

/**
 * Har asosiy ekranda uch tekshiruv birga (`docs/12` 7–8-bo'lim):
 *  (a) `axe` — jiddiy (`serious`/`critical`) qulaylik buzilishi yo'q;
 *  (b) gorizontal scroll yo'q — proyekt kengligida (390px va 1440px);
 *  (c) konsolda xato yo'q.
 * Ekranlar bitta sessiyada ketma-ket aylanib chiqiladi — bu 190 savolli oqimni
 * takrorlamaydi va to'plamni tez saqlaydi.
 */
async function checkScreen(page: Page, errors: string[], name: string): Promise<void> {
  await expectNoSeriousA11yViolations(page, name);
  await expectNoHorizontalScroll(page, name);
  expectNoConsoleErrors(errors, name);
  // Ro'yxat ekrandan ekranga o'tganda tozalanadi — aks holda bitta xato keyingi barcha
  // ekranlarda qayta hisoblanadi va aybdor ekran noto'g'ri ko'rsatiladi.
  errors.length = 0;
}

test('ommaviy ekranlar: qulaylik, tartib va konsol toza', async ({
  page,
  school,
  consoleErrors,
}) => {
  await openSchoolLink(page, school);
  await expect(page.getByRole('button', { name: UI.start })).toBeVisible();
  await checkScreen(page, consoleErrors, 'E-1 Landing');

  await page.getByRole('button', { name: UI.start }).click();
  await expect(page.getByRole('heading', { name: "Ro'yxatdan o'tish" })).toBeVisible();
  await checkScreen(page, consoleErrors, 'E-2 Anketa');

  await registerStudent(page, school, { fullName: uniqueStudentName('a11y') });
  await expect(page.getByRole('radiogroup').first()).toBeVisible();
  await checkScreen(page, consoleErrors, 'E-3 Test sahifasi');
});

test('admin ekranlari: qulaylik, tartib va konsol toza', async ({
  page,
  school,
  consoleErrors,
}) => {
  const credentials = readAdminCredentials();

  await page.goto('/admin/login');
  await expect(page.getByRole('button', { name: 'Kirish' })).toBeVisible();
  await checkScreen(page, consoleErrors, 'A-1 Kirish');

  await loginAsAdmin(page, credentials);
  await checkScreen(page, consoleErrors, 'A-2 Boshqaruv paneli');

  await gotoAdminSection(page, 'Maktablar');
  await expect(page.getByRole('table')).toBeVisible();
  await checkScreen(page, consoleErrors, 'A-3 Maktablar');

  await page.goto(`/admin/schools/${school.id}`);
  await expect(page.getByRole('heading', { name: school.name })).toBeVisible();
  await checkScreen(page, consoleErrors, 'A-3.1 Maktab sahifasi');

  await gotoAdminSection(page, "O'quvchilar");
  await checkScreen(page, consoleErrors, "A-4 O'quvchilar");

  await gotoAdminSection(page, 'Testlar katalogi');
  await expect(page.getByRole('heading', { name: 'Tizim metodikalari' })).toBeVisible();
  await checkScreen(page, consoleErrors, 'A-7 Testlar katalogi');
});
