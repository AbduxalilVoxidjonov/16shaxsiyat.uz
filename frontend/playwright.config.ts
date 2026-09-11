import { defineConfig, devices } from '@playwright/test';
import {
  API_BASE_URL,
  DIST_DIR,
  E2E_DIR,
  FRONTEND_DIR,
  WEB_BASE_URL,
  WEB_PORT,
} from './e2e/support/config';

/**
 * P30 — uchidan-uchigacha testlar (`docs/12` 8-bo'lim).
 *
 * MUHIT: testlar JONLI saytga (`https://16shaxsiyat.uz`) qarshi ISHLAMAYDI. `globalSetup`
 * `docker-compose.e2e.yml` ni alohida loyiha nomi (`shaxsiyat-e2e`) va alohida bo'sh baza
 * bilan ko'taradi; frontend esa shu yerda `vite build` qilinib, `e2e/support/preview-server.mjs`
 * orqali bitta origin ostida (`/api` backendga proksilanadi) uzatiladi — jonli `web` nginx
 * konteyneri sxemasining aynan o'zi.
 */
export default defineConfig({
  testDir: './e2e/tests',
  // `*.e2e.ts` — ATAYLAB `*.test.ts`/`*.spec.ts` EMAS: `npm run test` (vitest) standart
  // `**/*.{test,spec}.*` naqshi bilan yig'adi va E2E fayllarini olib ketmasligi shart.
  testMatch: '**/*.e2e.ts',
  outputDir: `${E2E_DIR}/.tmp/test-results`,

  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  // Lokalda 0 — beqarorlik yashirinmasin (`prompts/30` DoD). CI'da infratuzilma
  // uzilishlari uchun bitta qayta urinish.
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : 3,
  // 190 savolli to'liq oqim uchun standart 30s yetmaydi.
  timeout: 180_000,
  expect: { timeout: 10_000 },

  reporter: process.env.CI
    ? [['list'], ['html', { outputFolder: `${E2E_DIR}/.tmp/playwright-report`, open: 'never' }]]
    : [['list']],

  use: {
    baseURL: WEB_BASE_URL,
    // Standart holatda amal/navigatsiya kutishi CHEKSIZ va faqat test taymeriga tayanadi —
    // bunda yiqilgan test "Test timeout" deb, qaysi qadamda qotgani ko'rsatilmasdan tugaydi.
    actionTimeout: 20_000,
    navigationTimeout: 30_000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },

  projects: [
    {
      name: 'desktop-1440',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
    {
      // 390×844 — `docs/11` mobil-birinchi bazaviy kengligi.
      name: 'mobile-390',
      use: { ...devices['iPhone 13'], browserName: 'chromium' },
    },
  ],

  globalSetup: './e2e/global-setup.ts',
  globalTeardown: './e2e/global-teardown.ts',

  webServer: {
    command: 'npm run e2e:build && npm run e2e:serve',
    cwd: FRONTEND_DIR,
    url: WEB_BASE_URL,
    // Har run yangi build — eski `dist` bilan yashil natija bermasin. DIQQAT (P52 topilmasi,
    // `docs/12` 8.1-bo'lim): bu FAQAT shu portda boshqa jarayon TIRIK QOLMAGAN bo'lsa ishlaydi
    // — oldingi run noto'g'ri to'xtatilgan bo'lsa (masalan `Ctrl+C`), qoldiq
    // `preview-server.mjs` eski buildni davom ettirib xizmat qilishi mumkin. Shubha bo'lsa
    // qo'lda `VITE_API_BASE_URL= npm run e2e:build` bilan tekshiring.
    reuseExistingServer: false,
    timeout: 180_000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      // `frontend/.env` dagi `VITE_API_BASE_URL=https://16shaxsiyat.uz` ni BEKOR QILADI:
      // bo'sh qiymat = same-origin. `process.env` `.env` fayldan ustun (tekshirilgan).
      VITE_API_BASE_URL: '',
      E2E_WEB_PORT: String(WEB_PORT),
      E2E_DIST_DIR: DIST_DIR,
      E2E_API_TARGET: API_BASE_URL,
    },
  },
});
