# 02 — Biznes talablar

Belgilar: **[M]** — Must (MVP majburiy), **[S]** — Should (MVP'da bo'lsa yaxshi), **[C]** — Could (v2).

---

## 1. Foydalanuvchi hikoyalari

### 1.1 Superadmin

| ID | Hikoya | Prioritet |
|----|--------|-----------|
| SA-01 | Superadmin sifatida login/parol bilan kiraman, sessiyam JWT bilan himoyalangan | M |
| SA-02 | Yangi maktab qo'shaman: nomi, viloyat, tuman, raqami, mas'ul shaxs, telefon | M |
| SA-03 | Maktab uchun avtomatik unikal havola generatsiya qilinadi va uni nusxalab olaman | M |
| SA-04 | Havola tarqalib ketsa, uni bir tugma bilan **qayta generatsiya** qilaman (eskisi o'ladi) | M |
| SA-05 | Maktabni vaqtincha o'chiraman (`IsActive=false`) — havola ishlamay qoladi | M |
| SA-06 | Dashboardda umumiy statistikani ko'raman: maktablar, o'quvchilar, yakunlangan sessiyalar, tip taqsimoti | M |
| SA-07 | O'quvchilar ro'yxatini ko'raman, maktab/sinf/holat/sana bo'yicha filtrlayman va qidiraman | M |
| SA-08 | Bitta o'quvchining **individual profilini** ochaman: anketa, 4 test ballari, diagrammalar, AI hisobot | M |
| SA-09 | AI tahlilni **qayta ishga tushiraman** (boshqa provider yoki yangilangan prompt bilan) | M |
| SA-10 | O'quvchilar ro'yxatini Excel'ga eksport qilaman | M |
| SA-11 | Bitta o'quvchi hisobotini PDF qilib yuklab olaman (maktabga berish uchun) | S |
| SA-12 | Test savollari bankini ko'raman va tahrirlayman (matn, tartib, faol/nofaol) | S |
| SA-13 | AI provider sozlamalarini boshqaraman: qaysi provayder, model, API kalit, default | M |
| SA-14 | Audit logni ko'raman: kim, qachon, nima qildi | S |
| SA-15 | O'quvchi ma'lumotini butunlay o'chiraman (so'rov bo'yicha) | S |
| SA-16 | Ikki o'quvchini yoki bir sinfni taqqoslab ko'raman | C |
| SA-17 | Parolimni o'zgartiraman va 2FA yoqaman | S |
| SA-18 | Mavjud testga yangi savol qo'shaman, keraksizini o'chiraman, tartibini o'zgartiraman | M |
| SA-19 | **O'zimning anketamni yarataman**: nomi, shkalalari, savollari — kod yozmasdan | M |
| SA-20 | Anketani nashr qilishdan oldin o'quvchi ko'zi bilan ko'rib chiqaman (preview) | M |
| SA-21 | Testni nusxalab (duplicate) yangi versiyasini tayyorlayman | S |
| SA-22 | Qaysi test yangi sessiyalarga kiritilishini yoqib/o'chirib qo'yaman | M |

### 1.2 O'quvchi (mehmon)

| ID | Hikoya | Prioritet |
|----|--------|-----------|
| ST-01 | Maktab havolasini ochganimda maktab nomi va nima qilishim kerakligi tushuntiriladi | M |
| ST-02 | Anketani to'ldiraman: FISH, tug'ilgan sana, jinsi, sinf, telefon, (ixtiyoriy) email | M |
| ST-03 | Ma'lumotlarim ishlatilishiga rozilik beraman (checkbox) | M |
| ST-04 | Testlar necha ta, qancha vaqt olishini oldindan ko'raman | M |
| ST-05 | Savollarni birma-bir yoki sahifa-sahifa yechaman, progress bar ko'rinadi | M |
| ST-06 | Javoblarim avtomatik saqlanadi — brauzer yopilsa ham o'sha joydan davom etaman | M |
| ST-07 | Oldingi savolga qaytib javobimni o'zgartira olaman | S |
| ST-08 | Testni yakunlaganimda rahmat ekrani va (ruxsat berilgan bo'lsa) qisqa natija ko'rinadi | M |
| ST-09 | Telefonda ham qulay ishlaydi | M |
| ST-10 | Internet uzilsa, javoblarim yo'qolmaydi (lokal bufer + qayta yuborish) | S |

---

## 2. Funksional talablar

### FR-1 Maktab va havola boshqaruvi
- FR-1.1 [M] Maktab yozuvi: nomi, viloyat, tuman, maktab raqami, mas'ul FISH, telefon, izoh.
- FR-1.2 [M] Har maktabga `Slug` (o'qiladigan, unikal) + `AccessToken` (32 belgi, kriptografik random).
- FR-1.3 [M] Ommaviy havola shakli: `{FRONTEND_URL}/t/{slug}?k={token}`.
- FR-1.4 [M] Havolani qayta generatsiya — eski token darhol bekor bo'ladi; tugallanmagan sessiyalar
  o'z tokeni bilan davom etaveradi (sessiya tokeni alohida).
- FR-1.5 [S] ~~Ixtiyoriy 6 xonali `AccessCode`~~ — **admin UI'dan olib tashlangan (2026-09-07)**:
  backend'da ixtiyoriy/eskirgan (`null`; qiymatli eski maktabda tekshiruv ishlashda davom etadi,
  detalda "Eski sinf kodi" sifatida faqat ko'rinadi). O'rnini avtomatik `EntryCode` ("Maktab kodi", `docs/08` §3a) egalladi.
- FR-1.6 [M] Maktab bo'yicha kunlik ro'yxatdan o'tish limiti (default 500).

### FR-2 O'quvchi va sessiya
- FR-2.1 [M] Anketa maydonlari: FISH*, tug'ilgan sana*, jinsi*, sinf (1–11)*, sinf harfi, telefon*,
  ota-ona telefoni, email, rozilik checkbox*.
- FR-2.2 [M] Dublikat aniqlash: bir maktabda bir xil (FISH normalized + tug'ilgan sana) bo'lsa —
  mavjud o'quvchiga bog'lanadi, yangi sessiya ochiladi yoki tugallanmagan sessiya davom ettiriladi.
- FR-2.3 [M] Sessiya yaratilganda `SessionToken` qaytadi; frontend uni `localStorage` da saqlaydi.
- FR-2.4 [M] Sessiya holatlari: `Draft → InProgress → Completed → Analyzing → Analyzed | Failed`.
- FR-2.5 [M] Sessiya `ExpiresAt` (default 7 kun) — muddati o'tsa `Expired`.

### FR-3 Test yechish
- FR-3.1 [M] Testlar `DisplayOrder` bo'yicha ketma-ket beriladi; oldingisi tugamasdan keyingisi ochilmaydi.
- FR-3.2 [M] Savollar sahifa-sahifa (default 10 ta/sahifa), aralashtirish opsiyasi test darajasida.
- FR-3.3 [M] Javob saqlash **idempotent**: bir savolga qayta javob yuborilsa yangilanadi.
- FR-3.4 [M] Har javobda `DurationMs` yoziladi (ishonchlilik indeksi uchun).
- FR-3.5 [M] Testni yakunlash uchun barcha majburiy savollarga javob bo'lishi shart.
- FR-3.6 [M] Test yakunlanganda scoring darhol (sinxron) hisoblanadi va `TestResult` yoziladi.
- FR-3.7 [M] Barcha testlar tugagach sessiya `Completed`, AI tahlil **fon navbatiga** qo'yiladi.

### FR-4 Scoring
- FR-4.1 [M] Har metodika uchun alohida `IScoringStrategy` implementatsiyasi.
- FR-4.2 [M] Natija: `RawScores` (o'q/omil bo'yicha xom ball), `NormalizedScores` (0–100),
  `ResultCode` (masalan `INTJ`, `RIA`), `Interpretation` (qisqa matn).
- FR-4.3 [M] Teng ball holatida (tie-break) qat'iy qoida — `03-psixologik-metodikalar.md` da.
- FR-4.4 [M] **Ishonchlilik indeksi**: juda tez javoblar ulushi, bir xil variantni ketma-ket tanlash
  (straight-lining), teskari savollardagi ziddiyat → `ReliabilityScore` 0–100.

### FR-5 AI tahlil
- FR-5.1 [M] Kirish: barcha `TestResult` + anketa (yosh, sinf, jins) + ishonchlilik indeksi.
- FR-5.2 [M] Chiqish **qat'iy JSON schema**: umumiy xulosa, shaxsiyat portreti, kuchli tomonlar,
  o'sish zonalari, o'quv uslubi, motivatsiya profili, aktivlik darajasi, mos kasb yo'nalishlari (3–5),
  o'quvchiga tavsiyalar, o'qituvchi/ota-onaga tavsiyalar, diqqat talab bo'limi.
- FR-5.3 [M] Provider tanlash: superadmin sozlamasidagi default; so'rovda override qilinishi mumkin.
- FR-5.4 [M] Xato bo'lsa 3 marta retry (eksponensial), keyin fallback provider, keyin `Failed`.
- FR-5.5 [M] Har chaqiruvda `PromptVersion`, `Model`, token va taxminiy narx saqlanadi.
- FR-5.6 [M] Qayta ishga tushirish yangi `AiAnalysis` yozuvi yaratadi, eskisi tarixda qoladi.
- FR-5.7 [M] Prompt natijasi schema'ga mos kelmasa — parse xatosi sifatida retry, log'ga yoziladi.

### FR-6 Superadmin panel
- FR-6.1 [M] Dashboard: KPI kartalar + so'nggi sessiyalar + tip taqsimoti diagrammasi.
- FR-6.2 [M] Maktablar jadvali: qidiruv, faol/nofaol filtri, havola nusxalash, QR kod.
- FR-6.3 [M] O'quvchilar jadvali: server-side pagination, filtr (maktab, sinf, holat, sana oralig'i), qidiruv.
- FR-6.4 [M] Individual profil: anketa bloki, 4 test natijasi (radar/bar diagramma), AI hisobot,
  AI tahlillar tarixi, ishonchlilik indeksi, harakatlar (qayta tahlil, PDF, o'chirish).
- FR-6.5 [M] **Test bankini boshqarish** — ikki xil huquq darajasi:
  - **Tizim testlari** (`MBTI16`, `BIG5`, `RIASEC`, `ACTIVITY` — `IsSystem = true`):
    faqat savol **matni**, tartibi va faolligi tahrirlanadi. Savol qo'shish/o'chirish va
    `Scale`/`Direction` o'zgartirish **taqiqlangan** — ball normalizatsiyasi savol soniga bog'liq.
  - **O'z anketalari** (`Kind = Custom`): to'liq CRUD — shkalalar, savollar, `Scale`, `Direction`,
    `Weight`, tartib, faollik.
- FR-6.6 [M] **Anketa konstruktori:** superadmin kod yozmasdan yangi test yaratadi —
  nomi, tavsifi, shkalalari (kod, nom, talqin oraliqlari), savollari va har savolning shkalasi.
  Ball hisobi `SUM` strategiyasi bilan avtomatik (`docs/03` 6-bo'lim).
- FR-6.7 [M] **Draft → Publish oqimi:** yangi anketa `Draft` holatida tuziladi va sessiyalarga
  qo'shilmaydi; `Publish` qilinganda validatsiyadan o'tadi (har shkalada ≥ 4 savol, har savolda
  shkala belgilangan, kamida 1 shkala) va faollashadi.
- FR-6.8 [M] **Preview:** nashr qilishdan oldin anketani o'quvchi ko'radigan ko'rinishda ochish.
- FR-6.9 [S] **Duplicate:** mavjud testdan nusxa olib yangi versiya tayyorlash.
- FR-6.6 [M] AI sozlamalari: provider, model, API kalit (yozish-only, o'qishda maskalangan), default belgilash, "aloqani tekshirish" tugmasi.

### FR-7 Eksport va hisobot
- FR-7.1 [M] O'quvchilar ro'yxati → `.xlsx` (filtr bo'yicha).
- FR-7.2 [S] Bitta o'quvchi hisoboti → `.pdf` (maktab logotipi, diagrammalar, AI matn).
- FR-7.3 [C] Maktab bo'yicha yig'ma hisobot (sinf kesimida tip taqsimoti).

---

## 3. Nofunksional talablar

| Kod | Talab |
|-----|-------|
| NFR-1 | **Ishlash:** ommaviy API p95 < 300 ms; admin ro'yxat sahifasi p95 < 800 ms |
| NFR-2 | **Yuklama:** 300 bir vaqtdagi test yechuvchi; javob saqlash sekundiga 100 so'rov |
| NFR-3 | **Mobil:** ommaviy oqim 360px kenglikda to'liq ishlaydi (mobile-first) |
| NFR-4 | **Til:** UI o'zbek (lotin) asosiy; ma'lumot modeli ru/en uchun tayyor |
| NFR-5 | **Xavfsizlik:** HTTPS majburiy, parollar Argon2id/BCrypt, API kalitlar AES-shifrlangan, OWASP Top-10 |
| NFR-6 | **Maxfiylik:** ma'lumot minimallashtirish, o'chirish huquqi, audit log 1 yil |
| NFR-7 | **Ishonchlilik:** kunlik DB backup, 30 kun saqlash; AI navbati qayta urinish bilan |
| NFR-8 | **Kuzatuv:** strukturali log (Serilog), `/health` va `/health/ready`, xato tracking |
| NFR-9 | **Kengaytirilishi:** yangi metodika qo'shish faqat yangi seed + yangi `IScoringStrategy` talab qiladi |
| NFR-10 | **Erishimlilik:** kontrast AA, klaviatura navigatsiyasi, savollar `fieldset/legend` bilan |

---

## 4. Biznes qoidalari (BR)

- **BR-1** Bitta o'quvchi bir maktabda **bir dastur bo'yicha** 90 kun ichida faqat bitta
  **yakunlangan** sessiyaga ega bo'ladi (takror urinish uchun superadmin ruxsat beradi).
  Oyna `(o'quvchi, dastur)` juftligi bo'yicha hisoblanadi, o'quvchi bo'yicha umumiy EMAS
  (egasining qarori, 2026-09-07): har dastur — boshqa metodika to'plami, bir odamning
  ikkinchi dasturni topshirishi "takror" hisoblanmaydi. Maktabga (yoki ommaviy makonga) bir
  nechta dastur biriktirilgan bo'lsa, o'quvchi ularning har birini alohida topshira oladi.
  Tugallanmagan sessiyani davom ettirish ham dastur bo'yicha: A dasturda yarim qolgan sessiya
  A so'ralganda qaytariladi (`resumed`), B so'ralganda esa yangi sessiya ochiladi — shu sabab
  bir o'quvchida bir vaqtda bir nechta (har dasturda ko'pi bilan bitta) tugallanmagan sessiya
  bo'lishi mumkin.
- **BR-2** Sessiya yakunlanmaguncha AI tahlil boshlanmaydi.
- **BR-3** `ReliabilityScore < 40` bo'lsa, hisobot yuqorisida "natija ishonchsiz bo'lishi mumkin" bayrog'i qo'yiladi.
- **BR-4** O'chirilgan (nofaol) maktab havolasi 410 Gone qaytaradi.
- **BR-5** Muddati o'tgan sessiya davom ettirilmaydi, faqat yangi sessiya ochiladi.
- **BR-6** AI hisobotida hech qachon tibbiy/psixiatrik tashxis atamalari ishlatilmaydi (prompt darajasida taqiq + post-filtr).
- **BR-7** Superadmin faqat bitta faol hisob bo'lishi shart emas, lekin MVP'da seed orqali bitta yaratiladi.
- **BR-8** Tizim testlarining (`IsSystem = true`) savol soni, `Scale` va `Direction` qiymatlari
  o'zgartirilmaydi va bu testlar o'chirilmaydi — ilmiy metodika yaxlitligi va eski natijalar bilan
  taqqoslanuvchanlik shunga bog'liq.
- **BR-9** Nashr qilingan testga savol qo'shilsa yoki olib tashlansa `TestDefinition.Version`
  avtomatik oshadi. Eski natijalar qayta hisoblanmaydi — har `TestResult` o'zi hisoblangan
  `TestVersion` ni saqlaydi va hisobotda shu versiya ko'rsatiladi.
- **BR-10** Test faolligini o'chirish faqat **yangi** sessiyalarga ta'sir qiladi; boshlangan
  sessiyalar o'z testlarini oxirigacha ko'radi.
- **BR-11** Sessiyada ishlatilgan `Custom` test o'chirilmaydi — faqat arxivlanadi (`IsArchived`).
