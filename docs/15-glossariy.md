# 15 — Glossariy

## Loyiha atamalari

| O'zbekcha | Inglizcha / kodda | Ma'nosi |
|-----------|-------------------|---------|
| Maktab | `School` | Platformaga ulangan ta'lim muassasasi; har biriga shaxsiy havola beriladi |
| Shaxsiy havola | `access link` | `salohiyat.uz/t/{slug}?k={token}` — maktab o'quvchilari kiradigan URL |
| Slug | `Slug` | Havoladagi o'qiladigan qism: `12-maktab-kokand` |
| Kirish tokeni | `AccessToken` | Havoladagi maxfiy qism; qayta generatsiya qilinadi |
| Kirish kodi | `AccessCode` | Ixtiyoriy 6 raqamli qo'shimcha himoya |
| O'quvchi | `Student` | Anketa to'ldirgan shaxs |
| Sessiya / baholash | `Assessment` | Bitta o'quvchining bitta to'liq test o'tishi |
| Sessiya tokeni | `SessionToken` | O'quvchi brauzerida saqlanadigan kalit |
| Blok / test | `AssessmentTest` | Sessiya ichidagi bitta metodika (masalan Big Five) |
| Savol banki | `TestDefinition` + `Question` | Metodika va uning savollari |
| Javob | `Answer` | Bitta savolga berilgan qiymat |
| Ball hisoblash | `Scoring` | Javoblardan natija chiqarish (deterministik) |
| Natija | `TestResult` | Bitta metodika bo'yicha hisoblangan ballar |
| Ishonchlilik indeksi | `ReliabilityScore` | Javoblar jiddiy berilganini baholovchi 0–100 ball |
| Yetuklik indeksi | `MaturityIndex` | Big Five + o'z-o'zini boshqarishdan yig'ilgan agregat 0–100 |
| Aktivlik indeksi | `ActivityIndex` | Motivatsiya, intizom, ijtimoiy faollik, bandlikdan 0–100 |
| E'tibor bayrog'i | `NeedsAttention` | Past aktivlik — suhbat foydali bo'lishi mumkin signali |
| AI tahlil | `AiAnalysis` | Model tomonidan yozilgan to'liq hisobot |
| Provayder | `AiProvider` | Gemini / OpenAI / Anthropic |
| Zaxira zanjiri | `fallback chain` | Asosiy provayder ishlamasa navbatdagilar |
| Superadmin | `SuperAdmin` | Yagona to'liq huquqli boshqaruvchi |
| Tizim metodikasi | `IsSystem` | Seed'dan kelgan 4 ilmiy test — savoli va shkalasi qulflangan |
| O'z anketasi | `Kind = Custom` | Superadmin kod yozmasdan yaratgan test |
| Shkala | `TestScale` | Anketa ichidagi o'lchov o'qi (masalan "Stressga munosabat") |
| Talqin oralig'i | `InterpretationBand` | 0–100 dagi bo'lak va uning nomi (0–33 "Past") |
| Yig'indi hisobi | `SUM` | O'z anketalari uchun universal ball strategiyasi |
| Qoralama / Nashr | `Draft` / `Published` | Anketa tayyorlanmoqda / sessiyalarga chiqdi |
| Audit jurnali | `AuditLog` | Kim, qachon, nima qilgani yozuvi |

## Psixologik atamalar

| Atama | Ma'nosi |
|-------|---------|
| **16 tipli model** | Yung tipologiyasi asosidagi 4 dixotomiya (E/I, S/N, T/F, J/P) → 16 tip |
| **Ekstraversiya / Introversiya (E/I)** | Energiyani tashqi muhitdan yoki ichki dunyodan olish |
| **Sezish / Intuitsiya (S/N)** | Aniq faktlarga yoki umumiy g'oya va imkoniyatlarga tayanish |
| **Fikrlash / His-tuyg'u (T/F)** | Qarorda mantiq yoki odamlar va qadriyatlarni birinchi qo'yish |
| **Tartib / Moslashuvchanlik (J/P)** | Rejalashtirilgan yoki ochiq-erkin yashash uslubi |
| **Big Five (OCEAN)** | Shaxsiyatning 5 omili — ilmiy jihatdan eng asoslangan model |
| **Ochiqlik (O)** | Yangilikka, g'oyaga, tajribaga ochiqlik |
| **Vijdonlilik (C)** | Tartib, mas'uliyat, maqsadga yo'nalganlik |
| **Kelishuvchanlik (A)** | Hamkorlik, empatiya, ishonch |
| **Emotsional barqarorlik** | Stressga chidamlilik (neyrotizmning teskarisi) |
| **RIASEC / Holland modeli** | Kasb qiziqishlarining 6 tipi |
| **Realistik (R)** | Amaliy, texnik, qo'l mehnati bilan bog'liq ish |
| **Tadqiqotchi (I)** | Tahlil, izlanish, ilm |
| **Artistik (A)** | Ijod, dizayn, o'zini ifodalash |
| **Ijtimoiy (S)** | Odamlarga yordam, o'qitish, muloqot |
| **Tadbirkor (E)** | Boshqarish, ishontirish, tashabbus |
| **Konvensional (C)** | Tartib, hujjat, aniq qoidalar bilan ish |
| **Holland kodi** | Eng yuqori 3 tipning harflari, masalan `IRA` |
| **Differensiatsiya** | Eng yuqori va eng past qiziqish orasidagi farq; kichik bo'lsa qiziqish hali shakllanmagan |
| **Likert shkalasi** | "Umuman qo'shilmayman → To'liq qo'shilaman" 5 pog'onali javob |
| **Teskari savol** | Ball hisoblashda qiymati aylantiriladigan savol (`6 − v`) |
| **Straight-lining** | Savollarni o'qimay bir xil variantni ketma-ket tanlash |

## Texnik atamalar

| Atama | Ma'nosi |
|-------|---------|
| **Clean Architecture** | Qatlamli arxitektura: Domain → Application → Infrastructure/Api |
| **CQRS** | Buyruq (o'zgartirish) va so'rov (o'qish) ni ajratish |
| **MediatR** | Buyruq/so'rovlarni handler'larga yo'naltiruvchi kutubxona |
| **EF Core** | .NET uchun ORM |
| **Migratsiya** | DB sxemasining versiyalangan o'zgarishi |
| **jsonb** | PostgreSQL'ning indekslanadigan JSON turi |
| **JWT** | Imzolangan kirish tokeni |
| **Refresh token** | Access tokenni yangilash uchun uzoq muddatli token |
| **Rate limiting** | So'rovlar sonini cheklash |
| **IDOR** | Boshqa foydalanuvchi obyektiga ID orqali noqonuniy kirish |
| **Structured output** | AI javobini qat'iy JSON sxemaga majburlash |
| **Idempotent** | Bir amalni takrorlash natijani o'zgartirmasligi |
| **Soft delete** | Yozuvni o'chirmay, "o'chirilgan" deb belgilash |
| **Testcontainers** | Testlar uchun vaqtinchalik Docker DB ko'tarish |
| **Golden test** | Kutilgan natijasi oldindan qo'lda hisoblangan mos yozuvlar testi |
