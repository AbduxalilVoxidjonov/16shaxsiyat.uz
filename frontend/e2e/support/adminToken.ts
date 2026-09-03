import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { E2E_DIR } from './config';
import { loginAsAdminApi } from './adminApi';

const TOKEN_FILE = path.join(E2E_DIR, '.tmp', 'admin-token.json');

/**
 * Superadmin tokeni butun to'plam uchun BIR MARTA olinadi (`globalSetup`) va faylga
 * yoziladi. Sabab ikkita: (1) login IP bo'yicha 5 daqiqada 10 marta cheklangan;
 * (2) bir vaqtda yuborilgan ikkita login `409 CONCURRENCY_CONFLICT` beradi
 * (`adminApi.loginAsAdminApi` izohidagi P30-1 topilmasi).
 */
export async function issueAdminToken(clientIp: string): Promise<string> {
  const token = await loginAsAdminApi(clientIp);
  mkdirSync(path.dirname(TOKEN_FILE), { recursive: true });
  writeFileSync(TOKEN_FILE, JSON.stringify({ token }), 'utf8');
  return token;
}

export async function readAdminToken(clientIp: string): Promise<string> {
  try {
    const parsed = JSON.parse(readFileSync(TOKEN_FILE, 'utf8')) as { token?: string };
    if (parsed.token) return parsed.token;
  } catch {
    // Fayl yo'q (masalan bitta testni `globalSetup`siz ishga tushirish) — pastda login.
  }
  return issueAdminToken(clientIp);
}
