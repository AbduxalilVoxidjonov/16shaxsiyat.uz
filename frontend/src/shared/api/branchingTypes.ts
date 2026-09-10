/**
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — `docs/18-tarmoqlanuvchi-sorovnoma.md` §4.1.
 *
 * Backend P52-A3 (ommaviy API'ga `sections`/branching maydonlarini qo'shish) hali
 * tugallanmagan, shu sabab bu maydonlar `schema.d.ts`da yo'q (tekshirildi: `PublicQuestionDto`
 * hozircha faqat `id/code/order/text/type/isRequired/options/currentValue`,
 * `GetTestQuestionsResult`da `sections` umuman yo'q). Shakl `docs/18` §4.1 dagi JSON
 * namunasidan AYNAN olingan.
 *
 * Nomlash ataylab backend DTO nomlaridan (`PublicSectionDto`/`PublicQuestionDto`) FARQ QILADI
 * (`PublicSection`/`BranchingQuestionFields`) — `eslint.config.js`dagi "qo'lda DTO yozilmasin"
 * qoidasi `shared/api/**`da `*Dto`/`*Request`/`*Response`/`*Result`/`*Item`/`*Detail` bilan
 * tugagan nomlarni taqiqlaydi (`docs/10` §6, `features/programs/api/
 * useCatalogTestOptionsQuery.ts`dagi "MUVAQQAT" naqshiga o'xshab). Shakl baribir docs/18 §4.1
 * bilan bayt-bayt bir xil — faqat TS tomonidagi tip NOMI farq qiladi.
 *
 * Backend chiqib `npm run generate:api` ishga tushgach: bu fayl o'chiriladi,
 * `shared/api/types.ts`dagi `PublicQuestion`/`TestQuestionsResponse` generatsiya qilingan
 * yangi maydonlarni to'g'ridan-to'g'ri o'z ichiga oladi, va shu faylni ishlatuvchi
 * (`QuestionRenderer` va yangi savol komponentlari) importlari `shared/api/types`ga
 * almashtiriladi.
 */
import type { PublicQuestion } from './types';
import type { VisibilityRule } from '@/shared/lib/visibility';

/**
 * docs/18 §2.1 — hali `schema.d.ts`da yo'q savol turlari. `PublicQuestion.type` generatsiyada
 * `string` (qat'iy enum emas), shu sabab bu ro'yxat generatsiya qilingan tipni TORAYTIRMAYDI —
 * faqat mumkin bo'lgan qiymatlarni hujjatlaydi va `QuestionRenderer`da tur tekshiruvida
 * ishlatiladi.
 */
export type BranchingQuestionType = 'ShortText' | 'LongText' | 'MultiChoice' | 'Phone';

/** docs/18 §4.1 — `sections[]` elementi (backendda `PublicSectionDto`). */
export interface PublicSection {
  id: string;
  code: string;
  title: string;
  description: string | null;
  order: number;
  visibility: VisibilityRule | null;
}

/** docs/18 §4.1 — `PublicQuestionDto`ga qo'shiladigan yangi maydonlar. */
export interface BranchingQuestionFields {
  sectionId: string | null;
  placeholder: string | null;
  inputPattern: string | null;
  maxLength: number | null;
  minSelections: number | null;
  maxSelections: number | null;
  visibility: VisibilityRule | null;
  currentText: string | null;
  currentValues: number[] | null;
}

/**
 * `PublicQuestion` (generatsiya qilingan) + branching maydonlari — yangi savol komponentlari
 * (`TextQuestion`, `LongTextQuestion`, `MultiChoiceQuestion`, `QuestionRenderer`) shu tipni
 * kutadi. Backend chiqqach `PublicQuestion`ning o'zi shu shaklga ega bo'ladi va bu tip
 * kerak bo'lmay qoladi.
 */
export type BranchingQuestion = PublicQuestion & BranchingQuestionFields;

/** docs/18 §4.1 — `GetTestQuestionsResult`ga qo'shiladigan `sections` maydoni. */
export interface TestQuestionsWithSections {
  sections: PublicSection[] | null;
}
