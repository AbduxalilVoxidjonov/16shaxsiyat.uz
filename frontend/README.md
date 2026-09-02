# Shaxsiyat — frontend

`StudentRoadMap` loyihasining React 19 + TypeScript + Vite frontend'i.
Foydalanuvchiga ko'rinadigan barcha matnda **Shaxsiyat** nomi ishlatiladi
(`docs/00`–`docs/15`, `CLAUDE.md`ga qarang).

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

## Muhim arxitektura qoidalari (qisqacha — to'liq: `docs/10`)

- `features/*` bir-birini import qilmaydi; umumiy narsa `shared/` yoki `widgets/`ga chiqadi.
- Server holati — TanStack Query; klient holati — Zustand. Aralashmaydi.
- Admin access tokeni **faqat xotirada** (`shared/api/adminClient.ts` modul o'zgaruvchisi +
  `features/auth/store/authStore.ts`, persist YO'Q). `localStorage` faqat o'quvchi sessiya
  tokeni (`features/public-assessment/store/sessionStore.ts`) va javob navbati uchun.
- `dangerouslySetInnerHTML` taqiqlangan — ESLint xato darajasida majburlaydi.
- Barcha foydalanuvchi matni `react-i18next` kalitlari orqali (`src/locales/uz/common.json`
  asosiy; `ru/common.json` hozircha bo'sh skelet).

## Muhit o'zgaruvchilari

`.env.example`ga qarang: `VITE_API_BASE_URL`, `VITE_APP_NAME`, `VITE_SENTRY_DSN`.
