import { expect, test } from '../support/fixtures';
import { deleteSchoolsBySearch } from '../support/adminApi';
import { readAdminCredentials } from '../support/config';
import { gotoAdminSection } from '../support/adminNav';
import { loginAsAdmin } from '../support/flow';

/**
 * E2E-6 — superadmin oqimi: kirish → boshqaruv paneli → maktab yaratish → maktab ichiga
 * kirish → katalogda tizim metodikasini ochish.
 *
 * Login/parol KODDA EMAS — `E2E_ADMIN_USERNAME`/`E2E_ADMIN_PASSWORD` muhit
 * o'zgaruvchilaridan (`.env.example` ga qarang); shu qiymatlar bilan E2E bazasiga
 * superadmin seed qilinadi.
 */
test('E2E-6: login → panel → maktab yaratish → maktab sahifasi → katalog', async ({
  page,
  adminToken,
  clientIp,
}) => {
  const credentials = readAdminCredentials();
  const schoolName = `E2E UI ${String(Date.now()).slice(-8)}${Math.random().toString(36).slice(2, 5)}`;

  try {
    await loginAsAdmin(page, credentials);

    await gotoAdminSection(page, 'Maktablar');

    await page.getByRole('button', { name: 'Yangi maktab', exact: true }).click();

    const dialog = page.getByRole('dialog', { name: 'Yangi maktab' });
    await expect(dialog).toBeVisible();

    // Forma maydonlari ATAYLAB dialog ichida qidiriladi: filtrlar panelida ham "Viloyat"
    // yorlig'i bor va sahifa bo'yicha qidiruv "strict mode violation" beradi.
    //
    // `toPass` bloki — P30-9 topilmasi tufayli: dialog ochilgandan keyin forma
    // BIR MARTA, aniq bo'lmagan onda qayta tiklanadi (`reset`) va o'sha ondagacha
    // kiritilgan qiymatlar jimgina o'chib ketadi (ikkala viewport'da ham kuzatildi —
    // ba'zan "Nomi", ba'zan "Viloyat" bo'shab qoladi). Shu sabab to'ldirishdan keyin
    // qiymatlar TEKSHIRILADI va kerak bo'lsa qayta yoziladi.
    await expect(async () => {
      await dialog.getByLabel('Nomi').fill(schoolName);
      await dialog.getByLabel('Viloyat').selectOption("Farg'ona");
      await dialog.getByLabel('Tuman').fill("Qo'qon");

      await expect(dialog.getByLabel('Nomi')).toHaveValue(schoolName, { timeout: 2000 });
      await expect(dialog.getByLabel('Viloyat')).toHaveValue("Farg'ona", { timeout: 2000 });
      await expect(dialog.getByLabel('Tuman')).toHaveValue("Qo'qon", { timeout: 2000 });
    }).toPass({ timeout: 20_000 });

    await dialog.getByRole('button', { name: 'Yaratish' }).press('Enter');

    await expect(page.getByText('Maktab yaratildi')).toBeVisible();

    // Yangi maktab ro'yxatda ko'rinadi va uning ichki sahifasi ochiladi.
    const row = page.getByRole('link', { name: schoolName });
    await expect(row).toBeVisible();
    await row.click();

    await expect(page.getByRole('heading', { name: schoolName })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Ishtirok statistikasi' })).toBeVisible();

    // Katalog: tizim metodikasi ochiladi (`isSystem` — qulflangan, faqat ko'rish/tahrirlash).
    await gotoAdminSection(page, 'Testlar katalogi');
    await expect(page.getByRole('heading', { name: 'Tizim metodikalari' })).toBeVisible();

    await page.getByRole('button', { name: /MBTI16/ }).click();
    await expect(page.getByText('Bu tizim metodikasi.')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Savollar' })).toBeVisible();
  } finally {
    await deleteSchoolsBySearch(adminToken, clientIp, schoolName);
  }
});
