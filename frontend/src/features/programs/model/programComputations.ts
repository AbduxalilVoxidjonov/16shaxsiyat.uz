import type { CatalogTestOption } from '../api/useCatalogTestOptionsQuery';
import { MATURITY_BATTERY_TEST_CODES, type AdminProgramTestItem } from './types';

/**
 * `docs/06` §8 (2026-09-02): `MaturityIndex` faqat BIG5 **va** ACTIVITY sessiyada birga
 * bo'lganda hisoblanadi. Admin dastur tuzayotganda buni bilishi kerak (`prompts/35` 14-band:
 * "admin nima yo'qotayotganini bilishi kerak") — shu sabab tarkibdagi test kodlari shu
 * ro'yxat bilan solishtiriladi.
 *
 * **Eslatma (P52, 2026-09-11):** bu — `MaturityIndex` uchun kod ro'yxati, "ilmiy shaxsiyat
 * batareyasi" (`Domain.Catalog.PersonalityBattery`) mezoni EMAS. Oxirgisi endi backend
 * `hasPersonalityBattery` bayrog'idan olinadi (`AdminProgramDetail.hasPersonalityBattery`,
 * qattiq kod ro'yxati emas) — `ProgramFormDialog`ga qarang.
 */
export function hasFullMaturityBattery(tests: readonly AdminProgramTestItem[]): boolean {
  const codes = new Set(tests.map((test) => test.code));
  return MATURITY_BATTERY_TEST_CODES.every((code) => codes.has(code));
}

export interface ProgramDurationSummary {
  totalMinutes: number;
  totalQuestions: number;
  /**
   * `false` bo'lsa ba'zi testlarning vaqt/savol ma'lumoti topilmadi (katalog ro'yxati
   * hali yuklanmagan yoki mos yozuv yo'q) — UI bunday holda "~" yoki "—" ko'rsatishi kerak,
   * `0` EMAS (`docs/06` §8 "null qoidasi" ruhida — noma'lum qiymatni past qiymat sifatida
   * ko'rsatmaslik).
   */
  isComplete: boolean;
}

/**
 * Dastur tarkibidagi testlarning jami savol soni va taxminiy vaqtini hisoblaydi —
 * `prompts/35` C8-band ("jami savol soni va vaqt jonli hisoblanadi"). `AdminProgramTestItemDto`
 * o'zi bu maydonlarni bermaydi (faqat `testDefinitionId`/`code`/`nameUz`/`displayOrder`),
 * shu sabab katalog variantlar ro'yxatidan (`useCatalogTestOptionsQuery`) `testDefinitionId`
 * bo'yicha moslashtiriladi — katalog backend hali yo'qligi sabab bu ro'yxat bo'sh bo'lishi
 * mumkin, natijada `isComplete: false` qaytadi.
 */
export function computeProgramDuration(
  tests: readonly AdminProgramTestItem[],
  catalogOptions: readonly CatalogTestOption[] | undefined,
): ProgramDurationSummary {
  const byId = new Map((catalogOptions ?? []).map((option) => [option.id, option]));
  let totalMinutes = 0;
  let totalQuestions = 0;
  let isComplete = tests.length > 0;

  for (const test of tests) {
    const option = byId.get(test.testDefinitionId);
    if (!option) {
      isComplete = false;
      continue;
    }
    totalMinutes += option.estimatedMinutes;
    totalQuestions += option.questionCount;
  }

  return { totalMinutes, totalQuestions, isComplete };
}
