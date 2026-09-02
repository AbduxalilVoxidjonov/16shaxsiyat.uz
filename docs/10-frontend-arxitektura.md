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

- Backend `swagger.json` dan tiplar generatsiya qilinadi:
  `npx openapi-typescript http://localhost:5000/swagger/v1/swagger.json -o src/shared/api/schema.d.ts`
- `npm run generate:api` skripti; CI'da generatsiya natijasi commit bilan farq qilsa — build yiqiladi
  (kontrakt eskirganini erta ko'rish uchun).
- Qo'lda yozilgan DTO tiplariga **ruxsat yo'q** (faqat generatsiya yoki `types.ts` da re-export).

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
VITE_API_BASE_URL=https://api.salohiyat.uz
VITE_APP_NAME=Salohiyat
VITE_SENTRY_DSN=            # ixtiyoriy
```
