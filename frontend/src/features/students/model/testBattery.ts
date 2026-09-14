import type { AssessmentBatteryTestBlock } from '@/shared/api/assessmentBatteryTypes';

/**
 * `results`ning 4 TIZIM kaliti — `docs/07` 3.2 "kalit nomlari" jadvali bilan bir xil
 * (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`). `StudentSummaryCards`/`StudentDiagramsSection`
 * faqat shu to'rttasini biladi — dastur ichidagi boshqa (`Custom`/`Survey`) test bloklari
 * bu ekranda umuman ko'rsatilmaydi (admin ularni sessiya detali/`AnswersSection`da ko'radi).
 */
export const CORE_TEST_CODES = ['MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY'] as const;
export type CoreTestCode = (typeof CORE_TEST_CODES)[number];

/**
 * Anketaning shaxsiyat batareyasi ICHIDAGI roli (`PersonalityBatteryRole`,
 * `AdminLatestAssessmentTestItemDto.BatteryRole`) → `results` kalitiga moslama. Bu — bitta
 * haqiqat manbai bilan (`PersonalityBattery.RoleOf` domen qoidasi) BIR XIL: rol nomi
 * o'zgarsa, faqat shu obyekt yangilanadi.
 */
const ROLE_TO_CORE_CODE: Record<
  Exclude<NonNullable<AssessmentBatteryTestBlock['batteryRole']>, 'None'>,
  CoreTestCode
> = {
  PersonalityType: 'MBTI16',
  Traits: 'BIG5',
  CareerInterest: 'RIASEC',
  Activity: 'ACTIVITY',
};

/**
 * P52 jonli xato tuzatish (2026-09-12): egasi "bu profil qismida chiqishi kerak emas" deb
 * topgan kamchilik — o'quvchi FAQAT so'rovnoma topshirganda (dasturida shaxsiyat testlari
 * umuman yo'q) kartalar va diagrammalar baribir "Hali natija yo'q" holatida chizilardi,
 * garchi metodika sessiyada umuman bo'lmasa ham. Ikki holat oldin bir xil `undefined` bilan
 * ifodalanardi:
 * - test topshirilgan, hali hisoblanmagan → "Hali natija yo'q" O'RINLI kutish holati;
 * - test bu dasturda umuman YO'Q → ko'rsatishning O'ZI xato.
 *
 * `latestAssessment.tests[]` (backend P52-B, 2026-09-12) endi buni ajratadi: massiv —
 * sessiyaga BIRIKTIRILGAN test bloklari, natija hisoblanganmi emas. Shu funksiya rol →
 * mavjudlik moslamasini BIR joyda ushlab turadi (`StudentSummaryCards` va
 * `StudentDiagramsSection` ikkalasi ham shu yerdan foydalanadi — ikki nusxa qattiq yozilgan
 * kod ro'yxati emas).
 *
 * Mezon `test.code` EMAS, `test.batteryRole` (code-review, 2026-09-14): kod versiyalansa
 * (masalan `MBTI16-V2`) backend `PersonalityBattery.RoleOf` domen qoidasi bo'yicha baribir
 * natija beradi — mavjudlik ANIQ SHU qoida bilan bir xil manbadan aniqlanishi kerak, kod
 * ro'yxatidan emas. `batteryRole` `undefined` bo'lgan elementlar (eski backend/moslama)
 * HISOBGA OLINMAYDI — ya'ni "hammasi ko'rsatilsin" degan xavfli taxmin qilinmaydi (aynan shu
 * taxmin asl xatoga olib kelgan edi).
 */
export function buildPresentTestCodes(
  tests: AssessmentBatteryTestBlock[] | null | undefined,
): ReadonlySet<CoreTestCode> {
  const present = new Set<CoreTestCode>();
  for (const test of tests ?? []) {
    if (test.batteryRole && test.batteryRole !== 'None') {
      present.add(ROLE_TO_CORE_CODE[test.batteryRole]);
    }
  }
  return present;
}
