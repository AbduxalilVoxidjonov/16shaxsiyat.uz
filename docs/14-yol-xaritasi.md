# 14 — Yo'l xaritasi va bajarilish tartibi

## 1. Bosqichlar

| Bosqich | Nima chiqadi | Taxminiy | Promptlar |
|---------|--------------|----------|-----------|
| **B0 — Poydevor** | Solution qayta tuzilgan, DB ulangan, migratsiya, health | 2 kun | P01–P03 |
| **B1 — Katalog va scoring** | Test banki (190 savol), 4 scoring strategiyasi, oltin testlar | 4 kun | P04–P09 |
| **B2 — Ommaviy API** | Sessiya, savol, javob, yakunlash oqimi to'liq | 3 kun | P10–P12 |
| **B3 — Admin API** | Auth, maktablar, o'quvchilar, sessiyalar, dashboard | 3 kun | P13–P15 |
| **B4 — AI modul** | 3 provider, prompt, schema, navbat, fallback | 3 kun | P16–P18 |
| **B5 — Ommaviy UI** | O'quvchi oqimi to'liq, autosave, mobil | 4 kun | P19–P21 |
| **B6 — Admin UI** | Panel, jadvallar, **individual profil**, diagrammalar | 5 kun | P22–P26 |
| **B7 — Eksport va sozlamalar** | Excel, PDF, AI sozlamalari, audit, katalog UI | 3 kun | P27–P29 |
| **B7.5 — Anketa konstruktori** | Superadmin o'z testini kod yozmasdan yaratadi (`SUM` strategiyasi, Draft→Publish, preview) | 3 kun | P33 |
| **B8 — Sifat va chiqarish** | E2E, xavfsizlik, Docker, CI/CD, hujjat | 3 kun | P30–P32 |

**Jami:** ~33 ish kuni (bitta dasturchi + Claude Code bilan).

---

## 2. Kritik yo'l (bog'liqliklar)

```
P01 solution  ─▶ P02 domain ─▶ P03 EF/migratsiya ─▶ P04 katalog seed
                                                        │
                    ┌───────────────────────────────────┘
                    ▼
   P05–P08 scoring strategiyalari ─▶ P09 reliability
                    │
                    ▼
   P10 sessiya ─▶ P11 savol/javob ─▶ P12 yakunlash
                    │                      │
                    │                      ▼
                    │            P16 AI abstraksiya ─▶ P17 providerlar ─▶ P18 navbat
                    ▼
   P13 auth ─▶ P14 maktab/o'quvchi API ─▶ P15 dashboard
                                            │
                                            ▼
   P19 frontend skelet ─▶ P20 ommaviy UI ─▶ P21 autosave
                                            │
                                            ▼
   P22 admin skelet ─▶ P23 maktablar ─▶ P24 o'quvchilar ─▶ P25 profil ─▶ P26 diagrammalar
                                            │
                                            ▼
                        P27 eksport ─▶ P28 AI sozlamalari ─▶ P29 katalog/audit
                                            │
                                            ▼
                                 P33 anketa konstruktori
                                            │
                                            ▼
                                 P30 E2E ─▶ P31 xavfsizlik ─▶ P32 deploy
```

**Parallel ishlash mumkin:** P05–P08 (har strategiya mustaqil) · P13–P15 va P19–P21 ·
P27–P29 bir-biridan mustaqil.

---

## 3. Definition of Done (har prompt uchun)

Bir vazifa "tugadi" deyilishi uchun **hammasi** bajarilgan bo'lishi kerak:

- [ ] Kod yozildi va `dotnet build` / `npm run build` **ogohlantirishsiz** o'tadi
- [ ] Unit test yozildi va o'tadi (`dotnet test` / `npm run test`)
- [ ] Yangi endpoint bo'lsa — Swagger'da ko'rinadi va qo'lda sinaldi
- [ ] Migratsiya kerak bo'lsa — yaratildi va bo'sh DB'da xatosiz o'tadi
- [ ] Frontend bo'lsa — 390px va 1440px da tekshirildi
- [ ] `07-api-shartnoma.md` yoki tegishli hujjat yangilandi (o'zgargan bo'lsa)
- [ ] Sirlar kodda yo'q, log'da shaxsiy ma'lumot yo'q
- [ ] Commit xabari aniq: `feat(scoring): add Big Five strategy`

---

## 4. Sifat darvozalari (bosqich oxirida)

| Bosqich | Darvoza |
|---------|---------|
| B1 | 4 ta scoring oltin testi 100% yashil; qo'lda hisoblangan qiymatlarga mos |
| B2 | Integration test: to'liq ommaviy oqim boshidan oxirigacha o'tadi |
| B3 | Auth xavfsizlik testlari (401/403/IDOR/rate limit) yashil |
| B4 | AI moduli mock provider bilan to'liq ishlaydi; maxfiylik testi (prompt'da ism yo'q) yashil |
| B5 | Mobil qurilmada real o'quvchi bilan sinov (1 kishi) — 30 daqiqada tugatadi |
| B6 | Superadmin real ma'lumotda profilni ochadi, hamma diagramma to'g'ri |
| B7 | Excel/PDF fayllar to'g'ri ochiladi, ma'lumot to'liq |
| B7.5 | Superadmin nol koddan yangi anketa yaratib, nashr qilib, o'quvchi uni yechib, natija profilda ko'rinadi |
| B8 | 10 E2E stsenariy yashil; xavfsizlik ro'yxati (08-hujjat, 9-bo'lim) to'liq belgilangan |

---

## 5. Pilot rejasi (kod tayyor bo'lgach)

1. **1-hafta:** bitta maktab, bitta 9-sinf (~30 o'quvchi). Sinfxonada, o'qituvchi nazoratida.
   Kuzatiladi: tugatish vaqti, drop-off, tushunarsiz savollar, texnik nosozlik.
2. **2-hafta:** savol matnlarini tuzatish (o'quvchilar tushunmagan iboralar), AI promptini
   real natijalar asosida sozlash.
3. **3–4-hafta:** 3 maktab, ~300 o'quvchi. Yuklama va ishonchlilik tekshiruvi.
4. **Kengaytirish:** tuman miqyosida.

**Pilot uchun o'lchanadigan ko'rsatkichlar:** tugatish darajasi, o'rtacha vaqt, o'rtacha
`ReliabilityScore`, AI muvaffaqiyat foizi, superadmin hisobotdan qoniqishi (1–5).

---

## 6. v2 backlog (MVP'dan keyin)

| Prioritet | Xususiyat |
|-----------|-----------|
| Yuqori | Maktab admini / psixolog roli va kabinetlari |
| Yuqori | Sinf va maktab kesimidagi yig'ma hisobotlar |
| Yuqori | Takroriy test (yildan yilga dinamika grafiklari) |
| O'rta | Ota-ona uchun qisqa hisobot havolasi (bir martalik token) |
| O'rta | Telegram bot: natija tayyor bo'lganda xabar |
| O'rta | Flutter mobil ilova (API tayyor) |
| O'rta | Qo'shimcha metodikalar: emotsional intellekt, o'quv uslubi (VARK) |
| Past | Adaptiv test (IRT) — savol sonini 190 dan 100 ga tushirish |
| Past | Multi-tenant va tarif tizimi |
| Past | Rus tilidagi to'liq interfeys |

---

## 7. Ochiq savollar (loyiha egasi hal qiladi)

| № | Savol | Nega muhim |
|---|-------|------------|
| 1 | O'quvchiga natija ko'rsatiladimi yoki faqat maktabgami? | `showResultToStudent` sozlamasi va E-6 ekrani |
| 2 | Maktab bilan qanday shartnoma/rozilik formati bo'ladi? | `consentText` matni va huquqiy asos |
| 3 | ~~Domen nomi va brend nomi?~~ **Hal qilindi:** brend **Shaxsiyat**, domen `16shaxsiyat.uz` (olingan), API `api.16shaxsiyat.uz` | — |
| 4 | Boshlang'ich sinflar (1–4) ham qamraladimi? | Savol tili va metodika mosligi — hozircha 5–11 sinf mo'ljallangan |
| 5 | Testni qayta topshirishga necha oydan keyin ruxsat? | BR-1 dagi 90 kun taxminiy |
| 6 | Qaysi AI provider birinchi ishlatiladi (byudjet)? | Default provider va model tanlovi |
