import { describe, expect, it } from 'vitest';
import {
  buildQuestionCreatePayload,
  buildQuestionUpdatePayload,
  toQuestionFormValues,
  type QuestionFormValues,
} from './questionPayload';
import type { CatalogQuestionItem } from './types';

function question(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-1',
    code: 'MB-Q01',
    order: 1,
    textUz: 'Yangi odamlar bilan tanishish menga oson.',
    textRu: null,
    textEn: null,
    type: 'Likert5',
    scale: 'EI',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: true,
    ...overrides,
  };
}

function formValues(overrides: Partial<QuestionFormValues> = {}): QuestionFormValues {
  return {
    textUz: 'Yangilangan matn',
    textRu: '',
    textEn: '',
    order: 1,
    isActive: true,
    isRequired: true,
    scale: 'EI',
    direction: 1,
    weight: 1,
    ...overrides,
  };
}

describe('buildQuestionUpdatePayload', () => {
  it('tizim savolida `scale`/`direction`/`weight` YUBORILMAYDI (409 SYSTEM_TEST_LOCKED oldini oladi)', () => {
    const payload = buildQuestionUpdatePayload(question(), formValues(), { isSystem: true });

    expect(payload.scale).toBeUndefined();
    expect(payload.direction).toBeUndefined();
    expect(payload.weight).toBeUndefined();
    // `JSON.stringify` `undefined` kalitlarni umuman chiqarmaydi — backend `is not null`
    // tekshiruvi ishga tushmasligi uchun aynan shu muhim.
    expect(Object.keys(JSON.parse(JSON.stringify(payload)) as object).sort()).toEqual([
      'isActive',
      'isRequired',
      'order',
      'textEn',
      'textRu',
      'textUz',
    ]);
  });

  it('tizim savolida matn va holat maydonlari saqlanadi', () => {
    const payload = buildQuestionUpdatePayload(
      question(),
      formValues({ textUz: '  Yangi matn  ', textRu: ' Ru matn ', isActive: false, order: 7 }),
      { isSystem: true },
    );

    expect(payload).toEqual({
      textUz: 'Yangi matn',
      textRu: 'Ru matn',
      textEn: null,
      isActive: false,
      order: 7,
      isRequired: true,
    });
  });

  it('`Custom` savolda uchala shkala maydoni ham yuboriladi', () => {
    const payload = buildQuestionUpdatePayload(
      question({ isSystem: false, scale: 'STRESS' }),
      formValues({ scale: 'SUPPORT', direction: -1, weight: 2 }),
      { isSystem: false },
    );

    expect(payload.scale).toBe('SUPPORT');
    expect(payload.direction).toBe(-1);
    expect(payload.weight).toBe(2);
  });

  it("`Custom` savolda shkala bo'sh qoldirilsa mavjud qiymat saqlanadi", () => {
    const payload = buildQuestionUpdatePayload(
      question({ isSystem: false, scale: 'STRESS' }),
      formValues({ scale: '   ' }),
      { isSystem: false },
    );

    expect(payload.scale).toBe('STRESS');
  });

  it("bo'sh `textRu`/`textEn` `null` sifatida yuboriladi", () => {
    const payload = buildQuestionUpdatePayload(question(), formValues({ textRu: '  ' }), {
      isSystem: true,
    });

    expect(payload.textRu).toBeNull();
    expect(payload.textEn).toBeNull();
  });
});

describe('toQuestionFormValues', () => {
  it("savol qatorini forma qiymatlariga o'giradi (`null` matnlar bo'sh satr bo'ladi)", () => {
    expect(toQuestionFormValues(question({ textRu: 'Ru', direction: -1, weight: 1.5 }))).toEqual({
      textUz: 'Yangi odamlar bilan tanishish menga oson.',
      textRu: 'Ru',
      textEn: '',
      order: 1,
      isActive: true,
      isRequired: true,
      scale: 'EI',
      direction: -1,
      weight: 1.5,
    });
  });
});

describe('buildQuestionCreatePayload', () => {
  it('kodni katta harfga keltiradi va barcha shkala maydonlarini yuboradi', () => {
    const payload = buildQuestionCreatePayload({
      ...formValues({ scale: 'stress', direction: -1, weight: 2, order: 3 }),
      code: ' st-q01 ',
      type: 'Likert5',
    });

    expect(payload).toEqual({
      code: 'ST-Q01',
      order: 3,
      textUz: 'Yangilangan matn',
      textRu: null,
      textEn: null,
      type: 'Likert5',
      scale: 'stress',
      direction: -1,
      weight: 2,
      isRequired: true,
    });
  });
});
