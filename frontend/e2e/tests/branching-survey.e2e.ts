import { test, expect } from '../support/fixtures';
import { expectNoSeriousA11yViolations } from '../support/a11y';
import { waitForAnimationsToSettle } from '../support/animation';
import { expectNoConsoleErrors, expectNoHorizontalScroll } from '../support/layout';
import { UI, fillRegistration, typeAndVerifyFocus, uniqueStudentName } from '../support/flow';
import { assignTestToSchool, createAndPublishBranchingSurvey } from '../support/adminApi';
import type { Browser, Page } from '@playwright/test';
import { WEB_BASE_URL } from '../support/config';

/**
 * `docs/18` §8 (E2E qatori) — tarmoqlanuvchi so'rovnoma: 1.6 savolining UCHALA tarmog'i
 * (A/B/C) va "Boshqa (kiriting)" naqshi haqiqiy brauzerda (390px VA 1440px — `playwright.config.ts`
 * ikkala loyihada ham shu faylni ishga tushiradi, qo'shimcha kod shart emas).
 *
 * So'rovnoma admin API orqali (UI konstruktori emas — u alohida, og'ir oqim, mavjud
 * `AdminCatalogBranchingImportEndpointTests.cs` bilan bir xil ketma-ketlik) tuziladi:
 *
 * ```
 * S1 (hamma)      Q1 [FILTR] SingleChoice — A(1) / B(2) / C(3)
 * S2A (Q1=1)      Q2A ShortText, Q2A_PHONE Phone, Q2A_LONG LongText
 * S2B (Q1=2)      Q2B SingleChoice — "Oddiy javob"(1) / "Boshqa (kiriting)"(99)
 *                 Q2B_OTHER ShortText — FAQAT Q2B=99 da ko'rinadi (B-6 naqshi)
 * S2C (Q1=3)      Q2C ShortText
 * ```
 *
 * S2A ning uchta matn savoli (`ShortText`/`Phone`/`LongText`) ATAYLAB
 * `typeAndVerifyFocus` (`support/flow.ts`) bilan HARFMA-HARF to'ldiriladi — P52 jonli
 * bloklovchisi (6f81dc0) qamrovi: `.fill()` bu sinf xatosini (fokus o'g'irlanishi)
 * UMUMAN sinamaydi.
 */
const SURVEY_CODE_PREFIX = 'E2E-BRANCH';

function surveyDefinition(code: string) {
  return {
    code,
    nameUz: 'E2E tarmoqlanuvchi so\'rovnoma',
    descriptionUz: null,
    estimatedMinutes: 3,
    sections: [
      { code: 'S1', titleUz: 'Boshlang\'ich savol', displayOrder: 1, visibility: null },
      {
        code: 'S2A',
        titleUz: 'A tarmog\'i',
        displayOrder: 2,
        visibility: { match: 'All' as const, conditions: [{ questionCode: 'Q1', operator: 'Equals', values: [1] }] },
      },
      {
        code: 'S2B',
        titleUz: 'B tarmog\'i',
        displayOrder: 3,
        visibility: { match: 'All' as const, conditions: [{ questionCode: 'Q1', operator: 'Equals', values: [2] }] },
      },
      {
        code: 'S2C',
        titleUz: 'C tarmog\'i',
        displayOrder: 4,
        visibility: { match: 'All' as const, conditions: [{ questionCode: 'Q1', operator: 'Equals', values: [3] }] },
      },
    ],
    questions: [
      {
        code: 'Q1',
        order: 1,
        sectionCode: 'S1',
        textUz: 'Qo\'shimcha kursga qatnashasizmi?',
        type: 'SingleChoice',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        options: [
          { textUz: 'A - birinchi guruh', value: 1, displayOrder: 1 },
          { textUz: 'B - ikkinchi guruh', value: 2, displayOrder: 2 },
          { textUz: 'C - uchinchi guruh', value: 3, displayOrder: 3 },
        ],
      },
      {
        code: 'Q2A',
        order: 2,
        sectionCode: 'S2A',
        textUz: 'A guruhi uchun savol',
        type: 'ShortText',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        maxLength: 200,
      },
      {
        code: 'Q2A_PHONE',
        order: 3,
        sectionCode: 'S2A',
        textUz: 'Aloqa uchun telefon raqamingiz',
        type: 'Phone',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
      },
      {
        code: 'Q2A_LONG',
        order: 4,
        sectionCode: 'S2A',
        textUz: 'Batafsil fikringizni yozing',
        type: 'LongText',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        maxLength: 500,
      },
      {
        code: 'Q2B',
        order: 5,
        sectionCode: 'S2B',
        textUz: 'B guruhi uchun savol',
        type: 'SingleChoice',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        options: [
          { textUz: 'Oddiy javob', value: 1, displayOrder: 1 },
          { textUz: 'Boshqa (kiriting)', value: 99, displayOrder: 2 },
        ],
      },
      {
        code: 'Q2B_OTHER',
        order: 6,
        sectionCode: 'S2B',
        textUz: 'Iltimos, aniqlashtiring',
        type: 'ShortText',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        maxLength: 200,
        visibility: { match: 'All' as const, conditions: [{ questionCode: 'Q2B', operator: 'Equals', values: [99] }] },
      },
      {
        code: 'Q2C',
        order: 7,
        sectionCode: 'S2C',
        textUz: 'C guruhi uchun savol',
        type: 'ShortText',
        scale: 'SURVEY',
        direction: 1,
        weight: 1,
        isRequired: true,
        maxLength: 200,
      },
    ],
  };
}

async function checkScreen(page: Page, errors: string[], name: string): Promise<void> {
  await waitForAnimationsToSettle(page);
  await expectNoSeriousA11yViolations(page, name);
  await expectNoHorizontalScroll(page, name);
  expectNoConsoleErrors(errors, name);
  errors.length = 0;
}

/**
 * Landingda ikkita dastur bor (tizim standart dasturi + shu test uchun yaratilgan
 * "Assigned" dastur, `docs/18` bog'liq emas — E2E izolyatsiyasi) — dastur kartasi
 * TANLANMAGUNCHA "Boshlash" o'chirilgan turadi (`LandingPage`: `disabled={!selectedProgramCode}`).
 */
async function startRegistrationForProgram(page: Page, schoolPath: string, programNameUz: string): Promise<void> {
  await page.goto(schoolPath);
  const programRadio = page.getByRole('radiogroup', { name: 'Dasturni tanlang' }).getByRole('radio', {
    name: programNameUz,
  });
  await expect(programRadio).toBeVisible();
  await programRadio.click();
  await expect(programRadio).toHaveAttribute('aria-checked', 'true');
  await page.getByRole('button', { name: UI.start }).click();
  await expect(page.getByRole('heading', { name: "Ro'yxatdan o'tish" })).toBeVisible();
}

/**
 * Har filial (A/B/C) — MUSTAQIL "o'quvchi" bo'lgani uchun O'ZINING brauzer kontekstida
 * ishlaydi (yangi `localStorage`, shu jumladan sessiya tokeni). QA topilmasi: ilgari uchala
 * filial BITTA `page`da ketma-ket ro'yxatdan o'tkazilgan edi — `FinishPage`ning fon
 * `POST .../sessions/complete` so'rovi (`har mount'da chaqiriladi`, o'z izohiga qarang)
 * ba'zan KEYINGI filial ro'yxatdan o'tgandan keyin yetib borardi va o'sha paytdagi
 * (`localStorage` bitta origin ichida umumiy) YANGI sessiya tokeni bilan yuborilib,
 * begona sessiya uchun `409` (`ASSESSMENT_TESTS_NOT_DONE`) bilan konsolni ifloslardi.
 * Bu — sof E2E poyga sharoiti (haqiqiy foydalanuvchi soniyalar ichida 3 marta ro'yxatdan
 * o'tmaydi), P52 mahsulot kodi bilan bog'liq emas; alohida kontekst uni ILDIZIDAN yo'q qiladi.
 */
async function openIsolatedPage(browser: Browser, clientIp: string): Promise<{ page: Page; close: () => Promise<void> }> {
  const context = await browser.newContext();
  await context.addCookies([{ name: 'e2e_client_ip', value: clientIp, url: WEB_BASE_URL }]);
  const page = await context.newPage();
  return { page, close: () => context.close() };
}

test('tarmoqlanuvchi so\'rovnoma: 1.6 ning A/B/C tarmoqlari va "Boshqa (kiriting)" naqshi', async ({
  browser,
  school,
  adminToken,
  clientIp,
}, testInfo) => {
  const code = `${SURVEY_CODE_PREFIX}-${String(testInfo.workerIndex)}-${String(Date.now()).slice(-6)}`;
  const survey = surveyDefinition(code);

  const created = await createAndPublishBranchingSurvey(adminToken, clientIp, survey);
  // 2026-09-23: dastur emas — test to'g'ridan-to'g'ri maktabga biriktiriladi (landing'da test
  // nomi bilan alohida variant bo'lib chiqadi).
  const assignment = await assignTestToSchool(adminToken, clientIp, {
    testDefinitionId: created.id,
    schoolId: school.id,
  });
  expect(assignment.isConfigured).toBe(true);
  expect(assignment.schoolIds).toEqual([school.id]);

  // --- Filial A: Q1=1 → faqat S2A/Q2A ko'rinadi, S2B/S2C butunlay yashirin. ---
  {
    const { page, close } = await openIsolatedPage(browser, clientIp);
    const consoleErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });

    await startRegistrationForProgram(page, school.publicPath, survey.nameUz);
    await fillRegistration(page, { fullName: uniqueStudentName('branch-a') });

    await expect(page.getByRole('heading', { name: "Boshlang'ich savol" })).toBeVisible();
    await checkScreen(page, consoleErrors, 'BRANCH-1 Boshlang\'ich savol');

    await page.getByRole('radio', { name: 'A - birinchi guruh' }).check({ force: true });
    await page.getByRole('button', { name: UI.next }).click();

    // S2A ko'rinadi — Q2A matn savoli.
    await expect(page.getByRole('heading', { name: "A tarmog'i" })).toBeVisible();
    await expect(page.getByLabel('A guruhi uchun savol')).toBeVisible();
    // B/C tarmog'i savollari sahifada UMUMAN yo'q (yashirin bo'lim — DOMga chiqmaydi).
    await expect(page.getByLabel('B guruhi uchun savol')).toHaveCount(0);
    await expect(page.getByLabel('C guruhi uchun savol')).toHaveCount(0);
    await checkScreen(page, consoleErrors, 'BRANCH-2 A tarmog\'i');

    // P52 QA topilmasi (6f81dc0): matn maydoniga bitta harf yozilgach fokus o'g'irlanib
    // qolgan bloklovchi — shu sabab BU YERDA `.fill()` EMAS, HAQIQIY harfma-harf yozish
    // ishlatiladi, uchala matn turi (ShortText/Phone/LongText) uchun ham.
    await typeAndVerifyFocus(
      page.getByLabel('A guruhi uchun savol'),
      'Matematika bo\'yicha qo\'shimcha kurs',
    );
    await typeAndVerifyFocus(page.getByLabel('Aloqa uchun telefon raqamingiz'), '+998901234567');
    await typeAndVerifyFocus(
      page.getByLabel('Batafsil fikringizni yozing'),
      'Bu yerda batafsil fikrimni yozyapman, bu uzun matn maydoni sinovi uchun.',
    );
    await page.getByRole('button', { name: UI.next }).click();

    await expect(page).toHaveURL(new RegExp(`/t/${school.slug}/finish$`));
    // Sof `Survey` dastur (shaxsiyat batareyasi yo'q) — `pages.finish.heading` EMAS,
    // `publicAssessment.survey.thanksHeading` ("Rahmat!") ko'rsatiladi (`docs/06` 8-bo'lim).
    await expect(page.getByRole('heading', { name: 'Rahmat!' })).toBeVisible();

    await close();
  }

  // --- Filial B: Q1=2 → S2B, "Boshqa" tanlansa Q2B_OTHER paydo bo'ladi (B-6 naqshi). ---
  {
    const { page, close } = await openIsolatedPage(browser, clientIp);
    const consoleErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });

    await startRegistrationForProgram(page, school.publicPath, survey.nameUz);
    await fillRegistration(page, { fullName: uniqueStudentName('branch-b') });

    await page.getByRole('radio', { name: 'B - ikkinchi guruh' }).check({ force: true });
    await page.getByRole('button', { name: UI.next }).click();

    await expect(page.getByRole('heading', { name: "B tarmog'i" })).toBeVisible();
    // "Boshqa" tanlanmagunicha aniqlashtirish maydoni YO'Q.
    await expect(page.getByLabel('Iltimos, aniqlashtiring')).toHaveCount(0);

    await page.getByRole('radio', { name: 'Boshqa (kiriting)' }).check({ force: true });
    // Javob o'zgarganda ko'rinish DARHOL qayta hisoblanadi (`docs/18` §6.2) — serverga
    // yuborilishini kutmasdan matn maydoni paydo bo'lishi kerak.
    await expect(page.getByLabel('Iltimos, aniqlashtiring')).toBeVisible();
    await checkScreen(page, consoleErrors, 'BRANCH-3 B tarmog\'i — Boshqa ochilgan');

    // Fikrni o'zgartirsa ("Oddiy javob"ga qaytsa) — aniqlashtirish maydoni yana yashiriladi.
    await page.getByRole('radio', { name: 'Oddiy javob' }).check({ force: true });
    await expect(page.getByLabel('Iltimos, aniqlashtiring')).toHaveCount(0);

    await page.getByRole('radio', { name: 'Boshqa (kiriting)' }).check({ force: true });
    await page.getByLabel('Iltimos, aniqlashtiring').fill('Ingliz tili kursi');
    await page.getByRole('button', { name: UI.next }).click();

    await expect(page).toHaveURL(new RegExp(`/t/${school.slug}/finish$`));
    await expect(page.getByRole('heading', { name: 'Rahmat!' })).toBeVisible();

    await close();
  }

  // --- Filial C: Q1=3 → S2C, boshqa hech qanday tarmoq savoli ko'rinmaydi. ---
  {
    const { page, close } = await openIsolatedPage(browser, clientIp);
    const consoleErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });

    await startRegistrationForProgram(page, school.publicPath, survey.nameUz);
    await fillRegistration(page, { fullName: uniqueStudentName('branch-c') });

    await page.getByRole('radio', { name: 'C - uchinchi guruh' }).check({ force: true });
    await page.getByRole('button', { name: UI.next }).click();

    await expect(page.getByRole('heading', { name: "C tarmog'i" })).toBeVisible();
    await expect(page.getByLabel('A guruhi uchun savol')).toHaveCount(0);
    await expect(page.getByLabel('B guruhi uchun savol')).toHaveCount(0);
    await checkScreen(page, consoleErrors, 'BRANCH-4 C tarmog\'i');

    await page.getByLabel('C guruhi uchun savol').fill('Dizayn kursi');
    await page.getByRole('button', { name: UI.next }).click();

    await expect(page).toHaveURL(new RegExp(`/t/${school.slug}/finish$`));
    // Sof `Survey` dastur (shaxsiyat batareyasi yo'q) — `pages.finish.heading` EMAS,
    // `publicAssessment.survey.thanksHeading` ("Rahmat!") ko'rsatiladi (`docs/06` 8-bo'lim).
    await expect(page.getByRole('heading', { name: 'Rahmat!' })).toBeVisible();

    await close();
  }
});
