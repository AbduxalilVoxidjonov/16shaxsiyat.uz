import { expect, type Locator, type Page } from '@playwright/test';
import type { E2ESchool } from './adminApi';

export interface RegistrationInput {
  fullName: string;
  /** Tug'ilgan yil — standart holatda 16 yosh (`registrationSchema`: 6–20 oralig'i). */
  birthYear?: number;
  grade?: string;
  classLetter?: string;
  phone?: string;
}

/** `pages.landing.startCta` / `pages.testDone.continueCta` va h.k. — o'zbekcha ko'rinadigan matn. */
export const UI = {
  start: 'Boshlash',
  submitRegistration: 'Testni boshlash',
  next: 'Keyingi',
  back: 'Orqaga',
  continueBlock: 'Davom etish',
  finishHeading: 'Rahmat! Barcha savollarga javob berdingiz.',
  blockDoneHeading: 'Ajoyib! Blok tugadi',
} as const;

export function uniqueStudentName(seed: string): string {
  const suffix = `${String(Date.now()).slice(-6)}${Math.random().toString(36).slice(2, 5)}`;
  return `Testov Talaba ${seed} ${suffix}`;
}

/** Maktab havolasini ochadi (`/t/:slug?k=...`). */
export async function openSchoolLink(page: Page, school: E2ESchool): Promise<void> {
  await page.goto(school.publicPath);
}

/** Anketani to'ldirib yuboradi; test sahifasiga o'tishini kutadi. */
export async function fillRegistration(page: Page, input: RegistrationInput): Promise<void> {
  const birthYear = input.birthYear ?? new Date().getFullYear() - 16;

  await page.getByLabel('F.I.Sh.').fill(input.fullName);
  await page.getByLabel("Tug'ilgan kun").selectOption('15');
  await page.getByLabel("Tug'ilgan oy").selectOption('5');
  await page.getByLabel("Tug'ilgan yil").selectOption(String(birthYear));

  // Radio `sr-only` (vizual yashirin) — bosish yorliq ustida bo'ladi, shuning uchun
  // `force` bilan (aks holda Playwright "element intercepts pointer events" deydi).
  const gender = page.getByRole('radio', { name: "O'g'il bola" });
  await gender.check({ force: true });
  await expect(gender).toBeChecked();

  await page.getByLabel('Sinf', { exact: true }).selectOption(input.grade ?? '9');
  await page.getByLabel('Sinf harfi').fill(input.classLetter ?? 'B');
  // 2026-09-23 egasi qarori: standartda ota-ona telefoni MAJBURIY (birinchi), shaxsiy raqam ixtiyoriy.
  await page.getByLabel('Ota-ona telefoni', { exact: true }).fill(input.phone ?? '901234567');
  await page.getByLabel("Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman").check();

  await page.getByRole('button', { name: UI.submitRegistration }).click();
}

/**
 * Matn maydoniga HAQIQIY foydalanuvchi kabi HARFMA-HARF yozadi va yozib bo'lgach fokus
 * O'SHA maydonda qolganini tasdiqlaydi.
 *
 * NEGA `fill()` EMAS: P52 jonli bloklovchisi (6f81dc0) aynan shu ko'r nuqtadan o'tib
 * ketgan edi — `TestPage.tsx`dagi bo'lim sarlavhasiga fokus ko'chiruvchi effektning
 * bog'liqligida beqaror obyekt (`visibility`, `draftAnswers`ga tayanadigan `useMemo`, HAR
 * BOSILGAN HARFDA yangi obyekt) turgani sabab matn maydoniga bitta harfdan keyin fokus
 * o'g'irlanib qolardi. Playwright `fill()` qiymatni BIR MARTA (CDP `Input.insertText`)
 * qo'yadi — na harfma-harf yozishni, na fokus xatti-harakatini sinaydi, shu sabab bu
 * sinfdagi regressiyani UMUMAN ushlamaydi. `pressSequentially` esa har harfni HAQIQIY
 * klaviatura hodisasi sifatida yuboradi — fokus boshqa joyga ko'chib ketsa, keyingi
 * harflar maydonga YETIB BORMAYDI va quyidagi `toHaveValue` tekshiruvi yiqiladi.
 */
export async function typeAndVerifyFocus(locator: Locator, text: string): Promise<void> {
  await locator.click();
  await locator.pressSequentially(text, { delay: 30 });
  await expect(locator).toHaveValue(text);
  await expect(locator).toBeFocused();
}

/** Landing → anketa → birinchi test bloki. */
export async function registerStudent(
  page: Page,
  school: E2ESchool,
  input: RegistrationInput,
): Promise<void> {
  await openSchoolLink(page, school);
  await page.getByRole('button', { name: UI.start }).click();
  await expect(page.getByRole('heading', { name: "Ro'yxatdan o'tish" })).toBeVisible();
  await fillRegistration(page, input);
  await expect(page.getByRole('progressbar')).toBeVisible();
}

export interface AnswerOptions {
  /**
   * `true` — javoblar serverga yetib borishi KUTILADI (autosave javobiga, qat'iy
   * `waitForTimeout` emas). Standart `false`: o'quvchining haqiqiy tezligi shunday —
   * javob beriladi va DARHOL "Keyingi" bosiladi. Ilova endi `complete` so'rovini
   * autosave `flush()` tugagunicha yubormaydi (P30-2 tuzatildi), shu sabab bu yerda
   * sun'iy kutish kerak emas — aksincha, kutmaslik regressiyani ushlab qoladi.
   */
  waitForSave?: boolean;
}

/**
 * Joriy sahifadagi barcha savollarga javob beradi — 190 ta savolni sichqoncha bilan
 * bosib chiqmaslik uchun klaviatura yo'li ishlatiladi (`LikertQuestion`: `1..5` javob
 * tanlaydi va fokus keyingi savolga o'tadi — `docs/10` 4.3-bo'lim).
 */
export async function answerVisibleQuestions(
  page: Page,
  { waitForSave = false }: AnswerOptions = {},
): Promise<void> {
  const groups = page.getByRole('radiogroup');
  await expect(groups.first()).toBeVisible();
  const count = await groups.count();

  const saved = waitForSave
    ? page.waitForResponse(
        (response) =>
          response.request().method() === 'POST' &&
          response.url().includes('/answers') &&
          response.ok(),
      )
    : null;

  await groups.first().focus();
  for (let index = 0; index < count; index += 1) {
    await page.keyboard.press(String(1 + (index % 5)));
  }

  await expect(page.getByRole('radio', { checked: true })).toHaveCount(count);
  if (saved) await saved;
}

/**
 * Sahifadagi dastlabki `count` ta savolga javob beradi (qolganlari javobsiz qoladi) —
 * "sahifa yangilansa javoblar joyida" testi uchun: javoblar soni sahifa hajmidan kichik
 * bo'lsa, `TestPage` o'sha sahifaga qaytadi.
 */
export async function answerFirstQuestions(page: Page, count: number): Promise<void> {
  const groups = page.getByRole('radiogroup');
  await expect(groups.first()).toBeVisible();

  await groups.first().focus();
  for (let index = 0; index < count; index += 1) {
    await page.keyboard.press(String(1 + (index % 5)));
  }

  await expect(page.getByRole('radio', { checked: true })).toHaveCount(count);
}

/**
 * Joriy test blokini oxirigacha yechadi. Sahifa almashganini `data-question-id`
 * (ilovada allaqachon mavjud atribut) o'zgarishi bilan kutadi — qat'iy `waitForTimeout`
 * ishlatilmaydi.
 */
export async function completeCurrentTest(page: Page, options: AnswerOptions = {}): Promise<void> {
  const testPath = new URL(page.url()).pathname;

  for (let guard = 0; guard < 40; guard += 1) {
    const firstGroup = page.getByRole('radiogroup').first();
    await expect(firstGroup).toBeVisible();
    const firstQuestionId = await firstGroup.getAttribute('data-question-id');

    await answerVisibleQuestions(page, options);
    await page.getByRole('button', { name: UI.next }).click();

    await expect
      .poll(async () => {
        if (new URL(page.url()).pathname !== testPath) return 'left';
        const current = await page
          .getByRole('radiogroup')
          .first()
          .getAttribute('data-question-id')
          .catch(() => null);
        return current === firstQuestionId ? 'same' : 'changed';
      })
      .not.toBe('same');

    if (new URL(page.url()).pathname !== testPath) return;
  }

  throw new Error(`Test bloki 40 sahifadan keyin ham tugamadi: ${testPath}`);
}

/** Barcha bloklarni ketma-ket yechib, yakuniy ekranga chiqadi. */
export async function completeAllTests(page: Page, school: E2ESchool): Promise<void> {
  for (let guard = 0; guard < 10; guard += 1) {
    await completeCurrentTest(page);

    const pathname = new URL(page.url()).pathname;
    if (pathname === `/t/${school.slug}/finish`) return;

    await expect(page.getByRole('heading', { name: UI.blockDoneHeading })).toBeVisible();
    await page.getByRole('button', { name: UI.continueBlock }).click();
    await expect(page.getByRole('progressbar')).toBeVisible();
  }

  throw new Error('Bloklar 10 marta almashgandan keyin ham yakuniy ekranga chiqmadi.');
}

/**
 * Superadmin panelga UI orqali kiradi (parol muhit o'zgaruvchisidan, kodda emas).
 *
 * Xato holatida QAYTA URINADI — bu P30-1 topilmasining oqibati: ikkita login bir vaqtda
 * kelsa backend `409 CONCURRENCY_CONFLICT` qaytaradi va UI umumiy xato ko'rsatadi.
 * Kutish qat'iy emas — "panel sarlavhasi YOKI xato xabari" holatiga kutiladi.
 */
export async function loginAsAdmin(
  page: Page,
  credentials: { username: string; password: string },
  attempts = 3,
): Promise<void> {
  await page.goto('/admin/login');
  await page.getByLabel('Login').fill(credentials.username);
  await page.getByLabel('Parol').fill(credentials.password);

  // `exact: true` MUHIM: login sahifasining o'z sarlavhasi "Boshqaruv paneliga kirish"
  // bo'lib, `getByRole` ning standart QISM SATR moslashuvida "Boshqaruv paneli" unga ham
  // to'g'ri kelardi va login muvaffaqiyatli deb hisoblanardi (sahifa esa o'zgarmagan).
  const dashboard = page.getByRole('heading', { name: 'Boshqaruv paneli', level: 1, exact: true });
  const formError = page.getByRole('alert');

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    await page.getByRole('button', { name: 'Kirish' }).click();
    await expect(dashboard.or(formError).first()).toBeVisible();
    if (await dashboard.isVisible()) return;
  }

  await expect(dashboard).toBeVisible();
}
