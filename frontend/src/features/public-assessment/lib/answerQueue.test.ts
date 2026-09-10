import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  answersForTest,
  clearAnswerStore,
  filterVisibleAnswers,
  markSent,
  pendingByTestCode,
  readAnswerStore,
  toWireItem,
  upsertAnswer,
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

  it("answersForTest berilgan testCode'ga tegishli barcha javoblarni (pending yoki yo'q) qaytaradi", () => {
    let store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 10 });
    store = markSent(store, ['q1']);
    store = upsertAnswer(store, { testCode: 'BIG5', questionId: 'q2', value: 2, durationMs: 20 });
    store = upsertAnswer(store, { testCode: 'RIASEC', questionId: 'q3', value: 5, durationMs: 30 });

    expect(answersForTest(store, 'BIG5')).toEqual({ q1: { value: 4 }, q2: { value: 2 } });
    expect(answersForTest(store, 'RIASEC')).toEqual({ q3: { value: 5 } });
  });

  it('answersForTest matn/ko\'p-tanlov javoblarini ham to\'g\'ri qaytaradi (docs/18 §4.2)', () => {
    let store = upsertAnswer({}, { testCode: 'SURVEY', questionId: 'q1', text: 'Karimov Ali', durationMs: 10 });
    store = upsertAnswer(store, { testCode: 'SURVEY', questionId: 'q2', selectedValues: [1, 3], durationMs: 20 });

    expect(answersForTest(store, 'SURVEY')).toEqual({
      q1: { text: 'Karimov Ali' },
      q2: { selectedValues: [1, 3] },
    });
  });

  it('toWireItem faqat to\'ldirilgan maydonni qo\'shadi — ortiqcha undefined kalit yo\'q', () => {
    const store = upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 10 });
    const item = store['q1'];
    expect(item).toBeDefined();
    expect(toWireItem(item!)).toEqual({ questionId: 'q1', value: 4, durationMs: 10 });
    expect(Object.keys(toWireItem(item!))).toEqual(['questionId', 'durationMs', 'value']);
  });

  it("filterVisibleAnswers ko'rinmas ID'larni chiqarib tashlaydi (docs/18 §6.2)", () => {
    let store = upsertAnswer({}, { testCode: 'SURVEY', questionId: 'q1', value: 1, durationMs: 10 });
    store = upsertAnswer(store, { testCode: 'SURVEY', questionId: 'q2', value: 2, durationMs: 20 });
    const items = pendingByTestCode(store)['SURVEY'] ?? [];

    const visible = filterVisibleAnswers(items, new Set(['q1']));
    expect(visible.map((item) => item.questionId)).toEqual(['q1']);

    // `visibleIds` berilmasa — hech narsa filtrlanmaydi (bo'limsiz oqim, mavjud xatti-harakat).
    expect(filterVisibleAnswers(items, undefined).map((item) => item.questionId)).toEqual(['q1', 'q2']);
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
