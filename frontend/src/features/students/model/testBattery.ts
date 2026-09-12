import type { AssessmentBatteryTestBlock } from '@/shared/api/assessmentBatteryTypes';

/**
 * `results`ning 4 TIZIM kaliti — `docs/07` 3.2 "kalit nomlari" jadvali bilan bir xil
 * (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`). `StudentSummaryCards`/`StudentDiagramsSection`
 * faqat shu to'rttasini biladi — dastur ichidagi boshqa (`Custom`/`Survey`) test bloklari
 * bu ekranda umuman ko'rsatilmaydi (admin ularni sessiya detali/`AnswersSection`da ko'radi).
 */
export const CORE_TEST_CODES = ['MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY'] as const;
export type CoreTestCode = (typeof CORE_TEST_CODES)[number];

function isCoreTestCode(code: string): code is CoreTestCode {
  return (CORE_TEST_CODES as readonly string[]).includes(code);
}

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
 * sessiyaga BIRIKTIRILGAN test bloklari, natija hisoblanganmi emas. Shu funksiya kod →
 * mavjudlik moslamasini BIR joyda ushlab turadi (`StudentSummaryCards` va
 * `StudentDiagramsSection` ikkalasi ham shu yerdan foydalanadi — ikki nusxa qattiq yozilgan
 * kod ro'yxati emas).
 *
 * `tests` `undefined`/`null` bo'lsa (masalan eski test moslamasi yoki backend hali
 * `generate:api`gacha yangilanmagan holat) BO'SH to'plam qaytadi — ya'ni HECH NARSA
 * ko'rsatilmaydi, "hammasi ko'rsatilsin" degan xavfli taxmin qilinmaydi (aynan shu taxmin
 * asl xatoga olib kelgan edi).
 */
export function buildPresentTestCodes(
  tests: AssessmentBatteryTestBlock[] | null | undefined,
): ReadonlySet<CoreTestCode> {
  const present = new Set<CoreTestCode>();
  for (const test of tests ?? []) {
    if (isCoreTestCode(test.code)) {
      present.add(test.code);
    }
  }
  return present;
}
