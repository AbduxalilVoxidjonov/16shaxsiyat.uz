# Shaxsiyat — frontend

`StudentRoadMap` loyihasining React 19 + TypeScript + Vite frontend'i.
Foydalanuvchiga ko'rinadigan barcha matnda **Shaxsiyat** nomi ishlatiladi
(`docs/00`–`docs/15`, `CLAUDE.md`ga qarang).

Uch qatlam: **ommaviy tanishtiruv** (`/`, `/metodika`, `/biz-haqimizda`, `/aloqa` — maktab
rahbarlari/psixologlar/ota-onalar uchun, `MarketingLayout`), **ommaviy test oqimi**
(`/t/:slug/...` — o'quvchi uchun, `PublicLayout`, sessiyaga bog'liq) va **admin panel**
(`/admin/...` — superadmin, `AdminLayout`). Vizual dizayn tizimi (P45, 2026-09-05)
`16shaxsiyat.uz` statik saytidan ko'chirilgan — pastdagi "Dizayn tizimi" bo'limiga qarang.

Arxitektura tafsilotlari: `docs/10-frontend-arxitektura.md`. UX/ekranlar: `docs/11-ux-va-ekranlar.md`.

## Ishga tushirish

```bash
npm install
cp .env.example .env   # kerak bo'lsa VITE_API_BASE_URL ni o'zgartiring
npm run dev
```

## Buyruqlar

```bash
npm run dev            # dev server (Vite)
npm run build           # tsc -b + production build
npm run preview         # build natijasini lokal ko'rish
npm run typecheck       # tsc -b --noEmit
npm run lint             # ESLint (jsx-a11y, react-hooks, dangerouslySetInnerHTML — xato)
npm run format           # Prettier — yozadi
npm run format:check     # Prettier — faqat tekshiradi
npm run test              # Vitest (bir marta)
npm run test:watch        # Vitest (watch)
npm run test:e2e          # Playwright E2E (alohida sozlash kerak)
npm run generate:api       # backend swagger'dan TS tiplarini generatsiya qiladi
```

## `npm run generate:api` haqida — MUHIM

Skript `openapi-typescript` bilan backend swagger sxemasidan
`src/shared/api/schema.d.ts` faylini generatsiya qiladi
(`docs/10-frontend-arxitektura.md`, 6-bo'lim):

```
openapi-typescript ${API_URL:-http://localhost:5402}/swagger/v1/swagger.json -o src/shared/api/schema.d.ts
```

Manzil `API_URL` muhit o'zgaruvchisi bilan sozlanadi (standart — `http://localhost:5402`).
Ba'zi muhitlarda (masalan bu loyihaning sandbox CI konteyneri) `5000` porti bloklangan
(`403 Forbidden` chiqadi) — shu sabab standart port `5402`ga o'zgartirilgan (`prompts/20`,
P20 hisoboti). Boshqa port/host kerak bo'lsa:

```bash
API_URL=http://localhost:5000 npm run generate:api
```

**Backend Swagger faqat `Development`/`Staging`da ochiq** (`docs/07`, 4-bo'lim) — lokal ishga
tushirishda `ASPNETCORE_ENVIRONMENT=Development` va (DB'ga haqiqiy ulanish shart emas, faqat
`DbContext` ro'yxatdan o'tishi uchun) `ConnectionStrings__Postgres` beriladi:

```bash
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=srm;Username=srm;Password=x" ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../src/StudentRoadMap.Api --no-launch-profile --urls http://localhost:5402
# boshqa terminalda:
npm run generate:api
# tugagach backend'ni to'xtating (Ctrl+C yoki tegishli process'ni o'ldiring).
```

**P20'dan boshlab** (`PublicSessionController`ga `[ProducesResponseType]` qo'shilgach,
backend tuzatildi) generatsiya barcha 6 ommaviy endpoint uchun **to'liq** ishlaydi — so'rov
VA javob sxemalari, shu jumladan xato holatlari. Qoidalar:

- `src/shared/api/schema.d.ts` git'ga commit qilinadi; CI'da generatsiya natijasi bilan farq
  qilsa build yiqiladi (kontrakt eskirganini erta ko'rish uchun — docs/10, 6-bo'lim).
- Qo'lda yozilgan biznes DTO tipiga ruxsat yo'q — faqat `schema.d.ts`dan generatsiya yoki
  `types.ts` orqali re-export (`ProblemDetails`/`PagedResult<T>` kabi sof transport
  tiplaridan tashqari — ular generatsiyaga bog'liq emas, `types.ts`da qo'lda qoladi).
- **Majburiy va ixtiyoriy maydonlar to'g'ri ajratilgan.** Backend'da
  `SupportNonNullableReferenceTypes()` va `RequiredNonNullablePropertiesSchemaFilter` yoqilgan,
  shuning uchun `schema.d.ts` da non-nullable C# xususiyatlari majburiy, `T?` bo'lganlari esa
  ixtiyoriy (`?` + `| null`) bo'lib chiqadi. Demak majburiy maydonlarda `?? ''`/`?? []`
  himoyasi **yozilmaydi** — u tip xavfsizligini yo'q qiladi va haqiqiy `null` xatosini yashiradi.
  Null-ishlash faqat chindan ixtiyoriy maydonlarda: `accessCode`, `classLetter`, `parentPhone`,
  `email`, `languageCode`, `currentTestCode`, `scaleLabels`, `options`, `currentValue`.

## Papka tuzilmasi (qisqacha — to'liq: `docs/10` §2)

```
src/
├── app/            # router.tsx, providers.tsx, ErrorBoundary
├── shared/
│   ├── api/        # client.ts, publicClient.ts, adminClient.ts, schema.d.ts (generatsiya)
│   ├── ui/         # Badge, Button, Card, Dialog, DataTable, Toast, ... (19 komponent)
│   │   └── brand/  # GirihStar, Logo, Ornament (Divider/Blob/ArchTop), PatternBackdrop — P45
│   ├── hooks/ lib/ config/
├── features/
│   ├── marketing/          # P45 — HomePage/MethodologyPage/AboutPage/ContactPage + sections/
│   ├── public-assessment/  # Landing → Registration → Test → Finish → Result
│   ├── auth/ dashboard/ schools/ students/ assessments/ programs/
│   ├── ai-settings/ catalog/ audit/ settings/
├── widgets/        # PersonalityRadar, AxisBar, RiasecChart, IndexGauge, AiReportView, ...
└── layouts/
    ├── MarketingLayout.tsx  # P45 — sticky header + katta footer, ommaviy tanishtiruv uchun
    ├── PublicLayout.tsx     # minimal, navigatsiyasiz — ommaviy TEST oqimi uchun
    └── AdminLayout.tsx      # sidebar + header
```

**Qoida:** `features/*` bir-birini import qilmaydi; umumiy narsa `shared/` yoki `widgets/`ga chiqadi.

## Route xaritasi (qisqacha — to'liq: `docs/10` §3)

| Guruh | Yo'llar | Layout |
|---|---|---|
| Marketing (P45) | `/`, `/metodika`, `/biz-haqimizda`, `/aloqa` | `MarketingLayout` |
| Ommaviy test oqimi | `/t/:slug`, `/t/:slug/register`, `/t/:slug/test/:testCode(/done)`, `/t/:slug/finish`, `/t/:slug/result` | `PublicLayout` |
| Admin | `/admin/login`, `/admin`, `/admin/schools(/:id)`, `/admin/students(/:id)`, `/admin/assessments(/:id)`, `/admin/catalog(/tests/:id)`, `/admin/programs(/:id)`, `/admin/ai`, `/admin/audit`, `/admin/settings` | `AdminLayout` (`ProtectedRoute`) |

Barcha havolalar `shared/config/routes.ts` (`ROUTES`/`ROUTE_PATTERNS`) orqali quriladi — hardcode
path yo'q. **Marketing va test oqimi ATAYLAB ajratilgan:** test faqat maktab bergan havola
(`/t/:slug`) orqali ochiladi, marketing sahifalarida "Testni boshlash" tugmasi yo'q.

## Dizayn tizimi (P45, 2026-09-05 — to'liq: `docs/10` §9)

`16shaxsiyat.uz` (statik marketing sayti) dizayni shu frontendga ko'chirilgan. Eski
`primary-*`/`success-*`/`warning-*`/`danger-*`/`neutral-*` tokenlari (admin panel, `shared/ui`)
**saqlangan** — yangi tokenlar `src/index.css`ga FAQAT qo'shildi, hech narsa o'chirilmadi.
Ikkala palitra hozircha yonma-yon mavjud.

- **Ranglar:** `paper`/`paper-deep`/`paper-card` (fon), `ink`/`ink-soft`/`ink-muted`/`ink-faint`
  (matn), `line`/`line-strong` (chegara), `firuza-50…900` (brend aksenti), + dekorativ
  `binafsha-*`/`zumrad-*`/`lojuvard-*`/`zarhal-*`/`terakota-*`.
- **Shrift:** `font-sans` = Inter Variable, `font-display` = Plus Jakarta Sans Variable
  (`@fontsource-variable/*` — `main.tsx`da import qilinadi).
- **Boshqa tokenlar:** `rounded-4xl`/`rounded-5xl`, `shadow-soft`/`shadow-lift`/`shadow-glow`,
  `max-w-content` (72rem)/`max-w-prose` (44rem), `ease-signature`, `animate-fade-up|fade-in|
  float|spin-slow|grow`, `bg-girih`/`bg-girih-light` (girih naqsh fonlari).
- **Komponent klasslari** (`@layer components`, faqat ommaviy sahifalarda): `.wrap`/
  `.wrap-narrow`, `.card`/`.card-hover`, `.btn` + o'lcham/ko'rinish variantlari, `.chip`,
  `.eyebrow`, `.lead`, `.prose-uz`.
- **Brend komponentlari** — `shared/ui/brand/`: `GirihStar`, `Logo`, `Divider`/`Blob`/`ArchTop`
  (`Ornament.tsx`), `PatternBackdrop`.
- `shared/ui`ning 19 komponenti va `AdminLayout` yangi palitraga o'tkazilgan (Props API
  o'zgarmagan); `features/**`da eski tokenlarning 69+ ishlatilishi hali qoladi — texnik qarz.

## Muhim arxitektura qoidalari (qisqacha — to'liq: `docs/10`)

- `features/*` bir-birini import qilmaydi; umumiy narsa `shared/` yoki `widgets/`ga chiqadi.
- Server holati — TanStack Query; klient holati — Zustand. Aralashmaydi.
- Admin access tokeni **faqat xotirada** (`shared/api/adminClient.ts` modul o'zgaruvchisi +
  `features/auth/store/authStore.ts`, persist YO'Q). `localStorage` faqat o'quvchi sessiya
  tokeni (`features/public-assessment/store/sessionStore.ts`) va javob navbati uchun.
- `dangerouslySetInnerHTML` taqiqlangan — ESLint xato darajasida majburlaydi.
- Barcha foydalanuvchi matni `react-i18next` kalitlari orqali (`src/locales/uz/common.json`
  asosiy; `ru/common.json` **bo'sh obyekt** — til almashtirgich UI'da yo'q, `fallbackLng: 'uz'`
  hammasini o'zbekchaga qaytaradi).

## Muhit o'zgaruvchilari

`.env.example`ga qarang: `VITE_API_BASE_URL`, `VITE_APP_NAME`, `VITE_SENTRY_DSN`.
