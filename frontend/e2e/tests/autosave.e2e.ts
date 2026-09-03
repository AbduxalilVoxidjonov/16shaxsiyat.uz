import { expect, test } from '../support/fixtures';
import {
  UI,
  answerFirstQuestions,
  completeCurrentTest,
  registerStudent,
  uniqueStudentName,
} from '../support/flow';

/**
 * E2E-2 — "test yarmida sahifa yangilansa javoblar joyida qolishi". Bu xatti-harakat
 * ilgari faqat unit test bilan qulflangan edi (`useAutosave`); bu yerda HAQIQIY brauzerda
 * (`localStorage` + tarmoq + sahifa qayta yuklanishi) tasdiqlanadi.
 *
 * 10 tadan kam javob beriladi (sahifa hajmi 10): `TestPage` yangilangandan keyin
 * `answered / pageSize` bo'yicha o'sha sahifaga qaytadi va javoblar ko'rinib turadi.
 */
test('E2E-2: test yarmida sahifa yangilansa javoblar joyida qoladi', async ({ page, school }) => {
  await registerStudent(page, school, { fullName: uniqueStudentName('autosave') });

  const firstQuestionId = await page
    .getByRole('radiogroup')
    .first()
    .getAttribute('data-question-id');

  // Autosave debounce (1.5s) tugab, paket serverga yetib borishini KUTAMIZ — qat'iy
  // `waitForTimeout` emas, aynan javobga kutish.
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST' &&
      response.url().includes('/answers') &&
      response.ok(),
  );
  await answerFirstQuestions(page, 6);
  await saved;

  await page.reload();

  await expect(page.getByRole('radiogroup').first()).toHaveAttribute(
    'data-question-id',
    firstQuestionId ?? '',
  );
  await expect(page.getByRole('radio', { checked: true })).toHaveCount(6);
});

/**
 * E2E-3 — oflayn rejim: banner chiqadi, javoblar mahalliy navbatda saqlanadi va ulanish
 * tiklanganda avtomatik yuboriladi (`docs/10` 4.2-bo'lim).
 */
test('E2E-3: oflaynda javoblar yo\'qolmaydi, onlayn bo\'lganda yuboriladi', async ({
  page,
  context,
  school,
}) => {
  await registerStudent(page, school, { fullName: uniqueStudentName('offline') });

  const firstQuestionId = await page
    .getByRole('radiogroup')
    .first()
    .getAttribute('data-question-id');

  await context.setOffline(true);
  await expect(page.getByText('Internet yo\'q — javoblaring saqlanmoqda')).toBeVisible();

  await answerFirstQuestions(page, 5);

  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === 'POST' &&
      response.url().includes('/answers') &&
      response.ok(),
  );
  await context.setOffline(false);
  await saved;

  await page.reload();

  await expect(page.getByRole('radiogroup').first()).toHaveAttribute(
    'data-question-id',
    firstQuestionId ?? '',
  );
  await expect(page.getByRole('radio', { checked: true })).toHaveCount(5);
});

/**
 * E2E-2b (P30-2 regressiyasi) — BLOKLOVCHI xato: oxirgi sahifadagi savollarga TEZ javob
 * berilib (klaviatura bilan ~1.5 s ichida) darhol "Keyingi" bosilganda
 * `POST /sessions/tests/{code}/complete` autosave paketidan OLDIN yetib borar edi va
 * backend `400 VALIDATION_ERROR (unansweredCount: 10)` qaytarardi — o'quvchi hamma
 * savolga javob bergan bo'lsa ham testni YAKUNLAY OLMASDI.
 *
 * Bu yerda ataylab HECH QANDAY kutish yo'q (`waitForSave: false` — `flow.ts` standarti):
 * javob beriladi va shu zahoti "Keyingi" bosiladi. Autosave debounce'i 1.5 s bo'lgani
 * uchun bosilgan paytda javoblar hali yo'lga chiqmagan bo'ladi — ya'ni poyga har safar
 * takrorlanadi, "ba'zan" emas.
 */
test('E2E-2b: oxirgi sahifada tez javob berib darhol "Keyingi" bosilsa blok yakunlanadi (P30-2)', async ({
  page,
  school,
}) => {
  const failedCompletes: string[] = [];
  page.on('response', (response) => {
    if (response.url().includes('/complete') && !response.ok()) {
      failedCompletes.push(`${String(response.status())} ${response.url()}`);
    }
  });

  await registerStudent(page, school, { fullName: uniqueStudentName('poyga') });
  await completeCurrentTest(page);

  await expect(page.getByRole('heading', { name: UI.blockDoneHeading })).toBeVisible();
  expect(failedCompletes).toEqual([]);
});
