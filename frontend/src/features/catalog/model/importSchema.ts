import { z } from 'zod';
import {
  validateInterpretationBands,
  type InterpretationBandIssueCode,
} from './interpretationBands';

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

/**
 * `docs/03` §6.1 — superadmin anketasida javob turi `Likert5` (standart) yoki `Likert7`.
 * Seed JSON fayllarining hammasi `Likert5`; `Likert7` Excel yo'li bilan qo'shildi, chunki
 * eksport→import aylanmasida u JIMGINA `Likert5`ga aylanib ballash formulasini buzardi.
 */
const QUESTION_TYPE_VALUES = ['Likert5', 'Likert7'] as const;

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

/** `docs/03` §6.1 saqlash shakli: `{ "from": 0, "to": 33, "label": "Past" }`. */
const bandSchema = z.object({
  from: z.number(),
  to: z.number(),
  label: z.string().trim().min(1),
});

/**
 * Shkala — JSON seed sxemasida YO'Q, Excel faylida BOR. Ixtiyoriy: mavjud seed fayllari
 * (`big5.json` va h.k.) shkalasiz, ular baribir o'qilishi kerak.
 */
const scaleSchema = z.object({
  code: z.string().trim().min(1),
  nameUz: z.string().trim().min(1),
  descriptionUz: z.string().trim().nullish(),
  interpretationBands: z.array(bandSchema).default([]),
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
  descriptionUz: z.string().trim().nullish(),
  version: z.number().int().positive().optional(),
  displayOrder: z.number().int().optional(),
  estimatedMinutes: z.number().int().positive(),
  pageSize: z.number().int().positive().optional(),
  shuffleQuestions: z.boolean().optional(),
  scoringMode: z.enum(['Scored', 'Survey']).optional(),
  scales: z.array(scaleSchema).optional(),
  questions: z.array(questionSchema).min(1, 'Kamida bitta savol bo‘lishi kerak.'),
});

export type TestImportFile = z.infer<typeof testImportFileSchema>;

/**
 * `validateInterpretationBands` faqat KOD qaytaradi (backend `PublishIssueDto.Code` bilan
 * bir xil ro'yxat) — o'zbekcha matn shu yerda. Matnlar nashr oynasidagi bilan bir xil
 * ma'noda: superadmin ikki joyda bir xil xatoni bir xil tushunishi kerak.
 */
const INTERPRETATION_BAND_MESSAGES: Record<InterpretationBandIssueCode, string> = {
  SCALE_BANDS_MISSING: "talqin oraliqlari yo'q (\"Oraliqlar\" varag'iga qo'shing).",
  SCALE_BAND_NOT_INTEGER: 'oraliq chegaralari butun son bo\'lishi shart (33 ha, 33.5 yo\'q).',
  SCALE_BAND_INVALID: '"Dan" qiymati "Gacha" dan katta.',
  SCALE_BAND_INCOMPLETE: 'oraliqlar 0 dan boshlanib 100 da tugashi shart.',
  SCALE_BAND_GAP: "oraliqlar orasida bo'shliq bor (keyingi \"Dan\" = oldingi \"Gacha\" + 1).",
  SCALE_BAND_OVERLAP: '"Gacha" inklyuziv — oraliqlar ustma-ust tushmasin.',
};

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
  /** Excel faylida oraliqlar bo'lsa — nechtaligi. JSON faylda shkala bo'lmaydi → 0. */
  bandCount: number;
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

  return validateTestImportObject(json);
}

/**
 * Sxema + semantik tekshiruv — manba MUHIM EMAS: JSON fayl ham, `POST
 * /api/admin/catalog/import/parse-excel` qaytargan obyekt ham AYNAN shu yerdan o'tadi.
 * Shu sabab Excel yo'li uchun ikkinchi validatsiya/oldindan ko'rish mantiqi yozilmadi.
 *
 * `serverIssues` — Excel parseri topgan qator darajasidagi xatolar (yetishmagan ustun,
 * buzuq qator). Ular shu yerdagi xatolar bilan BIRGA ko'rsatiladi va `canImport`ga
 * bir xil ta'sir qiladi.
 */
export function validateTestImportObject(
  json: unknown,
  serverIssues: readonly ImportIssue[] = [],
): ImportValidationResult {
  const parsed = testImportFileSchema.safeParse(json);
  if (!parsed.success) {
    const issues: ImportIssue[] = parsed.error.issues.map((issue) => ({
      code: 'SCHEMA_INVALID',
      message: `${issue.path.join('.')}: ${issue.message}`,
    }));
    return { canImport: false, data: null, preview: null, issues: [...serverIssues, ...issues] };
  }

  const data = parsed.data;
  const issues: ImportIssue[] = [...serverIssues];

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

  const declaredScales = data.scales ?? [];
  const bandCountByScale = new Map(
    declaredScales.map((scale) => [scale.code, scale.interpretationBands.length]),
  );

  // Faylda e'lon qilingan, lekin bironta savoli yo'q shkala ham ro'yxatda ko'rinsin —
  // aks holda "shkala bor, savol yo'q" holati oldindan ko'rishda umuman ko'rinmasdi.
  for (const scale of declaredScales) {
    if (!scaleCounts.has(scale.code)) {
      scaleCounts.set(scale.code, 0);
    }
  }

  const scales: ScaleSummary[] = [...scaleCounts.entries()]
    .map(([scale, questionCount]) => ({
      scale,
      questionCount,
      bandCount: bandCountByScale.get(scale) ?? 0,
    }))
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

  if (declaredScales.length > 0) {
    const declaredCodes = new Set(declaredScales.map((scale) => scale.code));

    for (const scale of scales) {
      if (!declaredCodes.has(scale.scale)) {
        issues.push({
          code: 'QUESTION_SCALE_UNKNOWN',
          scale: scale.scale,
          message: `"${scale.scale}" shkalasi "Shkalalar" varag'ida e'lon qilinmagan.`,
        });
      }
    }

    // Talqin oraliqlari — `docs/03` §6.3. Bu tekshiruv NASHR qilishda baribir ishlaydi;
    // bu yerda u yuklashdan OLDIN ko'rsatiladi, aks holda superadmin importni tugatib,
    // keyin nashrda to'siqqa uriladi va sababini o'sha joydan uzoqda qidiradi.
    if (data.scoringMode !== 'Survey') {
      for (const scale of declaredScales) {
        for (const code of validateInterpretationBands(scale.interpretationBands)) {
          issues.push({
            code,
            scale: scale.code,
            message: `"${scale.code}" shkalasi: ${INTERPRETATION_BAND_MESSAGES[code]}`,
          });
        }
      }
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

/**
 * `POST /api/admin/catalog/import/parse-excel` javobi. `data` ATAYLAB `unknown`:
 * u to'g'ridan-to'g'ri {@link validateTestImportObject} ga beriladi va o'sha yerda
 * `testImportFileSchema` bilan tekshiriladi — ya'ni server javobi uchun ALOHIDA qo'lda
 * yozilgan DTO tipi kerak emas va shakl faqat BITTA joyda ta'riflanadi.
 */
export const excelParseResponseSchema = z.object({
  data: z.unknown().nullish(),
  issues: z
    .array(
      z.object({
        code: z.string(),
        message: z.string(),
        sheet: z.string().nullish(),
        row: z.number().nullish(),
        questionCode: z.string().nullish(),
        scale: z.string().nullish(),
      }),
    )
    .nullish(),
});

/** Server xatolarini UI ishlatadigan `ImportIssue` shakliga o'giradi (`sheet`/`row` matnga qo'shiladi). */
export function toImportIssues(raw: unknown): ImportIssue[] {
  const parsed = excelParseResponseSchema.safeParse(raw);
  if (!parsed.success) {
    return [];
  }

  return (parsed.data.issues ?? []).map((issue) => ({
    code: issue.code,
    message: issue.sheet ? `"${issue.sheet}" varag'i — ${issue.message}` : issue.message,
    questionCode: issue.questionCode ?? undefined,
    scale: issue.scale ?? undefined,
  }));
}

/**
 * `ProblemDetails` (RFC 9457) ning shu yerda kerak bo'lgan qismi — Excel yuklash xom
 * `fetch` orqali ketadi (`FormData`), ya'ni `apiRequest` ning `AppError` ga o'girish
 * mantiqidan o'tmaydi. `code` — `CLAUDE.md` 11-qoidasi bo'yicha har doim bor.
 */
export const problemDetailsSchema = z.object({
  code: z.string().nullish(),
  title: z.string().nullish(),
  detail: z.string().nullish(),
});
