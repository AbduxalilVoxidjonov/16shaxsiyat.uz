/**
 * `VisibilityEvaluator`/`VisibleQuestionResolver` (backend, `Domain/Catalog/Branching/`) ning
 * **AYNAN bir xil xatti-harakatli** TS egizagi — `docs/18-tarmoqlanuvchi-sorovnoma.md` §2.4–2.6.
 *
 * Ikki nusxa xavfi (C# va shu fayl bir-biridan ajralib ketishi) `tests/fixtures/
 * visibility-golden.json` bilan qulflangan — uni ikkala tomon o'qiydi
 * (`tests/StudentRoadMap.Domain.Tests/Branching/VisibilityGoldenTests.cs` va
 * `visibility.golden.test.ts`). Bu faylga mantiqiy o'zgartirish kiritsangiz, oltin fikstura
 * testini ham darhol qayta ishga tushiring — ikkalasi yashil bo'lmasa xatti-harakat
 * chetlashib ketgan bo'ladi.
 */

/** Bo'lim/savol shartidagi shartlarni qanday birlashtirish — hammasi ("All") yoki bittasi ("Any"). */
export type VisibilityMatch = 'All' | 'Any';

/** docs/18 §2.4 — `VisibilityOperator` enumining TS nusxasi. */
export type VisibilityOperator =
  | 'Equals'
  | 'NotEquals'
  | 'AnyOf'
  | 'NoneOf'
  | 'ContainsAny'
  | 'ContainsAll'
  | 'Answered'
  | 'NotAnswered';

/** Bitta shart — manba savol kodi, operator va solishtiriladigan qiymatlar (docs/18 §2.4). */
export interface VisibilityCondition {
  questionCode: string;
  operator: VisibilityOperator;
  values: readonly number[];
}

/** Bo'lim yoki savol darajasidagi ko'rsatish sharti (jsonb saqlash shakli — docs/18 §2.4). */
export interface VisibilityRule {
  match: VisibilityMatch;
  conditions: readonly VisibilityCondition[];
}

/**
 * Bitta savolga berilgan joriy javob — `docs/18` §2.7 invarianti bo'yicha uchta shakldan
 * (`rawValue`/`textValue`/`selectedValues`) aynan bittasi to'ldirilgan bo'ladi (yoki hech biri —
 * savolga hali javob berilmagan).
 */
export interface AnswerSnapshot {
  rawValue: number | null;
  textValue: string | null;
  selectedValues: readonly number[];
}

/** Rezolverga beriladigan bo'lim — `docs/18` §2.2. */
export interface SectionSnapshot {
  code: string;
  displayOrder: number;
  visibilityRule: VisibilityRule | null;
}

/** Rezolverga beriladigan savol — `docs/18` §2.3. */
export interface QuestionSnapshot {
  code: string;
  displayOrder: number;
  /** `null` — bo'limga tegishli emas (bo'limsiz anketa yoki bo'lim tashqarisidagi savol). */
  sectionCode: string | null;
  visibilityRule: VisibilityRule | null;
  /**
   * Standart `true`. Backend faol bo'lmagan savolni ommaviy ro'yxatga umuman qo'shmaydi, shu
   * sabab bu maydon amaliyotda deyarli hamisha aniqlanmagan (`undefined`) qoladi — shunga
   * qaramay rezolver §2.6 1-bandini to'liq bajarishi uchun mavjud.
   */
  isActive?: boolean;
}

/** `resolveVisibleQuestions` natijasi — docs/18 §2.6 (`VisibilityMap`). */
export interface VisibilityMap {
  visibleSectionCodes: ReadonlySet<string>;
  visibleQuestionCodes: ReadonlySet<string>;
}

/**
 * Savol javob berilgan hisoblanadimi (`AnswerSnapshot.IsAnswered`, docs/18 §2.5). Bo'sh yoki
 * faqat-bo'shliqli matn — javobsiz hisoblanadi (ataylab: `"   "` "javob berdim" degani emas).
 */
export function isAnswered(answer: AnswerSnapshot | undefined): boolean {
  if (!answer) return false;
  return (
    answer.rawValue !== null ||
    (answer.textValue !== null && answer.textValue.trim().length > 0) ||
    answer.selectedValues.length > 0
  );
}

/**
 * `Equals`/`NotEquals`/`AnyOf`/`NoneOf` uchun solishtiriladigan qiymatlar ro'yxati: odatda
 * `rawValue` (bitta element), manba savol `MultiChoice` bo'lsa esa `selectedValues` (docs/18
 * §2.5: "Equals → yagona tanlov shu qiymat").
 */
function comparableValues(answer: AnswerSnapshot): readonly number[] {
  return answer.rawValue !== null ? [answer.rawValue] : answer.selectedValues;
}

function evaluateCondition(
  condition: VisibilityCondition,
  answersByCode: Readonly<Record<string, AnswerSnapshot>>,
): boolean {
  const answer = answersByCode[condition.questionCode];
  const answered = isAnswered(answer);

  if (condition.operator === 'Answered') return answered;
  if (condition.operator === 'NotAnswered') return !answered;

  // Qolgan 6 operator uchun javob yo'q bo'lsa HAMMASI `false` — `NotEquals`/`NoneOf` ham shu
  // qatorga kiradi (docs/18 §2.5 jadvali va izohi): sahifa ochilishi bilan "hali javob
  // bermagan" holat "boshqa qiymat" deb hisoblanib, barcha "boshqacha bo'lsa" tarmoqlari
  // birdan ochilib ketmasin.
  if (!answer || !answered) return false;

  if (condition.operator === 'ContainsAny') {
    return answer.selectedValues.some((value) => condition.values.includes(value));
  }
  if (condition.operator === 'ContainsAll') {
    return condition.values.every((value) => answer.selectedValues.includes(value));
  }

  const values = comparableValues(answer);
  const equalsTarget = values.length === 1 && values[0] === condition.values[0];
  switch (condition.operator) {
    case 'Equals':
      return equalsTarget;
    case 'NotEquals':
      return !equalsTarget;
    case 'AnyOf':
      return values.some((value) => condition.values.includes(value));
    case 'NoneOf':
      return !values.some((value) => condition.values.includes(value));
    default:
      return false;
  }
}

/**
 * Bitta qoidani baholaydi. `rule` bo'lmasa (`null`/`undefined`) — har doim `true` (docs/18
 * §2.5: `rule is null` → har doim `true`).
 */
export function evaluateVisibility(
  rule: VisibilityRule | null | undefined,
  answersByCode: Readonly<Record<string, AnswerSnapshot>>,
): boolean {
  if (!rule) return true;
  if (rule.match === 'All') {
    return rule.conditions.every((condition) => evaluateCondition(condition, answersByCode));
  }
  return rule.conditions.some((condition) => evaluateCondition(condition, answersByCode));
}

/**
 * Butun anketa uchun ko'rinadigan bo'lim/savollar to'plamini **bir marta o'tishda**
 * (`displayOrder` bo'yicha) hisoblaydi (docs/18 §2.6).
 *
 * Savol ko'rinadi ⟺: (1) faol (`isActive !== false`); (2) bo'limi bor bo'lsa — bo'lim
 * ko'rinadi; (3) o'z sharti bajariladi. Bo'lim sharti savol shartidan USTUN — bo'lim yopiq
 * bo'lsa savolning o'z sharti tekshirilmaydi ham.
 *
 * **Kaskad:** savol (yoki uning bo'limi) yashirilsa, uning javobi KEYINGI shartlar uchun
 * "javobsiz" deb hisoblanadi — eski javob saqlanib qolgan bo'lsa ham. Shu sabab funksiya
 * `answersByCode`ning MAHALLIY nusxasini oladi va yashirilgan savolni undan o'chirib boradi;
 * kiruvchi obyekt o'zgarmaydi. B-4 qoidasi (shart faqat oldinroqdagi savolga havola qiladi)
 * shu bir martalik, tartiblangan o'tishni to'g'ri qiladi — biror shart keyingi savolga havola
 * qilsa, o'sha savol hali "javobsiz" holatida ko'rinadi (bu xato emas — konstruktor tomonida
 * `VISIBILITY_FORWARD_REFERENCE` bilan bloklanadi, docs/18 §5).
 */
export function resolveVisibleQuestions(
  sections: readonly SectionSnapshot[],
  questions: readonly QuestionSnapshot[],
  answersByCode: Readonly<Record<string, AnswerSnapshot>>,
): VisibilityMap {
  const sectionByCode = new Map(sections.map((section) => [section.code, section]));
  const sectionVisibleCache = new Map<string, boolean>();
  const visibleSectionCodes = new Set<string>();
  const visibleQuestionCodes = new Set<string>();
  // Kaskad uchun mahalliy, o'zgaruvchan nusxa — yashirilgan savol shu yerdan o'chiriladi
  // (kiruvchi `answersByCode` o'zgarishsiz qoladi).
  const liveAnswers: Record<string, AnswerSnapshot> = { ...answersByCode };

  const orderedQuestions = [...questions].sort((a, b) => a.displayOrder - b.displayOrder);

  for (const question of orderedQuestions) {
    if (question.isActive === false) {
      delete liveAnswers[question.code];
      continue;
    }

    let sectionVisible = true;
    if (question.sectionCode !== null) {
      const cached = sectionVisibleCache.get(question.sectionCode);
      if (cached === undefined) {
        const section = sectionByCode.get(question.sectionCode);
        sectionVisible = evaluateVisibility(section?.visibilityRule ?? null, liveAnswers);
        sectionVisibleCache.set(question.sectionCode, sectionVisible);
        if (sectionVisible) {
          visibleSectionCodes.add(question.sectionCode);
        }
      } else {
        sectionVisible = cached;
      }
    }

    const visible = sectionVisible && evaluateVisibility(question.visibilityRule, liveAnswers);
    if (visible) {
      visibleQuestionCodes.add(question.code);
    } else {
      delete liveAnswers[question.code];
    }
  }

  // Hech qanday savolga tegishli bo'lmagan (yoki yuqoridagi tsiklda hali "duch kelinmagan")
  // bo'limlar ham baholanadi — natijaviy (barcha kaskad tugagandan keyingi) `liveAnswers` bilan
  // (docs/18 §2.6: "VisibleSectionIds BARCHA bo'limlar bo'yicha to'liq bo'lishi kerak"; C#
  // egizagi — `VisibleQuestionResolver.Resolve` — shu bosqichni oxirida bajaradi, TS ham xuddi
  // shunday qilishi kerak, aks holda savolsiz bo'lim ikki tomonda boshqacha natija beradi —
  // oltin fikstura "bo'sh bo'lim (savolsiz)" holati shuni ushlagan).
  for (const section of sections) {
    if (!sectionVisibleCache.has(section.code)) {
      const visible = evaluateVisibility(section.visibilityRule, liveAnswers);
      sectionVisibleCache.set(section.code, visible);
      if (visible) {
        visibleSectionCodes.add(section.code);
      }
    }
  }

  return { visibleSectionCodes, visibleQuestionCodes };
}
