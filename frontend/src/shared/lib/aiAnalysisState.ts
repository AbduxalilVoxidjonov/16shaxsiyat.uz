/**
 * AI tahlil bo'limining holat mashinasi — `/admin/students/:id` (`docs/11` A-5) va
 * `/admin/assessments/:id` (A-6) ekranlari AYNAN bir xil qoidaga bo'ysunadi, shu sabab
 * qaror MANTIQI shu yerda BITTA joyda turadi (`docs/10` 2-bo'lim: "Umumiy narsa `shared/`
 * yoki `widgets/` ga chiqariladi"; ikki feature bir-birini import qila olmaydi). Ikki
 * ekranning JSX'i o'zicha qoladi — ular boshqa i18n bo'shlig'i va boshqa DTO shakli bilan
 * ishlaydi — lekin "qaysi holatda nima ko'rinadi" savoliga javob faqat shu fayldan keladi.
 *
 * Uch qoida shu funksiyada mustahkamlangan:
 *
 * 1. **Eski tahlil `Analyzing` paytida YO'QOLMAYDI.** Ilgari ikkala ekran ham `Analyzing`
 *    holatida butun bo'limni skeletga almashtirardi va admin qayta tahlil bosgan zahoti
 *    mavjud hisobotni YO'QOTARDI. Skelet endi FAQAT hech qachon tahlil qilinmagan
 *    sessiyada ko'rsatiladi (`showSkeleton`), mavjud hisobot esa banner ostida joyida
 *    qoladi (`showReport` + `showAnalyzingBanner`).
 * 2. **Ma'lumot yo'q ≠ nol.** "Hech qachon tahlil qilinmagan" (`action: 'run'`) va "tahlil
 *    yiqilgan" (`action: 'retry'`, `showFailure`) — ikki BOSHQA holat, ikkalasi ham
 *    "hisobot yo'q" ga qo'shib yuborilmaydi.
 * 3. **Tasdiq faqat ustiga yozilganda.** Birinchi tahlilda yo'qotiladigan narsa yo'q —
 *    `requiresConfirmation: false`; mavjud hisobot ustiga yozilganda esa `true`.
 */

/**
 * `AssessmentStatus` — `docs/05` raqamli enum jadvali. Ikki feature ham o'z faylida aynan
 * shu union'ni e'lon qiladi (`features/*` bir-birini import qilmaydi), ular struktura
 * jihatdan mos, shu sabab bu yerga uzatilaveradi.
 */
export type AiSectionAssessmentStatus =
  | 'Draft'
  | 'InProgress'
  | 'Completed'
  | 'Analyzing'
  | 'Analyzed'
  | 'AnalysisFailed'
  | 'Abandoned';

/**
 * `RerunAnalysisCommandHandler` → `Assessment.MarkAnalyzing` domen qo'riqchisi AYNAN shu
 * uchta holatdan ruxsat beradi (`Domain/Assessments/Assessment.cs`), boshqasidan
 * `ASSESSMENT_INVALID_TRANSITION` (`409`) otiladi.
 *
 * Diqqat: `Completed` ham ro'yxatda — ya'ni **birinchi** tahlil ham xuddi shu
 * `POST /api/admin/assessments/{id}/rerun-analysis` endpointi orqali ishga tushadi,
 * alohida "birinchi tahlil" endpointi kerak emas.
 */
export const AI_RUNNABLE_STATUSES: readonly AiSectionAssessmentStatus[] = [
  'Completed',
  'Analyzed',
  'AnalysisFailed',
];

/**
 * Hisobot bor-yo'qligini aniqlash uchun yetarli minimal shakl — ikki feature'ning
 * `AiAnalysisDto` / `AssessmentAiAnalysisDto` tiplari ham shunga mos.
 */
export interface AiAnalysisLike {
  status: string;
  summary?: string | null;
}

/**
 * O'qish mumkin bo'lgan HISOBOT bormi (yozuvning o'zi bor-yo'qligi emas).
 *
 * `AnalysisFailed` sessiyada `aiAnalysis` yozuvi BOR bo'lishi mumkin (`status: 'Failed'`,
 * `errorMessage` to'ldirilgan, mazmun bo'sh) — bu hisobot EMAS. Aksincha, `Pending`/
 * `Running` yozuv yonida oldingi muvaffaqiyatli hisobot `IsCurrent` bo'lib turishi mumkin
 * (`docs/09` 11-bo'lim, P18: yangi tahlil muvaffaqiyatli bo'lmaguncha eskisi almashmaydi),
 * shuning uchun mazmun bo'lsa u ham hisobot deb qabul qilinadi.
 */
export function hasAiReport(analysis: AiAnalysisLike | null | undefined): boolean {
  if (!analysis) return false;
  if (analysis.status === 'Failed') return false;
  if (analysis.status === 'Succeeded') return true;
  return typeof analysis.summary === 'string' && analysis.summary.trim().length > 0;
}

/** Tugmaning ma'nosi — matn va tasdiq oynasi shundan kelib chiqadi. */
export type AiAnalysisAction =
  /** Hech qachon tahlil qilinmagan — "AI tahlil qilish". */
  | 'run'
  /** Hisobot bor, ustiga yoziladi — "Qayta tahlil qilish". */
  | 'rerun'
  /** Oldingi urinish yiqilgan — "Qayta urinish". */
  | 'retry';

/** Tugma KO'RSATILMASLIGI sababi — sababsiz yo'qolgan tugma boshi berk ko'cha. */
export type AiAnalysisBlockedReason =
  /** `Draft`/`InProgress`/`Abandoned` — backend `409` qaytaradi, tugma ko'rsatilmaydi. */
  | 'sessionNotCompleted'
  /** O'quvchida umuman sessiya yo'q. */
  | 'noAssessment';

export interface AiAnalysisViewState {
  /** Fon navbatida tahlil ketmoqda — sahifa natijani kutadi (polling). */
  isAnalyzing: boolean;
  /** Skelet — FAQAT hech qachon tahlil qilinmagan sessiyada (birinchi tahlil). */
  showSkeleton: boolean;
  /** Mavjud hisobot ko'rsatiladi — `Analyzing` paytida ham QOLADI. */
  showReport: boolean;
  /** "Yangi tahlil tayyorlanmoqda" banneri — eski hisobot ustida. */
  showAnalyzingBanner: boolean;
  /** Qizil xato kartasi (`AnalysisFailed`). */
  showFailure: boolean;
  /** Ko'rsatiladigan tugma; `null` — tugma yo'q. */
  action: AiAnalysisAction | null;
  /** Tugma yo'qligining sababi; `null` — sabab ko'rsatish shart emas. */
  blockedReason: AiAnalysisBlockedReason | null;
  /** Tasdiq oynasi kerakmi — FAQAT mavjud hisobot ustiga yozilganda. */
  requiresConfirmation: boolean;
}

export interface AiAnalysisViewStateInput {
  /**
   * Sessiya holati. `null` — NOMA'LUM (masalan detal javobida yo'q): bunda serverning
   * qarori taxmin qilinmaydi, tugma ochiq qoladi va ruxsat etilmagan o'tishda backend
   * `409` bilan aniq sabab qaytaradi.
   */
  status: AiSectionAssessmentStatus | null;
  analysis: AiAnalysisLike | null | undefined;
  /** `false` — o'quvchida sessiya umuman yo'q. Berilmasa `true`. */
  hasAssessment?: boolean;
}

/** Yuqoridagi uch qoidani bitta sof funksiyaga jamlaydi — ikkala ekran shundan foydalanadi. */
export function resolveAiAnalysisViewState({
  status,
  analysis,
  hasAssessment = true,
}: AiAnalysisViewStateInput): AiAnalysisViewState {
  const showReport = hasAiReport(analysis);

  if (!hasAssessment) {
    return {
      isAnalyzing: false,
      showSkeleton: false,
      showReport: false,
      showAnalyzingBanner: false,
      showFailure: false,
      action: null,
      blockedReason: 'noAssessment',
      requiresConfirmation: false,
    };
  }

  if (status === 'Analyzing') {
    return {
      isAnalyzing: true,
      // Skelet FAQAT birinchi tahlilda — mavjud hisobot ekrandan yo'qolmaydi.
      showSkeleton: !showReport,
      showReport,
      showAnalyzingBanner: true,
      showFailure: false,
      // Tahlil allaqachon ketmoqda: ikkinchi navbat qo'shish AI xarajatini ikkilantiradi.
      action: null,
      blockedReason: null,
      requiresConfirmation: false,
    };
  }

  if (status === 'AnalysisFailed') {
    return {
      isAnalyzing: false,
      showSkeleton: false,
      showReport,
      showAnalyzingBanner: false,
      showFailure: true,
      action: 'retry',
      blockedReason: null,
      // Oldingi muvaffaqiyatli hisobot hali turgan bo'lsa — ustiga yozishdan oldin tasdiq.
      requiresConfirmation: showReport,
    };
  }

  if (status === null || AI_RUNNABLE_STATUSES.includes(status)) {
    return {
      isAnalyzing: false,
      showSkeleton: false,
      showReport,
      showAnalyzingBanner: false,
      showFailure: false,
      action: showReport ? 'rerun' : 'run',
      blockedReason: null,
      requiresConfirmation: showReport,
    };
  }

  // `Draft` / `InProgress` / `Abandoned` — backend `409` qaytaradi. Tugmani ko'rsatib
  // turib xatoga urib yuborish yomon UX: sabab OLDINDAN aytiladi.
  return {
    isAnalyzing: false,
    showSkeleton: false,
    showReport,
    showAnalyzingBanner: false,
    showFailure: false,
    action: null,
    blockedReason: 'sessionNotCompleted',
    requiresConfirmation: false,
  };
}
