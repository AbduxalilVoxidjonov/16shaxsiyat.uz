import type { MyAssessment } from '@/shared/api/types';

/**
 * `AssessmentStatus` enumi (`StudentRoadMap.Domain/Assessments/AssessmentStatus.cs`,
 * `docs/05` §3): `Draft`, `InProgress`, `Completed`, `Analyzing`, `Analyzed`, `AnalysisFailed`,
 * `Abandoned`. Backend `MyAssessmentDto.status` ni oddiy `string` sifatida beradi, shu sabab
 * bu ro'yxat frontendda nomlanadi — noma'lum qiymat kelsa hech qanday amal taklif qilinmaydi
 * (ro'yxat yiqilmaydi, lekin foydalanuvchi noto'g'ri tugma ham ko'rmaydi).
 */
const UNFINISHED_STATUSES: ReadonlySet<string> = new Set(['Draft', 'InProgress', 'Abandoned']);
const FINISHED_STATUSES: ReadonlySet<string> = new Set([
  'Completed',
  'Analyzing',
  'Analyzed',
  'AnalysisFailed',
]);

/**
 * Tugallanmagan sessiya — `POST /api/me/sessions` `{}` shu sessiyani qaytaradi
 * (`resumed: true`, yangi `sessionToken`; `docs/07` §5.4). `Abandoned` ham shu guruhda:
 * u "foydalanuvchi tashlab ketgan" belgisi, server uni ham davom ettiradi.
 */
export function isUnfinishedAssessment(status: string): boolean {
  return UNFINISHED_STATUSES.has(status);
}

/**
 * Tarix qatoridagi asosiy amal:
 * - `resume` — tugallanmagan: "Davom ettirish" (natija havolasi YO'Q — jarayondagi sessiya
 *   uchun u ma'nosiz);
 * - `result` — yakunlangan va natija HOZIR ochiq (`resultAvailable` — backend hisoblaydi,
 *   `docs/07` §5.6);
 * - `pending` — yakunlangan, natija hali ochilmagan (tahlil ketmoqda / ko'rsatish o'chirilgan);
 * - `none` — noma'lum holat: hech narsa taklif qilinmaydi.
 */
export type HistoryAction = 'resume' | 'result' | 'pending' | 'none';

export function historyActionFor(item: Pick<MyAssessment, 'status' | 'resultAvailable'>): HistoryAction {
  if (isUnfinishedAssessment(item.status)) return 'resume';
  if (item.resultAvailable) return 'result';
  return FINISHED_STATUSES.has(item.status) ? 'pending' : 'none';
}

/**
 * Kabinet tepasidagi "Tugallanmagan test bor" kartasi uchun sessiya. Server bir vaqtda faqat
 * BITTA tugallanmagan sessiyaga yo'l qo'yadi, lekin ro'yxatda nechta bo'lsa ham eng yangisi
 * (tartib `startedAt` bo'yicha kamayish — `docs/07` §5.2) tanlanadi.
 */
export function findUnfinishedAssessment(items: readonly MyAssessment[]): MyAssessment | null {
  return items.find((item) => isUnfinishedAssessment(item.status)) ?? null;
}
