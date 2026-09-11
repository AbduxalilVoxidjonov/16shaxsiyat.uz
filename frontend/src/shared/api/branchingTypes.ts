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
import type { PublicQuestion, TestQuestionsResponse } from './types';
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

/**
 * `GetTestQuestionsResult` (generatsiya qilingan) + `sections` va tarmoqlanuvchi savol
 * maydonlari — `useTestQuestions` shu tipni qaytaradi. Nom ataylab "…Response" bilan
 * TUGAMAYDI (`eslint.config.js` "qo'lda DTO yozilmasin" qoidasi, yuqoridagi izohga qarang) —
 * "Data" qo'shimchasi shu sabab tanlangan, mazmuni esa docs/18 §4.1 bilan bir xil.
 */
export type BranchingTestQuestionsData = Omit<TestQuestionsResponse, 'questions'> & {
  questions: BranchingQuestion[];
  sections?: PublicSection[] | null;
};

/**
 * docs/18 §4.2 — `POST .../answers` so'rov elementi: `value`/`text`/`selectedValues`dan
 * AYNAN bittasi to'ldiriladi (savol turiga mos). Nom ataylab "…Item" bilan TUGAMAYDI (yuqoridagi
 * "…Response" izohiga qarang — bu yerda `interface` ishlatilgani uchun intersection'ga
 * yashirinib bo'lmaydi, shu sabab "Entry" qo'shimchasi tanlangan).
 */
export interface BranchingSaveAnswerEntry {
  questionId: string;
  value?: number;
  text?: string;
  selectedValues?: number[];
  durationMs: number;
}

/**
 * ============================================================================
 * ADMIN QATLAMI — `docs/18` §5 (Admin API), P52-A6 (superadmin konstruktor UI).
 *
 * Backend A5 (bo'limlar/savol kengaytmasi admin endpointlari) hali `schema.d.ts`da yo'q,
 * shu sabab shu yerda ham xuddi yuqoridagi ommaviy qatlam kabi MUVAQQAT qo'lda yozilgan.
 * Nomlash xuddi shu sabab bilan (`eslint.config.js` "qo'lda DTO yozilmasin" qoidasi)
 * `Dto`/`Request`/`Response`/`Result`/`Item`/`Detail` bilan TUGAMAYDI. Backend chiqib
 * `npm run generate:api` ishga tushgach: bu blok o'chiriladi, `features/catalog/model/
 * types.ts`/`questionPayload.ts` importlari `shared/api/schema`dan generatsiya qilingan
 * tiplarga almashtiriladi.
 * ============================================================================
 */

/**
 * docs/18 §5 — `SingleChoice`/`ForcedChoice`/`MultiChoice` savolining bitta javob varianti
 * (`AnswerOption` proyeksiyasi). `id` faqat mavjud variantni tahrirlashda bo'ladi — yangi
 * variant qo'shilganda frontend hali ID bilmaydi (backend yaratadi).
 */
export interface AdminQuestionOption {
  id?: string;
  textUz: string;
  value: number;
  displayOrder: number;
}

/**
 * docs/18 §2.2, §5 — `GET/POST/PUT .../sections` javobi (`QuestionSection` entity
 * proyeksiyasi). `visibility` — bo'lim ko'rinish sharti (o'zi ham `null` bo'lishi mumkin —
 * bo'lim shartsiz, hammaga ko'rinadi).
 */
export interface AdminSection {
  id: string;
  testDefinitionId: string;
  code: string;
  titleUz: string;
  descriptionUz: string | null;
  displayOrder: number;
  visibility: VisibilityRule | null;
}

/**
 * docs/18 §5 — bo'lim yaratish/tahrirlash so'rov tanasi. `code` faqat YARATISHDA
 * yuboriladi (`SectionDialog`da tahrirlashda kod qulflangan — savol konstruktoridagi
 * naqshga o'xshab).
 */
export interface AdminSectionPayload {
  code?: string;
  titleUz: string;
  descriptionUz: string | null;
  displayOrder: number;
  visibility: VisibilityRule | null;
}

/**
 * docs/18 §2.3, §5 — `CatalogQuestionItemDto`ga (sxemada hali yo'q) qo'shiladigan
 * tarmoqlanuvchi so'rovnoma maydonlari. `features/catalog/model/types.ts` da
 * `CatalogQuestionItem` shu bilan kesishtiriladi.
 *
 * `hasAnswers` — MUVAQQAT (egasi topgan jonli xato, 2026-09-11): backend savolni
 * o'chirishga urinilganda javob mavjud bo'lsa `500` o'rniga `409 QUESTION_IN_USE` qaytaradigan
 * qilib tuzatilmoqda (parallel backend vazifasi), shu bilan birga ro'yxat DTO'siga
 * `hasAnswers: boolean` qo'shiladi — frontend shu bayroqqa qarab o'chirish tugmasini OLDINDAN
 * bloklaydi (foydalanuvchi bosib, keyin serverdan tushunarsiz xato olmasin). `generate:api`
 * ishga tushgach bu maydon ham boshqalar kabi sxemaga o'tadi.
 */
export interface AdminQuestionBranchingFields {
  sectionId: string | null;
  visibility: VisibilityRule | null;
  placeholder: string | null;
  inputPattern: string | null;
  maxLength: number | null;
  minSelections: number | null;
  maxSelections: number | null;
  options: AdminQuestionOption[] | null;
  hasAnswers: boolean;
}

/**
 * docs/18 §5 — `POST/PUT .../questions` so'rov tanasiga qo'shiladigan yangi maydonlar.
 * Bari ixtiyoriy: savol turiga mos kelmagani yuborilmaydi (`model/questionPayload.ts`).
 */
export interface AdminQuestionPayloadFields {
  sectionCode?: string | null;
  visibility?: VisibilityRule | null;
  placeholder?: string | null;
  inputPattern?: string | null;
  maxLength?: number | null;
  minSelections?: number | null;
  maxSelections?: number | null;
  options?: AdminQuestionOption[] | null;
}
