import type { VisibilityCondition, VisibilityOperator, VisibilityRule } from '@/shared/lib/visibility';
import {
  isTextQuestionType,
  type CatalogQuestionItem,
  type CatalogQuestionOption,
  type CatalogSection,
  type QuestionType,
} from './types';

/**
 * `VisibilityRuleEditor`/`SectionDialog` uchun sodda savol proyeksiyasi — faqat shart
 * muharririga kerakli maydonlar (`docs/18` §6.3).
 */
export interface VisibilityEditorQuestion {
  code: string;
  textUz: string;
  type: QuestionType;
  order: number;
  options: CatalogQuestionOption[] | null;
}

export function toVisibilityEditorQuestion(question: CatalogQuestionItem): VisibilityEditorQuestion {
  return {
    code: question.code,
    textUz: question.textUz,
    type: (question.type as QuestionType | undefined) ?? 'Likert5',
    order: question.order,
    options: question.options,
  };
}

/**
 * B-4 (`docs/18` §1): savol darajasidagi shart faqat OLDINROQDAGI savolga havola qila oladi.
 * `currentOrder` — tahrirlanayotgan savolning (yoki yangi savol uchun `nextOrder`ning)
 * `order`i; teng yoki kattaroq `order`li savollar chiqarib tashlanadi.
 */
export function questionsBeforeOrder(
  questions: readonly VisibilityEditorQuestion[],
  currentOrder: number,
): VisibilityEditorQuestion[] {
  return questions.filter((q) => q.order < currentOrder).sort((a, b) => a.order - b.order);
}

/**
 * Bo'lim sharti uchun "oldingi" savollar (`docs/18` §6.3): bo'limning o'zidan OLDINROQ
 * (`displayOrder` kichikroq) bo'limlarga tegishli savollar, PLYUS hali biror bo'limga
 * tegishli bo'lmagan savollar — ular orasidan faqat shu bo'limdan (yoki undan keyingi
 * bo'limlardan) birinchi savol paydo bo'lgunga qadar bo'lganlari.
 *
 * Bu — UI qulayligi uchun MAHALLIY evristika (backend `VISIBILITY_FORWARD_REFERENCE` bilan
 * yakuniy tekshiradi, `docs/18` §5); maqsad — admin konstruktorda ko'pchilik holatda to'g'ri
 * ro'yxatni ko'rsatish, aylanma/oldinga havolani oldindan kamaytirish.
 */
export function questionsBeforeSection(
  sections: readonly CatalogSection[],
  questions: readonly VisibilityEditorQuestion[],
  questionSectionCode: ReadonlyMap<string, string | null>,
  targetSectionCode: string | null,
  targetDisplayOrder: number,
): VisibilityEditorQuestion[] {
  const sectionOrderByCode = new Map(sections.map((s) => [s.code, s.displayOrder]));

  // Savollar bo'lgan bo'limlar orasida targetdan KATTA yoki TENG `displayOrder`ga ega
  // bo'lganlarning eng kichik savol `order`i — bo'limsiz savollarni shu chegaragacha kesish
  // uchun (aks holda hali bo'limlanmagan, lekin "keyingi" bo'limga tegishli bo'lajak savollar
  // ham "oldingi" deb hisoblanib qolardi).
  let boundaryOrder = Infinity;
  for (const question of questions) {
    const sectionCode = questionSectionCode.get(question.code) ?? null;
    if (sectionCode === targetSectionCode) continue;
    const order = sectionCode ? (sectionOrderByCode.get(sectionCode) ?? null) : null;
    if (order !== null && order >= targetDisplayOrder && question.order < boundaryOrder) {
      boundaryOrder = question.order;
    }
  }

  return questions
    .filter((question) => {
      const sectionCode = questionSectionCode.get(question.code) ?? null;
      if (sectionCode === targetSectionCode) return false;
      if (sectionCode) {
        const order = sectionOrderByCode.get(sectionCode);
        return order !== undefined && order < targetDisplayOrder;
      }
      return question.order < boundaryOrder;
    })
    .sort((a, b) => a.order - b.order);
}

/** `docs/18` §2.4 — manba savol turiga qarab ruxsat etilgan operatorlar ro'yxati. */
export function operatorsForQuestionType(type: QuestionType): VisibilityOperator[] {
  if (type === 'MultiChoice') {
    return ['ContainsAny', 'ContainsAll', 'Answered', 'NotAnswered'];
  }
  if (isTextQuestionType(type)) {
    return ['Answered', 'NotAnswered'];
  }
  return ['Equals', 'NotEquals', 'AnyOf', 'NoneOf', 'Answered', 'NotAnswered'];
}

/** Operator qiymat(lar) talab qiladimi (`Answered`/`NotAnswered` — yo'q). */
export function operatorRequiresValues(operator: VisibilityOperator): boolean {
  return operator !== 'Answered' && operator !== 'NotAnswered';
}

/** Operator bir nechta qiymatni bir vaqtda tanlashga ruxsat beradimi. */
export function operatorAllowsMultipleValues(operator: VisibilityOperator): boolean {
  return operator === 'AnyOf' || operator === 'NoneOf' || operator === 'ContainsAny' || operator === 'ContainsAll';
}

export interface VisibilityValueOption {
  value: number;
  label: string;
}

/**
 * Shart qiymati qayerdan tanlanadi (`docs/18` §6.3: "Qiymat(lar) — manba savolning
 * variantlaridan tanlanadi… yoki shkala darajasidan"). Matn turlari uchun `null` — ularda
 * faqat `Answered`/`NotAnswered` bor, qiymat kerak emas.
 */
export function valueOptionsForQuestion(
  question: VisibilityEditorQuestion | undefined,
): VisibilityValueOption[] | null {
  if (!question) return null;
  if (question.options && question.options.length > 0) {
    return [...question.options]
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((option) => ({ value: option.value, label: option.textUz }));
  }
  if (question.type === 'Likert5') {
    return [1, 2, 3, 4, 5].map((value) => ({ value, label: String(value) }));
  }
  if (question.type === 'Likert7') {
    return [1, 2, 3, 4, 5, 6, 7].map((value) => ({ value, label: String(value) }));
  }
  if (question.type === 'Binary') {
    return [
      { value: 0, label: "0 (Yo'q)" },
      { value: 1, label: '1 (Ha)' },
    ];
  }
  return null;
}

function quoteLabels(labels: string[]): string {
  return labels.map((label) => `"${label}"`).join(', ');
}

/**
 * Bitta shartni o'zbekcha jumlaga o'giradi (`docs/18` §6.3 namunasi: «Q1_6 savoliga javob
 * "Ha, Intellect o'quv markazida" bo'lsa»). Manba savol topilmasa (kod o'chirilgan/xato)
 * xom kodni ko'rsatadi — soxta jumla yasashdan ko'ra shubhani ko'rsatish yaxshiroq.
 */
export function describeVisibilityCondition(
  condition: VisibilityCondition,
  question: VisibilityEditorQuestion | undefined,
): string {
  const code = condition.questionCode;
  if (!question) {
    return `${code} savoliga shart (savol topilmadi)`;
  }

  const valueOptions = valueOptionsForQuestion(question);
  const labelFor = (value: number) =>
    valueOptions?.find((option) => option.value === value)?.label ?? String(value);
  const labels = condition.values.map(labelFor);

  switch (condition.operator) {
    case 'Equals':
      return `${code} savoliga javob ${quoteLabels(labels)} bo'lsa`;
    case 'NotEquals':
      return `${code} savoliga javob ${quoteLabels(labels)} bo'lmasa`;
    case 'AnyOf':
      return `${code} savoliga javob ${labels.map((l) => `"${l}"`).join(' yoki ')} dan biri bo'lsa`;
    case 'NoneOf':
      return `${code} savoliga javob ${quoteLabels(labels)} dan hech biri bo'lmasa`;
    case 'ContainsAny':
      return `${code} savolida ${quoteLabels(labels)} dan kamida bittasi tanlangan bo'lsa`;
    case 'ContainsAll':
      return `${code} savolida ${quoteLabels(labels)} — hammasi tanlangan bo'lsa`;
    case 'Answered':
      return `${code} savoliga javob berilgan bo'lsa`;
    case 'NotAnswered':
      return `${code} savoliga hali javob berilmagan bo'lsa`;
    default:
      return `${code} savoliga shart`;
  }
}

/** Butun qoidani jumlaga o'giradi — `VisibilityRuleEditor` jonli oldindan ko'rishi uchun. */
export function describeVisibilityRule(
  rule: VisibilityRule | null,
  questions: readonly VisibilityEditorQuestion[],
): string {
  if (!rule || rule.conditions.length === 0) return '';
  const byCode = new Map(questions.map((q) => [q.code, q]));
  const parts = rule.conditions.map((condition) =>
    describeVisibilityCondition(condition, byCode.get(condition.questionCode)),
  );
  const joiner = rule.match === 'All' ? ' VA ' : ' YOKI ';
  return `${parts.join(joiner)} ko'rsatilsin.`;
}

/** Yangi (bo'sh) shart — birinchi mavjud savolga `Answered` operatori bilan. */
export function createDefaultCondition(
  question: VisibilityEditorQuestion,
): VisibilityCondition {
  const operators = operatorsForQuestionType(question.type);
  const operator = operators[0] ?? 'Answered';
  if (!operatorRequiresValues(operator)) {
    return { questionCode: question.code, operator, values: [] };
  }
  const valueOptions = valueOptionsForQuestion(question);
  const firstValue = valueOptions?.[0]?.value ?? 0;
  return { questionCode: question.code, operator, values: [firstValue] };
}
