import { STORAGE_KEYS } from '@/shared/config/storageKeys';

/**
 * O'quvchi javobining mahalliy yozuvi — bir vaqtning o'zida ikkita rolni bajaradi:
 * 1) **resume kesh** — sahifa yangilanganda/orqaga qaytilganda tanlangan qiymatni darhol
 *    ko'rsatish uchun (server javobini kutmasdan);
 * 2) **yuborish navbati** — `pending: true` bo'lgan yozuvlar hali backend'ga tasdiqlanmagan,
 *    `useAutosave` ularni topib qayta yuboradi (docs/10, 4.2-bo'lim; CLAUDE.md MAXSUS DIQQAT 1-band).
 *
 * Muvaffaqiyatli yuborilgach yozuv **o'chirilmaydi** — faqat `pending: false` qilinadi. Aks
 * holda ekran shu zahoti "javobsiz" holatga qaytib ketardi (server'dan qaytgan `currentValue`
 * eski, keshni yangilash uchun qayta so'rov yubormaguncha).
 */
export interface StoredAnswer {
  testCode: string;
  questionId: string;
  value: number;
  durationMs: number;
  pending: boolean;
}

/** Kalit — `questionId` (UUID bo'lgani uchun turli testlar orasida to'qnashish amalda mumkin emas). */
export type AnswerStore = Record<string, StoredAnswer>;

/** `localStorage`dan xavfsiz o'qiydi — buzilgan/yo'q bo'lsa bo'sh obyekt qaytaradi. */
export function readAnswerStore(): AnswerStore {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEYS.pendingAnswers);
    if (!raw) return {};
    const parsed: unknown = JSON.parse(raw);
    return parsed && typeof parsed === 'object' ? (parsed as AnswerStore) : {};
  } catch {
    return {};
  }
}

/** `localStorage`ga xavfsiz yozadi (kvota to'lgan/xususiy rejim — xato yutiladi). */
export function writeAnswerStore(store: AnswerStore): void {
  try {
    window.localStorage.setItem(STORAGE_KEYS.pendingAnswers, JSON.stringify(store));
  } catch {
    // Xotiraga yozib bo'lmadi — hook baribir xotiradagi holat bilan davom etadi.
  }
}

/** Bitta javobni yozadi/yangilaydi va `pending: true` deb belgilaydi (yangi tahrir — qayta yuborilishi kerak). */
export function upsertAnswer(
  store: AnswerStore,
  entry: { testCode: string; questionId: string; value: number; durationMs: number },
): AnswerStore {
  return {
    ...store,
    [entry.questionId]: { ...entry, pending: true },
  };
}

/** Berilgan `questionId`larni muvaffaqiyatli yuborilgan deb belgilaydi (`pending: false`), o'chirmaydi. */
export function markSent(store: AnswerStore, questionIds: readonly string[]): AnswerStore {
  if (questionIds.length === 0) return store;
  const next = { ...store };
  for (const id of questionIds) {
    const existing = next[id];
    if (existing) {
      next[id] = { ...existing, pending: false };
    }
  }
  return next;
}

/** Hali yuborilmagan (`pending: true`) yozuvlarni `testCode` bo'yicha guruhlaydi. */
export function pendingByTestCode(store: AnswerStore): Record<string, StoredAnswer[]> {
  const groups: Record<string, StoredAnswer[]> = {};
  for (const entry of Object.values(store)) {
    if (!entry.pending) continue;
    const list = groups[entry.testCode];
    if (list) {
      list.push(entry);
    } else {
      groups[entry.testCode] = [entry];
    }
  }
  return groups;
}

/** Joriy testga tegishli barcha mahalliy qiymatlar (`questionId -> value`) — pending yoki yo'qligidan qat'i nazar. */
export function valuesForTest(store: AnswerStore, testCode: string): Record<string, number> {
  const result: Record<string, number> = {};
  for (const entry of Object.values(store)) {
    if (entry.testCode === testCode) {
      result[entry.questionId] = entry.value;
    }
  }
  return result;
}

/** Sessiya tugaganda/tozalanganda (`410`) chaqiriladi — CLAUDE.md MAXSUS DIQQAT 7-band. */
export function clearAnswerStore(): void {
  writeAnswerStore({});
}
