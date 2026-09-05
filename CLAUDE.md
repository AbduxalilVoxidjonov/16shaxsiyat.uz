# StudentRoadMap — loyiha konteksti (Claude Code uchun)

## Loyiha nima

O'quvchining shaxsiyati, psixologik yetukligi, qiziqishlari va aktivligini onlayn testlar orqali
aniqlaydigan, natijalarni AI bilan tahlil qiladigan CRM platforma. Maktablarga shaxsiy havola
beriladi; o'quvchi anketa to'ldirib 4 blok test yechadi; **bitta superadmin** barcha natijalarni
va har o'quvchining individual profilini boshqaradi.

## Brend

Ommaviy nom — **Shaxsiyat** (`16shaxsiyat.uz`), API `api.16shaxsiyat.uz`.
`StudentRoadMap` — ichki kod nomi (repo, solution, konteynerlar). Foydalanuvchiga ko'rinadigan
barcha matnda "Shaxsiyat" ishlatiladi.

## Stek

- **Backend:** ASP.NET Core (net10.0), Clean Architecture + CQRS (MediatR), EF Core + Npgsql
- **DB:** PostgreSQL 16 (`jsonb` ballar va AI javoblari uchun)
- **Frontend:** React 19 + TypeScript (strict) + Vite + Tailwind + TanStack Query
- **AI:** provider-agnostik — Gemini / OpenAI / Anthropic (superadmin kalit kiritganlari ishlaydi)
- **Fon ishlari:** navbat + retry + fallback zanjiri
- **Deploy:** Docker compose, GitHub Actions

## Hujjatlar — haqiqat manbai

Har ishdan oldin tegishli hujjatni o'qi. Hujjatda yo'q narsani **taxmin qilma** — so'ra.

| Mavzu | Fayl |
|-------|------|
| Indeks va o'qish tartibi | `docs/00-README.md` |
| Maqsad, rollar, MVP chegarasi | `docs/01-vision-va-qamrov.md` |
| Talablar, biznes qoidalari (BR) | `docs/02-biznes-talablar.md` |
| **Scoring formulalari** | `docs/03-psixologik-metodikalar.md` |
| Entity'lar, holat mashinasi | `docs/04-domain-model.md` |
| DB sxemasi, indekslar, enum raqamlari | `docs/05-database-schema.md` |
| Arxitektura, qatlamlar, ADR, xato kodlari | `docs/06-arxitektura.md` |
| **API shartnomasi** | `docs/07-api-shartnoma.md` |
| Auth, xavfsizlik, maxfiylik | `docs/08-auth-va-xavfsizlik.md` |
| AI modul, promptlar, JSON schema | `docs/09-ai-analiz-moduli.md` |
| Frontend arxitekturasi | `docs/10-frontend-arxitektura.md` |
| Ekranlar va UX | `docs/11-ux-va-ekranlar.md` |
| Testlash strategiyasi | `docs/12-testlash-strategiyasi.md` |
| Deploy va infratuzilma | `docs/13-deploy-va-infratuzilma.md` |
| Yo'l xaritasi, DoD | `docs/14-yol-xaritasi.md` |
| Atamalar | `docs/15-glossariy.md` |

Ishlab chiqish **`prompts/` papkasidagi tartibda** olib boriladi — `prompts/00-qollanma.md` dan boshla.

## Qat'iy qoidalar

1. **Til:** kod inglizcha; izohlar va foydalanuvchiga ko'rinadigan matn o'zbekcha (lotin).
2. **Domain toza:** `Domain` va `Application` da EF, HTTP, `DateTime.Now` yo'q. Vaqt parametr bilan.
3. **Scoring deterministik:** formulalar `docs/03` da; o'zgarsa avval oltin test yangilanadi.
4. **Sirlar kodda yo'q** — env yoki user-secrets. `appsettings.json` da ham yo'q.
5. **AI'ga shaxsiy ma'lumot yuborilmaydi:** ism, telefon, email, aniq tug'ilgan sana, maktab nomi.
   Faqat yosh, sinf, jins va ballar.
6. **AI tashxis qo'ymaydi** — taqiqlangan atamalar ro'yxati promptda va post-filtrda.
6a. **Raqobatchi kontenti ko'chirilmaydi:** 16Personalities/NERIS va MBTI savollari, tip nomlari,
    tavsiflari va tovar belgilari ishlatilmaydi (`docs/17` 8-bo'lim).
7. **Migratsiya faqat EF Core orqali**; destruktiv o'zgarish ikki bosqichda.
8. **Ommaviy API'da ID qabul qilinmaydi** — faqat `X-Session-Token` (IDOR himoyasi).
9. **`scale` va `scaleDirection` o'quvchi API'siga hech qachon yuborilmaydi** (admin katalogida ko'rinadi).
9a. **Tizim metodikalari qulflangan:** `IsSystem = true` testda savol qo'shilmaydi/o'chirilmaydi,
    `Scale`/`Direction`/`Weight` o'zgarmaydi. Superadmin `Custom` anketalarida hammasi ochiq.
10. **Test yozilmagan biznes mantiq tugallanmagan hisoblanadi.**
11. Barcha xatolar `ProblemDetails` formatida, `code` maydoni bilan (`docs/06` 6-bo'lim).
12. `dangerouslySetInnerHTML` frontendda taqiqlangan.

## Foydali buyruqlar

```bash
# Backend
dotnet build && dotnet test
dotnet run --project src/StudentRoadMap.Api
dotnet run --project src/StudentRoadMap.Api -- --migrate
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet ef migrations add <Nom> -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api

# Frontend
cd frontend && npm run dev
npm run typecheck && npm run lint && npm run test && npm run build
npm run generate:api          # swagger'dan TS tiplari

# Infra
docker compose up -d db
docker compose up -d
```

## Avtonom ishlash rejimi

Loyihani **PM agent** olib boradi. Boshlash uchun bitta jumla yetarli:

> `PM.md` ni o'qi va navbatdagi ishni davom ettir.

- `PM.md` — PM agentning to'liq ko'rsatmasi (ish sikli, topshirish, git intizomi, to'xtash shartlari)
- `PROGRESS.md` — holat jurnali va **PM ning xotirasi**; har vazifadan keyin yangilanadi
- `.claude/agents/` — 5 mutaxassis agent: `backend-dotnet`, `frontend-react`,
  `scoring-psychometrics`, `ai-integration`, `qa-reviewer`

Git: har vazifa alohida `feat/PNN-*` branch va PR; `main` ga to'g'ridan-to'g'ri yozilmaydi.

## Joriy holat

`prompts/` bo'yicha bosqichma-bosqich quriladi. Bajarilgan promptlarni shu yerda belgilab bor:

- [ ] P01 solution · [ ] P02 domain · [ ] P03 persistence · [ ] P04 seed infra
- [ ] P05–P08 savol banklari · [ ] P09 scoring
- [ ] P10–P12 ommaviy API · [ ] P13–P15 admin API · [ ] P16–P18 AI modul
- [ ] P19–P21 ommaviy UI · [ ] P22–P26 admin UI · [ ] P27–P29 eksport/sozlamalar
- [ ] P30 E2E · [ ] P31 xavfsizlik · [ ] P32 deploy · [ ] P33 anketa konstruktori
- [x] P45 ommaviy dizayn (16shaxsiyat.uz vizual tizimi frontendga ko'chirildi — yangi marketing
  qatlami `/`, `/metodika`, `/biz-haqimizda`, `/aloqa`; `docs/10` §9, `PROGRESS.md`)
