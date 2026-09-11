import { readFileSync } from 'node:fs';
import path from 'node:path';
import { describe, expect, it } from 'vitest';
import type { components } from '@/shared/api/schema';
import {
  toImportIssues,
  validateTestImportFile,
  validateTestImportObject,
  type TestImportFile,
} from './importSchema';

/**
 * `docs/18` §7 — egasining namunasi. Bu fayl endi SEED (`DbSeeder.SeedSurveysAsync`, yagona
 * manba `Infrastructure/Persistence/SeedData/surveys/`), avvalgi `docs/examples/` nusxasi
 * olib tashlangan — ikki joyda holat saqlash chalkashlikka olib keladi (`CLAUDE.md`).
 * `process.cwd()` — Vitest'ni ishga tushirgan katalog (`frontend/`, `package.json`dagi
 * `test` buyrug'i shu yerdan chaqiriladi); `import.meta.url` bu yerda ishlatilmaydi, chunki
 * Vitest'ning modul transformi uni doim `file://` sxemali qilib bermaydi.
 */
const SOROVNOMA_INTELLECT_PATH = path.resolve(
  process.cwd(),
  '../src/StudentRoadMap.Infrastructure/Persistence/SeedData/surveys/intellect-survey.json',
);

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
      scales: [{ scale: 'STRESS', questionCount: 4, bandCount: 0 }],
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
      { scale: 'A', questionCount: 4, bandCount: 0 },
      { scale: 'B', questionCount: 4, bandCount: 0 },
    ]);
  });

  it("noto'g'ri kod formatida (kichik harf) SCHEMA_INVALID beradi", () => {
    const file = validFile({ code: 'stress' });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.canImport).toBe(false);
    expect(result.issues.some((issue) => issue.code === 'SCHEMA_INVALID')).toBe(true);
  });
});

/**
 * Excel yo'li (P39) — `POST /api/admin/catalog/import/parse-excel` javobi MAVJUD JSON import
 * sxemasiga tushishi shart, aks holda "ikkita import mantiqi yo'q" qarori buziladi.
 *
 * Fikstura AYNAN backend DTO'si bilan tiplangan (`schema.d.ts`) — server shakli o'zgarsa
 * `npm run typecheck` qizaradi; sxema mos kelmay qolsa esa quyidagi tekshiruv qizaradi. Ya'ni
 * drift ikkala tomondan ham ushlanadi.
 */
describe('Excel parse natijasi ↔ JSON import sxemasi', () => {
  const parsed: components['schemas']['CatalogExcelTestDto'] = {
    code: 'STRESS',
    nameUz: 'Stressga chidamlilik anketasi',
    descriptionUz: null,
    estimatedMinutes: 6,
    pageSize: 10,
    scoringMode: 'Scored',
    scales: [
      {
        code: 'STRESS',
        nameUz: 'Stressga munosabat',
        descriptionUz: null,
        interpretationBands: [
          { from: 0, to: 33, label: 'Past' },
          { from: 34, to: 66, label: "O'rtacha" },
          { from: 67, to: 100, label: 'Yuqori' },
        ],
      },
    ],
    questions: Array.from({ length: 4 }, (_, index) => ({
      code: `ST-Q0${String(index + 1)}`,
      order: index + 1,
      textUz: 'Savol matni',
      type: 'Likert5',
      scale: 'STRESS',
      direction: 1,
      weight: 1,
      isRequired: true,
    })),
  };

  it('serverdan kelgan obyektni o‘zgarishsiz qabul qiladi', () => {
    const result = validateTestImportObject(parsed);

    expect(result.issues).toEqual([]);
    expect(result.canImport).toBe(true);
    expect(result.preview?.scales).toEqual([
      { scale: 'STRESS', questionCount: 4, bandCount: 3 },
    ]);
  });

  it("oraliqlar qoidasi buzilsa yuklashga ruxsat bermaydi (nashrgacha ushlanadi)", () => {
    // `0–33` / `35–100` — 34 hech qaysi oraliqqa tushmaydi. Bu xato ilgari faqat NASHR
    // bosqichida ko'rinardi, ya'ni import tugagach, xato joyidan uzoqda.
    const result = validateTestImportObject({
      ...parsed,
      scales: [
        {
          ...parsed.scales[0]!,
          interpretationBands: [
            { from: 0, to: 33, label: 'Past' },
            { from: 35, to: 100, label: 'Yuqori' },
          ],
        },
      ],
    });

    expect(result.canImport).toBe(false);
    expect(result.issues.map((issue) => issue.code)).toContain('SCALE_BAND_GAP');
  });

  it('kasrli chegara aniq xato beradi', () => {
    const result = validateTestImportObject({
      ...parsed,
      scales: [
        {
          ...parsed.scales[0]!,
          interpretationBands: [
            { from: 0, to: 33.3, label: 'Past' },
            { from: 33.4, to: 100, label: 'Yuqori' },
          ],
        },
      ],
    });

    expect(result.issues.map((issue) => issue.code)).toContain('SCALE_BAND_NOT_INTEGER');
  });

  it('server xatolari varaq nomi bilan birga ko‘rsatiladi va yuklashni to‘xtatadi', () => {
    const serverIssues = toImportIssues({
      data: parsed,
      issues: [
        {
          code: 'QUESTION_DIRECTION_INVALID',
          message: "5-qatorda yo'nalish faqat 1 yoki -1 bo'lishi mumkin.",
          sheet: 'Savollar',
          row: 5,
          questionCode: 'ST-Q05',
          scale: null,
        },
      ],
    } satisfies components['schemas']['ParseCatalogExcelResultDto']);

    expect(serverIssues[0]?.message).toContain('"Savollar" varag\'i');

    const result = validateTestImportObject(parsed, serverIssues);
    expect(result.canImport).toBe(false);
    // Oldindan ko'rish BARIBIR chiqadi — admin nechta savol/shkala borligini ko'rishi kerak.
    expect(result.preview).not.toBeNull();
  });
});

/**
 * `docs/18` §7 — egasining haqiqiy so'rovnomasi (5 bo'lim, 25 savol, 1.6-savol filtri,
 * ikkita "Boshqa (kiriting)" tarmog'i, bitta `ContainsAny` sharti). Import dialogi buni
 * XATOSIZ o'qiy olishi SHART (P52 topshirig'i) — shu sabab bu test QULFLANGAN: fayl yoki
 * sxema o'zgarsa, shu yerda darhol qizarishi kerak.
 */
describe("SeedData/surveys/intellect-survey.json — tarmoqlanuvchi so'rovnoma namunasi", () => {
  it('xatosiz o‘qiladi va importga tayyor', () => {
    const raw = readFileSync(SOROVNOMA_INTELLECT_PATH, 'utf-8');
    const result = validateTestImportFile(raw);

    expect(result.issues).toEqual([]);
    expect(result.canImport).toBe(true);
    expect(result.data?.sections).toHaveLength(5);
    expect(result.data?.questions).toHaveLength(25);
    expect(result.preview?.questionCount).toBe(25);
  });

  it('bo‘lim va savol sxemasi to‘g‘ri o‘qiladi (kod, sarlavha, shart, sectionCode)', () => {
    const raw = readFileSync(SOROVNOMA_INTELLECT_PATH, 'utf-8');
    const result = validateTestImportFile(raw);

    const s2a = result.data?.sections?.find((s) => s.code === 'S2A');
    expect(s2a?.visibility).toEqual({
      match: 'All',
      conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }],
    });

    const q16 = result.data?.questions.find((q) => q.code === 'Q1_6');
    expect(q16?.sectionCode).toBe('S1');
    expect(q16?.type).toBe('SingleChoice');
    expect(q16?.options).toHaveLength(3);

    const otherQuestion = result.data?.questions.find((q) => q.code === 'Q2A_1_OTHER');
    expect(otherQuestion?.visibility).toEqual({
      match: 'All',
      conditions: [{ questionCode: 'Q2A_1', operator: 'ContainsAny', values: [99] }],
    });
  });
});

describe("bo'lim/shart bo'lmagan eski fayllar (regressiya)", () => {
  it("sections/visibility/options maydonlarisiz fayl o'zgarishsiz o'qiladi", () => {
    const result = validateTestImportFile(JSON.stringify(validFile()));
    expect(result.canImport).toBe(true);
    expect(result.data?.sections).toBeUndefined();
    expect(result.data?.questions[0]?.sectionCode).toBeUndefined();
    expect(result.data?.questions[0]?.visibility).toBeUndefined();
  });
});

describe("yangi savol turlari va variantlar — QUESTION_OPTIONS_REQUIRED/QUESTION_OPTION_VALUE_DUPLICATE", () => {
  it("variantli savolda 2 tadan kam variant bo'lsa QUESTION_OPTIONS_REQUIRED", () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1 }),
        validQuestion({ code: 'Q02', order: 2 }),
        validQuestion({ code: 'Q03', order: 3 }),
        validQuestion({
          code: 'Q04',
          order: 4,
          type: 'SingleChoice',
          options: [{ textUz: 'A', value: 1, displayOrder: 1 }],
        }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(result.issues.some((issue) => issue.code === 'QUESTION_OPTIONS_REQUIRED')).toBe(true);
  });

  it('takroriy variant qiymati bo‘lsa QUESTION_OPTION_VALUE_DUPLICATE', () => {
    const file = validFile({
      questions: [
        validQuestion({ code: 'Q01', order: 1 }),
        validQuestion({ code: 'Q02', order: 2 }),
        validQuestion({ code: 'Q03', order: 3 }),
        validQuestion({
          code: 'Q04',
          order: 4,
          type: 'SingleChoice',
          options: [
            { textUz: 'A', value: 1, displayOrder: 1 },
            { textUz: 'B', value: 1, displayOrder: 2 },
          ],
        }),
      ],
    });
    const result = validateTestImportFile(JSON.stringify(file));
    expect(
      result.issues.some((issue) => issue.code === 'QUESTION_OPTION_VALUE_DUPLICATE'),
    ).toBe(true);
  });
});

