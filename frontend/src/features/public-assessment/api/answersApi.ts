import { env } from '@/shared/config/env';
import { publicRequest } from '@/shared/api/publicClient';
import type { SaveAnswerItem, SaveAnswersResponse } from '@/shared/api/types';

function answersUrl(testCode: string): string {
  return `/api/public/sessions/tests/${encodeURIComponent(testCode)}/answers`;
}

/**
 * `POST /sessions/tests/{testCode}/answers` — docs/07-api-shartnoma.md, 1.6-bo'lim.
 * `useAutosave` chaqiradi; xato (tarmoq, 410, 429, 5xx) chaqiruvchiga uloqtiriladi — u
 * yozuvlarni navbatda qoldirib qayta uradi (CLAUDE.md MAXSUS DIQQAT 1-band).
 */
export function saveAnswers(
  testCode: string,
  answers: readonly SaveAnswerItem[],
): Promise<SaveAnswersResponse> {
  return publicRequest<SaveAnswersResponse>(answersUrl(testCode), {
    method: 'POST',
    body: { answers },
  });
}

/**
 * Brauzer bitta `beforeunload`/`visibilitychange` hodisasi davomida yuborilgan barcha
 * `keepalive` so'rovlar uchun jami ~64 KB tana hajmini cheklaydi. Bir nechta test blokidan
 * (`pendingByTestCode` guruhlari) parallel so'rovlar ketishi mumkinligini hisobga olib, xavfsiz
 * zaxira bilan pastroq umumiy byudjet va bitta so'rov uchun kichikroq bo'lak hajmi tanlangan.
 */
const KEEPALIVE_TOTAL_BYTE_BUDGET = 32_000;
const KEEPALIVE_CHUNK_BYTE_SIZE = 8_000;

/**
 * `answers`ni `keepalive` so'rovlar uchun bo'laklarga ajratadi — umumiy byudjetdan oshib
 * ketgan qism localStorage navbatida (`pending: true`) qoladi va keyingi tashrifda oddiy
 * autosave orqali qayta yuboriladi (backend upsert idempotent — docs/07 4-bo'lim).
 */
function chunkAnswersForKeepalive(answers: readonly SaveAnswerItem[]): SaveAnswerItem[][] {
  const chunks: SaveAnswerItem[][] = [];
  let currentChunk: SaveAnswerItem[] = [];
  let currentChunkBytes = 0;
  let totalBytes = 0;

  for (const answer of answers) {
    const itemBytes = JSON.stringify(answer).length;
    if (totalBytes + itemBytes > KEEPALIVE_TOTAL_BYTE_BUDGET) {
      break; // Umumiy byudjet tugadi — qolganlari mahalliy navbatda qoladi.
    }
    if (currentChunkBytes + itemBytes > KEEPALIVE_CHUNK_BYTE_SIZE && currentChunk.length > 0) {
      chunks.push(currentChunk);
      currentChunk = [];
      currentChunkBytes = 0;
    }
    currentChunk.push(answer);
    currentChunkBytes += itemBytes;
    totalBytes += itemBytes;
  }
  if (currentChunk.length > 0) {
    chunks.push(currentChunk);
  }
  return chunks;
}

/**
 * `beforeunload`/`visibilitychange` (→ `hidden`) da navbatdagi javoblarni yuborishga urinadi —
 * docs/10, 4.2-bo'lim.
 *
 * `navigator.sendBeacon` EMAS — u maxsus header (`X-Session-Token`) qo'ya olmaydi, shu sabab
 * amalda doim `401` bilan rad etilardi (PM qarori). Buning o'rniga oddiy `fetch` — `keepalive:
 * true` bilan: bu aynan shu holat uchun ishlab chiqilgan (so'rov sahifa yopilgandan keyin ham
 * yakunlanadi) va header qo'yishga ruxsat beradi, shu sabab token ikkinchi (nostandart) yo'l
 * bilan emas, odatdagidek `X-Session-Token` header orqali ketadi.
 *
 * Bu — "eng yaxshi urinish" (best-effort, fire-and-forget): sahifa yopilayotgani uchun javobni
 * kutib bo'lmaydi, shu sabab yozuvlar `markSent` qilinmaydi. Asosiy himoya baribir navbat
 * (`localStorage`, `pending: true`) — agar bu so'rov yetib bormasa ham, keyingi tashrifda oddiy
 * autosave orqali qayta yuboriladi (backend upsert idempotent).
 */
export function sendAnswersKeepalive(
  testCode: string,
  answers: readonly SaveAnswerItem[],
  sessionToken: string,
): void {
  if (typeof fetch !== 'function' || answers.length === 0) {
    return;
  }
  const url = `${env.apiBaseUrl}${answersUrl(testCode)}`;
  for (const chunk of chunkAnswersForKeepalive(answers)) {
    fetch(url, {
      method: 'POST',
      keepalive: true,
      headers: { 'Content-Type': 'application/json', 'X-Session-Token': sessionToken },
      body: JSON.stringify({ answers: chunk }),
    }).catch(() => {
      // Sahifa yopilmoqda — javobni kutish/qayta ishlov berish imkoni yo'q; yozuv baribir
      // mahalliy navbatda qoladi (yuqoridagi izohga qarang).
    });
  }
}
