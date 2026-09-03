import { expect, type Page } from '@playwright/test';

/**
 * Admin bo'limiga o'tish. Mobil (390px) da navigatsiya drawer ichida — "Menyuni ochish"
 * tugmasi (`md:hidden`) ko'rinib turgani mobil ekanini bildiradi; shu sabab bitta
 * yordamchi ikkala viewport'da ham ishlaydi.
 *
 * `exact: true` MAJBURIY: `getByRole(..., { name })` standart holatda QISM SATRNI
 * qidiradi va nomlar bir-birini yutib yuboradi (masalan "Boshqaruv paneli" login
 * sahifasidagi "Boshqaruv paneliga kirish" sarlavhasiga ham mos keladi).
 *
 * Havola SICHQONCHA bilan bosiladi — ataylab: P30-8 tuzatilgandan keyin (mobil drawer
 * `index.css` dagi `dialog-drawer-start` istisnosi bilan chap chetga yopishadi) bu qadam
 * regressiya himoyasi bo'lib qoladi. Drawer yana markazga tushsa yoki ekrandan chiqib
 * ketsa, `click()` "element ko'rinmayapti / boshqa element ustida" xatosi bilan yiqiladi.
 * Ilgari bu yerda `press('Enter')` workaround'i bor edi — u aynan o'sha buzilishni
 * yashirardi.
 */
export async function gotoAdminSection(page: Page, linkName: string): Promise<void> {
  const menuButton = page.getByRole('button', { name: 'Menyuni ochish', exact: true });
  const isMobile = await menuButton.isVisible();
  if (isMobile) {
    await menuButton.click();
  }

  const link = page.getByRole('link', { name: linkName, exact: true }).first();
  await expect(link).toBeVisible();
  await link.click();

  await expect(page.getByRole('heading', { name: linkName, level: 1, exact: true })).toBeVisible();
}
