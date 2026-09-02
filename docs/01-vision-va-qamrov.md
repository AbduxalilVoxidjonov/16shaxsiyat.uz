# 01 — Vizyon va qamrov

## 1. Muammo

Maktablarda o'quvchining qobiliyati, qiziqishi va psixologik holati **tizimli o'lchanmaydi**.
Natijada:

- O'quvchi o'ziga mos bo'lmagan yo'nalishni tanlaydi;
- Passiv yoki qiyinchilikdagi o'quvchi vaqtida aniqlanmaydi;
- Psixolog va sinf rahbarida obyektiv, taqqoslanadigan ma'lumot yo'q;
- Qog'ozdagi testlar qo'lda sanaladi, natija hech qayerda saqlanmaydi.

## 2. Yechim

Onlayn platforma:

1. Maktabga **shaxsiy havola** beriladi (masalan `16shaxsiyat.uz/t/maktab-12-kokand`).
2. O'quvchi havoladan kiradi, anketa to'ldiradi (FISH, tug'ilgan sana, sinf, telefon).
3. Ketma-ket 4 blok test yechadi: **16 Personality**, **Big Five**, **Holland RIASEC**, **Aktivlik/motivatsiya**.
4. Tizim ballarni avtomatik hisoblaydi (deterministik scoring engine).
5. **AI** barcha natijalarni birlashtirib to'liq shaxsiy tahlil yozadi: kuchli tomonlar, o'sish zonalari,
   motivatsiya profili, aktivlik darajasi, mos kasb yo'nalishlari, o'qituvchi/ota-ona uchun tavsiyalar.
6. **Superadmin** panelda: maktablar, o'quvchilar, natijalar, individual profil, taqqoslash, eksport.

## 2a. Brend va domen

| | Qiymat |
|---|---|
| **Ommaviy nom (brend)** | **Shaxsiyat** — o'quvchi, ota-ona va maktab ko'radigan nom |
| **Ichki kod nomi** | `StudentRoadMap` — repozitoriy, solution, konteyner nomlari |
| **Asosiy domen** | `16shaxsiyat.uz` (olingan) |
| **API subdomeni** | `api.16shaxsiyat.uz` |
| **Maktab havolasi** | `https://16shaxsiyat.uz/t/{slug}?k={token}` |
| **Xizmat pochtasi** | `info@16shaxsiyat.uz` (aloqa), `noreply@16shaxsiyat.uz` (tizim) |

Nomning ma'nosi platformaning vazifasiga to'g'ri keladi: test o'quvchining **shaxsiyatini**
— ya'ni imkoniyat va o'sish yo'nalishini — ko'rsatadi, baho qo'ymaydi. Bu tanlov hisobot tilida ham
saqlanadi: "kuchli tomonlar" va "o'sish zonalari", "yaxshi/yomon" emas.

UI matnlarida platforma **"Shaxsiyat"** deb ataladi; `StudentRoadMap` faqat texnik hujjatlar va
kodda qoladi. `docs/` va `prompts/` da `StudentRoadMap` nomi shu sababli o'zgarmaydi.

---

## 3. Foydalanuvchi rollari

| Rol | Kirish usuli | Nima qila oladi |
|-----|--------------|-----------------|
| **Superadmin** | Login + parol (+ 2FA opsional) | Hammasi: maktablar CRUD, havola generatsiya, o'quvchilar, natijalar, AI qayta ishga tushirish, test bankini boshqarish, eksport, audit log, AI provider sozlamalari |
| **O'quvchi (anonim mehmon)** | Maktab havolasi + o'zi kiritgan anketa | Faqat o'z sessiyasi: anketa, testlarni yechish, (ixtiyoriy) qisqa natijani ko'rish |

> MVP'da **faqat bitta superadmin roli** bor. Maktab admini / psixolog / o'qituvchi rollari
> v2 uchun rejalashtirilgan — lekin ma'lumotlar modeli buni ochiq qoldiradi (`AdminUser.Role`).

## 4. MVP qamrovi (IN SCOPE)

- [x] Maktablar CRUD + har biriga unikal, qayta generatsiya qilinadigan havola (slug + token)
- [x] Ommaviy test oqimi: anketa → 4 test → yakunlash (autosave bilan)
- [x] 4 metodika savol banki (seed data bilan), ko'p tilli maydonlar (uz asosiy, ru tayyor)
- [x] Deterministik scoring engine (har metodikaga alohida strategiya)
- [x] AI tahlil moduli: provider-agnostik, JSON schema bilan structured output
- [x] Superadmin panel: dashboard, maktablar, o'quvchilar, sessiyalar, individual profil
- [x] Individual profil sahifasi: barcha ball diagrammalari + AI hisobot
- [x] Excel/PDF eksport (o'quvchilar ro'yxati, bitta o'quvchi hisoboti)
- [x] Audit log, rate limiting, ma'lumotlar himoyasi
- [x] Docker compose bilan bir buyruqda ko'tarish

## 5. MVP'dan tashqarida (OUT OF SCOPE — v2+)

- Maktab admini / psixolog / ota-ona shaxsiy kabinetlari
- Mobil ilova (Flutter) — API tayyor bo'ladi, ilova keyin
- SMS/Telegram xabarnomalar
- Ko'p sessiyali dinamika (o'quvchini yildan yilga kuzatish grafiklari) — ma'lumot yig'iladi, UI keyin
- To'lov / tarif tizimi
- Multi-tenant (har maktab o'z admini bilan)
- Adaptiv test (IRT), video/audio javoblar

## 6. Muvaffaqiyat mezonlari

| Mezon | Maqsad |
|-------|--------|
| Bitta o'quvchi to'liq sessiyani yakunlash vaqti | 25–35 daqiqa |
| Sessiyani tashlab ketish (drop-off) darajasi | < 20% |
| AI tahlil tayyor bo'lish vaqti | < 60 soniya (fon jarayonda) |
| Bir vaqtning o'zida test yechayotgan o'quvchi | ≥ 300 (bitta maktab sinfxonalari) |
| Scoring aniqligi | Oltin testlarda 100% (deterministik) |
| Superadmin bitta o'quvchi profilini ochish | < 1.5 soniya |

## 7. Asosiy risklar va ularni kamaytirish

| Risk | Ta'sir | Kamaytirish |
|------|--------|-------------|
| AI noto'g'ri/zararli xulosa berishi (diagnoz qo'yishi) | Yuqori | Promptda qat'iq taqiq: tibbiy/psixiatrik diagnoz yo'q; faqat ta'limiy-yo'naltiruvchi til; har hisobotda disclaimer; superadmin tasdiqlashi mumkin |
| O'quvchi savollarni tasodifiy bosishi | O'rta | Javob vaqti (`DurationMs`) o'lchanadi, "ishonchlilik indeksi" hisoblanadi, past bo'lsa hisobotda belgilanadi |
| Havola boshqa maktabga tarqalishi | O'rta | Har havolaga kunlik limit, IP rate limit, ixtiyoriy 6 xonali kirish kodi, dublikat aniqlash (FISH+tug'ilgan sana) |
| Shaxsiy ma'lumot sizib chiqishi | Yuqori | HTTPS majburiy, ma'lumot minimallashtirish, API kalitlari shifrlangan, audit log, roleless public API |
| AI provider ishlamay qolishi | O'rta | Fallback provider zanjiri, navbat + retry, tahlilsiz ham ballar ko'rinadi |
| Test natijasi o'quvchini yorliqlab qo'yishi | Yuqori | Hisobotda "bu tashxis emas, bu — hozirgi holat surati" tili; o'sish zonalari ijobiy ramkada |

## 8. Etik chegaralar (majburiy)

1. Platforma **tashxis qo'ymaydi**. Hech qaysi hisobotda kasallik, buzilish, "normal emas" degan
   ifoda bo'lmaydi.
2. 18 yoshgacha bo'lgan o'quvchilar uchun **ota-ona/maktab roziligi** talab qilinadi — anketada
   checkbox va maktab bilan tuzilgan shartnoma.
3. Natija o'quvchini saralash yoki jazolash uchun emas, **yo'naltirish** uchun ishlatiladi.
4. O'quvchi so'rasa, ma'lumotlari o'chiriladi (`DELETE /api/admin/students/{id}` + audit yozuvi).
5. AI hisoboti har doim "AI tomonidan tayyorlangan, mutaxassis ko'rigi tavsiya etiladi" izohi bilan chiqadi.
