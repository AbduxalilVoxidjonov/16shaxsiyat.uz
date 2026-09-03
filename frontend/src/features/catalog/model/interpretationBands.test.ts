import { describe, expect, it } from 'vitest';
import {
  MAX_EQUAL_SPLIT_COUNT,
  splitIntoEqualBands,
  validateInterpretationBands,
} from './interpretationBands';
import type { InterpretationBand } from './types';

function band(from: number, to: number, label = 'Daraja'): InterpretationBand {
  return { from, to, label };
}

/**
 * Qoida manbai — `docs/03` §6.3 va backend `CatalogPublishValidator.ValidateBandCoverage`.
 * Bu testlar ikkalasining ham "oltin" holatlari: hujjatdagi to'g'ri va xato misollar.
 */
describe('validateInterpretationBands', () => {
  it("0–33 / 34–66 / 67–100 — hujjatdagi to'g'ri misol, xato yo'q", () => {
    expect(validateInterpretationBands([band(0, 33), band(34, 66), band(67, 100)])).toEqual([]);
  });

  it("bitta 0–100 oralig'i ham to'g'ri", () => {
    expect(validateInterpretationBands([band(0, 100)])).toEqual([]);
  });

  it("tartibsiz kiritilgan (lekin to'g'ri) oraliqlar saralanadi va o'tadi", () => {
    expect(validateInterpretationBands([band(67, 100), band(0, 33), band(34, 66)])).toEqual([]);
  });

  it("oraliq umuman yo'q → SCALE_BANDS_MISSING", () => {
    expect(validateInterpretationBands([])).toEqual(['SCALE_BANDS_MISSING']);
  });

  it("kasrli chegara → faqat SCALE_BAND_NOT_INTEGER (qolgan tekshiruvlar to'xtaydi)", () => {
    // `0–33.3` / `33.4–66.6` / `66.7–100` — `33.35` ballli o'quvchi hech qaysi oraliqqa
    // tushmaydi va `SUM` ishlash vaqtida yiqiladi. Aynan shu holat uchun qoida qat'iylashtirilgan.
    expect(validateInterpretationBands([band(0, 33.3), band(33.4, 66.6), band(66.7, 100)])).toEqual(
      ['SCALE_BAND_NOT_INTEGER'],
    );
  });

  it("bo'sh maydon (NaN) ham butun son emas → SCALE_BAND_NOT_INTEGER", () => {
    expect(validateInterpretationBands([band(0, Number.NaN), band(34, 100)])).toEqual([
      'SCALE_BAND_NOT_INTEGER',
    ]);
  });

  it("bo'shliq (0–33 / 35–100) → SCALE_BAND_GAP", () => {
    expect(validateInterpretationBands([band(0, 33), band(35, 100)])).toEqual(['SCALE_BAND_GAP']);
  });

  it('ustma-ust (0–50 / 50–100, `to` inklyuziv) → SCALE_BAND_OVERLAP', () => {
    expect(validateInterpretationBands([band(0, 50), band(50, 100)])).toEqual([
      'SCALE_BAND_OVERLAP',
    ]);
  });

  it("to'liq emas (0–99) → SCALE_BAND_INCOMPLETE", () => {
    expect(validateInterpretationBands([band(0, 99)])).toEqual(['SCALE_BAND_INCOMPLETE']);
  });

  it('0 dan boshlanmasa ham SCALE_BAND_INCOMPLETE', () => {
    expect(validateInterpretationBands([band(1, 100)])).toEqual(['SCALE_BAND_INCOMPLETE']);
  });

  it('teskari oraliq (from > to) → faqat SCALE_BAND_INVALID', () => {
    expect(validateInterpretationBands([band(50, 20), band(51, 100)])).toEqual([
      'SCALE_BAND_INVALID',
    ]);
  });

  it("bir nechta buzilish birga qaytadi (to'liq emas + bo'shliq)", () => {
    expect(validateInterpretationBands([band(0, 30), band(40, 99)])).toEqual([
      'SCALE_BAND_INCOMPLETE',
      'SCALE_BAND_GAP',
    ]);
  });
});

describe('splitIntoEqualBands', () => {
  const labelAt = (index: number) => `D${String(index + 1)}`;

  it('3 → 0–33 / 34–66 / 67–100 (docs/03 §6.3 misoli)', () => {
    expect(splitIntoEqualBands(3, labelAt)).toEqual([
      { from: 0, to: 33, label: 'D1' },
      { from: 34, to: 66, label: 'D2' },
      { from: 67, to: 100, label: 'D3' },
    ]);
  });

  it('1 → yagona 0–100', () => {
    expect(splitIntoEqualBands(1, labelAt)).toEqual([{ from: 0, to: 100, label: 'D1' }]);
  });

  it("natija har doim validatsiyadan o'tadi", () => {
    for (let count = 1; count <= MAX_EQUAL_SPLIT_COUNT; count += 1) {
      expect(validateInterpretationBands(splitIntoEqualBands(count, labelAt))).toEqual([]);
    }
  });

  it("noto'g'ri son (0, kasr, chegaradan katta) → bo'sh ro'yxat", () => {
    expect(splitIntoEqualBands(0, labelAt)).toEqual([]);
    expect(splitIntoEqualBands(2.5, labelAt)).toEqual([]);
    expect(splitIntoEqualBands(MAX_EQUAL_SPLIT_COUNT + 1, labelAt)).toEqual([]);
  });
});
