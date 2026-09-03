import { describe, expect, it } from 'vitest';
import { validateTestImportFile, type TestImportFile } from './importSchema';

/** Fayl ichidagi bitta savol — `testImportFileSchema.questions` elementi. */
type ImportQuestion = TestImportFile['questions'][number];

function validQuestion(overrides: Partial<ImportQuestion> = {}): ImportQuestion {
  return {
    code: 'Q01',
    order: 1,
    textUz: 'Savol matni',
    type: 'Likert5',
    scale: 'STRESS',
    direction: 1,
    weight: 1,
    isRequired: true,
    ...overrides,
  };
}

function validFile(overrides: Partial<TestImportFile> = {}): TestImportFile {
  return {
    code: 'STRESS',
    nameUz: 'Stressga chidamlilik anketasi',
    descriptionUz: 'Tavsif',
    estimatedMinutes: 6,
    pageSize: 10,
    shuffleQuestions: false,
    questions: [
      validQuestion({ code: 'Q01', order: 1 }),
      validQuestion({ code: 'Q02', order: 2, direction: -1 }),
      validQuestion({ code: 'Q03', order: 3 }),
      validQuestion({ code: 'Q04', order: 4 }),
    ],
    ...overrides,
  };
}

describe('validateTestImportFile', () => {
  it("to'g'ri JSON bilan preview qaytaradi va yuklashga ruxsat beradi", () => {
    const result = validateTestImportFile(JSON.stringify(validFile()));
    expect(result.canImport).toBe(true);
    expect(result.issues).toHaveLength(0);
    expect(result.preview).toEqual({
      code: 'STRESS',
      nameUz: 'Stressga chidamlilik anketasi',
      questionCount: 4,
      scales: [{ scale: 'STRESS', questionCount: 4 }],
      estimatedMinutes: 6,
    });
  });

  it('yaroqsiz JSON matnida INVALID_JSON xatosi beradi va preview bermaydi', () => {
    const result = validateTestImportFile('{ not json');
    expect(result.canImport).toBe(false);
    expect(result.preview).toBeNull();
    expect(result.issues[0]?.code).toBe('INVALID_JSON');
  });

  it("sxemaga mos kelmasa (masalan `questions` yo'q) SCHEMA_INVALID xatosi beradi", () => {
    const result = validateTestImportFile(JSON.stringify({ code: 'X', nameUz: 'X' }));
    expect(result.canImport).toBe(false);
    expect(result.preview).toBeNull();
    expect(result.issues.some((issue) => issue.code === 'SCHEMA_INVALID')).toBe(true);
  });

  it('bir xil savol kodi takrorlansa QUESTION_CODE_DUPLICATE xatosi beradi, lekin preview baribir qaytadi', () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1 }),
        validQuestion({ code: 'Q01', order: 2 }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.canImport).toBe(false);
    expect(result.preview).not.toBeNull();
    expect(result.issues.some((issue) => issue.code === 'QUESTION_CODE_DUPLICATE')).toBe(true);
  });

  it('bir xil tartib raqami takrorlansa QUESTION_ORDER_DUPLICATE xatosi beradi', () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1 }),
        validQuestion({ code: 'Q02', order: 1 }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.issues.some((issue) => issue.code === 'QUESTION_ORDER_DUPLICATE')).toBe(true);
  });

  it("shkalada 4 tadan kam savol bo'lsa SCALE_TOO_FEW_QUESTIONS xatosi beradi", () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1, scale: 'A' }),
        validQuestion({ code: 'Q02', order: 2, scale: 'A' }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.canImport).toBe(false);
    expect(result.issues).toEqual([
      expect.objectContaining({ code: 'SCALE_TOO_FEW_QUESTIONS', scale: 'A' }),
    ]);
  });

  it("bir nechta shkalani to'g'ri hisoblaydi va alifbo tartibida qaytaradi", () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1, scale: 'B' }),
        validQuestion({ code: 'Q02', order: 2, scale: 'B' }),
        validQuestion({ code: 'Q03', order: 3, scale: 'B' }),
        validQuestion({ code: 'Q04', order: 4, scale: 'B' }),
        validQuestion({ code: 'Q05', order: 5, scale: 'A' }),
        validQuestion({ code: 'Q06', order: 6, scale: 'A' }),
        validQuestion({ code: 'Q07', order: 7, scale: 'A' }),
        validQuestion({ code: 'Q08', order: 8, scale: 'A' }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.canImport).toBe(true);
    expect(result.preview?.scales).toEqual([
      { scale: 'A', questionCount: 4 },
      { scale: 'B', questionCount: 4 },
    ]);
  });

  it("noto'g'ri kod formatida (kichik harf) SCHEMA_INVALID beradi", () => {
    const file = validFile({ code: 'stress' });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.canImport).toBe(false);
    expect(result.issues.some((issue) => issue.code === 'SCHEMA_INVALID')).toBe(true);
  });
});
