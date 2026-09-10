import { describe, expect, it } from 'vitest';
import {
  isQuestionAnswered,
  orderedVisibleSections,
  questionsInSection,
  resolveVisibleQuestionIds,
} from './branchingFlow';
import type { BranchingQuestion, PublicSection } from '@/shared/api/branchingTypes';

/** `docs/examples/sorovnoma-intellect.json` namunasidagi 1.6-filtr oqimiga o'xshash — qisqartirilgan. */
const SECTIONS: PublicSection[] = [
  { id: 's1', code: 'S1', title: 'Asosiy', description: null, order: 1, visibility: null },
  {
    id: 's2a',
    code: 'S2A',
    title: 'Intellect',
    description: null,
    order: 2,
    visibility: { match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }] },
  },
  {
    id: 's2b',
    code: 'S2B',
    title: 'Boshqa markaz',
    description: null,
    order: 3,
    visibility: { match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [2] }] },
  },
  { id: 's3', code: 'S3', title: 'Yakun', description: null, order: 4, visibility: null },
];

function question(overrides: Partial<BranchingQuestion> & Pick<BranchingQuestion, 'id' | 'code' | 'order'>): BranchingQuestion {
  return {
    text: overrides.code,
    type: 'ShortText',
    isRequired: true,
    options: null,
    currentValue: null,
    sectionId: null,
    placeholder: null,
    inputPattern: null,
    maxLength: null,
    minSelections: null,
    maxSelections: null,
    visibility: null,
    currentText: null,
    currentValues: null,
    ...overrides,
  };
}

const QUESTIONS: BranchingQuestion[] = [
  question({ id: 'q1', code: 'Q1_1', order: 1, sectionId: 's1' }),
  question({ id: 'q6', code: 'Q1_6', order: 6, sectionId: 's1', type: 'SingleChoice' }),
  question({ id: 'q2a1', code: 'Q2A_1', order: 10, sectionId: 's2a' }),
  question({ id: 'q2b1', code: 'Q2B_1', order: 20, sectionId: 's2b' }),
  question({ id: 'q31', code: 'Q3_1', order: 30, sectionId: 's3' }),
];

describe('resolveVisibleQuestionIds', () => {
  it("filtrga hali javob berilmaganda faqat 1-bo'lim va 3-bo'lim ko'rinadi", () => {
    const { visibleQuestionIds, visibleSectionIds } = resolveVisibleQuestionIds(SECTIONS, QUESTIONS, {});

    expect(visibleSectionIds).toEqual(new Set(['s1', 's3']));
    expect(visibleQuestionIds).toEqual(new Set(['q1', 'q6', 'q31']));
  });

  it("Q1_6='1' (Intellect) tanlansa 2-A ko'rinadi, 2-B ko'rinmaydi", () => {
    const { visibleQuestionIds, visibleSectionIds } = resolveVisibleQuestionIds(SECTIONS, QUESTIONS, {
      q6: { value: 1 },
    });

    expect(visibleSectionIds).toEqual(new Set(['s1', 's2a', 's3']));
    expect(visibleQuestionIds).toEqual(new Set(['q1', 'q6', 'q2a1', 'q31']));
  });

  it("Q1_6='2' (boshqa markaz) tanlansa 2-B ko'rinadi, 2-A ko'rinmaydi", () => {
    const { visibleQuestionIds, visibleSectionIds } = resolveVisibleQuestionIds(SECTIONS, QUESTIONS, {
      q6: { value: 2 },
    });

    expect(visibleSectionIds).toEqual(new Set(['s1', 's2b', 's3']));
    expect(visibleQuestionIds).toEqual(new Set(['q1', 'q6', 'q2b1', 'q31']));
  });

  it("Q1_6 A dan B ga o'zgartirilsa 2-A darhol yo'qoladi, 2-B paydo bo'ladi", () => {
    const afterA = resolveVisibleQuestionIds(SECTIONS, QUESTIONS, { q6: { value: 1 } });
    expect(afterA.visibleQuestionIds.has('q2a1')).toBe(true);

    const afterB = resolveVisibleQuestionIds(SECTIONS, QUESTIONS, { q6: { value: 2 } });
    expect(afterB.visibleQuestionIds.has('q2a1')).toBe(false);
    expect(afterB.visibleQuestionIds.has('q2b1')).toBe(true);
  });

  it('mahalliy javob server currentValue\'dan USTUN turadi', () => {
    const questionsWithServerValue = QUESTIONS.map((q) =>
      q.id === 'q6' ? { ...q, currentValue: 2 } : q,
    );
    const { visibleSectionIds } = resolveVisibleQuestionIds(SECTIONS, questionsWithServerValue, {
      q6: { value: 1 }, // mahalliy — hali serverga yetib bormagan
    });
    expect(visibleSectionIds.has('s2a')).toBe(true);
    expect(visibleSectionIds.has('s2b')).toBe(false);
  });
});

describe('orderedVisibleSections / questionsInSection', () => {
  it("ko'rinadigan bo'limlarni order bo'yicha qaytaradi", () => {
    const result = orderedVisibleSections(SECTIONS, new Set(['s3', 's1']));
    expect(result.map((s) => s.code)).toEqual(['S1', 'S3']);
  });

  it("bo'limga tegishli, ko'rinadigan savollarni order bo'yicha qaytaradi", () => {
    const result = questionsInSection(QUESTIONS, 's1', new Set(['q1', 'q6']));
    expect(result.map((q) => q.code)).toEqual(['Q1_1', 'Q1_6']);
  });

  it("yashirin savol bo'lim ichida bo'lsa ham chiqarilmaydi", () => {
    const result = questionsInSection(QUESTIONS, 's1', new Set(['q1']));
    expect(result.map((q) => q.code)).toEqual(['Q1_1']);
  });
});

describe('isQuestionAnswered', () => {
  it('Likert/SingleChoice — value bo\'lsa javob berilgan', () => {
    const q = question({ id: 'q1', code: 'Q1', order: 1, type: 'Likert5' });
    expect(isQuestionAnswered(q, undefined)).toBe(false);
    expect(isQuestionAnswered(q, { value: 3 })).toBe(true);
  });

  it("matn savollari (ShortText/LongText/Phone) — bo'sh/faqat-bo'shliq matn javobsiz hisoblanadi", () => {
    const q = question({ id: 'q1', code: 'Q1', order: 1, type: 'ShortText' });
    expect(isQuestionAnswered(q, { text: '' })).toBe(false);
    expect(isQuestionAnswered(q, { text: '   ' })).toBe(false);
    expect(isQuestionAnswered(q, { text: 'Karimov Ali' })).toBe(true);
  });

  it("MultiChoice — bo'sh selectedValues javobsiz", () => {
    const q = question({ id: 'q1', code: 'Q1', order: 1, type: 'MultiChoice' });
    expect(isQuestionAnswered(q, { selectedValues: [] })).toBe(false);
    expect(isQuestionAnswered(q, { selectedValues: [2] })).toBe(true);
  });

  it("mahalliy javob yo'q bo'lsa server currentValue/currentText/currentValues ishlatiladi", () => {
    const q = question({ id: 'q1', code: 'Q1', order: 1, type: 'MultiChoice', currentValues: [1] });
    expect(isQuestionAnswered(q, undefined)).toBe(true);
  });
});
