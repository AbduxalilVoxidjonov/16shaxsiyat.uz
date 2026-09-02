# 11 — UX va ekranlar

## 0. Brend

Foydalanuvchiga ko'rinadigan nom — **Shaxsiyat** (`16shaxsiyat.uz`). Sarlavha, logotip, sahifa
`<title>` va PDF kolontitulida shu nom turadi. `StudentRoadMap` hech qaysi ekranda ko'rinmaydi.

Tag-line (landing sahifada, ixtiyoriy): *"O'zingni bilib, yo'lingni tanla"*.

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
│  11. Yangi odamlar bilan tanishish menga     │
│      oson.                                   │
│                                              │
│  ( Umuman qo'shilmayman )                    │
│  ( Qo'shilmayman        )                    │
│  ( Bilmadim             )                    │
│  ( Qo'shilaman          )   ✓                │
│  ( To'liq qo'shilaman   )                    │
│                                              │
│  12. …                                       │
├──────────────────────────────────────────────┤
│  ← Orqaga                    Keyingi →       │  ← sticky footer
└──────────────────────────────────────────────┘
```
- Javob tanlanganda yumshoq animatsiya, avtomatik keyingi savolga scroll (oxirgi savolda emas).
- To'ldirilmagan savol bo'lsa "Keyingi" bosilganda birinchi bo'shiga scroll + qizil ramka.
- Yuqorida kichkina "Saqlandi ✓" indikatori (autosave holati).
- Offline banner: "Internet yo'q — javoblaringiz saqlanmoqda, ulanish tiklanganda yuboriladi".

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
- Tip kartasi: harflar, o'zbekcha nom, 2 jumlalik tavsif.
- 3 ta kuchli tomon.
- 3 ta mos yo'nalish.
- Pastda: "Bu tashxis emas — bu hozirgi holating surati. To'liq tahlil maktabingizda."
- **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot.

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
