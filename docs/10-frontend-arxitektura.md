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
| UI | **Tailwind CSS 4** + `shadcn/ui` (Radix) | Tez, erishimli, mobile-first |
| Diagramma | **Recharts** | Radar (Big Five), bar (RIASEC), gauge (indekslar) |
| Jadval | **Qo'lda yozilgan `DataTable`** (`shared/ui/DataTable.tsx`) | Server-side pagination/sort, URL query bilan sinxron. TanStack Table qo'shilmadi — bizga faqat server tomonda hisoblangan sahifa/saralashni ko'rsatish kerak, mijoz tomonida saralash/filtrlash/guruhlash kerak emas. Kutubxonaning asosiy qiymati aynan o'sha mijoz-tomon imkoniyatlarida, shuning uchun u ishlatilmaydigan bog'liqlik bo'lib qolardi (P22 da olib tashlandi) |
| i18n | **i18next** | `uz` asosiy, `ru` tayyor |
| Test | **Vitest + Testing Library**, **Playwright** (E2E) | |

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
│   │   ├── ui/                   # Button, Card, Input, Select, Badge, Skeleton, Modal, Table
│   │   ├── hooks/                # useDebounce, useLocalStorage, useOnline, usePageTitle
│   │   ├── lib/                  # formatDate, formatPhone, cn(), scoreColor()
│   │   └── config/               # env, ROUTES, QUERY_KEYS
│   │
│   ├── features/
│   │   ├── public-assessment/
│   │   │   ├── api/              # useSchoolInfo, useStartSession, useSaveAnswers, ...
│   │   │   ├── components/       # LikertQuestion, QuestionPage, ProgressBar,
│   │   │   │                     # TestIntroCard, BreakScreen, ConsentBlock, OfflineBanner
│   │   │   ├── store/            # sessionStore.ts (Zustand + persist)
│   │   │   └── pages/            # LandingPage, RegistrationPage, TestPage,
│   │   │                         # TestCompletePage, FinishPage, StudentResultPage
│   │   ├── auth/                 # LoginPage, useLogin, authStore, ProtectedRoute
│   │   ├── dashboard/            # DashboardPage, StatCard, DistributionChart
│   │   ├── schools/              # SchoolsPage, SchoolFormDialog, LinkCell, QrDialog
│   │   ├── students/             # StudentsPage, StudentFilters, StudentProfilePage
│   │   ├── assessments/          # AssessmentsPage, AssessmentDetailPage, AnswersDialog
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
│       ├── PublicLayout.tsx      # sodda, brendsiz, mobil-birinchi
│       └── AdminLayout.tsx       # sidebar + header + breadcrumb
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
| `/t/:slug` | Public | Maktab landing + testlar tavsifi |
| `/t/:slug/register` | Public | Anketa |
| `/t/:slug/test/:testCode` | Public | Savollar (sahifalangan) |
| `/t/:slug/test/:testCode/done` | Public | Blok tugadi / tanaffus |
| `/t/:slug/finish` | Public | Rahmat ekrani |
| `/t/:slug/result` | Public | Qisqa natija (ruxsat bo'lsa) |
| `/admin/login` | — | Kirish |
| `/admin` | Admin | Dashboard |
| `/admin/schools` | Admin | Maktablar |
| `/admin/students` | Admin | O'quvchilar |
| `/admin/students/:id` | Admin | **Individual profil** |
| `/admin/assessments` | Admin | Sessiyalar |
| `/admin/assessments/:id` | Admin | Sessiya detali |
| `/admin/catalog` | Admin | Testlar va savollar |
| `/admin/ai` | Admin | AI provayderlar va promptlar |
| `/admin/audit` | Admin | Audit log |
| `/admin/settings` | Admin | Parol, 2FA, umumiy sozlamalar |

`ProtectedRoute` — token yo'q bo'lsa `/admin/login` ga (qaytish yo'li `?returnUrl=`).

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
- Likert — 5 ta katta tugma (min 44×44 px), klaviatura: `1..5` raqamlari, `Enter` keyingi.
- Javob berilgan savol yashil chiziq bilan belgilanadi.
- "Keyingi sahifa" tugmasi barcha majburiy savollar to'ldirilmaguncha o'chiq,
  to'ldirilmagan savollar qizil ramka bilan ko'rsatiladi.
- Progress: `answered / total` + umumiy 4 blok progress bar.
- Har savolda `durationMs` — savol ko'ringan vaqtdan javobgacha (`performance.now()`).

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
