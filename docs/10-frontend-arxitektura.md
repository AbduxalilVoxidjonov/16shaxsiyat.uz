# 10 — Frontend arxitekturasi

## 1. Stek

| Qatlam | Tanlov | Sabab |
|--------|--------|-------|
| Framework | **React 19 + TypeScript (strict)** | Jamoa tajribasi, tiplangan API |
| Build | **Vite 6** | Tez dev, yengil build |
| Routing | **React Router 7** | Ikki route guruhi: `/t/*`, `/admin/*` |
| Server state | **TanStack Query v5** | Kesh, retry, invalidatsiya, autosave uchun mutation |
| Client state | **Zustand** | Sessiya tokeni, test progress, UI holati |
| Forma | **React Hook Form + Zod** | Anketa validatsiyasi, backend qoidalariga mos |
| UI | **Tailwind CSS 4** + qo'lda yozilgan `shared/ui/*` va `shared/ui/brand/*` — tashqi komponent kutubxonasi YO'Q | Tez, erishimli, mobile-first; ranglar/shrift/soya/animatsiya tokenlari `src/index.css`da (§9) |
| Diagramma | **Recharts** | Radar (Big Five), bar (RIASEC), gauge (indekslar) |
| Jadval | **Qo'lda yozilgan `DataTable`** (`shared/ui/DataTable.tsx`) | Server-side pagination/sort, URL query bilan sinxron. TanStack Table qo'shilmadi — bizga faqat server tomonda hisoblangan sahifa/saralashni ko'rsatish kerak, mijoz tomonida saralash/filtrlash/guruhlash kerak emas. Kutubxonaning asosiy qiymati aynan o'sha mijoz-tomon imkoniyatlarida, shuning uchun u ishlatilmaydigan bog'liqlik bo'lib qolardi (P22 da olib tashlandi) |
| i18n | **i18next** | `uz` asosiy, `ru` bo'sh skelet (pastga qarang) |
| Test | **Vitest + Testing Library**, **Playwright** (E2E) | |

> **Tuzatildi (P45, 2026-09-05):** bu jadval ilgari UI qatorida `shadcn/ui` (Radix) deb yozgan
> edi — `package.json`da bunday paket hech qachon bo'lmagan (tekshirildi: `dependencies`/
> `devDependencies` da yo'q). Amalda UI qo'lda yozilgan `shared/ui/*` komponentlari va Tailwind 4
> tokenlari bilan quriladi; tashqi headless-komponent kutubxonasi ishlatilmaydi. `i18n` qatoridagi
> "`ru` tayyor" ham noto'g'ri edi — `locales/ru/common.json` bo'sh obyekt (`{}`), til
> almashtirgich UI'da yo'q, `i18n.ts`dagi `fallbackLng: 'uz'` hamma narsani o'zbekchaga qaytaradi.

---

## 2. Papka strukturasi

```
frontend/
├── src/
│   ├── app/
│   │   ├── router.tsx            # barcha route'lar, lazy import
│   │   ├── providers.tsx         # QueryClient, i18n, Toast, ErrorBoundary
│   │   └── main.tsx
│   ├── shared/
│   │   ├── api/
│   │   │   ├── client.ts         # fetch wrapper: baseUrl, xato → AppError
│   │   │   ├── publicClient.ts   # X-Session-Token qo'shadi
│   │   │   ├── adminClient.ts    # Bearer + 401 da refresh
│   │   │   └── types.ts          # backend DTO tiplari (qo'lda yoki generatsiya)
│   │   ├── ui/                   # Badge, Button, Card, Checkbox, ConfirmDialog, DataTable,
│   │   │   │                     # Dialog, EmptyState, ErrorState, Input, PlaceholderPage,
│   │   │   │                     # Select, Skeleton, Spinner, Table, Textarea, Toast, ...
│   │   │   └── brand/            # Ommaviy dizayn elementlari (P45) — §9.4: GirihStar, Logo,
│   │   │                         # Ornament.tsx (Divider/Blob/ArchTop), PatternBackdrop
│   │   ├── hooks/                # useDebounce, useLocalStorage, useOnline, usePageTitle
│   │   ├── lib/                  # formatDate, formatPhone, cn(), scoreColor()
│   │   └── config/               # env, ROUTES, QUERY_KEYS
│   │
│   ├── features/
│   │   ├── marketing/            # Ommaviy tanishtiruv sahifalari (P45), §9.6 — test oqimidan
│   │   │   │                     # mustaqil, sessiyaga bog'liq emas
│   │   │   ├── pages/             # HomePage, MethodologyPage, AboutPage, ContactPage
│   │   │   ├── sections/          # HeroSection, HowItWorksSection, MethodBlocksSection,
│   │   │   │                      # BenefitsSection, FaqSection, CtaBandSection
│   │   │   └── components/        # PageHero
│   │   ├── public-assessment/
│   │   │   ├── api/              # useSchoolInfo, useStartSession, useSaveAnswers, ...
│   │   │   ├── components/       # LikertQuestion (P45: moslashuvchan — §4.3), QuestionPage,
│   │   │   │                     # ProgressBar, TestIntroCard, BreakScreen, ConsentBlock,
│   │   │   │                     # OfflineBanner, publicStyles.ts (tugma o'lcham/soya klasslari)
│   │   │   ├── store/            # sessionStore.ts (Zustand + persist)
│   │   │   └── pages/            # LandingPage, RegistrationPage, TestPage,
│   │   │                         # TestCompletePage, FinishPage, StudentResultPage
│   │   ├── auth/                 # LoginPage, useLogin, authStore, ProtectedRoute
│   │   ├── dashboard/            # DashboardPage, StatCard, DistributionChart
│   │   ├── schools/              # SchoolsPage, SchoolFormDialog, LinkCell, QrDialog
│   │   ├── students/             # StudentsPage, StudentFilters, StudentProfilePage
│   │   ├── assessments/          # AssessmentsPage, AssessmentDetailPage, AnswersDialog
│   │   ├── programs/             # ProgramsPage, ProgramDetailPage, ProgramFiltersBar
│   │   ├── ai-settings/          # AiProvidersPage, ProviderCard, TestConnectionButton
│   │   ├── catalog/              # TestsPage, QuestionsTable, QuestionEditDialog
│   │   └── audit/                # AuditLogPage
│   │
│   ├── widgets/                  # bir necha feature ishlatadigan katta bloklar
│   │   ├── PersonalityRadar.tsx  # Big Five radar
│   │   ├── AxisBar.tsx           # 16 tip o'qlari
│   │   ├── RiasecChart.tsx
│   │   ├── IndexGauge.tsx        # Maturity / Activity
│   │   ├── ReliabilityBadge.tsx
│   │   └── AiReportView.tsx      # AI hisobotini render qilish
│   │
│   └── layouts/
│       ├── MarketingLayout.tsx   # Ommaviy tanishtiruv qatlami (P45) — sticky shaffof header +
│       │                         # girih naqshli katta footer, §9.5
│       ├── PublicLayout.tsx      # Ommaviy TEST oqimi uchun minimal — navigatsiyasiz, §9.5
│       └── AdminLayout.tsx       # sidebar + header + breadcrumb (P45: yangi palitrada, §9.9)
├── public/
├── index.html
├── vite.config.ts
├── tailwind.config.ts
└── tsconfig.json
```

**Qoida:** `features/*` bir-birini import qilmaydi. Umumiy narsa `shared/` yoki `widgets/` ga chiqariladi.

---

## 3. Route xaritasi

| Yo'l | Layout | Sahifa |
|------|--------|--------|
| `/` | **Marketing** (P45) | Bosh sahifa — M-1, §9.6 |
| `/metodika` | Marketing | Metodika — M-2 |
| `/metodika/:kod` | Marketing | Bitta shaxsiyat tipi (16 tipdan biri) — M-2.1 |
| `/biz-haqimizda` | Marketing | Biz haqimizda — M-3 |
| `/aloqa` | Marketing | Aloqa (forma YO'Q) — M-4 |
| `/t/:slug` | Public | Maktab landing + testlar tavsifi |
| `/t/:slug/register` | Public | Anketa |
| `/t/:slug/test/:testCode` | Public | Savollar (sahifalangan) |
| `/t/:slug/test/:testCode/done` | Public | Blok tugadi / tanaffus |
| `/t/:slug/finish` | Public | Rahmat ekrani |
| `/t/:slug/result` | Public | Qisqa natija (ruxsat bo'lsa) |
| `/admin/login` | — | Kirish |
| `/admin` | Admin | Dashboard |
| `/admin/schools` | Admin | Maktablar |
| `/admin/schools/:id` | Admin | Maktab detali |
| `/admin/students` | Admin | O'quvchilar |
| `/admin/students/:id` | Admin | **Individual profil** |
| `/admin/assessments` | Admin | Sessiyalar |
| `/admin/assessments/:id` | Admin | Sessiya detali |
| `/admin/catalog` | Admin | Testlar va savollar |
| `/admin/catalog/tests/:id` | Admin | Anketa konstruktori / tizim testi tahriri |
| `/admin/programs` | Admin | Dasturlar |
| `/admin/programs/:id` | Admin | Dastur detali |
| `/admin/ai` | Admin | AI provayderlar va promptlar |
| `/admin/audit` | Admin | Audit log |
| `/admin/settings` | Admin | Parol, 2FA, umumiy sozlamalar |

`ProtectedRoute` — token yo'q bo'lsa `/admin/login` ga (qaytish yo'li `?returnUrl=`).

> **Tuzatildi (P45):** `/admin/schools/:id`, `/admin/catalog/tests/:id`, `/admin/programs` va
> `/admin/programs/:id` bu jadvalda yo'q edi (P23/P35/P38 dan beri, ya'ni bu jadval P45'dan ancha
> oldin ham eskirgan edi) — `router.tsx`ga qarab to'ldirildi. Marketing qatori (P45) yangi.
> Haqiqiy manba doim `frontend/src/shared/config/routes.ts` (`ROUTES`/`ROUTE_PATTERNS`).

---

## 4. Ommaviy oqim — asosiy texnik nuqtalar

### 4.1 Sessiya saqlash
```ts
// sessionStore.ts (Zustand + persist: localStorage)
type SessionState = {
  sessionToken: string | null;
  slug: string | null;
  assessmentId: string | null;
  setSession(t: string, slug: string, id: string): void;
  clear(): void;
};
```
Ilova ochilganda `GET /api/public/sessions/me` bilan holat tiklanadi. `410` kelsa store tozalanadi
va foydalanuvchi landing sahifaga qaytariladi ("Sessiya muddati tugagan, qaytadan boshlang").

### 4.2 Autosave
- Har javob **darhol** lokal store'ga yoziladi (UI hech qachon kutmaydi).
- **Debounce 1.5 s** + sahifa almashganda + har 10 s — `POST /answers` paketda yuboriladi.
- Navbat: yuborilmagan javoblar `pendingAnswers` massivida, `localStorage` da saqlanadi.
- Offline: `navigator.onLine === false` → banner ko'rsatiladi, navbat to'planadi,
  online bo'lganda avtomatik yuboriladi.
- `beforeunload` da `navigator.sendBeacon` bilan oxirgi paket yuboriladi.

### 4.3 Savol UI
- Bir sahifada 10 savol (`pageSize` backenddan).
- **Savol ko'rinishi shkala turiga MOSLASHADI** (`LikertQuestion`, P45) — daraja soni
  `scaleLabels`/`options[]` uzunligidan avtomatik kelib chiqadi, `LikertQuestion` o'zi
  qaysi shkala (`Likert7`/`Likert5`/`Binary`/`SingleChoice`/`ForcedChoice`) ekanini bilmaydi:
  - **4+ daraja** (`Likert7`, `Likert5`) — chetdan markazga kichrayuvchi doiralar qatori
    (60→34→60 px, kuchliroq fikr kattaroq nishon), ostida faqat ikki qutb yorlig'i
    (birinchi/oxirgi daraja matni, o'rtadagilar yozilmaydi);
  - **3 va undan kam daraja** (`Binary`) — markazlashgan yirik doiralar, HAR birining
    OSTIDA o'z yorlig'i (ikki qutb sarlavhasi bunda ortiqcha bo'lardi);
  - **`options[]` bo'lsa** (`SingleChoice`/`ForcedChoice`) — doira EMAS, variant KARTALARI
    (variant matni uzun bo'lishi mumkin, doiraga sig'maydi).
  - Barcha ko'rinishda teginish maydoni min 44×44 px; klaviatura: `1..9` raqamlari mos
    variantni tanlaydi (doira/karta soniga qarab, avval `1..5` edi), `Enter` keyingi savolga
    o'tadi.
- Javob berilgan savol firuza fon/chegara bilan belgilanadi (`.card` + `border-firuza-200
  bg-firuza-50/40`, ilgari "yashil chiziq" edi — endi butun karta ranglanadi).
- "Keyingi sahifa" tugmasi barcha majburiy savollar to'ldirilmaguncha o'chiq,
  to'ldirilmagan savollar qizil ramka bilan ko'rsatiladi.
- Progress: `answered / total` + umumiy 4 blok progress bar.
- Har savolda `durationMs` — savol ko'ringan vaqtdan javobgacha (`performance.now()`).

**P52-A4 — tarmoqlanuvchi so'rovnoma va bo'lim-qadam rejimi** (to'liq shartnoma `docs/18`,
frontend qismi §6.2): `TestPage` javob shaklidan (`GET .../questions` javobida `sections`
bor/yo'qligidan) ikki rejimga bo'linadi, lekin BIR XIL savol render/o'zaro ta'sir yadrosini
(`QuestionRenderer`) ishlatadi:

- **Bo'limsiz** (`sections` yo'q) — yuqoridagi sahifalash oqimi AYNAN o'zgarishsiz (4 ta tizim
  metodikasi, regressiya testi bilan qulflangan).
- **Bo'limli** — bitta so'rovda kelgan BARCHA faol savol orasidan `shared/lib/visibility.ts`
  (`resolveVisibleQuestions`, backend `VisibleQuestionResolver`ning TS egizagi, oltin fikstura
  bilan qulflangan) yordamida HOZIR ko'rinadigan bo'lim/savollar hisoblanadi
  (`features/public-assessment/lib/branchingFlow.ts` — kod↔ID moslashtirish qatlami) va bir
  ekranda BITTA ko'rinadigan bo'lim (`SectionIntro` + shu bo'lim savollari) ko'rsatiladi.
  "Keyingi" keyingi ko'rinadigan bo'limga o'tadi (yashirilganlar sakrab o'tiladi), progress
  `visibleQuestionIds` bo'yicha hisoblanadi (`totalQuestions` emas). Javob o'zgarganda ko'rinish
  `useMemo` bilan DARHOL qayta hisoblanadi — server javobini kutmaydi.
- Yangi savol turlari (`ShortText`/`Phone`/`LongText`/`MultiChoice`) — `QuestionRenderer` savol
  turiga qarab `TextQuestion`/`LongTextQuestion`/`MultiChoiceQuestion`dan birini tanlaydi;
  eski turlar (`Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice`) — o'zgarishsiz
  `LikertQuestion`.
- Javob yuborish shakli — `{ questionId, value?, text?, selectedValues?, durationMs }`
  (`docs/18` §4.2), autosave navbati (`answerQueue.ts`) shu uchtadan bittasini saqlaydi.
  **Yashirilgan savolning mahalliy javobi yuborilmaydi** — `useAutosave`ning
  `visibleQuestionIds` parametri orqali navbatdan filtrlanadi (backend `400
  QUESTION_NOT_VISIBLE` qaytarmasin uchun), lekin javobning o'zi yo'qolmaydi (qayta ko'rinsa
  keyingi flush yuboradi).
- Bo'lim almashganda fokus yangi bo'lim sarlavhasiga ko'chadi (a11y), `prefers-reduced-motion`
  hurmat qilinadi.

### 4.4 Foydalanuvchi tajribasi bo'yicha talablar
- Blok oxirida "Tanaffus qilish" ekrani: nechta blok qoldi, taxminiy vaqt.
- Orqaga qaytish mumkin (javob o'zgartirilsa `revisionCount` oshadi).
- Sahifa yopilsa — "Javoblaringiz saqlandi, xohlagan vaqt davom ettirasiz".
- Mobil: 360px da barcha element sig'adi, sticky pastki panel (progress + Keyingi).

---

## 5. Admin panel — texnik nuqtalar

### 5.1 API qatlam
```ts
// adminClient.ts
// - Bearer token xotirada (Zustand, persist YO'Q)
// - refresh token httpOnly cookie'da; 401 → bitta refresh chaqiruvi (mutex), keyin so'rov qaytariladi
// - refresh ham 401 bersa → logout + /admin/login
```

### 5.2 TanStack Query konvensiyasi
```ts
export const QUERY_KEYS = {
  schools: (f: SchoolFilters) => ['schools', f] as const,
  student:  (id: string)       => ['student', id] as const,
  dashboard:(f: DateRange)     => ['dashboard', f] as const,
};
// staleTime: ro'yxatlar 30s, dashboard 60s, profil 0 (har doim yangi)
// Mutation muvaffaqiyatli → tegishli kalitlarni invalidate
```

### 5.3 Jadvallar
- Server-side pagination/sort/filter — barcha holat **URL query** da (`useSearchParams`),
  shunda sahifa ulashilishi va yangilanishi mumkin.
- Qidiruv 400 ms debounce.
- Skeleton loading; bo'sh holat ("Hali o'quvchi yo'q — maktab havolasini ulashing").

### 5.4 Individual profil sahifasi (eng muhim ekran)
Tartib:
1. **Sarlavha:** FISH, maktab, sinf, yosh, holat belgisi, `ReliabilityBadge`, harakat tugmalari
   (PDF, Qayta tahlil, O'chirish).
2. **Yig'ma kartalar:** tip (`INTJ — Loyihachi`), `MaturityIndex` gauge, `ActivityIndex` gauge,
   Holland kodi (`IRA`).
3. **Diagrammalar:** 16 tip o'qlari (4 ta gorizontal bar, markazda 50), Big Five radar,
   RIASEC bar, Aktivlik 4 shkala.
4. **AI hisobot:** `AiReportView` — bo'limlar akkordeoni, chop etishga tayyor.
5. **Tarix:** oldingi sessiyalar va AI tahlillar ro'yxati (provider, sana, ochish).
6. Sessiya `Analyzing` bo'lsa — 5 soniyada bir `refetchInterval` bilan yangilanadi.
7. `AnalysisFailed` bo'lsa — xato sababi + "Boshqa provider bilan urinish" tanlovi.

### 5.5 Anketa konstruktori — tarmoqlanuvchi so'rovnoma (P52, `docs/18`)

`features/catalog` ichida `CatalogTestDetailPage` bo'limlari (`Custom` va `Survey` rejimida):

- `SectionsSection`/`SectionDialog` — bo'limlar CRUD (`ScalesSection.tsx` naqshi).
- `QuestionEditorDialog` — `docs/18` §2.1 yangi turlari (`ShortText`/`LongText`/
  `MultiChoice`/`Phone`) va ular uchun maydonlar (`placeholder`/`inputPattern`/`maxLength`/
  `min`-`maxSelections`/`options[]`) FAQAT `scoringMode === 'Survey'` bo'lganda ko'rinadi
  (B-1/B-2) — `Scored` anketada bu turlar tanlov ro'yxatidan chiqarib tashlanadi, bo'lim va
  ko'rsatish sharti bloklari esa tushuntirish matni bilan almashtiriladi.
- `VisibilityRuleEditor` — savol/bo'lim shartini tahrirlaydi: manba savol FAQAT oldinroqdagi
  savollardan (`model/visibilityEditorHelpers.ts` — `questionsBeforeOrder`/
  `questionsBeforeSection`, B-4), operator manba savol turiga qarab filtrlanadi, qiymat(lar)
  variant/shkala darajasidan tanlanadi (erkin son kiritish yo'q), pastda o'zbekcha jonli
  jumla ko'rinishida oldindan ko'rish.
- `BranchingPreview` — faqat o'qish, "qaysi javob qaysi bo'limga olib boradi" ro'yxati.
- `shared/api/branchingTypes.ts` — backend admin endpointlari hali sxemada yo'qligi sababli
  MUVAQQAT qo'lda yozilgan tiplar (`AdminSection`, `AdminQuestionOption`, …); `npm run
  generate:api` chiqqach shu blok o'chiriladi.

Tafsilot, admin API yo'llari va nashr validatsiyasi kodlari — `docs/18` §5–§6.3.

---

## 6. Tiplar va API sinxronizatsiyasi

### 6.1 Asosiy qoida

- Backend `swagger.json` dan tiplar generatsiya qilinadi:
  `npm run generate:api` → `src/shared/api/schema.d.ts` (`API_URL` bilan port ko'rsatiladi).
- CI'da generatsiya natijasi commit bilan farq qilsa — build yiqiladi (`api-contract-sync` job,
  `docs/13`). Kontrakt eskirganini erta ko'rish uchun.
- Qo'lda yozilgan DTO tiplariga **ruxsat yo'q** — faqat generatsiya yoki `types.ts` da re-export.

### 6.2 Re-export naqshlari

```ts
import type { components } from '@/shared/api/schema';

// 1. To'g'ridan-to'g'ri re-export — DEFAULT
export type SchoolListItemDto = components['schemas']['AdminSchoolListItemDto'];

// 2. Enum toraytirish — backend enum'ni `ToString()` bilan qaytaradi, sxemada `string`.
//    `Omit` + qayta e'lon: maydon NOMI/soni sxemadan tekshiriladi, TIPI toraytiriladi.
export type StudentListItemDto = Omit<
  components['schemas']['AdminStudentListItemDto'],
  'activityLevel'
> & { activityLevel?: ActivityLevel | null };

// 3. So'rov/filtr shakllari (`*Query`) — bular backend DTO'si EMAS, qo'lda yoziladi.
export interface SchoolsListQuery { page: number; pageSize: number }
```

**Sxema `null` ni ifodalay olmaydigan joy (`$ref` maydonlari).** Swashbuckle OpenAPI 3.0 da
`$ref` yonida `nullable: true` chiqara olmaydi, shu sabab obyekt tipidagi nullable maydon
sxemada faqat IXTIYORIY (`?`) bo'lib ko'rinadi — `| null` siz. Backend esa
`DefaultIgnoreCondition` sozlanmagani uchun (`Api/Program.cs`) uni ANIQ `null` bilan yuboradi
(`latestAssessment`, `aiAnalysis`, `results.MBTI16`, `AssessmentDetailDto.tests` …). Bunday
maydonda `Omit<…> & { k?: X | null }` bilan `| null` QAYTA QO'SHILADI va sabab izohda
yoziladi: sxemaga so'zma-so'z ergashish bu yerda runtime xatoga olib keladi
(`x === undefined` tekshiruvi `null` ni o'tkazib yuboradi). Bu — YAGONA holat, unda frontend
sxemadan kengroq bo'lishi to'g'ri; boshqa hamma joyda sxema haqiqat.

**Ochiq lug'atlar (`Record<string, …>`) haqida.** Swashbuckle `IReadOnlyDictionary<string, X>`
ni ochiq lug'at qilib chiqaradi, ya'ni kalitlar shartnomadan yo'qoladi. Kalitlar shartnomaning
bir qismi bo'lgan joyda (`RIASEC.types` — `R I A S E C`; `BIG5.factors` — `O C E A N`;
`MBTI16.axes` — `EI SN TF JP`) tip **yopiq kalitlar bilan** qo'lda yoziladi va sabab izohda
yoziladi. Qiymat tipi baribir sxemadan olinadi
(`components['schemas']['AdminFactorDto']`).

### 6.3 ESLint qo'riqchisi

`eslint.config.js` — `features/*/model/**` **va `shared/api/**`** ichida backend DTO'siga o'xshash nomni
(`*Dto`, `*Request`, `*Response`, `*Result`, `*Item`, `*Detail`) `interface` yoki inline
`type = { … }` bilan e'lon qilish **xato**. Ruxsat etilgan shakllar: `components['schemas'][…]`
dan re-export va `Omit<…> & { … }` kesishmasi.

Qamrov `shared/api/**` ga 2026-09-03 da yoyildi: qoida faqat feature'larni qamragani uchun
`shared/api/types.ts` dagi uchta ommaviy javob tipi va `shared/api/adminClient.ts` dagi
`RefreshResponse` ushlanmay qolgan edi (oxirgisida backend HECH QACHON yubormaydigan
`refreshToken` maydoni bor edi).

Sxema chindan eskirgan bo'lsa — sabab tegishli faylda **maydon darajasida** yoziladi va tip
nomi `eslint.config.js` dagi istisno ro'yxatiga qo'shiladi. Bu ro'yxat `npm run generate:api`
dan keyin **qisqarishi shart** — u texnik qarzning ochiq reyestri. 2026-09-03 da sxema qayta
generatsiya qilingach ro'yxat **26 nomdan 2 taga** qisqardi va qolgan ikkitasi "sxema
eskirgan" sababidan EMAS — ikkalasi ham backend DTO'si emas, shu sabab doimiy:

| Nom | Nega doimiy |
|---|---|
| `PagedResult<T>` (`shared/api/types.ts`) | Transport generigi; sxemada har element tipi uchun alohida nom (`AdminSchoolListItemDtoPagedResult` …), generik shakl re-export qilib bo'lmaydi |
| `ImportValidationResult` (`features/catalog/model/importSchema.ts`) | Mijoz tomonidagi zod tekshiruvi natijasi, tarmoqqa chiqmaydi; nomi shunchaki naqshga tushgan |

### 6.4 Test mock'lari sxemadan tiplanadi

`src/test/apiMock.ts` — barcha API mock'lari uchun yagona tiplangan yordamchi:

| Yordamchi | Qachon |
|---|---|
| `jsonResponse<'SxemaNomi'>(body)` | Javob `schema.d.ts` da bor — DEFAULT |
| `listResponse<'SxemaNomi'>(items)` | Sahifalanmagan massiv javob (`[...]`) |
| `pagedResponse<'SxemaNomi'>(items)` | Sahifalangan ro'yxat (`docs/07` §4) |
| `typedResponse<FeatureDto>(body)` | Sxemada hali yo'q / sxemasi eskirgan javob |
| `problemResponse(code, status)` | `ProblemDetails` xatosi (`docs/06` §6) |
| `emptyResponse(status)` | Tanasiz javob (`204`) |

Fixture'lar `satisfies Schemas['…']` bilan tasdiqlanadi. Test faylida o'z
`function jsonResponse(body: unknown, …)` nusxasini yozish **taqiqlanadi**: `unknown` hech
narsani tekshirmaydi, shu sabab mock backenddan uzilib qolsa ham test yashil qolardi — ya'ni
test frontendning o'z taxminini tasdiqlar, backendni emas.

### 6.5 Nega bu qat'iy — 2026-09-02/03 hodisasi

Bir kunda **oltita** shartnoma nomuvofiqligi topildi, hammasi bir sababdan (qo'lda yozilgan
DTO + tiplanmagan mock):

| Nomuvofiqlik | Oqibati |
|---|---|
| `results.mbti16` ↔ `MBTI16` | O'quvchi profilida bo'limlar bo'sh |
| RIASEC `A` ↔ `ART` | Diagramma `TypeError` bilan buzildi |
| `backupCodes` ↔ `recoveryCodes` | 2FA zaxira kodlari ko'rinmadi, 2FA esa yoqilgan |
| `currentPassword` ↔ `password` | 2FA'ni o'chirib bo'lmadi |
| AI provayder `baseUrl` yuborilmasdi | Har saqlashda sozlama jimgina o'chardi |
| AI hisobotining 5 bo'limi DTO'da yo'q | AI yozdi, admin ko'rmadi |

Sxema qayta generatsiya qilinib (2026-09-03) qolgan tiplar ham re-export'ga o'tkazilganda
YANA to'rtta nomuvofiqlik chiqdi — hammasi `tsc` bosqichida:

| Nomuvofiqlik | Oqibati |
|---|---|
| `RefreshResponse.refreshToken` (`adminClient.ts`) | Backend uni HECH QACHON yubormaydi (`httpOnly` cookie) — o'lik maydon; `expiresIn` esa majburiy bo'lsa-da ixtiyoriy deb yozilgan edi |
| `AdminSchoolStatsDto.completionRate` `=== null` | Sxemada maydon IXTIYORIY — javobda bo'lmasa `undefined * 100` → `NaN%` |
| `AdminActivityDto.activityIndex` `!== null` | Xuddi shu naqsh: maydon tushib qolsa diagramma `undefined` ball bilan chizilardi |
| `AdminAiUsageDto.estimatedCostUsd` `=== null` | Maydon yo'q bo'lsa `$undefined` chiqardi |
| Mock'da `careerSuggestions[].exampleProfessions` yo'q | Backend DTO'sida MAJBURIY — mock shartnomadan uzilgan edi |

Himoya uch qatlamli: **(1)** re-export (`tsc` shakl farqini ushlaydi) → **(2)** ESLint
(yangi qo'lda DTO yozilmaydi) → **(3)** CI drift (`schema.d.ts` eskirmaydi).

---

## 7. Sifat qoidalari

- `tsconfig`: `strict: true`, `noUncheckedIndexedAccess: true`, `noImplicitOverride`.
- ESLint: `react-hooks`, `@typescript-eslint`, `jsx-a11y`; `dangerouslySetInnerHTML` — xato.
- Prettier + `lint-staged` (pre-commit).
- Har sahifa `ErrorBoundary` ichida; xato ekranida "Qayta urinish" tugmasi.
- Barcha matn i18n kalitlari orqali (`t('assessment.next')`), hardcode matn yo'q.
- Bundle: route bo'yicha `lazy()`; admin va public alohida chunk.
- Lighthouse maqsad: Performance ≥ 85 (mobil), Accessibility ≥ 95.

---

## 8. Muhit o'zgaruvchilari

```
VITE_API_BASE_URL=https://api.16shaxsiyat.uz
VITE_APP_NAME=Shaxsiyat
VITE_SENTRY_DSN=            # ixtiyoriy
```

---

## 9. Dizayn tizimi va ommaviy tanishtiruv qatlami (P45)

`16shaxsiyat.uz` (Next.js 15 + Tailwind 3 statik sayt) loyihasining vizual dizayni **P45**
(2026-09-05) da shu frontendga ko'chirildi: yangi rang/shrift/soya tokenlari, brend
komponentlari (`shared/ui/brand/`) va butunlay yangi **marketing qatlami** (`/`, `/metodika`,
`/biz-haqimizda`, `/aloqa` — ilgari `/` `404` qaytarardi). Ommaviy TEST oqimi (§4, `docs/11`
E-1…E-6) va admin panel (§5) **mantiqan o'zgarmadi** — faqat JSX/`className` yangi tizimga
o'tkazildi.

### 9.1 Ikki rang tizimi yonma-yon

`src/index.css`dagi eski `--color-primary-*`/`success-*`/`warning-*`/`danger-*`/`neutral-*`
tokenlari **SAQLANGAN** — admin panel va `shared/ui`ning aksariyat komponenti (`Button`,
`Badge`, `Toast`, jadval holat ranglari va h.k.) hamon ularga tayanadi (§9.9). Yangi tokenlar
FAQAT qo'shildi, hech narsa o'chirilmadi yoki qayta nomlanmadi. Amalda ikki mustaqil palitra bir
vaqtda mavjud — bu ATAYLAB qilingan (eski palitraga tayangan 69 dan ortiq faylni bir yo'la
ko'chirish P45 doirasiga kirmagan), lekin uzoq muddatda ikkita rang tizimini saqlash o'zi
texnik qarz (§9.9, `PROGRESS.md`).

### 9.2 Yangi tokenlar (`@theme`, `src/index.css`)

| Guruh | Tokenlar | Izoh |
|---|---|---|
| Qog'oz (fon) | `paper` `paper-deep` `paper-card` | Ommaviy sahifalarning iliq fon qatlamlari |
| Siyoh (matn) | `ink` `ink-soft` `ink-muted` `ink-faint` | Matn ierarxiyasi: asosiy → yumshoq → so'nik → juda och |
| Chiziq | `line` `line-strong` | Chegaralar, ajratkichlar |
| Brend aksenti | `firuza-50…900` | Asosiy harakat rangi (CTA, faol holat) |
| Dekorativ palitralar | `binafsha-*` `zumrad-*` `lojuvard-*` `zarhal-*` `terakota-*` | Har biri 50–900, urg'u va farqlash uchun (masalan `LikertQuestion`da rad javobi — binafsha, `StudentResultPage`da kasb yo'nalishi chipi — lojuvard) |
| Shrift | `font-sans` = Inter Variable, `font-display` = Plus Jakarta Sans Variable | `@fontsource-variable/inter` va `@fontsource-variable/plus-jakarta-sans` (`main.tsx`da import); `font-display` Tailwind 4 avtomatik yaratmagani uchun `@utility` bilan qo'lda qo'shilgan |
| Yumaloqlash | `rounded-4xl` (2rem) `rounded-5xl` (2.75rem) | |
| Soya | `shadow-soft` `shadow-lift` `shadow-glow` | |
| Konteyner | `max-w-content` (72rem) `max-w-prose` (44rem) | `.wrap`/`.wrap-narrow` shulardan foydalanadi |
| Tezlanish | `ease-signature` | Loyihaning "imzo" kubik-bezye tezlanishi |
| Animatsiya | `animate-fade-up` `animate-fade-in` `animate-float` `animate-spin-slow` `animate-grow` | `@keyframes` alohida yozilgan — Tailwind 4 `--animate-*` faqat qisqartmani beradi |
| Utility | `bg-girih` `bg-girih-light` (girih naqsh fonlari, data-URI SVG) `mask-fade-b` `balance` `no-scrollbar` | |

**Ikkita ataylab kontrast og'ishi** (asl 16shaxsiyat dizaynidan farqli, WCAG AA sababli,
`index.css` izohida batafsil):
- `.btn-primary` foni `firuza-600`, `firuza-500` EMAS — oq matn `firuza-500` ustida 3.28:1
  (AA 4.5:1 dan past), `firuza-600` bilan 4.76:1.
- `.eyebrow` rangi `ink-soft`, `ink-muted` EMAS — `ink-muted` `paper` fonida 4.49:1 (chegaradan
  bir chizim past), `ink-soft` 9.29:1.

### 9.3 Komponent klasslari (`@layer components`, faqat ommaviy sahifalarda)

`.wrap`/`.wrap-narrow` (kenglik konteynerlari) · `.card`/`.card-hover` · `.btn` +
o'lcham (`.btn-lg|md|sm`) + ko'rinish (`.btn-primary|ghost|dark`) · `.chip` · `.eyebrow` ·
`.lead` · `.prose-uz`. Nomlar prefikssiz — loyiha bo'ylab tekshirilgan, hech qayerda
to'qnashuv yo'q (`shared/ui/Card.tsx`/`Button.tsx` faqat utility klass bilan yozilgan).
Admin panel va `shared/ui` bu klasslarga tayanmaydi, faqat ommaviy sahifalar (`PublicLayout`,
`MarketingLayout`, `features/marketing/**`, `features/public-assessment/**`) ishlatadi.

### 9.4 Brend komponentlari — `shared/ui/brand/`

| Komponent | Fayl | Vazifasi |
|---|---|---|
| `GirihStar` | `GirihStar.tsx` | Ikki 45°ga burilgan kvadrat + ixtiyoriy markaziy doira (`withCircle`) — sakkiz qirrali yulduzcha. Rang `currentColor` orqali, sof dekorativ (`aria-hidden`) |
| `Logo` | `Logo.tsx` | Gradientli `GirihStar` nishoni + `app.name` (i18n). `asLink` — headerda `/` ga havola, footerda oddiy `<span>` |
| `Divider`, `Blob`, `ArchTop` | `Ornament.tsx` (uchtasi bitta faylda) | `Divider` — bo'lim ajratkich chiziq+girih; `Blob` — xira rangli fon dog'i (`className` bilan rang/joy beriladi); `ArchTop` — gumbaz yoy chizig'i |
| `PatternBackdrop` | `PatternBackdrop.tsx` | `bg-girih`/`bg-girih-light` fon qatlami, `mask-fade-b` bilan pastga so'nadi |

Barchasi `shared/ui/brand/index.ts` orqali va shu bilan birga `shared/ui/index.ts`dan ham
qayta eksport qilinadi.

### 9.5 Ikki qatlam-komponent: `MarketingLayout` va `PublicLayout`

Ikkalasi ham `bg-paper text-ink` ommaviy palitrasini ishlatadi, lekin **ataylab bir-biriga
tegmaydi**:

| | `MarketingLayout.tsx` | `PublicLayout.tsx` |
|---|---|---|
| Kimga | Maktab rahbarlari, psixologlar, ota-onalar | Testni yechayotgan o'quvchi |
| Header | Sticky, shaffof → skrollda `bg-paper/85 backdrop-blur-xl`ga o'tadi, to'liq navigatsiya + mobil menyu | Statik, faqat `Logo` + shior — navigatsiya YO'Q (chalg'itmaslik uchun) |
| Footer | Katta, girih naqshli, sayt xaritasi + aloqa + huquqiy eslatma | Kichik, faqat `Divider` + shior |
| Sabab | Sahifalar orasida yurish uchun to'liq navigatsiya kerak | `docs/11` §1: "bir ekranda bitta vazifa" — o'quvchi chalg'imasin |

`body` global uslubi (`bg-neutral-50 text-neutral-900`, admin panel uchun) **o'zgarmagan** —
ikkala qatlam ham qog'oz palitrasini o'z wrapperida beradi.

### 9.6 Marketing qatlami — kontent va qoidalar

To'rt sahifa (`features/marketing/pages/`), umumiy `sections/`larga bo'lingan (`HeroSection`,
`HowItWorksSection`, `MethodBlocksSection`, `BenefitsSection`, `FaqSection`, `CtaBandSection`)
va umumiy `PageHero` komponenti (ichki sahifalarning bir xil sarlavha bloki). Ekran
tavsiflari — `docs/11` §2a (M-1…M-4).

Muhim qoidalar:
- **Test bu yerda BOSHLANMAYDI.** O'quvchi testga faqat maktab bergan havola (`/t/:slug`)
  orqali kiradi — hech bir CTA `/t/:slug`ga olib bormaydi, hammasi `/aloqa` yoki `/metodika`ga.
  `HomePage.test.tsx` da regressiya testi bor: `hrefs.some(href => href?.startsWith('/t/'))`
  — `false` bo'lishi shart.
- **`/aloqa`da forma YO'Q** — xabar yuborish backend endpointi mavjud emas, ishlamaydigan
  forma foydalanuvchini aldaydi. Faqat haqiqiy kanallar (email, Telegram) ko'rsatiladi.
- **Kontent 100% yangi yozilgan** — CLAUDE.md 6a-qoidasi (raqobatchi kontenti ko'chirilmaydi):
  16shaxsiyat'ning tur kodlari, guruh nomlari, "MBTI" so'zi bu yerga ko'chirilmagan.
  `HomePage.test.tsx`da buni qo'riqlaydigan regressiya testi bor (matnda `MBTI`,
  `INTJ`/`INFP`/`ESTJ`/`ENFP`, "Tahlilchilar"/"Diplomatlar"/"Posbonlar"/"Izlovchilar" YO'Q).
- Har sahifada "AI tashxis qo'ymaydi" eslatmasi ko'rinarli joyda (footer'da doimiy).

### 9.7 Ommaviy TEST oqimi — qayta ko'rinish (mantiq o'zgarmagan)

`PublicLayout.tsx` va `features/public-assessment/**` (komponent+sahifalar) yangi palitraga
o'tkazildi — faqat JSX/`className`, autosave/sessiya/scoring mantig'i (§4) tegilmagan.
`LikertQuestion` endi moslashuvchan komponent — to'liq tavsif §4.3da.

### 9.8 Natija sahifasi (`StudentResultPage`, E-6) — diagramma ATAYLAB yo'q

`GET /sessions/result` ball yoki o'lcham qaytarmaydi (`docs/07` §1.9), shu sabab bu ekranda
`TraitBar`/foiz/progress-bar **QASDDAN yo'q** — mavjud bo'lmagan ma'lumotni "chizib qo'yish"
soxta xulosa bo'lardi. Uch holat ishlangan (barchasi bir xil "kartasiz" `NoticeCard` uslubida,
qizil xato ko'rinishisiz — bular ODATIY holatlar):

| Holat | Sabab | Ko'rinish |
|---|---|---|
| Tayyor | `200` va `personalityType` bor | To'liq natija kartasi: tip, kuchli tomonlar, kasb yo'nalishlari |
| Tayyorlanmoqda | `202` | Aylanuvchi `GirihStar` + "Qayta urinish" tugmasi |
| Ko'rsatilmaydi | `403` (`App:ShowResultToStudent=false`, **standart sozlama**) | Xushmuomala tushuntirish, xato EMAS |

### 9.9 `shared/ui` va `AdminLayout` — yangi palitraga o'tkazish

`shared/ui`ning 19 komponenti (`Badge`, `Button`, `Card`, `Checkbox`, `ConfirmDialog`,
`DataTable`, `Dialog`, `EmptyState`, `ErrorState`, `Input`, `PlaceholderPage`, `Select`,
`Skeleton`, `Spinner`, `Table`, `Textarea`, `Toast`, `VisuallyHidden` va h.k.) hamda
`AdminLayout` yangi palitraga o'tkazildi: `primary-*` → `firuza-*`, `neutral-*` →
`paper`/`ink`/`line`, `danger` → `terakota`, `success` → `zumrad`, `warning` → `zarhal`.
**Props API o'zgarmagan** — bu sof vizual ko'chirish, `features/**`dagi chaqiruv joylari
tegilmagan.

**Ochiq holat:** eski `--color-primary-*`/`success-*`/`warning-*`/`danger-*`/`neutral-*`
tokenlari `index.css`da hali turibdi va **69 dan ortiq faylda** (`features/**`, `widgets/**`)
to'g'ridan-to'g'ri ishlatiladi (masalan `features/catalog/**`, `features/assessments/**`,
`features/ai-settings/**`) — bular P45 qamroviga kirmadi. Demak hozircha loyihada IKKITA
rang tizimi yonma-yon yashaydi (§9.1); to'liq birlashtirish keyingi bosqich.

### 9.10 Bilib turish kerak bo'lgan cheklovlar

- **`ru` lokali bo'sh** (`locales/ru/common.json` = `{}`), til almashtirgich UI'da yo'q —
  `i18n.ts`dagi `fallbackLng: 'uz'` bilan hamma narsa o'zbekchada ko'rinadi. `marketing.*`
  bo'limi (~190 kalit) faqat `uz/common.json`da.
- **E2E (Playwright) bu o'zgarishlar bilan hali ishga tushirilmagan** — boshqa agent shu
  sababdan yozilgan branch bilan parallel tekshirmoqda. `docs/12`dagi E2E ssenariylari
  yangi ekranlar (marketing) uchun hali yo'q.
- 390px/1440px real brauzerda vizual tekshiruv bu tur qatlami uchun ham hali qilinmagan
  (loyihadagi eski, umumiy cheklov — `PROGRESS.md` "Ma'lum risklar" jadvali).
