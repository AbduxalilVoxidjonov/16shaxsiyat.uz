# PROGRESS — loyiha holati jurnali

> Bu fayl **PM agentning xotirasi**. Sessiya uzilsa ham ish shu yerdan davom etadi.
> PM har vazifa boshlanganda va tugaganda **darhol** yangilaydi.

**Loyiha:** Salohiyat (`salohiyat.uz`) · ichki nom `StudentRoadMap`
**Oxirgi yangilanish:** 2026-08-31 · **Joriy bosqich:** B0 (Poydevor) · **Joriy vazifa:** P02 (Domain qatlami)

---

## Holat belgilari

`⬜ Kutmoqda` · `🟡 Ish jarayonida` · `🔵 Review'da (PR ochilgan)` · `✅ Merge qilingan` · `⛔ Bloklangan`

---

## Vazifalar jadvali

| № | Vazifa | Agent | Holat | PR | Izoh |
|---|--------|-------|-------|----|------|
| P01 | Solution'ni Clean Architecture ga o'tkazish | backend-dotnet | 🔵 | branch `feat/P01-clean-architecture` | QA: PASS · build 0 ogohlantirish · 4 test yashil |
| P02 | Domain qatlami | backend-dotnet | 🟡 | — | branch `feat/P02-domain-qatlami` |
| P03 | EF Core, DbContext, migratsiya | backend-dotnet | 🟡 | — | Docker yo'q — jonli DB tekshiruvlari P04 ga qoldi |
| P04 | Katalog va seed infratuzilmasi | backend-dotnet | ⬜ | — | |
| P05 | 16 tip savol banki (60) | scoring-psychometrics | 🔵 | — | JSON tayyor; seed integratsiyasi P04 da |
| P06 | Big Five savol banki (50) | scoring-psychometrics | 🔵 | — | JSON tayyor; naqsh 2 marta qaytarildi |
| P07 | RIASEC savol banki (48) | scoring-psychometrics | 🔵 | — | JSON tayyor |
| P08 | Aktivlik anketasi (32) | scoring-psychometrics | 🔵 | — | JSON tayyor; shkalalar aralashtirildi |
| P09 | Scoring engine + oltin testlar | scoring-psychometrics | ⬜ | — | **Kritik** — 100% qoplama |
| P10 | Application skeleti + sessiya API | backend-dotnet | ⬜ | — | |
| P11 | Savol va javob API | backend-dotnet | ⬜ | — | |
| P12 | Yakunlash va scoring ulash | backend-dotnet | ⬜ | — | |
| P13 | Auth va JWT | backend-dotnet | ⬜ | — | P19 bilan parallel |
| P14 | Maktab va o'quvchi admin API | backend-dotnet | ⬜ | — | |
| P15 | Sessiya va dashboard API | backend-dotnet | ⬜ | — | |
| P16 | AI abstraksiya va prompt | ai-integration | ⬜ | — | |
| P17 | 3 provider (Gemini/OpenAI/Anthropic) | ai-integration | ⬜ | — | Real kalit kerak (§8-1) |
| P18 | Fon navbati, fallback | ai-integration | ⬜ | — | |
| P19 | Frontend skeleti | frontend-react | ⬜ | — | |
| P20 | Ommaviy UI: landing va anketa | frontend-react | ⬜ | — | |
| P21 | Ommaviy UI: test oqimi, autosave | frontend-react | ⬜ | — | |
| P22 | Admin skelet va login | frontend-react | ⬜ | — | |
| P23 | Admin: maktablar va havolalar | frontend-react | ⬜ | — | |
| P24 | Admin: o'quvchilar ro'yxati | frontend-react | ⬜ | — | |
| P25 | Individual profil sahifasi | frontend-react | ⬜ | — | **Asosiy ekran** |
| P26 | Diagramma widgetlari | frontend-react | ⬜ | — | |
| P27 | Excel va PDF eksport | backend-dotnet | ⬜ | — | P28, P29 bilan parallel |
| P28 | AI sozlamalari UI | frontend-react | ⬜ | — | |
| P29 | Katalog va audit UI | frontend-react | ⬜ | — | |
| P30 | E2E testlar | qa-reviewer | ⬜ | — | |
| P31 | Xavfsizlik va mustahkamlash | qa-reviewer | ⬜ | — | |
| P32 | Docker, CI/CD, yakuniy hujjat | backend-dotnet | ⬜ | — | |
| P33 | Anketa konstruktori | backend + frontend | ⬜ | — | P29 dan keyin |

---

## Qabul qilingan qarorlar (sessiyalar davomida)

| Sana | Qaror | Sabab | Qayerga yozildi |
|------|-------|-------|-----------------|
| 2026-08-31 | Brend "Salohiyat", domen `salohiyat.uz`, API `api.salohiyat.uz` | Domen olindi | `docs/01` 2a-bo'lim |
| 2026-08-31 | Superadmin uchun anketa konstruktori (`SUM` strategiyasi) qo'shildi | Superadmin o'z testini kirita olishi kerak | `docs/06` ADR-13..15 |
| 2026-08-31 | Lokal git repozitoriysi ochildi (`main` + `feat/*`) | PM.md 6-bo'limidagi git intizomi shuni talab qiladi | `docs/06` 8-bo'lim |
| 2026-08-31 | EF Core paketlari `9.x` da qulflandi (net10.0 da) | Npgsql'ning EF Core 10 provayderi hali yo'q | `docs/06` 8-bo'lim |
| 2026-08-31 | **Push qilinmaydi** — faqat lokal commit va branch | Loyiha egasining ko'rsatmasi; repo `AbduxalilVoxidjonov/salohiyat` tayyor, keyinroq push qilinadi | shu jurnal |

---

## Insondan kutilayotgan javoblar

| № | Savol | Kerak bo'ladigan vazifa | Holat |
|---|-------|-------------------------|-------|
| 1 | O'quvchiga natija ko'rsatiladimi (qisqa versiya)? | P12, P21 | ⏳ |
| 2 | Maktab bilan rozilik matni (`consentText`) | P10, P20 | ⏳ |
| 3 | Qaysi sinflar qamraladi (hozir 5–11 mo'ljallangan)? | P05–P08 savol tili | ⏳ |
| 4 | Qayta topshirishga necha oydan keyin ruxsat (hozir 90 kun)? | P10 (BR-1) | ⏳ |
| 5 | Birinchi AI provider va byudjet | P17 | ⏳ |
| 6 | `.env` sirlari: DB paroli, `Jwt:Key`, `EncryptionKey` | P03 | ⏳ |

> PM: bu javoblarsiz ham ishni boshlash mumkin — hujjatlardagi default qiymatlar bilan ket,
> javob kelganda o'zgartir. Faqat 6-band (`P03` uchun lokal `.env`) haqiqatan to'sqinlik qiladi.

---

## Kundalik jurnal

### 2026-08-31
- Hujjatlar (`docs/00–15`) va promptlar (`prompts/00–33`) tayyorlandi.
- Brend va domen qat'iylashtirildi: **Salohiyat**, `salohiyat.uz`.
- Anketa konstruktori qamrovga qo'shildi (P33).
- PM agent (`PM.md`), 5 mutaxassis agent (`.claude/agents/`) va shu jurnal yaratildi.
- **P01 bajarildi** — MVC shabloni `src/StudentRoadMap.Api/` ga aylantirildi; 4 `src/` + 3 `tests/`
  loyihasi, `Directory.Build.props` (`TreatWarningsAsErrors=true`), `.editorconfig`, `README.md`.
  `Program.cs`: Serilog, Swagger (faqat Dev), CORS `App:FrontendUrl` dan, `/health`, `/health/ready`,
  ProblemDetails. Razor/MVC qoldiqlari yo'q, Domain paketsiz, sirlar yo'q.
  Tekshiruv (PM o'zi): `dotnet build` 0 xato 0 ogohlantirish · `dotnet test` 4/4 yashil ·
  `/health` 200 `Healthy` · `/health/ready` 200 · `/swagger` 200. **QA verdikti: PASS.**
- **P02 boshlandi** (`feat/P02-domain-qatlami`).
- **Ish tartibi o'zgardi (egasining ko'rsatmasi: "tezroq agentlar asosida ishla"):**
  P05–P08 savol banklari (190 savol) endi P04 ni kutmasdan, P02/P03 bilan **parallel** yozilmoqda.
  Ular faqat `SeedData/test-definitions/*.json` kontentini yozadi — seeder integratsiyasi
  va "DB'da N qator" DoD bandi P04 da tekshiriladi. JSON sxemasi P04 promptidan olindi.
- **P05–P08 savol banklari tayyor:** MBTI16 60, BIG5 50, RIASEC 48, ACTIVITY 32 = **190 savol**.
  Ikkita sifat nuqsoni topilib qaytarildi: BIG5 da teskari savollar birinchi/ikkinchi yarimga
  bo'linib qolgan edi, ACTIVITY da har shkala bitta blokda turardi — ikkalasi ham javob to'plami
  effektiga olib keladi. Endi hamma bankda `direction` ketma-ketligi ≤ 2, `scale` ketma-ketligi = 1.
  BIG5 tartibi `random.seed(20260831)` bilan deterministik qayta hisoblangan.
- **Muhit muammosi:** bu mashinada Docker ishlamayapti, PostgreSQL yo'q. P03 migratsiyani
  yozadi, lekin `docker compose up -d db` / `dotnet ef database update` / `psql` tekshiruvlari
  bajarilmaydi. Egasiga aytildi; bandlar P04 da qaytib tekshiriladi.
- **Keyingi:** P02 testlari + P03 tugashi → QA → commit.

---

## Ma'lum risklar va texnik qarzlar

| Risk / qarz | Ta'sir | Reja |
|-------------|--------|------|
| Savol banklari (190 savol) sifati — real o'quvchida sinalmagan | Natija ishonchliligi | Pilotdan keyin matnlarni tuzatish (`docs/14` 5-bo'lim) |
| AI prompt sifati faqat mock bilan sinaladi | Hisobot sifati | P17 dan keyin 10 ta oltin namuna bilan qo'lda baholash |
| `answers` jadvali tez o'sadi (190 qator/sessiya) | Ishlash | 100k sessiyadan keyin partitsiya (v2) |
| EF Core 9.x paketlari net10.0 loyihada (Npgsql EF10 provayderi yo'q) | P03 da runtime muammosi bo'lishi mumkin | P03 boshida DbContext bilan haqiqiy so'rov sinab ko'riladi; provayder chiqqach yangilanadi |
| `/health` va `/health/ready` hozir bir xil (bog'liqlik tekshiruvi yo'q) | Orkestrator noto'g'ri "ready" deb o'ylashi mumkin | P03 da DB health check qo'shilib, `tag` bo'yicha ajratiladi |
| Push qilinmaydi (egasining qarori) — PR oqimi ishlamaydi | Ko'rikni faqat `qa-reviewer` beradi, tashqi review yo'q | Repo: `AbduxalilVoxidjonov/salohiyat`. Egasi aytganda `origin` qayta qo'shilib, barcha branch birdan push qilinadi |
