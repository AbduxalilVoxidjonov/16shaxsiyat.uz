import { test as base, expect, type ConsoleMessage, type Page } from '@playwright/test';
import { WEB_BASE_URL } from './config';
import { createSchool, deleteSchoolCascade, type E2ESchool } from './adminApi';
import { readAdminToken } from './adminToken';

export interface WorkerFixtures {
  /** Superadmin `accessToken` — worker uchun bir marta olinadi (login limitini bezovta qilmaydi). */
  adminToken: string;
  /** Worker uchun barqaror "mijoz IP" — admin chaqiruvlari shu bo'lakda hisoblanadi. */
  workerIp: string;
}

export interface TestFixtures {
  /** Har test uchun unikal "mijoz IP" — tezlik cheklovi bo'laklari aralashmaydi. */
  clientIp: string;
  /** Shu testga tegishli yangi maktab; test oxirida o'quvchilari bilan o'chiriladi. */
  school: E2ESchool;
  /** Sahifada yozilgan `console.error` va ushlanmagan istisnolar. */
  consoleErrors: string[];
}

function ipFromNumbers(a: number, b: number, c: number): string {
  return `10.${String(a % 254)}.${String(b % 254)}.${String((c % 253) + 1)}`;
}

/**
 * DIQQAT: Playwright fixture'ining ikkinchi argumenti odatda `use` deb ataladi, lekin bu
 * loyihaning ESLint sozlamasi (`react-hooks/rules-of-hooks`) `use(...)` chaqiruvini React
 * hook'i deb hisoblaydi. Argument pozitsion bo'lgani uchun nom erkin — `provide`.
 */
let testCounter = 0;

export const test = base.extend<TestFixtures, WorkerFixtures>({
  workerIp: [
    // eslint-disable-next-line no-empty-pattern
    async ({}, provide, workerInfo) => {
      await provide(ipFromNumbers(200, workerInfo.workerIndex, 1));
    },
    { scope: 'worker' },
  ],

  adminToken: [
    async ({ workerIp }, provide) => {
      await provide(await readAdminToken(workerIp));
    },
    { scope: 'worker' },
  ],

  // eslint-disable-next-line no-empty-pattern
  clientIp: async ({}, provide, testInfo) => {
    testCounter += 1;
    await provide(ipFromNumbers(testInfo.workerIndex + 1, Math.floor(testCounter / 250), testCounter));
  },

  /**
   * Brauzer konteksti o'zining "mijoz IP"sini COOKIE orqali e'lon qiladi; preview server
   * uni `X-Forwarded-For` ga aylantirib backendga uzatadi (`preview-server.mjs` izohi).
   *
   * NEGA cookie, `extraHTTPHeaders` EMAS: kontekst darajasidagi qo'shimcha sarlavha
   * BARCHA so'rovlarga, shu jumladan boshqa origin'larga (`fonts.googleapis.com`) ham
   * qo'shiladi va ularni CORS preflight'ga majburlaydi — shriftlar yuklanmay qoladi va
   * konsol xatolarga to'ladi. Cookie faqat o'z origin'iga yuboriladi.
   */
  context: async ({ context, clientIp }, provide) => {
    await context.addCookies([{ name: 'e2e_client_ip', value: clientIp, url: WEB_BASE_URL }]);
    await provide(context);
  },

  school: async ({ adminToken, clientIp }, provide, testInfo) => {
    const school = await createSchool(adminToken, clientIp, testInfo.title.slice(0, 40));
    await provide(school);
    await deleteSchoolCascade(adminToken, clientIp, school.id);
  },

  consoleErrors: async ({ page }, provide) => {
    const errors: string[] = [];
    page.on('console', (message: ConsoleMessage) => {
      if (message.type() === 'error') errors.push(message.text());
    });
    page.on('pageerror', (error: Error) => {
      errors.push(`pageerror: ${error.message}`);
    });
    await provide(errors);
  },

  /**
   * Xavfsizlik to'ri: E2E hech qachon JONLI muhitga (`16shaxsiyat.uz`) so'rov yubormasligi
   * kerak — u yerda haqiqiy maktab va o'quvchi ma'lumotlari bor. Birinchi qatlam — build
   * vaqtidagi tekshiruv (`preview-server.mjs`), bu — ikkinchisi.
   *
   * Google Fonts (`fonts.googleapis.com`/`fonts.gstatic.com`) bloklanmaydi: `index.html`
   * shriftni o'sha yerdan yuklaydi va bu ilovaning haqiqiy xatti-harakati (hisobotdagi
   * topilmalarga qarang).
   */
  page: async ({ page }, provide) => {
    const liveHosts: string[] = [];
    page.on('request', (request) => {
      if (new URL(request.url()).hostname.endsWith('16shaxsiyat.uz')) {
        liveHosts.push(request.url());
      }
    });
    // Nosozlikni tekshirish uchun: `E2E_DEBUG_API=1` bilan barcha muvaffaqiyatsiz API
    // javoblari (tana bilan) stdout'ga chiqadi — yiqilgan test sababini topish osonlashadi.
    if (process.env.E2E_DEBUG_API === '1') {
      page.on('response', (response) => {
        if (!response.url().includes('/api/') || response.status() < 400) return;
        void response
          .text()
          .then((body) => {
            console.log(`[api] ${String(response.status())} ${response.url()} ${body.slice(0, 400)}`);
          })
          .catch(() => undefined);
      });
    }

    await provide(page);
    expect(liveHosts, 'E2E jonli saytga so\'rov yubormasligi kerak').toEqual([]);
  },
});

export { expect };
export type { Page };
