import { describe, expect, it } from 'vitest';
import type { VisibilityRule } from '@/shared/lib/visibility';
import {
  createDefaultCondition,
  describeVisibilityCondition,
  describeVisibilityRule,
  operatorAllowsMultipleValues,
  operatorRequiresValues,
  operatorsForQuestionType,
  questionsBeforeOrder,
  questionsBeforeSection,
  valueOptionsForQuestion,
  type VisibilityEditorQuestion,
} from './visibilityEditorHelpers';
import type { CatalogSection } from './types';

function q(overrides: Partial<VisibilityEditorQuestion> = {}): VisibilityEditorQuestion {
  return {
    code: 'Q1',
    textUz: 'Savol',
    type: 'Likert5',
    order: 1,
    options: null,
    ...overrides,
  };
}

function section(overrides: Partial<CatalogSection> = {}): CatalogSection {
  return {
    id: 's-1',
    testDefinitionId: 't-1',
    code: 'S1',
    titleUz: 'Bo’lim',
    descriptionUz: null,
    displayOrder: 1,
    visibility: null,
    ...overrides,
  };
}

describe('operatorsForQuestionType', () => {
  it('MultiChoice uchun faqat ContainsAny/ContainsAll + Answered/NotAnswered', () => {
    expect(operatorsForQuestionType('MultiChoice')).toEqual([
      'ContainsAny',
      'ContainsAll',
      'Answered',
      'NotAnswered',
    ]);
  });

  it('matn turlarida faqat Answered/NotAnswered', () => {
    expect(operatorsForQuestionType('ShortText')).toEqual(['Answered', 'NotAnswered']);
    expect(operatorsForQuestionType('LongText')).toEqual(['Answered', 'NotAnswered']);
    expect(operatorsForQuestionType('Phone')).toEqual(['Answered', 'NotAnswered']);
  });

  it('qiymatli turlarda (SingleChoice, Likert, Binary) qiymat operatorlari + Answered/NotAnswered', () => {
    expect(operatorsForQuestionType('SingleChoice')).toEqual([
      'Equals',
      'NotEquals',
      'AnyOf',
      'NoneOf',
      'Answered',
      'NotAnswered',
    ]);
    expect(operatorsForQuestionType('Likert5')).toContain('Equals');
    expect(operatorsForQuestionType('Binary')).toContain('AnyOf');
  });
});

describe('operatorRequiresValues / operatorAllowsMultipleValues', () => {
  it('Answered/NotAnswered qiymat talab qilmaydi', () => {
    expect(operatorRequiresValues('Answered')).toBe(false);
    expect(operatorRequiresValues('NotAnswered')).toBe(false);
    expect(operatorRequiresValues('Equals')).toBe(true);
  });

  it('AnyOf/NoneOf/ContainsAny/ContainsAll bir nechta qiymatga ruxsat beradi', () => {
    expect(operatorAllowsMultipleValues('AnyOf')).toBe(true);
    expect(operatorAllowsMultipleValues('ContainsAny')).toBe(true);
    expect(operatorAllowsMultipleValues('Equals')).toBe(false);
  });
});

describe('questionsBeforeOrder', () => {
  it('faqat order kichikroq savollarni qaytaradi, tartiblab', () => {
    const questions = [q({ code: 'Q3', order: 3 }), q({ code: 'Q1', order: 1 }), q({ code: 'Q5', order: 5 })];
    expect(questionsBeforeOrder(questions, 4).map((x) => x.code)).toEqual(['Q1', 'Q3']);
  });

  it('teng order ham chiqarib tashlanadi (B-4: qatiy kichikroq)', () => {
    const questions = [q({ code: 'Q1', order: 1 })];
    expect(questionsBeforeOrder(questions, 1)).toEqual([]);
  });
});

describe('questionsBeforeSection', () => {
  it("oldingi bo'limdagi savollarni qaytaradi, joriy bo'limdagini emas", () => {
    const sections = [section({ code: 'S1', displayOrder: 1 }), section({ code: 'S2', displayOrder: 2 })];
    const questions = [
      q({ code: 'Q1', order: 1 }),
      q({ code: 'Q2', order: 2 }),
      q({ code: 'Q3', order: 3 }),
    ];
    const sectionByQuestion = new Map([
      ['Q1', 'S1'],
      ['Q2', 'S1'],
      ['Q3', 'S2'],
    ]);

    const result = questionsBeforeSection(sections, questions, sectionByQuestion, 'S2', 2);
    expect(result.map((x) => x.code)).toEqual(['Q1', 'Q2']);
  });

  it("bo'limsiz savollarni keyingi bo'lim chegarasigacha kiritadi", () => {
    const sections = [section({ code: 'S1', displayOrder: 1 }), section({ code: 'S2', displayOrder: 2 })];
    const questions = [
      q({ code: 'Q1', order: 1 }), // bo'limsiz, S2 dan oldin
      q({ code: 'Q2', order: 2 }), // S2 ga tegishli
    ];
    const sectionByQuestion = new Map([['Q2', 'S2']]);

    const result = questionsBeforeSection(sections, questions, sectionByQuestion, 'S2', 2);
    expect(result.map((x) => x.code)).toEqual(['Q1']);
  });
});

describe('valueOptionsForQuestion', () => {
  it('variantli savolda variantlar tartib bo’yicha qaytadi', () => {
    const question = q({
      type: 'SingleChoice',
      options: [
        { textUz: 'B', value: 2, displayOrder: 2 },
        { textUz: 'A', value: 1, displayOrder: 1 },
      ],
    });
    expect(valueOptionsForQuestion(question)).toEqual([
      { value: 1, label: 'A' },
      { value: 2, label: 'B' },
    ]);
  });

  it('Likert5 — 1..5, Likert7 — 1..7, Binary — 0/1', () => {
    expect(valueOptionsForQuestion(q({ type: 'Likert5' }))?.map((o) => o.value)).toEqual([
      1, 2, 3, 4, 5,
    ]);
    expect(valueOptionsForQuestion(q({ type: 'Likert7' }))?.map((o) => o.value)).toEqual([
      1, 2, 3, 4, 5, 6, 7,
    ]);
    expect(valueOptionsForQuestion(q({ type: 'Binary' }))?.map((o) => o.value)).toEqual([0, 1]);
  });

  it('matn turlarida null (qiymat tanlanmaydi)', () => {
    expect(valueOptionsForQuestion(q({ type: 'ShortText' }))).toBeNull();
  });

  it('savol topilmasa null', () => {
    expect(valueOptionsForQuestion(undefined)).toBeNull();
  });
});

describe('describeVisibilityCondition / describeVisibilityRule', () => {
  const question = q({
    code: 'Q1_6',
    type: 'SingleChoice',
    options: [
      { textUz: "Ha, Intellect o'quv markazida o'qiyman", value: 1, displayOrder: 1 },
      { textUz: 'Yo’q', value: 2, displayOrder: 2 },
    ],
  });

  it("docs/18 §6.3 namunasiga mos jumla quradi", () => {
    const text = describeVisibilityCondition(
      { questionCode: 'Q1_6', operator: 'Equals', values: [1] },
      question,
    );
    expect(text).toBe(
      'Q1_6 savoliga javob "Ha, Intellect o\'quv markazida o\'qiyman" bo\'lsa',
    );
  });

  it('savol topilmasa xom kod bilan ogohlantiradi', () => {
    expect(
      describeVisibilityCondition({ questionCode: 'GHOST', operator: 'Equals', values: [1] }, undefined),
    ).toContain('GHOST');
  });

  it('describeVisibilityRule bir nechta shartni VA/YOKI bilan birlashtiradi', () => {
    const rule: VisibilityRule = {
      match: 'All',
      conditions: [
        { questionCode: 'Q1_6', operator: 'Equals', values: [1] },
        { questionCode: 'Q1_6', operator: 'Answered', values: [] },
      ],
    };
    const text = describeVisibilityRule(rule, [question]);
    expect(text).toContain(' VA ');
    expect(text.endsWith("ko'rsatilsin.")).toBe(true);
  });

  it('shart yo’q bo’lsa bo’sh satr', () => {
    expect(describeVisibilityRule(null, [question])).toBe('');
    expect(describeVisibilityRule({ match: 'All', conditions: [] }, [question])).toBe('');
  });
});

describe('createDefaultCondition', () => {
  it("qiymatli savolda birinchi operator va birinchi qiymatni tanlaydi", () => {
    const question = q({
      type: 'SingleChoice',
      options: [{ textUz: 'A', value: 5, displayOrder: 1 }],
    });
    expect(createDefaultCondition(question)).toEqual({
      questionCode: 'Q1',
      operator: 'Equals',
      values: [5],
    });
  });

  it('matn savolida qiymatsiz Answered operatori', () => {
    const question = q({ code: 'QT', type: 'ShortText' });
    expect(createDefaultCondition(question)).toEqual({
      questionCode: 'QT',
      operator: 'Answered',
      values: [],
    });
  });
});
