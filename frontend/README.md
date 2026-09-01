# Salohiyat — frontend

`StudentRoadMap` loyihasining React 19 + TypeScript + Vite frontend'i.
Foydalanuvchiga ko'rinadigan barcha matnda **Salohiyat** nomi ishlatiladi
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
openapi-typescript http://localhost:5000/swagger/v1/swagger.json -o src/shared/api/schema.d.ts
```

**P19 (frontend skelet) bosqichida bu skript ishga tushirilmagan.** Backend ommaviy
API endpointlari (`prompts/10`–`12`) shu paytda parallel yozilmoqda va swagger sxemasi
deyarli bo'sh/beqaror. Shuning uchun:

- `src/shared/api/schema.d.ts` hali mavjud emas.
- `src/shared/api/types.ts` faqat transport qatlami uchun umumiy tiplarni saqlaydi
  (`ProblemDetails`, `PagedResult<T>`) — bular generatsiyaga bog'liq emas.
- Featurelar hali biznes DTO tiplarini import qilmaydi (chunki featurelar hali skelet).

**P20 da** (backend tayyor bo'lgach) quyidagilar bajariladi:

1. Backend ishga tushiriladi (`dotnet run --project src/StudentRoadMap.Api`).
2. `npm run generate:api` ishga tushiriladi — `schema.d.ts` yaratiladi/yangilanadi.
3. `schema.d.ts` git'ga commit qilinadi; CI'da generatsiya natijasi bilan farq
   qilsa build yiqiladi (kontrakt eskirganini erta ko'rish uchun — docs/10, 6-bo'lim).
4. Qo'lda yozilgan DTO tipiga ruxsat yo'q — faqat `schema.d.ts`dan generatsiya yoki
   `types.ts` orqali re-export.

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
