/// <reference types="node" />
// ^ `tsconfig.app.json`dagi `types: ["vite/client"]` global Node ambient tiplarini kiritmaydi
// (atayin — brauzer kodi Node globallarini ko'rmasligi kerak). Bu fayl faqat TEST muhitida
// ishlaydi (Vitest — Node process) va `node:fs`/`process` ishlatadi, shu sabab shu faylga
// XOS ravishda Node tiplari ulanadi (global tsconfig o'zgarmaydi).
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  resolveVisibleQuestions,
  type AnswerSnapshot,
  type QuestionSnapshot,
  type SectionSnapshot,
  type VisibilityRule,
} from './visibility';

/**
 * OLTIN FIKSTURA (docs/18-tarmoqlanuvchi-sorovnoma.md §6.1) — `tests/fixtures/
 * visibility-golden.json`ni C# tomoni bilan BIR XIL fayldan o'qiydi
 * (`tests/StudentRoadMap.Domain.Tests/Branching/VisibilityGoldenTests.cs`). Bu — ikkala
 * nusxaning bir-biridan ajralib ketmasligining YAGONA kafolati.
 *
 * `fs.readFileSync` bilan `process.cwd()`dan (REPO ILDIZI EMAS, `frontend/` — `npm run test`
 * shu papkadan ishga tushiriladi, `CLAUDE.md` "Foydali buyruqlar") NISBIY o'qiladi —
 * `import fixture from '../../../../tests/…'` emas: (1) `tsconfig.app.json`dagi
 * `include: ["src"]` chegarasi frontend papkasidan tashqaridagi faylni statik import qilishga
 * yo'l qo'ymaydi; (2) Vitest/Vite `import.meta.url`dan nisbiy `new URL(...)` orqali loyiha
 * ildizidan (bu yerda `frontend/`) TASHQARIGA chiqadigan yo'lni haqiqiy `file://` sifatida
 * EMAS, balki `http://localhost:.../@fs/...` virtual manzili sifatida qaytaradi (sinab
 * ko'rilgan — `fileURLToPath` bunday manzilda ishlamaydi). `process.cwd()` esa oddiy Node
 * satri, Vite modul grafigidan mustaqil.
 */
const FIXTURE_PATH = existsSync(resolve(process.cwd(), '../tests/fixtures/visibility-golden.json'))
  ? resolve(process.cwd(), '../tests/fixtures/visibility-golden.json')
  : resolve(process.cwd(), 'tests/fixtures/visibility-golden.json');

interface GoldenAnswer {
  rawValue: number | null;
  textValue: string | null;
  selectedValues: number[];
}

interface GoldenSection {
  code: string;
  order: number;
  visibility: VisibilityRule | null;
}

interface GoldenQuestion {
  code: string;
  order: number;
  type: string;
  sectionCode: string | null;
  visibility: VisibilityRule | null;
  options?: number[];
}

interface GoldenCase {
  name: string;
  sections: GoldenSection[];
  questions: GoldenQuestion[];
  answers: Record<string, GoldenAnswer>;
  expectedVisibleSectionCodes: string[];
  expectedVisibleQuestionCodes: string[];
}

interface GoldenFixture {
  cases: GoldenCase[];
}

function loadFixture(): GoldenFixture {
  const raw = readFileSync(FIXTURE_PATH, 'utf-8');
  return JSON.parse(raw) as GoldenFixture;
}

const fixture = loadFixture();

describe("visibility — oltin fikstura (docs/18 §6.1, C# bilan qulflangan)", () => {
  it("fikstura kamida bitta holat qamraydi (bo'sh fayl sezilmay qolmasin)", () => {
    expect(fixture.cases.length).toBeGreaterThan(0);
  });

  it.each(fixture.cases.map((testCase): [string, GoldenCase] => [testCase.name, testCase]))(
    '%s',
    (_name, testCase) => {
      const sections: SectionSnapshot[] = testCase.sections.map((section) => ({
        code: section.code,
        displayOrder: section.order,
        visibilityRule: section.visibility,
      }));
      const questions: QuestionSnapshot[] = testCase.questions.map((question) => ({
        code: question.code,
        displayOrder: question.order,
        sectionCode: question.sectionCode,
        visibilityRule: question.visibility,
      }));
      const answersByCode: Record<string, AnswerSnapshot> = {};
      for (const [code, answer] of Object.entries(testCase.answers)) {
        answersByCode[code] = {
          rawValue: answer.rawValue,
          textValue: answer.textValue,
          selectedValues: answer.selectedValues,
        };
      }

      const result = resolveVisibleQuestions(sections, questions, answersByCode);

      expect(result.visibleSectionCodes).toEqual(new Set(testCase.expectedVisibleSectionCodes));
      expect(result.visibleQuestionCodes).toEqual(new Set(testCase.expectedVisibleQuestionCodes));
    },
  );
});
