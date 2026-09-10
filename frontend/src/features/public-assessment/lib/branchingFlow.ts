import {
  resolveVisibleQuestions,
  type AnswerSnapshot,
  type QuestionSnapshot,
  type SectionSnapshot,
} from '@/shared/lib/visibility';
import type { BranchingQuestion, PublicSection } from '@/shared/api/branchingTypes';
import type { AnswerPayload } from './answerQueue';

/** `MultiChoice`/matn savollari (`docs/18` §2.1) — "javob bor" tekshiruvi turga qarab farqlanadi. */
const TEXT_TYPES = new Set(['ShortText', 'LongText', 'Phone']);

/**
 * Serverdan kelgan joriy javob (`currentValue`/`currentText`/`currentValues`) va mahalliy
 * (hali yuborilmagan yoki yangi tahrirlangan) javobni birlashtiradi — MAHALLIY ustun turadi
 * (`docs/18` §6.2: "javob o'zgarganda ko'rinish DARHOL qayta hisoblanadi", serverga
 * yuborilishini kutmaydi).
 */
export function effectiveAnswer(
  question: BranchingQuestion,
  local: AnswerPayload | undefined,
): { value: number | null; text: string | null; selectedValues: readonly number[] } {
  if (local) {
    return {
      value: local.value ?? null,
      text: local.text ?? null,
      selectedValues: local.selectedValues ?? [],
    };
  }
  return {
    value: question.currentValue ?? null,
    text: question.currentText ?? null,
    selectedValues: question.currentValues ?? [],
  };
}

/** Savolga (turi bo'yicha) javob berilganmi — `docs/18` §2.1 jadvali. */
export function isQuestionAnswered(question: BranchingQuestion, local: AnswerPayload | undefined): boolean {
  const answer = effectiveAnswer(question, local);
  if (question.type === 'MultiChoice') return answer.selectedValues.length > 0;
  if (TEXT_TYPES.has(question.type)) return answer.text !== null && answer.text.trim().length > 0;
  return answer.value !== null;
}

export interface VisibleQuestionsResult {
  visibleQuestionIds: ReadonlySet<string>;
  visibleSectionIds: ReadonlySet<string>;
}

/**
 * `BranchingQuestion[]`/`PublicSection[]` (ommaviy API shakli — savol KODI + ID) dan
 * `shared/lib/visibility.ts`ning kod-asosli kirishini quradi va natijadagi kod to'plamlarini
 * qaytadan ID to'plamlariga o'giradi (`docs/18` §2.6, §6.1–6.2). `localAnswers` — joriy
 * ekrandagi (hali serverga yetib bormagan bo'lishi mumkin) javoblar, `questionId` bo'yicha.
 */
export function resolveVisibleQuestionIds(
  sections: readonly PublicSection[],
  questions: readonly BranchingQuestion[],
  localAnswers: Readonly<Record<string, AnswerPayload>>,
): VisibleQuestionsResult {
  const sectionIdToCode = new Map(sections.map((section) => [section.id, section.code]));

  const sectionSnapshots: SectionSnapshot[] = sections.map((section) => ({
    code: section.code,
    displayOrder: section.order,
    visibilityRule: section.visibility,
  }));
  const questionSnapshots: QuestionSnapshot[] = questions.map((question) => ({
    code: question.code,
    displayOrder: question.order,
    sectionCode: question.sectionId ? (sectionIdToCode.get(question.sectionId) ?? null) : null,
    visibilityRule: question.visibility,
  }));

  const answersByCode: Record<string, AnswerSnapshot> = {};
  for (const question of questions) {
    const { value, text, selectedValues } = effectiveAnswer(question, localAnswers[question.id]);
    answersByCode[question.code] = { rawValue: value, textValue: text, selectedValues };
  }

  const map = resolveVisibleQuestions(sectionSnapshots, questionSnapshots, answersByCode);

  const questionCodeToId = new Map(questions.map((question) => [question.code, question.id]));
  const sectionCodeToId = new Map(sections.map((section) => [section.code, section.id]));

  const visibleQuestionIds = new Set<string>();
  for (const code of map.visibleQuestionCodes) {
    const id = questionCodeToId.get(code);
    if (id) visibleQuestionIds.add(id);
  }
  const visibleSectionIds = new Set<string>();
  for (const code of map.visibleSectionCodes) {
    const id = sectionCodeToId.get(code);
    if (id) visibleSectionIds.add(id);
  }

  return { visibleQuestionIds, visibleSectionIds };
}

/** `sections` bo'lgan (`order` bo'yicha) va HOZIR ko'rinadigan bo'limlar ro'yxati. */
export function orderedVisibleSections(
  sections: readonly PublicSection[],
  visibleSectionIds: ReadonlySet<string>,
): PublicSection[] {
  return sections
    .filter((section) => visibleSectionIds.has(section.id))
    .slice()
    .sort((a, b) => a.order - b.order);
}

/** Berilgan bo'limga tegishli, HOZIR ko'rinadigan savollar (`order` bo'yicha). */
export function questionsInSection(
  questions: readonly BranchingQuestion[],
  sectionId: string,
  visibleQuestionIds: ReadonlySet<string>,
): BranchingQuestion[] {
  return questions
    .filter((question) => question.sectionId === sectionId && visibleQuestionIds.has(question.id))
    .slice()
    .sort((a, b) => a.order - b.order);
}
