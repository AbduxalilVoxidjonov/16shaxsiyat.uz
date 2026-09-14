/**
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — P52 jonli xato tuzatish (2026-09-12, egasi topgan
 * kamchilik: faqat so'rovnoma topshirgan o'quvchi profilida "Hali natija yo'q" deb TO'RTALA
 * shaxsiyat kartasi va diagramma ham chiqadi, garchi dasturda ular umuman bo'lmasa ham).
 *
 * Backend (parallel vazifa) `GET /api/admin/students/{id}` javobidagi `latestAssessment`ga
 * ikkita yangi maydon qo'shmoqda — PM ko'rsatmasi:
 * ```json
 * "latestAssessment": {
 *   "id": "…", "results": { … }, "aiAnalysis": …, "aiHistory": [ … ],
 *   "tests": [ { "code": "INTELLECT-SURVEY", "nameUz": "…", "status": "Completed", "scoringMode": "Survey" } ],
 *   "hasPersonalityBattery": false
 * }
 * ```
 * `schema.d.ts`da hali yo'q (`AdminLatestAssessmentDto`da faqat `id/results/aiAnalysis/
 * aiHistory` bor — tekshirildi), shu sabab shu yerda MUVAQQAT qo'lda yoziladi (`docs/10` §6,
 * `shared/api/branchingTypes.ts`dagi naqsh: backend chiqib `npm run generate:api` ishga
 * tushgach bu fayl o'chiriladi va `features/students/model/profileTypes.ts`dagi
 * `LatestAssessmentDto` generatsiya qilingan maydonlarni to'g'ridan-to'g'ri o'z ichiga oladi).
 *
 * Nomlash ataylab `Dto`/`Item`/`Result` bilan TUGAMAYDI (`eslint.config.js` "qo'lda DTO
 * yozilmasin" qoidasi `shared/api/**`ni ham qamraydi).
 */

/**
 * `latestAssessment.tests[]` elementi — sessiyaga biriktirilgan HAR bir test bloki
 * (`docs/07` 3.3dagi `AdminAssessmentDetailDto.tests[]` bilan bir xil ma'noda, lekin bu YANGI
 * maydonda PM ko'rsatmasi bo'yicha kod maydoni nomi `code` — `testCode` EMAS).
 *
 * `code` — `TestDefinition.Code` (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`/maxsus so'rovnoma kodi
 * kabi `INTELLECT-SURVEY`). **`code` endi "bu metodika sessiyada bormi" mezoni EMAS** —
 * kod versiyalansa (masalan `MBTI16-V2`) backend baribir natija beradi, lekin kod bo'yicha
 * tekshirilsa kartani yashirib qo'yardi (code-review, 2026-09-14). Mavjudlik endi
 * `batteryRole` orqali aniqlanadi — `features/students/model/testBattery.ts`.
 */
export interface AssessmentBatteryTestBlock {
  code: string;
  nameUz: string;
  status: string;
  scoringMode: 'Scored' | 'Survey';
  /**
   * Anketaning shaxsiyat batareyasi ICHIDAGI roli — `PersonalityBattery.RoleOf` DOMEN
   * qoidasidan (`AdminLatestAssessmentTestItemDto.BatteryRole`), kod ro'yxatidan EMAS.
   * `results` kaliti bilan moslama: `PersonalityType → MBTI16`, `Traits → BIG5`,
   * `CareerInterest → RIASEC`, `Activity → ACTIVITY`, `None` — hech qaysi kartaga tegishli
   * emas. `undefined` — backend hali bu maydonni yubormayapti (eski moslama/mock): bu holda
   * "xavfli taxmin" qilinmaydi, mavjudlik ANIQLANMAGAN deb hisoblanadi
   * (`features/students/model/testBattery.ts`).
   */
  batteryRole?: 'None' | 'PersonalityType' | 'Traits' | 'CareerInterest' | 'Activity';
}

/**
 * `latestAssessment`ga qo'shiladigan ikkita yangi maydon.
 *
 * `hasPersonalityBattery` — sessiyada AI tahlilga tayanadigan kamida bitta ballanadigan
 * shaxsiyat metodikasi bormi (`CompleteSessionCommandHandler` `Survey` bloklarini tahlildan
 * chiqarib tashlaydi — `docs/06` §8). `false` bo'lsa AI bo'limi "Tahlilni ishga tushirish"
 * chaqiruvini emas, tushuntirish matnini ko'rsatadi (`AiReportSection`).
 */
export interface LatestAssessmentBatteryFields {
  tests: AssessmentBatteryTestBlock[];
  hasPersonalityBattery: boolean;
}
