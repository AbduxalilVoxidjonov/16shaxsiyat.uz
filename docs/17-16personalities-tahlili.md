# 17 — 16Personalities tahlili (raqobat va metodika o'rganilishi)

**Manba:** `https://www.16personalities.com/uz` va uning ichki sahifalari
**Tekshirilgan sana:** 2026-08-31 · **Kim uchun:** mahsulot egasi, PM, psixometrika va frontend agentlari

> **Bu hujjatning maqsadi — nusxa ko'chirish emas, o'rganish.**
> 16Personalities'ning savollari, tip tavsiflari va `NERIS Type Explorer®` nomi — himoyalangan
> intellektual mulk. Biz ulardan **strukturaviy va UX darslarini** olamiz; matn, savol yoki
> tavsifni ko'chirmaymiz. Huquqiy chegara 8-bo'limda batafsil.

---

## 1. Fakt kartochkasi

| | |
|---|---|
| **Egasi** | NERIS Analytics Limited (Buyuk Britaniya), ©2011–2026 |
| **Instrument nomi** | **NERIS Type Explorer®** (MBTI® emas — bu boshqa kompaniya mulki) |
| **Test hajmi** | **60 savol**, ~10 daqiqa |
| **Javob shkalasi** | **7 balli** Likert ("Roziman" ↔ "Rozi emasman") |
| **Kirish talabi** | Yo'q — ro'yxatdan o'tmasdan yechiladi, natija darhol chiqadi |
| **Tillar** | 45 dan ortiq |
| **Hajm (sayt da'vosi)** | 1.57 mlrd + test yechilgan, kuniga ~256 000 |
| **"Aniqlik" da'vosi** | 91.2% (o'z-o'zini baholash so'rovi asosida — ilmiy validlik ko'rsatkichi emas) |
| **Biznes modeli** | Bepul natija → pullik **Premium Career Suite ($29)** |
| **O'zbek tili** | Bor, lekin saytning o'z izohiga ko'ra tarjimalar qisman **AI yordamida** qilingan; rasmiy matn — inglizcha |

**Bosh sahifa sarlavhasi (uz):** *"Nihoyat meni tushunishlari juda hayratlanarli"*
**Va'da:** 10 daqiqada o'zi haqida "ajablanarli tarzda aniq" tavsif.

---

## 2. Metodika — 5 o'lcham

16Personalities MBTI'ning 4 dixotomiyasini oladi va ustiga **beshinchi** o'lchov qo'shadi.
Muhim farq: **Yung kognitiv funksiyalari ishlatilmaydi** ("ilmiy o'lchash va tasdiqlash juda qiyin"
deb izohlaydilar), o'rniga **Big Five (xususiyat) modeli** asos qilib olingan.

| № | O'lcham (ingl.) | Qutblari | Ma'nosi |
|---|-----------------|----------|---------|
| 1 | **Mind** — Ong | Introverted / Extraverted (I/E) | Energiyani yolg'izlikdan yoki odamlardan olish |
| 2 | **Energy** — Energiya | Observant / Intuitive (S/N) | Aniq faktlar va hozir / g'oya va kelajak |
| 3 | **Nature** — Tabiat | Thinking / Feeling (T/F) | Mantiq va obyektivlik / his-tuyg'u va uyg'unlik |
| 4 | **Tactics** — Taktika | Judging / Prospecting (J/P) | Reja va tartib / moslashuvchanlik va o'z-o'zidanlik |
| 5 | **Identity** — O'ziga ishonch | **Assertive / Turbulent (A/T)** | Stressga chidamlilik va o'ziga ishonch darajasi |

> **Diqqat:** 5-o'lcham (Identity) mohiyatan **neyrotizmning** o'lchovi — ya'ni Big Five'dagi `N`
> omilining boshqa nom bilan qo'yilgani. Natija `INTJ-A` yoki `INTJ-T` ko'rinishida yoziladi.

**Natija ko'rinishi:** qat'iy "sen INTJ san" emas, balki **har o'q bo'yicha foiz**
(masalan "Intuitive 71%") — ya'ni tip emas, spektrdagi joy ko'rsatiladi.

---

## 3. 16 tip — o'zbekcha nomlari (saytdagi holicha)

| Guruh (uz) | Kod | Nomi (uz) |
|------------|-----|-----------|
| **Tahlilchilar** (Analysts) | INTJ | Arxitektor |
| | INTP | Mantiqchi |
| | ENTJ | Qoʻmondon |
| | ENTP | Bahschi |
| **Diplomatlar** (Diplomats) | INFJ | Vakil |
| | INFP | Vositachi |
| | ENFJ | Protagonist |
| | ENFP | Faol |
| **Soqchilar** (Sentinels) | ISTJ | Logist |
| | ISFJ | Himoyachi |
| | ESTJ | Rahbar |
| | ESFJ | Konsul |
| **Kashfiyotchilar** (Explorers) | ISTP | Virtuoz |
| | ISFP | Sarguzashtchi |
| | ESTP | Tadbirkor |
| | ESFP | Artist |

> Bu nomlar — **ularning tarjimasi**. Bizning `type-catalog.json` da **o'z nomlarimiz** bo'ladi
> (`docs/03`, `prompts/04`). Masalan `INTJ` uchun **"Loyihachi"** — nusxa emas, mustaqil tanlov.

### Rollar va Strategiyalar

**4 Rol** (Energy + Nature kesishmasi): Analysts (N+T) · Diplomats (N+F) · Sentinels (S+J) · Explorers (S+P).

**4 Strategiya** (Mind + Identity kesishmasi):

| Strategiya | Tarkibi | Ma'nosi |
|------------|---------|---------|
| Confident Individualism | Introvert + Assertive | Mustaqil, o'ziga ishongan |
| People Mastery | Extravert + Assertive | Odamlar bilan erkin, ishonchli |
| Constant Improvement | Introvert + Turbulent | O'zini doim yaxshilashga intiladi |
| Social Engagement | Extravert + Turbulent | Ijtimoiy, lekin tashqi baholashga sezgir |

---

## 4. Test mexanikasi (brauzerda bevosita kuzatildi)

| Element | Qanday |
|---------|--------|
| Savol soni | `Savol 1 / 60` — progres savol raqami bilan ko'rsatiladi |
| Sahifadagi savol | **6 ta**, pastda bitta "Keyingisi" tugmasi |
| Javob shkalasi | 7 ta doira: chapda "Roziman", o'ngda "Rozi emasman" |
| Shkala yorliqlari (uz) | Butunlay qoʻshilaman · Biroz qoʻshilaman · Qoʻshilaman · **Amin emasman** · Qoʻshilmayman · Biroz qoʻshilmayman · Umuman qoʻshilmayman |
| Vizual yechim | Doiralar markazdan chetga kattalashadi — kuchli rozilik = katta doira |
| Savol shakli | Ikkinchi shaxs: *"Siz ... qilasiz"* (biz birinchi shaxsni tanladik: *"Men ..."*) |
| Majburiylik | Barcha 6 savolga javob berilmasa keyingi sahifaga o'tmaydi |
| Ro'yxatdan o'tish | Testdan **oldin** so'ralmaydi; natijadan keyin email taklif qilinadi |
| Vaqt o'lchash | Foydalanuvchiga ko'rsatilmaydi |

**Savol mavzulari** (nusxa ko'chirmaslik uchun faqat mavzular): yangi tanishuv orttirish ·
noma'lum g'oyalarni o'rganish · munozarada ta'sirlanish · muddatlar bilan munosabat ·
o'zini himoyalangan his qilish · telefon qo'ng'irog'idan qochish.

> **Diqqat — huquqiy:** bu savollarning **matnini** hech qanday ko'rinishda (tarjima,
> qayta yozish, "ilhomlanish") bizning `SeedData` ga ko'chirmaymiz. Mavzu darajasidagi
> qamrov umumiy psixometrik bilim, matn esa ularning mulki.

---

## 5. Foydalanuvchi oqimi (funnel)

```
Bosh sahifa (ijtimoiy dalil: "1.57 mlrd test")
   ↓  "Testni topshirish"
Test kirish ekrani — 3 karta: Testni topshirish · Batafsil natijalarni ochish · Shaxsiyatingizni oching
   ↓
60 savol · 10 sahifa · 6 savoldan
   ↓
Natija: tip kodi (masalan INTJ-A) + 5 o'q foizlari + rol + strategiya + qisqa tavsif
   ↓
Email so'raladi (natijani saqlash/yuborish uchun) → hisob yaratiladi
   ↓
Tip sahifasi: uzun tavsif bo'limlari (bepul)
   ↓
Premium Career Suite — $29
```

**Konversiya nuqtalari:** natija ekranidan keyin email, tip sahifasining har bo'limi oxirida
premium bloki, karyera bo'limida eng kuchli bosim.

---

## 6. Sayt strukturasi

**Asosiy navigatsiya (uz):** Shaxsiyat testi · Shaxsiyat turlari · Karyera toʻplami · Xizmatlar · Maqolalar

**Tip sahifasi bo'limlari** (ingliz versiyasida to'liq):
Kirish → Kuchli va zaif tomonlar → Romantik munosabatlar → Do'stlik → Ota-onalik →
Karyera yo'llari → Ish joyidagi odatlar → Xulosa.

O'zbek tip sahifasi hozircha qisqaroq: kirish + 4 tematik blok
("Kashshoflik ruhiyati", "Bilimga chanqoqlik", "Ijtimoiy qiyinchiliklar", "Hayot shaxmat o'yini")
va oxirida premium havolasi.

**Boshqa bo'limlar:** Our Framework (metodika), Country Profiles (mamlakatlar kesimida statistika),
Maqolalar (kontent marketing / SEO), Team Assessments va Reports for Professionals (B2B),
NPQE®, MindTrackers® (boshqa mahsulotlari).

---

## 7. Biznes modeli

| Mahsulot | Narx | Tarkibi |
|----------|------|---------|
| Bepul test va natija | 0 | Tip, foizlar, rol, strategiya, uzun tip tavsifi |
| **Premium Career Suite** | **$29** (30 kunlik pul qaytarish kafolati) | 40+ sahifali karyera qo'llanmasi (PDF), **5 ta AI karyera mentori**, **31 ta test va vosita** (burnout riski, karyera qadriyatlari, yetakchilik uslubi, ish muhiti afzalliklari) |
| Team Assessments | — | Jamoalar uchun (B2B) |
| Reports for Professionals | — | Psixolog/HR mutaxassislari uchun |

**Diqqatga sazovor:** premium bitta kasb nomini aytmaydi — **soha, rol va ish muhiti** tavsiya qiladi.
Bu to'g'ri yondashuv va biz ham `docs/09` da shu prinsipni qo'llaganmiz.

---

## 8. Huquqiy chegara — biz uchun qat'iy qoidalar

| Nima | Holati | Biz uchun qoida |
|------|--------|-----------------|
| Test savollari (60 ta, har tildagi tarjimasi) | Mualliflik huquqi | ❌ Ko'chirilmaydi, tarjima qilinmaydi, "qayta yozilmaydi" |
| Tip tavsiflari, bo'lim matnlari | Mualliflik huquqi | ❌ Ko'chirilmaydi |
| Tip nomlari ("Arxitektor", "Vositachi"…) | Ularning tarjima tanlovlari | ❌ Ishlatilmaydi — o'z nomlarimiz |
| `NERIS Type Explorer®`, `16Personalities` | Tovar belgisi | ❌ Ishlatilmaydi |
| `MBTI®`, `Myers-Briggs®` | Boshqa kompaniya tovar belgisi | ❌ Ishlatilmaydi (`docs/03` 0-bo'lim) |
| `-A` / `-T` qo'shimchasi (INTJ-A) | Ularning belgisi | ❌ Ishlatilmaydi — biz "barqaror / sezgir" deb yozamiz |
| 4 dixotomiya g'oyasi (E/I, S/N, T/F, J/P) | Umumiy psixologik meros (Yung, 1921) | ✅ Ishlatish mumkin |
| 4 harfli kod (`INTJ`) | Umumiy ishlatiladi, himoyalanmagan | ✅ Ishlatish mumkin |
| Big Five (OCEAN), IPIP savol banki | Public domain | ✅ Ishlatish mumkin |
| Holland RIASEC, O*NET | Public domain | ✅ Ishlatish mumkin |
| UX yechimlari (7 balli shkala, sahifadagi savol soni) | Himoyalanmaydi | ✅ O'rganish mumkin |

**Amaliy qoida agentlar uchun:** `scoring-psychometrics` agenti savol yozganda 16Personalities
sahifasini **ochmaydi** ham. Savollar `docs/03` dagi qamrov ro'yxati va IPIP kabi ochiq
manbalar asosida noldan yoziladi.

---

## 9. Kuchli tomonlari — nimasi ishlaydi

1. **Kirish to'sig'i nol.** Ro'yxatdan o'tish yo'q, natija darhol. Biz ham shunday qildik
   (maktab havolasi + anketa, login yo'q).
2. **7 balli shkala va vizual doiralar** — "o'rtacha" javobga qochishni kamaytiradi va
   bosish qulay.
3. **Sahifada 6 savol** — 60 savol 10 ta qisqa qadamga bo'linadi, "hali ko'p qoldi" hissi yo'q.
4. **Foizli natija** — qat'iy yorliq o'rniga spektr. Bu ilmiy jihatdan ham to'g'riroq.
5. **Til iliq va shaxsiy** — o'quvchiga emas, insonga murojaat. Yorliqlash yo'q.
6. **Tip = shaxsiyat, ism emas.** "Arxitektor", "Vositachi" — obraz beradi, esda qoladi.
7. **Karyera tavsiyasi soha darajasida**, bitta kasb emas.

---

## 10. Zaif tomonlari va ilmiy tanqid

| Muammo | Mohiyati | Bizga xulosa |
|--------|----------|--------------|
| **Test-retest barqarorligi past** | Tadqiqotlarga ko'ra ~5 hafta ichida yechuvchilarning taxminan yarmi boshqa tip oladi | Natijani "o'zgarmas xususiyat" deb taqdim etmaymiz — `docs/01` etik chegara sifatida allaqachon yozilgan |
| **Sun'iy dixotomiya** | Real ma'lumotda o'lchamlar uzluksiz, ikkiga bo'linmaydi | Biz `borderline` (45–55%) zonasini bayroqlaymiz — `docs/03` 2.2 |
| **Ish samaradorligini bashorat qilmaydi** | Tip ish natijasini Big Five'dan ortiq tushuntirmaydi | Shuning uchun bizda Big Five **asosiy**, 16 tip esa tushunarli "til" sifatida ishlatiladi |
| **Barnum effekti** | Tavsiflar shunchalik umumiyki, har kim o'zini tanidi deb o'ylaydi | AI promptimizda har xulosa **aniq ballga bog'lanishi** talab qilinadi (`docs/09` 4.2) |
| **"91.2% aniqlik"** | Bu foydalanuvchining o'z bahosi, validlik ko'rsatkichi emas | Biz bunday marketing raqamini ishlatmaymiz |
| **Ishonchlilik nazorati yo'q** | Tez yoki tasodifiy bosilgan javoblar aniqlanmaydi | Bizda `ReliabilityScore` bor — **aniq ustunlik** (`docs/03` 7-bo'lim) |
| **O'zbek tili qisman AI tarjimasi** | Ba'zi savol va matnlar g'aliz ("Sizga ruhiy munozaralar bilan ta'sir qilish qiyin") | Bizning katta ustunligimiz: **o'smir uchun yozilgan tabiiy o'zbekcha** |
| **Yosh moslashuvi yo'q** | Kattalar uchun yozilgan, maktab konteksti yo'q | Bizda savollar maktab hayotidan (`prompts/05–08`) |

---

## 11. "Shaxsiyat" bilan taqqoslash

| Jihat | 16Personalities | **Shaxsiyat** |
|-------|-----------------|---------------|
| Auditoriya | Kattalar, global | O'zbekiston maktab o'quvchilari (5–11 sinf) |
| Metodika | 1 ta test (60 savol, 5 o'lcham) | **4 ta metodika, 190 savol**: 16 tip + Big Five + RIASEC + Aktivlik |
| Big Five | Yashirin (faqat Identity ko'rinadi) | **Ochiq va to'liq** — 5 omil ham ko'rsatiladi |
| Kasb qiziqishlari | Tipdan chiqariladi | **Alohida o'lchanadi** (RIASEC) — ancha ishonchli |
| Aktivlik/motivatsiya | Yo'q | **Bor** — o'quvchining hozirgi holati va e'tibor bayrog'i |
| Ishonchlilik nazorati | Yo'q | **Bor** (`ReliabilityScore`) |
| Tahlil | Oldindan yozilgan shablon matn | **AI** — o'quvchining aniq ballari asosida yoziladi |
| Kim ko'radi | Faqat foydalanuvchi | O'quvchi (qisqa) + **maktab/superadmin (to'liq)** |
| Boshqaruv paneli | Yo'q (B2B alohida mahsulot) | **CRM markazda**: maktab, sinf, individual profil, eksport |
| Til | 45+ til, qisman AI tarjima | **Ona tilida yozilgan** o'zbekcha, o'smir tilida |
| Monetizatsiya | B2C, $29 premium | B2B — maktab/tuman bilan ishlash |
| Etika | Umumiy | Yozma etik chegaralar, rozilik, tashxis taqiqi (`docs/01` 8-bo'lim) |

**Bizning asosiy ustunligimiz uch narsada:** (1) bitta emas, **to'rt metodika**;
(2) natija **maktabga** ham boradi va boshqariladi; (3) matn **shu yerda, shu yosh uchun** yozilgan.

---

## 12. Aniq xulosalar — nima qilamiz

| № | Xulosa | Ta'siri |
|---|--------|---------|
| 1 | **7 balli shkalaga o'tishni ko'rib chiqish** — hozir bizda `Likert5`. 7 ball "o'rtacha"ga qochishni kamaytiradi, lekin o'smirga og'irroq. **Tavsiya:** 16 tip blokida 7 ball, qolganida 5 ball sinab ko'rish (pilotda A/B) | `docs/03`, `prompts/05` |
| 2 | **Sahifada 5–6 savol** (hozir 10) — qisqa qadamlar tugatish darajasini oshiradi | `docs/11` E-3, `TestDefinition.PageSize` |
| 3 | **Foizli natija ko'rsatish** — allaqachon rejamizda (`axisPct`), UI'da ham foiz chiqsin | `docs/11`, `widgets/AxisBar` |
| 4 | **Identity o'lchovini qo'shimcha savolsiz olamiz** — Big Five'dagi `N` omilidan "emotsional barqarorlik" allaqachon hisoblanadi. Uni tip yoniga **o'z so'zimiz bilan** ("barqaror" / "sezgir") qo'shsak, 16Personalities'ning 5-o'lchami bepul qo'lga kiradi | `docs/03` 3.2, `prompts/09` |
| 5 | **Tip nomlari obrazli bo'lsin** — "Loyihachi", "Otashqalb" kabi. Lekin **ularning nomlari emas** | `prompts/04` `type-catalog.json` |
| 6 | **Karyera tavsiyasi soha darajasida** — bitta kasb aytmaslik. Bizda allaqachon shunday | `docs/09` |
| 7 | **Ishonchlilik indeksini marketing ustunligi sifatida ishlatish** — maktabga "javoblar jiddiy berilganmi" ko'rsatkichi beriladi, raqobatchida yo'q | `docs/01`, sotuv materiali |
| 8 | **"91.2% aniqlik" kabi da'volardan qochish** — biz metodika va cheklovlarni ochiq yozamiz | `docs/01` 8-bo'lim |
| 9 | **Tip sahifalari kontenti** — har tip uchun uzun, sifatli o'zbekcha tavsif kelajakda SEO va ishonch beradi (v2 kontent rejasi) | `docs/14` v2 backlog |
| 10 | **Testni ro'yxatdan o'tkazmasdan boshlash** — bizda shunday, saqlanadi | `docs/07` |

---

## 13. Manbalar

- [16Personalities — bosh sahifa (uz)](https://www.16personalities.com/uz)
- [Bepul shaxsiyat testi (uz)](https://www.16personalities.com/uz/bepul-shaxsiyat-testi) — savol soni, shkala va sahifalash brauzerda bevosita kuzatildi
- [Shaxsiyat turlari (uz)](https://www.16personalities.com/uz/shaxsiyat-turlari)
- [INTJ — Arxitektor (uz)](https://www.16personalities.com/uz/shaxsiyat-turi-intj)
- [Our Framework](https://www.16personalities.com/articles/our-theory)
- [Premium Career Suite](https://www.16personalities.com/premium/career-suite)
- [16Personalities Career Test Review (2026) — MyPassion.AI](https://mypassion.ai/blog/16personalities-career-test-review) — ilmiy tanqid va test-retest ma'lumotlari
