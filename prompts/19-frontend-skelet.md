# P19 — Frontend skeleti

## Kontekst
Backend to'liq ishlaydi. Endi React SPA. Bu promptda faqat poydevor — hech qanday ekran to'liq emas.

## O'qish shart
- `docs/10-frontend-arxitektura.md` (**to'liq**)

## Vazifa
1. `frontend/` da Vite + React 19 + TypeScript (strict) loyihasi.
2. Paketlar: `react-router`, `@tanstack/react-query`, `zustand`,
   `react-hook-form`, `zod`, `tailwindcss`, `recharts`, `i18next` + `react-i18next`,
   `lucide-react`; dev: `vitest`, `@testing-library/react`, `eslint`, `prettier`, `playwright`.
3. `tsconfig`: `strict`, `noUncheckedIndexedAccess`, `@/*` path alias.
4. Tailwind sozlamasi: semantik rang tokenlari (`docs/11` 1-bo'limi), `Inter` shrifti,
   mobile-first breakpointlar.
5. `shared/ui/` bazaviy komponentlar: `Button`, `Input`, `Select`, `Card`, `Badge`, `Dialog`,
   `Skeleton`, `Table`, `Toast`, `Spinner`, `EmptyState`, `ErrorState`.
6. `shared/api/client.ts` — `fetch` o'rami: `baseUrl`, JSON, `ProblemDetails` → `AppError`
   (`code`, `message`, `errors`), 204 ishlash.
   `publicClient.ts` (`X-Session-Token`), `adminClient.ts` (Bearer + 401 da bitta refresh, mutex).
7. `app/router.tsx` — barcha route'lar (`docs/10` 3-bo'limi), `lazy()` bilan; hozircha
   sahifalar joy egallovchi (placeholder).
8. `app/providers.tsx` — QueryClient (`staleTime` siyosati), i18n, Toast, `ErrorBoundary`.
9. i18n: `locales/uz/common.json` — bazaviy kalitlar; `ru` bo'sh skelet.
10. `npm run generate:api` — `openapi-typescript` bilan backend swagger'dan tiplar.
11. ESLint qoidalari: `jsx-a11y`, `react-hooks`, `dangerouslySetInnerHTML` — xato.

## Cheklovlar
- Biror ekranning to'liq mantiqini yozma — faqat skelet va infratuzilma.
- Hardcode matn yo'q — barcha matn i18n kalitlari orqali.
- `localStorage` faqat sessiya tokeni va javob navbati uchun (auth token uchun **emas**).

## DoD
- [ ] `npm run dev` ishlaydi, barcha route'lar ochiladi (placeholder bilan)
- [ ] `npm run build`, `npm run lint`, `npm run typecheck` toza
- [ ] `npm run generate:api` tiplarni yaratadi
- [ ] Bitta smoke test (`Vitest`) o'tadi

## Tekshiruv
```bash
cd frontend && npm ci && npm run typecheck && npm run lint && npm run build && npm run test
```
