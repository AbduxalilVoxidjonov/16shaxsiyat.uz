/**
 * Sana formatlash — docs/10-frontend-arxitektura.md 2-bo'limdagi `shared/lib/` ro'yxatida
 * ko'zda tutilgan (`formatDate`), lekin P24'gacha hech kim ishlatmagani uchun yaratilmagan
 * edi. Backend sanalarni ISO-8601 UTC qaytaradi (`docs/07`, 4-bo'lim) — bu yerda o'quvchiga
 * tanish `kun.oy.yil` ko'rinishiga o'giramiz.
 */

function pad2(value: number): string {
  return String(value).padStart(2, '0');
}

/**
 * `ISO-8601` sanani `KK.OO.YYYY` shaklida qaytaradi; `null`/noto'g'ri qiymatda `'—'`.
 *
 * `Intl.DateTimeFormat` ataylab ishlatilmagan — `uz-UZ` lokali ICU ma'lumotlariga qarab
 * ajratkichni `.` emas `/` deb chiqarishi mumkin (muhitga bog'liq, sinovda topildi), shu
 * sabab UTC (backend sanalari UTC — docs/07, 4-bo'lim) komponentlari qo'lda formatlanadi —
 * natija muhitdan qat'iy nazar bir xil.
 */
export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '—';
  return `${pad2(date.getUTCDate())}.${pad2(date.getUTCMonth() + 1)}.${String(date.getUTCFullYear())}`;
}
