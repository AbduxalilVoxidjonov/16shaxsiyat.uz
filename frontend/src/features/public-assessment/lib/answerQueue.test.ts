import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  clearAnswerStore,
  markSent,
  pendingByTestCode,
  readAnswerStore,
  upsertAnswer,
  valuesForTest,
  writeAnswerStore,
} from './answerQueue';

describe('answerQueue', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("bo'sh localStorage'dan bo'sh obyekt o'qiydi", () => {
    expect(readAnswerStore()).toEqual({});
  });

  it('yozilgan store keyinroq xuddi shu shaklda o\'qiladi (resume)', () => {
    const store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 1200 });
    writeAnswerStore(store);

    expect(readAnswerStore()).toEqual({
      q1: { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 1200, pending: true },
    });
  });

  it('upsertAnswer yangi javobni pending:true bilan qo\'shadi', () => {
    const store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 3, durationMs: 500 });
    expect(store['q1']).toMatchObject({ value: 3, pending: true });
  });

  it("upsertAnswer qayta chaqirilsa qiymatni yangilaydi va yana pending:true qiladi", () => {
    let store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 3, durationMs: 500 });
    store = markSent(store, ['q1']);
    expect(store['q1']?.pending).toBe(false);

    store = upsertAnswer(store, { testCode: 'BIG5', questionId: 'q1', value: 5, durationMs: 900 });
    expect(store['q1']).toMatchObject({ value: 5, durationMs: 900, pending: true });
  });

  it("markSent yozuvni o'chirmaydi — faqat pending:false qiladi (natija ekranda saqlanib qolishi uchun)", () => {
    let store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 2, durationMs: 100 });
    store = markSent(store, ['q1']);

    expect(store['q1']).toMatchObject({ value: 2, pending: false });
    expect(Object.keys(store)).toContain('q1');
  });

  it("markSent mavjud bo'lmagan questionId'larni e'tiborsiz qoldiradi", () => {
    const store = markSent({}, ['unknown']);
    expect(store).toEqual({});
  });

  it("pendingByTestCode faqat pending:true yozuvlarni testCode bo'yicha guruhlaydi", () => {
    let store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 1, durationMs: 10 });
    store = upsertAnswer(store, { testCode: 'BIG5', questionId: 'q2', value: 2, durationMs: 20 });
    store = upsertAnswer(store, { testCode: 'RIASEC', questionId: 'q3', value: 3, durationMs: 30 });
    store = markSent(store, ['q2']); // q2 endi pending emas

    const groups = pendingByTestCode(store);
    expect(groups['BIG5']?.map((entry) => entry.questionId)).toEqual(['q1']);
    expect(groups['RIASEC']?.map((entry) => entry.questionId)).toEqual(['q3']);
  });

  it("valuesForTest berilgan testCode'ga tegishli barcha qiymatlarni (pending yoki yo'q) qaytaradi", () => {
    let store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 10 });
    store = markSent(store, ['q1']);
    store = upsertAnswer(store, { testCode: 'BIG5', questionId: 'q2', value: 2, durationMs: 20 });
    store = upsertAnswer(store, { testCode: 'RIASEC', questionId: 'q3', value: 5, durationMs: 30 });

    expect(valuesForTest(store, 'BIG5')).toEqual({ q1: 4, q2: 2 });
    expect(valuesForTest(store, 'RIASEC')).toEqual({ q3: 5 });
  });

  it("clearAnswerStore localStorage'ni tozalaydi (410 sessiya tugagandan keyin)", () => {
    writeAnswerStore(upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 1, durationMs: 1 }));
    clearAnswerStore();
    expect(readAnswerStore()).toEqual({});
  });

  it("buzilgan JSON localStorage'da bo'lsa xatosiz bo'sh obyekt qaytaradi", () => {
    localStorage.setItem('shaxsiyat.pendingAnswers', '{not-json');
    expect(readAnswerStore()).toEqual({});
  });
});
