import { expect, test } from '../support/fixtures';
import { expectNoSeriousA11yViolations } from '../support/a11y';
import { waitForAnimationsToSettle } from '../support/animation';
import { expectNoConsoleErrors, expectNoHorizontalScroll } from '../support/layout';
import { readAdminCredentials } from '../support/config';
import { gotoAdminSection } from '../support/adminNav';
import {
  UI,
  loginAsAdmin,
  openSchoolLink,
  registerStudent,
  uniqueStudentName,
} from '../support/flow';
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
  // Kirish animatsiyasi tugaguncha kutiladi — sabab `support/animation.ts` izohida
  // (yarim shaffof matn `axe` ga kontrast buzilishi bo'lib ko'rinadi).
  await waitForAnimationsToSettle(page);
  await expectNoSeriousA11yViolations(page, name);
  await expectNoHorizontalScroll(page, name);
  expectNoConsoleErrors(errors, name);
  // Ro'yxat ekrandan ekranga o'tganda tozalanadi — aks holda bitta xato keyingi barcha
  // ekranlarda qayta hisoblanadi va aybdor ekran noto'g'ri ko'rsatiladi.
  errors.length = 0;
}

/**
 * P45 tanishtiruv qatlami — `/`, `/metodika`, `/biz-haqimizda`, `/aloqa`. Bu sahifalar
 * sessiyaga umuman bog'liq emas, shu sabab `school` fixture'i KERAK EMAS (test tezroq
 * ishlaydi va bo'sh maktab yaratmaydi). Ilgari `/` 404 qaytarardi — endi bu qatlam ham
 * qolgan ekranlar bilan bir xil talab ostida.
 */
const MARKETING_SCREENS = [
  { path: '/', name: 'M-1 Bosh sahifa' },
  { path: '/metodika', name: 'M-2 Metodika' },
  // Tip sahifasi (`/metodika/:kod`) — `/metodika` dagi 16 tip bo'limidan ochiladi. `INTJ`
  // seed'dagi 16 koddan biri (`SeedData/type-catalog.json`), shu sabab ekran har doim to'la.
  { path: '/metodika/intj', name: 'M-2.1 Tip sahifasi' },
  { path: '/biz-haqimizda', name: 'M-3 Biz haqimizda' },
  { path: '/aloqa', name: 'M-4 Aloqa' },
] as const;

test('tanishtiruv ekranlari: qulaylik, tartib va konsol toza', async ({ page, consoleErrors }) => {
  for (const screen of MARKETING_SCREENS) {
    await page.goto(screen.path);
    // Har sahifada aynan bitta `<h1>` — sahifa haqiqatan yuklanganini va sarlavha
    // ierarxiyasi buzilmaganini bir vaqtda tekshiradi.
    await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
    await checkScreen(page, consoleErrors, screen.name);
  }
});

/**
 * `/metodika` dagi 16 tip bo'limi HAQIQIY backend javobi bilan (`GET /api/public/type-catalog`,
 * `docs/07` 1.10-bo'lim) to'ladimi va karta tip sahifasiga olib boradimi. Bu — vitest bilan
 * qoplab bo'lmaydigan yagona joy: u yerda javob mock qilinadi, bu yerda esa seed qilingan
 * baza (`SeedData/type-catalog.json`, 16 yozuv) va ommaviy endpoint zanjirining o'zi sinaladi.
 */
test('tanishtiruv: metodika sahifasida 16 tip kartasi va tip sahifasi ochiladi', async ({
  page,
}) => {
  await page.goto('/metodika');

  const section = page.getByRole('region', { name: '16 ta shaxsiyat tipi' });
  await expect(section.getByRole('link')).toHaveCount(16);

  await section.getByRole('link').first().click();

  // Tip sahifasining kontenti — faqat tip TOPILGANDA chiziladi ("topilmadi" ekranida yo'q).
  await expect(page.getByRole('heading', { name: 'Kuchli tomonlar' })).toBeVisible();
  await expect(page.getByRole('heading', { name: "Kasb yo'nalishlari" })).toBeVisible();
});

/** Mobil menyu — `lg` dan kichik ekranda ochiladi, havola bosilganda o'z-o'zidan yopiladi. */
test('tanishtiruv mobil menyusi ochiladi va navigatsiya ishlaydi', async ({
  page,
  consoleErrors,
}) => {
  await page.goto('/');

  const menuButton = page.getByRole('button', { name: 'Menyuni ochish', exact: true });
  if (!(await menuButton.isVisible())) {
    // Desktop kengligida menyu tugmasi umuman yo'q — gorizontal navigatsiya ko'rinadi.
    await expect(page.getByRole('navigation', { name: 'Asosiy menyu' })).toBeVisible();
    return;
  }

  await menuButton.click();
  const drawer = page.getByRole('navigation', { name: 'Mobil menyu' });
  await expect(drawer).toBeVisible();
  await checkScreen(page, consoleErrors, 'M-1.1 Mobil menyu');

  await drawer.getByRole('link', { name: 'Metodika', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
  // Sahifa almashgach menyu o'z-o'zidan yopiladi (`MarketingLayout` dagi `openedAtPath`).
  await expect(drawer).toBeHidden();
});

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
