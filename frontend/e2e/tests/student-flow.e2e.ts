import { expect, test } from '../support/fixtures';
import { setSchoolActive } from '../support/adminApi';
import {
  UI,
  completeAllTests,
  fillRegistration,
  openSchoolLink,
  registerStudent,
  uniqueStudentName,
} from '../support/flow';

/**
 * E2E-1 / E2E-5 / E2E-10 — `docs/12` 8-bo'lim. Eng muhim yo'l: bu oqim buzilsa mahsulot
 * umuman ishlamaydi. `mobile-390` proyektida shu test E2E-10 (mobil viewport) ni qoplaydi.
 */
test('E2E-1: maktab havolasi → anketa → barcha bloklar → yakuniy ekran', async ({
  page,
  school,
}) => {
  const studentName = uniqueStudentName('oqim');

  await registerStudent(page, school, { fullName: studentName });
  await completeAllTests(page, school);

  await expect(page.getByRole('heading', { name: UI.finishHeading })).toBeVisible();
  // AI kaliti E2E stekida ataylab yo'q — natija o'quvchiga ko'rsatilmaydi (standart sozlama).
  await expect(page.getByText('Natijalar maktab psixologiga yuboriladi.')).toBeVisible();

  // E2E-5: yakunlangan sessiyadan keyin o'sha o'quvchi qayta ro'yxatdan o'ta olmaydi
  // (`409 DUPLICATE_ASSESSMENT`, `docs/07` 1.2-bo'lim). Sessiya bilan bog'liq mahalliy
  // holat tozalanadi — o'quvchi boshqa qurilmadan kirgani bilan bir xil holat.
  await page.evaluate(() => {
    localStorage.clear();
  });
  await openSchoolLink(page, school);
  await page.getByRole('button', { name: UI.start }).click();
  await fillRegistration(page, { fullName: studentName });

  await expect(page.getByRole('alert')).toContainText('Siz allaqachon testni topshirgansiz');
});

/** E2E-4 — nofaol maktab havolasi tushunarli xato ekranini ko'rsatadi (`410 SCHOOL_INACTIVE`). */
test('E2E-4: nofaol maktab havolasi — tushunarli xato ekrani', async ({
  page,
  school,
  adminToken,
  clientIp,
}) => {
  await setSchoolActive(adminToken, clientIp, school.id, false);

  await openSchoolLink(page, school);

  const alert = page.getByRole('alert');
  await expect(alert).toContainText('Test vaqtincha yopilgan');
  await expect(page.getByRole('button', { name: UI.start })).toHaveCount(0);
});

/**
 * E2E-5 (ikkinchi shakl) — tugallanmagan sessiyasi bor o'quvchi qayta kirsa yangi sessiya
 * OCHILMAYDI, o'sha sessiya davom ettiriladi (`resumed: true`, `docs/07` 1.2-bo'lim).
 */
test('E2E-5: tugallanmagan sessiyasi bor o\'quvchi o\'sha sessiyaga qaytadi', async ({
  page,
  school,
}) => {
  const studentName = uniqueStudentName('dublikat');

  await registerStudent(page, school, { fullName: studentName });
  const firstUrl = new URL(page.url()).pathname;

  await page.evaluate(() => {
    localStorage.clear();
  });
  await openSchoolLink(page, school);
  await page.getByRole('button', { name: UI.start }).click();
  await fillRegistration(page, { fullName: studentName });

  await expect(page.getByRole('progressbar')).toBeVisible();
  expect(new URL(page.url()).pathname).toBe(firstUrl);
  await expect(page.getByText('Boshlagan testingizni davom ettiramiz.')).toBeVisible();
});
