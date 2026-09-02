import { z } from 'zod';

/**
 * JSON test yuklash — `prompts/35` B3-band: "seed fayllari bilan bir xil sxema
 * (`code`, `nameUz`, `questions[]` va h.k. — `prompts/04` dagi sxema)". Aynan
 * `src/StudentRoadMap.Infrastructure/Persistence/SeedData/test-definitions/*.json`
 * shakli (masalan `big5.json`) tekshirilib olingan — quyidagi sxema o'sha fayllarning
 * hammasini (`mbti16`, `big5`, `riasec`, `activity`) muvaffaqiyatli o'qiydi.
 *
 * Bu validatsiya **mijoz tomonida, yuklashdan oldin** ishlaydi (`prompts/35` B4-band:
 * "yuklashdan oldin validatsiya va preview") — fayl hech qachon tarmoqqa yuborilmasdan
 * turib xatolar ko'rsatiladi. Backend (`POST /api/admin/catalog/tests` +
 * `.../questions/import`) yakuniy haqiqat manbai bo'lib qoladi; bu yerdagi tekshiruv
 * faqat tezroq va aniqroq xabar berish uchun (`schoolFormSchema.ts`dagi naqsh).
 */

const QUESTION_TYPE_VALUES = ['Likert5'] as const;

const questionSchema = z.object({
  code: z.string().trim().min(1),
  order: z.number().int().positive(),
  textUz: z.string().trim().min(1),
  type: z.enum(QUESTION_TYPE_VALUES),
  scale: z.string().trim().min(1),
  direction: z.union([z.literal(1), z.literal(-1)]),
  weight: z.number().positive(),
  isRequired: z.boolean().optional(),
});

export const testImportFileSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1)
    .regex(
      /^[A-Z0-9_-]+$/,
      "Kod faqat lotin katta harflari, raqam, '_' va '-' belgilaridan iborat bo'lishi kerak.",
    ),
  nameUz: z.string().trim().min(1),
  descriptionUz: z.string().trim().optional(),
  version: z.number().int().positive().optional(),
  displayOrder: z.number().int().optional(),
  estimatedMinutes: z.number().int().positive(),
  pageSize: z.number().int().positive().optional(),
  shuffleQuestions: z.boolean().optional(),
  questions: z.array(questionSchema).min(1, 'Kamida bitta savol bo‘lishi kerak.'),
});

export type TestImportFile = z.infer<typeof testImportFileSchema>;

/** Kamida shu sondagi savol bo'lmasa shkala talqini ishonchsiz bo'ladi (`docs/11` A-8 namunasi). */
const MIN_QUESTIONS_PER_SCALE = 4;

export interface ImportIssue {
  code: string;
  message: string;
  questionCode?: string;
  scale?: string;
}

export interface ScaleSummary {
  scale: string;
  questionCount: number;
}

export interface ImportPreview {
  code: string;
  nameUz: string;
  questionCount: number;
  scales: ScaleSummary[];
  estimatedMinutes: number;
}

/**
 * Natija — sxema noto'g'ri bo'lsa ham (`data`/`preview` yo'q) fayl haqida imkon qadar ko'proq
 * ma'lumot beriladi; sxema to'g'ri bo'lsa doim `preview` qaytadi — hatto semantik xatolar
 * (`issues`) bo'lsa ham, admin nechta savol/shkala borligini ko'rishi kerak
 * (`prompts/35` B4-band: "preview + xatolar ro'yxati" — ikkalasi BIRGA). `canImport`
 * `false` bo'lsa yuklash tugmasi o'chiriladi.
 */
export interface ImportValidationResult {
  canImport: boolean;
  data: TestImportFile | null;
  preview: ImportPreview | null;
  issues: ImportIssue[];
}

/**
 * Fayl matnini o'qiydi: 1) JSON parse, 2) sxema (`testImportFileSchema`), 3) semantik
 * qoidalar (dublikat kod/tartib, shkala boshiga minimal savol soni). Struktura xatosi
 * bo'lsa (JSON emas / sxemaga mos emas) `preview: null` bilan qaytadi — semantik tekshiruv
 * faqat struktura to'g'ri bo'lsa ishlaydi (`questions[].scale`ga tayanadi).
 */
export function validateTestImportFile(raw: string): ImportValidationResult {
  let json: unknown;
  try {
    json = JSON.parse(raw) as unknown;
  } catch {
    return {
      canImport: false,
      data: null,
      preview: null,
      issues: [{ code: 'INVALID_JSON', message: 'Fayl yaroqli JSON emas.' }],
    };
  }

  const parsed = testImportFileSchema.safeParse(json);
  if (!parsed.success) {
    const issues: ImportIssue[] = parsed.error.issues.map((issue) => ({
      code: 'SCHEMA_INVALID',
      message: `${issue.path.join('.')}: ${issue.message}`,
    }));
    return { canImport: false, data: null, preview: null, issues };
  }

  const data = parsed.data;
  const issues: ImportIssue[] = [];

  const seenCodes = new Set<string>();
  const seenOrders = new Set<number>();
  const scaleCounts = new Map<string, number>();

  for (const question of data.questions) {
    if (seenCodes.has(question.code)) {
      issues.push({
        code: 'QUESTION_CODE_DUPLICATE',
        questionCode: question.code,
        message: `Savol kodi takrorlangan: ${question.code}`,
      });
    }
    seenCodes.add(question.code);

    if (seenOrders.has(question.order)) {
      issues.push({
        code: 'QUESTION_ORDER_DUPLICATE',
        questionCode: question.code,
        message: `Tartib raqami takrorlangan (${String(question.order)}): ${question.code}`,
      });
    }
    seenOrders.add(question.order);

    scaleCounts.set(question.scale, (scaleCounts.get(question.scale) ?? 0) + 1);
  }

  const scales: ScaleSummary[] = [...scaleCounts.entries()]
    .map(([scale, questionCount]) => ({ scale, questionCount }))
    .sort((a, b) => a.scale.localeCompare(b.scale));

  for (const { scale, questionCount } of scales) {
    if (questionCount < MIN_QUESTIONS_PER_SCALE) {
      issues.push({
        code: 'SCALE_TOO_FEW_QUESTIONS',
        scale,
        message: `"${scale}" shkalasida kamida ${String(MIN_QUESTIONS_PER_SCALE)} savol kerak, hozir ${String(questionCount)}`,
      });
    }
  }

  const preview: ImportPreview = {
    code: data.code,
    nameUz: data.nameUz,
    questionCount: data.questions.length,
    scales,
    estimatedMinutes: data.estimatedMinutes,
  };

  return { canImport: issues.length === 0, data, preview, issues };
}
