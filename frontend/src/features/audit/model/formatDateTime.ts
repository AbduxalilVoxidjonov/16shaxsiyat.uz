/**
 * `shared/lib/formatDate.ts` faqat sanani (`KK.OO.YYYY`) qaytaradi — audit jadvalida bitta
 * kunda ko'p yozuv bo'lishi mumkin, shu sabab vaqt ham kerak. `Intl.DateTimeFormat` ataylab
 * ishlatilmagan (`formatDate.ts`dagi izohdagi sabab bilan bir xil — lokal ICU ma'lumotiga
 * qarab ajratkich farq qilishi mumkin), UTC qo'lda formatlanadi.
 */
function pad2(value: number): string {
  return String(value).padStart(2, '0');
}

/** `ISO-8601` sanani `KK.OO.YYYY HH:mm` shaklida qaytaradi (UTC); `null`/noto'g'ri qiymatda `'—'`. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '—';
  const day = pad2(date.getUTCDate());
  const month = pad2(date.getUTCMonth() + 1);
  const year = String(date.getUTCFullYear());
  const hours = pad2(date.getUTCHours());
  const minutes = pad2(date.getUTCMinutes());
  return `${day}.${month}.${year} ${hours}:${minutes}`;
}
