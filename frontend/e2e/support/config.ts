import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/** `frontend/e2e/support` → `frontend/e2e` → `frontend` → repo ildizi. */
const supportDir = path.dirname(fileURLToPath(import.meta.url));
export const E2E_DIR = path.resolve(supportDir, '..');
export const FRONTEND_DIR = path.resolve(E2E_DIR, '..');
export const REPO_ROOT = path.resolve(FRONTEND_DIR, '..');

/**
 * Repo ildizidagi `.env` — `docker compose` uni o'zi o'qiydi, lekin Playwright jarayoni
 * o'qimaydi. Shu sabab bir marta shu yerda o'qib qo'yiladi. MAVJUD `process.env`
 * qiymatlari USTUN — CI'da sirlar muhit o'zgaruvchisi orqali keladi.
 */
function loadRootEnv(): void {
  const file = path.join(REPO_ROOT, '.env');
  if (!existsSync(file)) return;

  for (const rawLine of readFileSync(file, 'utf8').split('\n')) {
    const line = rawLine.trim();
    if (line.length === 0 || line.startsWith('#')) continue;
    const separator = line.indexOf('=');
    if (separator <= 0) continue;
    const key = line.slice(0, separator).trim();
    const value = line
      .slice(separator + 1)
      .trim()
      .replace(/^(['"])(.*)\1$/, '$2');
    if (process.env[key] === undefined) {
      process.env[key] = value;
    }
  }
}

loadRootEnv();

export const COMPOSE_FILE = 'docker-compose.e2e.yml';
export const COMPOSE_PROJECT = 'shaxsiyat-e2e';
export const E2E_API_IMAGE = 'shaxsiyat-e2e-api:latest';

/**
 * Portlar ATAYLAB jonli stek portlaridan boshqa (`docker-compose.yml` da web umuman host'ga
 * chiqarilmaydi, API esa tunnel ortida) — ikkala stek bir vaqtda ishlay oladi.
 */
export const WEB_PORT = Number(process.env.E2E_WEB_PORT ?? 5199);
export const API_PORT = Number(process.env.E2E_API_PORT ?? 5081);
export const WEB_BASE_URL = `http://127.0.0.1:${String(WEB_PORT)}`;
export const API_BASE_URL = `http://127.0.0.1:${String(API_PORT)}`;

/** `vite build` chiqishi — `frontend/dist` ga TEGMAYDI (uni `npm run build` egallaydi). */
export const DIST_DIR = path.join(E2E_DIR, '.tmp', 'dist');

/** `1` bo'lsa stek ko'tarilmaydi/o'chirilmaydi — allaqachon ishlab turgani ishlatiladi. */
export const REUSE_STACK = process.env.E2E_REUSE_STACK === '1';

/** Testdan keyin stek o'chirilmaydi (nosozlikni tekshirish uchun qulay). */
export const KEEP_STACK = process.env.E2E_KEEP_STACK === '1';

export interface AdminCredentials {
  username: string;
  password: string;
}

/**
 * Superadmin login/parol — CLAUDE.md 4-qoida: KODDA EMAS, muhit o'zgaruvchisida
 * (`.env.example` da izohi bor). Shu qiymatlar bilan E2E bazasiga superadmin seed
 * qilinadi va shu qiymatlar bilan admin oqimi testlari kiradi.
 */
export function readAdminCredentials(): AdminCredentials {
  const username = process.env.E2E_ADMIN_USERNAME;
  const password = process.env.E2E_ADMIN_PASSWORD;

  if (!username || !password) {
    throw new Error(
      "E2E uchun `E2E_ADMIN_USERNAME` va `E2E_ADMIN_PASSWORD` berilishi shart " +
        '(repo ildizidagi `.env` yoki CI sirlari — namuna uchun `.env.example` ga qarang). ' +
        'Parol kodda saqlanmaydi.',
    );
  }

  return { username, password };
}
