# 11 — UX va ekranlar

## 0. Brend

Foydalanuvchiga ko'rinadigan nom — **Shaxsiyat** (`16shaxsiyat.uz`). Sarlavha, logotip, sahifa
`<title>` va PDF kolontitulida shu nom turadi. `StudentRoadMap` hech qaysi ekranda ko'rinmaydi.

Tag-line (landing sahifada, ixtiyoriy): *"O'zingni bilib, yo'lingni tanla"*.

Til: hozircha faqat o'zbekcha (lotin) ko'rinadi — til almashtirgich UI'da yo'q, `ru`
tarjimasi bo'sh skelet (`fallbackLng: 'uz'` hammasini o'zbekchaga qaytaradi).

---

## 1. Dizayn prinsiplari

1. **Mobil birinchi** — o'quvchilarning aksariyati telefonda yechadi.
2. **Bir ekranda bitta vazifa** — chalg'itadigan element yo'q.
3. **Har doim progress ko'rinadi** — "yana qancha qoldi" savoli javobsiz qolmasin.
4. **Xatolik qo'rqinchli bo'lmasin** — "javoblaringiz saqlandi" iliq tilda.
5. **Admin panel — ma'lumot zich, lekin o'qiladigan**; asosiy harakat har doim yuqori o'ngda.

**Rang tizimi (semantik):**

| Ma'no | Ishlatilishi |
|-------|--------------|
| Asosiy (primary) | Harakat tugmalari, faol progress |
| Muvaffaqiyat | Tugallangan blok, `Reliable` |
| Ogohlantirish | `Questionable`, `Analyzing` |
| Xavf | `Unreliable`, `AnalysisFailed`, o'chirish |
| Neytral | Fon, chegaralar, ikkilamchi matn |

Diagramma ranglari **ball darajasini bildirmaydi** (past ball "yomon" emas) — bir xil neytral
palitra, faqat farqlash uchun.

> **P45 (2026-09-05) — token nomlari.** Yuqoridagi jadval semantik ROLLARni tavsiflaydi, ular
> hamon amal qiladi; **konkret token nomlari** endi ikkiga bo'lingan (`docs/10` §9.1):
> - **Admin panel** (bo'lim 3) va `shared/ui` — eski tokenlar: `primary-*` (asosiy),
>   `success-*` (muvaffaqiyat), `warning-*` (ogohlantirish), `danger-*` (xavf), `neutral-*`.
> - **Ommaviy sahifalar** (bo'lim 2, 2a) — yangi qog'oz-siyoh palitra: `paper`/`paper-deep`/
>   `paper-card` (fon), `ink`/`ink-soft`/`ink-muted`/`ink-faint` (matn), `line`/`line-strong`
>   (chegara), `firuza-*` (asosiy — CTA, faol holat), `binafsha-*` (masalan Likert savolida
>   "rad" qutbi), `zumrad-*`/`lojuvard-*`/`zarhal-*`/`terakota-*` (dekorativ/urg'u aksentlari,
>   masalan natija ekranidagi "AI tashxis qo'ymaydi" izohi `zarhal` rangda).
>
> Ikkalasi ham "diagramma/rang ball darajasini bildirmaydi" qoidasiga bo'ysunadi — yangi
> palitrada ham `LikertQuestion` doiralarining rangi rozilik/rad QUTBINI ko'rsatadi, ball
> "yaxshi/yomon"ligini emas.

---

## 2. Ommaviy oqim (o'quvchi)

```
/t/:slug          →  /t/:slug/register  →  /t/:slug/test/MBTI16  →  .../done
    landing              anketa              savollar               tanaffus
                                                  │                    │
                                                  └────────────────────┘
                                                     (4 blok takrorlanadi)
                                                             ▼
                                              /t/:slug/finish  →  /t/:slug/result
```

### E-1 Landing (`/t/:slug`)
- Maktab nomi, "Sen haqingdagi test" sarlavhasi.
- 3 ta oddiy jumlada nima bo'lishi: "190 savol · ~30 daqiqa · to'g'ri yoki noto'g'ri javob yo'q".
- 4 ta test kartasi: nomi, nima o'lchaydi, savol soni, vaqti.
- "Bu test baho emas" izohi.
- Katta tugma: **Boshlash**.
- Xato holatlar: havola noto'g'ri → "Havola ishlamayapti, maktabingizdan yangisini so'rang";
  maktab nofaol → "Bu maktab uchun test vaqtincha yopilgan".

### E-2 Anketa (`/t/:slug/register`)
- Maydonlar tartibi: FISH → tug'ilgan sana → jins → sinf + harf → telefon → ota-ona telefoni
  → email (ixtiyoriy).
- Telefon maskasi `+998 (__) ___-__-__`, inline validatsiya.
- Tug'ilgan sana — 3 ta select (kun/oy/yil), kalendar emas (telefonda qulayroq).
- Rozilik bloki: qisqa matn + checkbox ("Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman").
- Agar `requiresAccessCode` — 6 raqamli kod maydoni.
- Dublikat javobi (`409`) → "Sen allaqachon testni topshirgansan. Qayta topshirish uchun
  o'qituvchingga murojaat qil."
- Yakuni: **Testni boshlash**.

### E-3 Test sahifasi (`/t/:slug/test/:testCode`)
```
┌──────────────────────────────────────────────┐
│ 16 tipli shaxsiyat modeli    ●●○○   2/4 blok │  ← sticky header
│ ████████████░░░░░░░░░░░  24/60 savol         │
├──────────────────────────────────────────────┤
│                                              │
│         11. Yangi odamlar bilan tanishish    │
│             menga oson.                      │
│                                              │
│   ◔      ◕      ⬤      ◕      ◔             │  ← doiralar chetdan
│  Umuman qo'shilmayman     To'liq qo'shilaman │     markazga kichrayadi
│                                              │
│  12. …                                       │
├──────────────────────────────────────────────┤
│  ← Orqaga                    Keyingi →       │  ← sticky footer
└──────────────────────────────────────────────┘
```
- **Ko'rinish shkala turiga moslashadi** (`LikertQuestion`, P45 — `docs/10` §4.3): 4+ darajali
  shkala (`Likert7`/`Likert5`) — chetdan markazga kichrayuvchi doiralar qatori (yuqoridagi
  maket), ostida faqat ikki qutb yorlig'i; 3 va undan kam daraja (`Binary`) — markazlashgan
  yirik doiralar, har birining OSTIDA o'z yorlig'i; `options[]` bilan keladigan savol
  (`SingleChoice`/`ForcedChoice`) — doira EMAS, vertikal variant KARTALARI.
- Klaviatura: `1..9` raqamlari mos variantni tanlaydi (doira/karta soniga qarab), `Enter`
  keyingi savolga o'tadi.
- Javob tanlanganda yumshoq animatsiya, avtomatik keyingi savolga scroll (oxirgi savolda emas).
- To'ldirilmagan savol bo'lsa "Keyingi" bosilganda birinchi bo'shiga scroll + qizil ramka.
- Yuqorida kichkina "Saqlandi ✓" indikatori (autosave holati).
- Offline banner: "Internet yo'q — javoblaringiz saqlanmoqda, ulanish tiklanganda yuboriladi".

**P52-A4 — bo'lim-qadam ko'rinishi** (tarmoqlanuvchi so'rovnoma, to'liq shartnoma `docs/18`,
frontend qismi `docs/10` §4.3): anketada bo'lim bor bo'lsa (`sections`), yuqoridagi maket bir
farq bilan — bir ekranda savollar sahifasi o'rniga BITTA ko'rinadigan bo'lim ko'rsatiladi
(sarlavha + tavsif + o'sha bo'limga tegishli, HOZIR ko'rinadigan savollar). "Keyingi" keyingi
ko'rinadigan bo'limga o'tadi (masalan "qo'shimcha kursga qatnashasizmi?" savoliga javobga
qarab — 2-A/2-B/2-C dan biri, yoki hech biri), progress bo'lim ichidagi savol soniga emas,
BUTUN testdagi HOZIR ko'rinadigan savollar soniga qarab hisoblanadi. Bo'lim almashganda fokus
yangi sarlavhaga o'tadi (ekran o'quvchisi uchun). Savol darajasida ham shart bo'lishi mumkin —
masalan "Boshqa (kiriting)" varianti tanlansa ostida matn maydoni darhol paydo bo'ladi.
Bo'limsiz anketalarda (4 ta tizim metodikasi) yuqoridagi sahifalangan ko'rinish o'zgarishsiz.

### E-4 Blok yakuni (`/t/:slug/test/:testCode/done`)
- "Ajoyib! 1-blok tugadi 🎉" (emoji faqat shu ekranda, o'quvchi motivatsiyasi uchun).
- Qolgan bloklar va taxminiy vaqt.
- Tugmalar: **Davom etish** · "Keyinroq davom ettiraman" (havolani eslatib qo'yadi).

### E-5 Yakuniy ekran (`/t/:slug/finish`)
- "Rahmat! Barcha savollarga javob berding."
- "Natijang tahlil qilinmoqda" + yengil animatsiya.
- Ruxsat berilgan bo'lsa 10–30 s ichida `result` sahifasiga o'tish tugmasi faollashadi.
- Ruxsat berilmagan bo'lsa: "Natijalar maktab psixologiga yuboriladi."

### E-6 Qisqa natija (`/t/:slug/result`)
- Tip kartasi: harflar (gradient firuza emblema ustida), o'zbekcha nom, qisqa tavsif.
- Kuchli tomonlar ro'yxati (mavjud bo'lsa).
- Mos yo'nalishlar — chip ro'yxati (mavjud bo'lsa).
- Pastda doim ko'rinadigan eslatma: "AI tashxis qo'ymaydi" (CLAUDE.md 6-qoida).
- **Diagramma/foiz/progress-bar ATAYLAB YO'Q** (P45, `docs/10` §9.8) — `GET /sessions/result`
  ball yoki o'lcham qaytarmaydi, mavjud bo'lmagan ma'lumotni "chizib qo'yish" soxta xulosa
  bo'lardi. Uch holat, uchalasi ham "kartasiz", qizil xato ko'rinishisiz (odatiy oqim):
  - **Tayyor** — to'liq natija kartasi yuqoridagi tavsif bilan.
  - **Tayyorlanmoqda** (`202`) — aylanuvchi girih nishoni + "Qayta urinish" tugmasi.
  - **Ko'rsatilmaydi** (`403`, `App:ShowResultToStudent=false` — **standart sozlama**, xato
    EMAS) — xushmuomala tushuntirish: "Natijalar maktab psixologiga yuboriladi".
- **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot.

---

## 2a. Ommaviy tanishtiruv (marketing) sahifalari — P45

Auditoriya — maktab rahbarlari, psixologlar, ota-onalar (o'quvchi EMAS). Bu sahifalar
(`MarketingLayout`, `docs/10` §9.5) test oqimidan (bo'lim 2) ATAYLAB mustaqil — sessiyaga
umuman bog'liq emas va **testni boshlash tugmasi yo'q**: o'quvchi testga faqat maktab
bergan havola (`/t/:slug`) orqali kiradi. Barcha CTA `/aloqa` yoki `/metodika`ga olib boradi.

Umumiy chrome: sticky header (sahifa boshida shaffof, pastga siljiganda qog'oz fon +
blur bilan ajraladi) to'liq navigatsiya va mobilda to'liq ekranli menyu bilan; katta,
girih naqshli footer — sayt xaritasi, aloqa kanallari va "AI tashxis qo'ymaydi" eslatmasi
(CLAUDE.md 6-qoida) har sahifada ko'rinadi.

### M-1 Bosh sahifa (`/`)
Bo'limlar ketma-ketligi: kim uchun → qanday ishlaydi → nimani o'lchaydi → maktabga nima
beradi → savol-javob → yakuniy taklif.
- **Hero:** katta va'da sarlavhasi, qisqa tavsif, ikkita CTA (**"Maktabingiz uchun so'rov"**
  → `/aloqa`, **"Metodikani ko'rish"** → `/metodika`), ishonch ro'yxati (test faqat maktab
  havolasi orqali ochiladi / ballash qoidalari oldindan belgilangan / hisobot tashxis emas)
  va namunaviy profil kartasi — aniq izoh bilan: **"bu — sahifani ko'rsatish uchun tuzilgan
  namunaviy ko'rsatkichlar, haqiqiy o'quvchi ma'lumoti emas"**.
- **Qanday ishlaydi:** to'rt qadamli tartiblangan ro'yxat (havola → anketa+test → tahlil →
  hisobot).
- **Nimani o'lchaydi:** 4 blok metodika qisqacha (16 tipli model, Big Five, RIASEC, Aktivlik)
  — atama darajasida, savol/natija namunasisiz.
- **Nega tanlash kerak:** maktabga beriladigan foyda (bitta panel, standartlashtirilgan
  ballash, ishonchlilik tekshiruvi).
- **FAQ:** `<details>`/`<summary>` akkordeoni (JS holatisiz, brauzer o'zi boshqaradi).
- **Yakuniy CTA band.**

### M-2 Metodika (`/metodika`)
- 4 blok metodikaning batafsilroq tavsifi.
- Ishonchlilik darajalari — bo'lim 1dagi rang ohangi bilan mos (`Reliable`/`Questionable`/
  `Unreliable` tushunchalari, lekin bu yerda umumiy tushuntirish, individual natija emas).
- AI tahlil qanday ishlashi (provayder-agnostik, shaxsiy ma'lumot yuborilmasligi — CLAUDE.md
  5-qoida) oddiy tilda tushuntiriladi.
- **16 ta shaxsiyat tipi bo'limi** — har bir tip uchun bitta karta (kod + nom + qisqa tavsif),
  karta M-2.1 sahifasiga olib boradi. Ma'lumot `GET /api/public/type-catalog` dan
  (`docs/07` 1.10-bo'lim); yuklanish/xato/bo'sh holatlari ishlangan. Tiplar guruhlarga
  BO'LINMAYDI va guruh nomlari ishlatilmaydi (`CLAUDE.md` 6a-qoida) — kartalar ohangi faqat
  kodning birinchi harfiga qarab beriladi.

### M-2.1 Shaxsiyat tipi (`/metodika/:kod`)
- Girih emblema + kod, tip nomi va qisqa tavsif (hero); so'ng to'liq tavsif, kuchli tomonlar,
  o'sish yo'nalishlari va kasb yo'nalishlari.
- "Bu tavsif tashxis emas" eslatmasi majburiy (`CLAUDE.md` 6-qoida ruhida): tip qobiliyatni
  yoki kelajakdagi muvaffaqiyatni o'lchamaydi, kasb yo'nalishlari — tavsiya, cheklov emas.
- Alifbo tartibidagi ro'yxat bo'yicha oldingi/keyingi tipga navigatsiya; noma'lum kod uchun
  "topilmadi" ekrani (barcha tiplar ro'yxatiga qaytish havolasi bilan).

### M-3 Biz haqimizda (`/biz-haqimizda`)
- Missiya va yondashuv haqida qisqa matn (jamoa/rivojlanish tarixi emas — MVP bosqichida
  loyiha yosh).

### M-4 Aloqa (`/aloqa`)
- Ikki aloqa kanali karta ko'rinishida: email (`mailto:`) va Telegram kanal havolasi.
- Mumkin bo'lgan mavzular ro'yxati (nima haqida yozish mumkin): pilot loyihasi, metodika,
  hisobot, texnik savol.
- **Forma ATAYLAB YO'Q** — xabar yuborish uchun backend endpointi mavjud emas, ishlamaydigan
  forma foydalanuvchini aldardi. Forma kerak bo'lsa alohida vazifa sifatida (mos endpoint
  bilan birga) qo'shiladi.

---

## 3. Admin panel ekranlari

### A-1 Kirish (`/admin/login`)
Markazda karta: logotip, username, parol, (TOTP kod maydoni faqat kerak bo'lganda), "Kirish".
Xato: "Login yoki parol noto'g'ri" (qaysi biri ekani aytilmaydi). Blok: "Hisob 15 daqiqaga bloklandi".

### A-2 Dashboard (`/admin`)
```
┌───────────┬───────────┬───────────┬───────────┐
│ Maktablar │ O'quvchi  │Yakunlangan│ E'tibor   │
│    42     │   5 820   │   5 104   │   318     │
└───────────┴───────────┴───────────┴───────────┘
┌────────────────────────┬────────────────────────┐
│ Shaxsiyat tiplari      │ Aktivlik darajalari    │
│ (bar chart, 16 ustun)  │ (donut, 5 segment)     │
└────────────────────────┴────────────────────────┘
┌──────────────────────────────────────────────────┐
│ So'nggi sessiyalar (10 qator, holat belgisi bilan)│
└──────────────────────────────────────────────────┘
```
Yuqorida sana oralig'i filtri (7 kun / 30 kun / o'quv yili / ixtiyoriy).
"Tahlil kutilmoqda: 12" — bosilsa `AnalysisFailed`/`Analyzing` filtri bilan sessiyalarga o'tadi.

### A-3 Maktablar (`/admin/schools`)
- Jadval: Nomi · Viloyat/Tuman · Havola · O'quvchi · Yakunlangan · Holat · ⋯
- Havola ustuni: qisqartirilgan URL + 📋 nusxalash + QR ikonkasi.
- QR modal: katta QR + "PNG yuklab olish" (maktabga chop etib berish uchun).
- "Havolani yangilash" — tasdiq modali: "Eski havola ishlamay qoladi. Davom etasizmi?"
- Yaratish/tahrirlash — o'ng tomondan chiquvchi panel (drawer).

### A-4 O'quvchilar (`/admin/students`)
- Filtr paneli: maktab, sinf, holat, tip, aktivlik darajasi, "faqat e'tibor talab qiladiganlar",
  sana oralig'i, qidiruv.
- Jadval: FISH · Maktab · Sinf · Tip · Yetuklik · Aktivlik · Ishonchlilik · Sana · ⋯
- `NeedsAttention` qatorlarida chap chekkada yupqa rangli chiziq (butun qator bo'yalmaydi).
- Yuqori o'ngda: "Excel'ga eksport" (joriy filtr bilan).
- Qatorga bosish → individual profil.

### A-5 Individual profil (`/admin/students/:id`) — **asosiy ekran**
```
┌────────────────────────────────────────────────────────────┐
│ ← Orqaga                                                    │
│ Aliyev Sardor Bekzodovich                                   │
│ 12-maktab, Qo'qon · 9-B sinf · 16 yosh · ♂                  │
│ [Analyzed] [Ishonchli 82]        [PDF] [Qayta tahlil] [⋯]   │
├────────────────────────────────────────────────────────────┤
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐         │
│ │  INTJ    │ │ Yetuklik │ │ Aktivlik │ │  IRA     │         │
│ │Loyihachi │ │  68.4    │ │  65.2    │ │Tadqiqotch│         │
│ └──────────┘ └──────────┘ └──────────┘ └──────────┘         │
├────────────────────────────────────────────────────────────┤
│  16 tip o'qlari          │  Big Five (radar)                │
│  I ███████░░░░░ E  28%   │        O                         │
│  S ░░░░███████ N  72%    │      /   \                       │
│  T ███████░░░░ F  33%    │    A       C                     │
│  P ░░░░██████ J  64%     │      \   /                       │
│                          │        E                         │
├────────────────────────────────────────────────────────────┤
│  Kasb qiziqishlari (RIASEC bar)  │  Aktivlik (4 shkala bar) │
├────────────────────────────────────────────────────────────┤
│  🤖 AI tahlil        Gemini · v1.0 · 31.08.2026 · [Tarix ▾] │
│  ┌ Umumiy xulosa ──────────────────────────────────────┐    │
│  ┌ Shaxsiyat portreti ─────────────────────────────────┐    │
│  ┌ Kuchli tomonlar (4) ────────────────────────────────┐    │
│  ┌ O'sish zonalari (3) ────────────────────────────────┐    │
│  ┌ O'quv uslubi · Motivatsiya · Aktivlik ──────────────┐    │
│  ┌ Kasb yo'nalishlari (4) ─────────────────────────────┐    │
│  ┌ Tavsiyalar: o'quvchiga / o'qituvchiga / ota-onaga ──┐    │
│  └ Izoh: bu tahlil tashxis emas… ─────────────────────┘     │
├────────────────────────────────────────────────────────────┤
│  Sessiyalar tarixi (jadval)                                 │
└────────────────────────────────────────────────────────────┘
```
Holatlar:
- `Analyzing` → AI bloki o'rnida skeleton + "Tahlil tayyorlanmoqda (odatda 1 daqiqa)".
- `AnalysisFailed` → qizil karta: sabab + provider tanlab "Qayta urinish".
- `Unreliable` → hisobot ustida sariq banner: "Javoblar juda tez berilgan, natija ishonchsiz
  bo'lishi mumkin. Qayta topshirish tavsiya etiladi."

### A-6 Sessiyalar (`/admin/assessments`)
Ro'yxat + holat filtri. Detal sahifasi profil bilan bir xil, qo'shimcha "Xom javoblar"
bo'limi (savol · javob · vaqt) — audit uchun.

### A-7 AI sozlamalari (`/admin/ai`)
- 3 ta provider kartasi: nomi, holat belgisi (kalit bor/yo'q, faol/nofaol), model, "Default" belgisi.
- Kalit maydoni: `password` tipi, saqlangandan keyin `AIza••••7f2b` ko'rinishida.
- "Aloqani tekshirish" → yashil "Ishlayapti · 640 ms" yoki qizil xato matni.
- Fallback tartibini drag-and-drop bilan o'zgartirish.
- Pastda: joriy oy bo'yicha chaqiruvlar, tokenlar, taxminiy xarajat.
- Promptlar bo'limi: versiyalar ro'yxati, faol versiya, ko'rish (faqat o'qish MVP'da).

### A-8 Test katalogi va anketa konstruktori (`/admin/catalog`)

**Ro'yxat ekrani:** test kartalari ikki guruhda —

- **Tizim metodikalari** (4 ta, qulf ikonkasi bilan): 16 tipli model, Big Five, RIASEC, Aktivlik.
  Kartada: savol soni, versiya, faollik toggle'i. "Savollarni ko'rish" → jadval, matn inline
  tahrirlanadi; `Scale`/`Direction` ustunlari kulrang va qulflangan, ustiga borilganda tooltip:
  "Bu qiymatlar ball hisobiga ta'sir qiladi va o'zgartirilmaydi".
- **Mening anketalarim:** superadmin yaratganlari. Har kartada holat chipi
  (`Qoralama` / `Nashr etilgan` / `Arxiv`), savol va shkala soni, taxminiy vaqt.
  Yuqori o'ngda **"Yangi anketa"** tugmasi.

**Konstruktor** (`/admin/catalog/tests/:id/edit`) — chapda tahrirlash, o'ngda jonli ko'rish:

```
┌──────────────────────────────────┬────────────────────────┐
│ 1  Anketa ma'lumoti              │  KO'RINISH (preview)   │
│    Nomi, kodi, tavsifi, tartib   │  ┌──────────────────┐  │
│    Sahifadagi savollar: 10       │  │ 3. Yangi vazifa  │  │
│                                  │  │    boshlaganda…  │  │
│ 2  Shkalalar               [+]   │  │  ( ) ( ) ( ) …   │  │
│    STRESS   Stressga munosabat ⋯ │  └──────────────────┘  │
│    SUPPORT  Qo'llab-quvvatlash ⋯ │                        │
│    → talqin oraliqlari: 0–33 Past│  Vaqt: ~6 daqiqa       │
│                                  │  Savol: 16 · Shkala: 2 │
│ 3  Savollar                [+]   │                        │
│    # Matn          Shkala  ± ⋯   │  ⚠ SUPPORT shkalasida  │
│    1 …             STRESS  +     │     faqat 2 savol      │
│    2 …             SUPPORT −     │                        │
├──────────────────────────────────┴────────────────────────┤
│  Saqlash (qoralama)              Tekshirish va nashr qilish│
└───────────────────────────────────────────────────────────┘
```

Tafsilotlar:
- **Savol qatori:** matn maydoni, shkala select'i, yo'nalish toggle'i (`+` to'g'ri / `−` teskari,
  yonida tushuntirish tooltip'i), og'irlik (default 1.0, kengaytirilgan rejimda), sudrab tartiblash.
- **Yo'nalish tushuntirishi** (birinchi marta ko'rsatiladi): "Teskari savol — javob qiymati
  aylantiriladi. Masalan `Ishni oxirgi kunga qoldiraman` savoli intizom shkalasida teskari."
- **Shkala tahriri** drawer'da: kodi, nomi, tavsifi va talqin oraliqlari (jadval:
  `0–33 Past`, `34–66 O'rtacha`, `67–100 Yuqori`) — bo'shliq yoki ustma-ustlik bo'lsa qizil xato.
- **Nashr qilish** bosilganda validatsiya paneli ochiladi: har xato qatoriga bosilsa
  tegishli savol/shkalaga scroll qiladi. Hammasi yashil bo'lsa "Nashr qilish" faollashadi.
- **Nashr etilgan anketaga** savol qo'shilsa: "Versiya 2 ga oshadi. Eski natijalar
  o'zgarmaydi va hisobotda 1-versiya deb belgilanadi." — tasdiq dialogi.
- **Faollik toggle:** "Yangi sessiyalarga qo'shilsinmi?" — boshlangan sessiyalarga ta'sir qilmaydi.
- **Nusxalash:** "Nusxa olish" → yangi `Qoralama`, kodga `-v2` qo'shiladi.
- **O'chirish:** ishlatilmagan bo'lsa o'chadi; ishlatilgan bo'lsa faqat "Arxivlash" taklif qilinadi.
- **Umumiy vaqt ogohlantirishi:** barcha faol testlar yig'indisi 40 daqiqadan oshsa,
  ro'yxat ekrani tepasida sariq banner: "Sessiya juda uzun — o'quvchilar tashlab ketishi mumkin".

**Tarmoqlanuvchi so'rovnoma (P52, `docs/18`, haqiqiy joriy amalga oshirish
`CatalogTestDetailPage.tsx` — yuqoridagi drawer sxemasi emas, Card-larga bo'lingan sahifa):**
- **Bo'limlar** bo'limi (`SectionsSection`) — savollar bo'limi tepasida, faqat `Survey`
  rejimidagi anketalarda; `Scored`da o'rniga bitta izoh qatori ("faqat so'rovnoma rejimida
  ishlaydi"). Har bo'lim qatorida: nomi (kodi), savollar soni, "Shartli"/"Har doim ko'rinadi"
  yorlig'i, tartiblash/tahrirlash/o'chirish tugmalari.
- **Savol oynasi** (`QuestionEditorDialog`) — `Survey` anketada savol turi tanlanganda
  (`Qisqa matn`/`Uzun matn`/`Bir nechta variant`/`Telefon`) turga mos maydonlar (placeholder,
  belgi chegarasi, kiritish shabloni, tanlov soni chegarasi, variantlar muharriri) ochiladi;
  shkala/yo'nalish/og'irlik esa bu turlarda umuman ko'rinmaydi (ular ballanmaydi). Har bir
  savol/bo'limda ixtiyoriy **"Ko'rsatish sharti"**: manba savol (faqat oldinroqdagi), operator,
  qiymat — va pastda o'zbekcha gap ko'rinishida jonli oldindan ko'rish, masalan: «**Q1_6**
  savoliga javob **"Ha, Intellect o'quv markazida o'qiyman"** bo'lsa ko'rsatilsin.»
- **Oqim ko'rinishi** (`BranchingPreview`, faqat o'qish) — bo'limlar ro'yxati tartib bo'yicha,
  har biri savollar soni va ko'rsatish sharti (yoki "Har doim ko'rinadi") bilan.
- Import (JSON) — `docs/examples/sorovnoma-intellect.json` namunasi (5 bo'lim, 25 savol)
  xatosiz o'qiladi; nashr qilishdan oldingi 10 ta yangi tekshiruv kodi (`VISIBILITY_*`,
  `QUESTION_TYPE_NOT_SCORABLE`, `SECTION_EMPTY` va h.k.) o'zbekcha matn bilan ko'rsatiladi.

### A-9 Audit log (`/admin/audit`)
Filtr: harakat turi, obyekt, sana. Qator kengaytirilsa `before/after` JSON diff ko'rinadi.

---

## 4. Erishimlilik (a11y)

- Har savol `fieldset` + `legend`, javoblar `radio` guruh.
- Tab bilan to'liq navigatsiya; fokus halqasi ko'rinadigan.
- Kontrast ≥ 4.5:1 (matn), ≥ 3:1 (katta matn va ikonkalar).
- Diagrammalar yonida raqamli qiymat — faqat rangga tayanmaydi.
- Har diagrammaning matnli alternativi (`aria-label` yoki jadval ko'rinishi).
- Xato xabarlari `aria-live="polite"` bilan e'lon qilinadi.
- Sensor nishonlar ≥ 44×44 px.
- `prefers-reduced-motion` — animatsiyalar o'chadi.

---

## 5. Matn uslubi (UX copy)

| Holat | Yomon | Yaxshi |
|-------|-------|--------|
| Sessiya tugagan | "Token invalid" | "Sessiya muddati tugagan. Qaytadan boshlaymizmi?" |
| Bo'sh javob | "Xato: majburiy maydon" | "Bu savolga javob berishni unutdik" |
| Past aktivlik (adminda) | "Passiv o'quvchi" | "Faollik past — suhbat foydali bo'lishi mumkin" |
| Past ball | "Yomon natija" | "Bu soha hozircha kuchli emas — o'sish uchun joy bor" |
| Yuklanmoqda | "Loading..." | "Natijalaring tayyorlanmoqda…" |

Har doim **"sen"** shakli o'quvchi bilan, **"siz"** shakli admin panelida.
