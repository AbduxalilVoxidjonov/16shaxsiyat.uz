import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import tseslint from 'typescript-eslint';
import prettierConfig from 'eslint-config-prettier';

/** CLAUDE.md 12-qoida: `dangerouslySetInnerHTML` frontendda taqiqlangan. */
const NO_DANGEROUS_HTML = {
  selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
  message:
    'dangerouslySetInnerHTML taqiqlangan (CLAUDE.md 12-qoida). AI matni oddiy matn sifatida render qilinadi.',
};

/**
 * Backend DTO'siga o'xshash nom naqshi. Loyihaning haqiqiy nomlash konvensiyasidan olingan
 * (`AdminSchoolDetailDto`, `UpdateAiProviderRequest`, `LoginResponse`, `EnableTotpResult`,
 * `AdminProgramListItem`, `CatalogTestDetail`).
 *
 * So'rov/filtr shakllari (`*Query`), enum union'lari va UI holati tiplari bu naqshga
 * TUSHMAYDI — ular qo'lda yozilishi to'g'ri va qoida ularga tegmaydi.
 */
const DTO_NAME_PATTERN = '/(Dto|Request|Response|Result|Item|Detail)$/';

const NO_HANDWRITTEN_DTO_MESSAGE =
  "Backend DTO'si qo'lda yozilmaydi — `shared/api/schema.d.ts` dan re-export qiling " +
  "(`docs/10` 6-bo'lim). Namuna: `export type FooDto = components['schemas']['AdminFooDto'];`. " +
  'Enum yoki lug\'at kalitini toraytirish kerak bo\'lsa `Omit<components["schemas"]["X"], "k"> & { k: … }` ' +
  "ishlating. Sxema chindan ESKIRGAN bo'lsa — sababini maydon darajasida izohda yozing va " +
  "nomni `eslint.config.js` dagi VAQTINCHALIK istisno ro'yxatiga qo'shing.";

/**
 * `features` ostidagi `model` papkalari uchun "qo'lda DTO yozilmasin" qoidasi.
 *
 * Ikki selektor:
 * - `interface FooDto { … }` — har doim taqiqlanadi;
 * - `type FooDto = { … }` — inline obyekt literali taqiqlanadi. `components['schemas'][…]`
 *   dan re-export (`TSIndexedAccessType`) va `Omit<…> & { … }` kesishmasi RUXSAT ETILADI —
 *   literal u yerda `TSIntersectionType` ichida, alias'ning bevosita farzandi emas.
 *
 * @param {string[]} exemptNames vaqtinchalik istisno qilingan tip nomlari
 */
function noHandwrittenDtoRule(exemptNames = []) {
  const not = exemptNames.map((name) => `:not([id.name='${name}'])`).join('');
  return [
    'error',
    NO_DANGEROUS_HTML,
    {
      selector: `TSInterfaceDeclaration[id.name=${DTO_NAME_PATTERN}]${not}`,
      message: NO_HANDWRITTEN_DTO_MESSAGE,
    },
    {
      selector: `TSTypeAliasDeclaration[id.name=${DTO_NAME_PATTERN}]${not} > TSTypeLiteral`,
      message: NO_HANDWRITTEN_DTO_MESSAGE,
    },
  ];
}

export default tseslint.config(
  {
    ignores: ['dist', 'coverage', 'node_modules', 'src/shared/api/schema.d.ts'],
  },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      reactHooks.configs.flat['recommended-latest'],
      jsxA11y.flatConfigs.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: 'module',
      globals: globals.browser,
      parserOptions: {
        ecmaFeatures: { jsx: true },
      },
    },
    rules: {
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/consistent-type-imports': ['error', { prefer: 'type-imports' }],
      'no-restricted-syntax': ['error', NO_DANGEROUS_HTML],
    },
  },

  // ---------------------------------------------------------------------------
  // `docs/10` §6: "Qo'lda yozilgan DTO tiplariga ruxsat yo'q — faqat generatsiya
  // yoki `types.ts` da re-export".
  //
  // Nega qoida kerak: 2026-09-02/03 da frontend va backend orasida shartnoma
  // nomuvofiqliklari topildi (`results.mbti16` ↔ `MBTI16`, RIASEC `A` ↔ `ART`,
  // `backupCodes` ↔ `recoveryCodes`, `currentPassword` ↔ `password`, AI provayder
  // `baseUrl`, AI hisobotining 5 bo'limi, `AdminAssessmentAnswerDto.value` ↔ `rawValue`,
  // `RefreshResponse.refreshToken`) — hammasi qo'lda yozilgan DTO'dan kelib chiqqan.
  //
  // QAMROV: `features/*/model/**` VA `shared/api/**`. Ikkinchisi 2026-09-03 da qo'shildi —
  // qoida faqat feature'larni qamragani uchun `shared/api/types.ts` dagi uchta ommaviy
  // javob tipi va `shared/api/adminClient.ts` dagi `RefreshResponse` ushlanmay qolgan edi
  // (oxirgisida backend HECH QACHON yubormaydigan `refreshToken` maydoni bor edi).
  // ---------------------------------------------------------------------------
  {
    files: ['src/features/*/model/**/*.{ts,tsx}', 'src/shared/api/**/*.{ts,tsx}'],
    ignores: [
      'src/features/*/model/**/*.test.{ts,tsx}',
      'src/shared/api/**/*.test.{ts,tsx}',
    ],
    rules: { 'no-restricted-syntax': noHandwrittenDtoRule() },
  },

  // --- ISTISNOLAR -------------------------------------------------------------
  // 2026-09-03 da `npm run generate:api` yangi sxema bergach ro'yxat 26 nomdan IKKITAGA
  // qisqardi. Qolgan ikkisi "sxema eskirgan" sababidan EMAS — ikkalasi ham backend DTO'si
  // emas, shu sabab ular doimiy istisno. Yangi nom qo'shish uchun PM tasdig'i kerak.

  {
    // `PagedResult<T>` — TRANSPORT qatlami generigi (`docs/07` §4 sahifalash konvensiyasi),
    // biror bitta backend DTO'si emas: sxemada u har element tipi uchun alohida nom bilan
    // chiqadi (`AdminSchoolListItemDtoPagedResult`, `AdminStudentListItemDtoPagedResult` …),
    // ya'ni generik shakl sxemadan re-export QILIB BO'LMAYDI.
    files: ['src/shared/api/types.ts'],
    rules: { 'no-restricted-syntax': noHandwrittenDtoRule(['PagedResult']) },
  },
  {
    // `ImportValidationResult` — MIJOZ tomonidagi zod tekshiruvi natijasi (`prompts/35` B4:
    // "yuklashdan oldin validatsiya va preview"). Tarmoqqa umuman chiqmaydi va backendda
    // mos DTO yo'q; nomi shunchaki naqshga tushib qolgan.
    files: ['src/features/catalog/model/importSchema.ts'],
    rules: { 'no-restricted-syntax': noHandwrittenDtoRule(['ImportValidationResult']) },
  },

  {
    files: ['**/*.test.{ts,tsx}', 'src/test/**/*.{ts,tsx}'],
    languageOptions: {
      globals: { ...globals.browser, ...globals.node },
    },
  },
  {
    files: ['*.config.{js,ts}', 'vite.config.ts', 'vitest.config.ts'],
    languageOptions: {
      globals: globals.node,
    },
  },
  prettierConfig,
);
