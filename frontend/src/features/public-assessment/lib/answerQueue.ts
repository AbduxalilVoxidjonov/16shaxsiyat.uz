import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import type { BranchingSaveAnswerEntry } from '@/shared/api/branchingTypes';

/**
 * Bitta savolga berilgan javob QIYMATI — `docs/18` §2.7/§4.2 invarianti bo'yicha uchta
 * shakldan (`value`/`text`/`selectedValues`) AYNAN bittasi to'ldiriladi (savol turiga mos).
 * Ikkinchisi/uchinchisi shunchaki YO'Q (`undefined`) — `null` EMAS, aks holda
 * `JSON.stringify` orqali serverga bo'sh maydon ham ketardi.
 */
export interface AnswerPayload {
  value?: number;
  text?: string;
  selectedValues?: number[];
}

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
export interface StoredAnswer extends AnswerPayload {
  testCode: string;
  questionId: string;
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
  entry: { testCode: string; questionId: string; durationMs: number } & AnswerPayload,
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

/**
 * Berilgan yozuvlardan HOZIR ko'rinadiganlarini ajratadi (`docs/18` §6.2: "yashirilgan
 * savolning mahalliy javobi yuborilmaydi — autosave navbatidan chiqariladi"). `visibleIds`
 * berilmasa (bo'limsiz oqim) — hammasi qaytariladi, filtr qo'llanmaydi.
 */
export function filterVisibleAnswers(
  items: readonly StoredAnswer[],
  visibleIds: ReadonlySet<string> | undefined,
): StoredAnswer[] {
  if (!visibleIds) return [...items];
  return items.filter((item) => visibleIds.has(item.questionId));
}

/**
 * Mahalliy yozuvni `POST .../answers` so'rov elementi shakliga o'giradi (`docs/18` §4.2) —
 * faqat TO'LDIRILGAN maydon (`value`/`text`/`selectedValues`dan bittasi) qo'shiladi, aks
 * holda eski (bo'limsiz) so'rovlar ham `text: undefined, selectedValues: undefined` kabi
 * ortiqcha kalitlar bilan ketardi.
 */
export function toWireItem(entry: StoredAnswer): BranchingSaveAnswerEntry {
  const { questionId, value, text, selectedValues, durationMs } = entry;
  return {
    questionId,
    durationMs,
    ...(value !== undefined ? { value } : {}),
    ...(text !== undefined ? { text } : {}),
    ...(selectedValues !== undefined ? { selectedValues } : {}),
  };
}

/** Joriy testga tegishli barcha mahalliy javoblar (`questionId -> {value|text|selectedValues}`) — pending yoki yo'qligidan qat'i nazar. */
export function answersForTest(store: AnswerStore, testCode: string): Record<string, AnswerPayload> {
  const result: Record<string, AnswerPayload> = {};
  for (const entry of Object.values(store)) {
    if (entry.testCode === testCode) {
      const { value, text, selectedValues } = entry;
      result[entry.questionId] = {
        ...(value !== undefined ? { value } : {}),
        ...(text !== undefined ? { text } : {}),
        ...(selectedValues !== undefined ? { selectedValues } : {}),
      };
    }
  }
  return result;
}

/** Sessiya tugaganda/tozalanganda (`410`) chaqiriladi — CLAUDE.md MAXSUS DIQQAT 7-band. */
export function clearAnswerStore(): void {
  writeAnswerStore({});
}
