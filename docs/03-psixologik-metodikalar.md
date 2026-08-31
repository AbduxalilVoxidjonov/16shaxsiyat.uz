# 03 — Psixologik metodikalar va scoring

Bu hujjat **backend uchun spetsifikatsiya**: savol banki qanday tuzilishi va ball qanday
hisoblanishi. Har bir formula deterministik — bir xil javoblar har doim bir xil natija beradi.

---

## 0. Litsenziya va manba (muhim!)

Rasmiy **MBTI®**, **NEO-PI-R**, **Strong Interest Inventory®** — tijorat mulki, ruxsatsiz
ishlatilmaydi. Shuning uchun platformada **ochiq (public domain) analoglar** ishlatiladi:

| Bizdagi test | Ochiq metodika asosi | Litsenziya |
|--------------|----------------------|------------|
| 16 Personality (`MBTI16`) | Yung tipologiyasi asosidagi 4 dixotomiya shkalasi (OEJTS/Open-Source 16 tip yondashuvi) | Public domain |
| Big Five (`BIG5`) | IPIP-50 (International Personality Item Pool) | Public domain |
| Kasb qiziqishlari (`RIASEC`) | IPIP RIASEC / O*NET Interest Profiler yondashuvi | Public domain |
| Aktivlik va motivatsiya (`ACTIVITY`) | O'zimiz tuzgan anketa (ta'lim motivatsiyasi, o'z-o'zini boshqarish, ijtimoiy faollik) | Bizniki |

UI'da hech qachon "MBTI" so'zi ishlatilmaydi — **"16 tipli shaxsiyat modeli"** deyiladi.
Kodda `TestCode = MBTI16` faqat ichki identifikator.

---

## 1. Umumiy tuzilma

Har bir savol quyidagilarga ega:

| Maydon | Ma'nosi |
|--------|---------|
| `Code` | Unikal kod, masalan `BIG5-Q17` |
| `TestDefinitionId` | Qaysi testga tegishli |
| `DisplayOrder` | Ko'rsatish tartibi |
| `Text` (uz/ru/en) | Savol matni |
| `QuestionType` | `Likert5`, `Likert7`, `Binary`, `ForcedChoice`, `SingleChoice` |
| `Scale` | Qaysi o'q/omilga ishlaydi. **Yagona ro'yxat** (`docs/05` bilan bir xil): `EI`, `SN`, `TF`, `JP` (16 tip) · `O`, `C`, `E`, `A`, `N` (Big Five) · `R`, `I`, `ART`, `SOC`, `ENT`, `CONV` (RIASEC) · `MOT`, `SELF`, `SOCA`, `ENG` (Aktivlik) |
| `ScaleDirection` | `+1` yoki `-1` (teskari hisoblanadigan savol) |
| `Weight` | Default `1.0` |
| `IsRequired` | Default `true` |

### Likert-5 javob qiymatlari

| Javob | Qiymat |
|-------|--------|
| Umuman qo'shilmayman | 1 |
| Qo'shilmayman | 2 |
| Bilmadim / o'rtacha | 3 |
| Qo'shilaman | 4 |
| To'liq qo'shilaman | 5 |

**Teskari savol (`ScaleDirection = -1`) qiymati:** `v' = (max + min) − v = 6 − v`.

---

## 2. Test 1 — 16 tipli shaxsiyat modeli (`MBTI16`)

### 2.1 Tuzilishi
- **60 savol**, 4 dixotomiya × 15 savol.
- Format: `Likert5`.
- O'qlar: `EI` (Ekstraversiya/Introversiya), `SN` (Sezish/Intuitsiya), `TF` (Fikrlash/His-tuyg'u),
  `JP` (Tartib/Moslashuvchanlik).

### 2.2 Scoring algoritmi

Har bir o'q uchun:

```
axisRaw   = Σ ( direction_i × value_i )   // direction ∈ {+1,-1}, value ∈ [1..5]
axisMin   = Σ (direction_i > 0 ? 1 : -5)
axisMax   = Σ (direction_i > 0 ? 5 : -1)
axisPct   = ( axisRaw − axisMin ) / ( axisMax − axisMin ) × 100     // 0..100
```

`axisPct` — **birinchi qutb foydasiga foiz**. Konvensiya:

| O'q | 0 tomon | 100 tomon |
|-----|---------|-----------|
| `EI` | I (introversiya) | E (ekstraversiya) |
| `SN` | S (sezish) | N (intuitsiya) |
| `TF` | T (fikrlash) | F (his-tuyg'u) |
| `JP` | P (moslashuvchan) | J (tartibli) |

**Harf tanlash:** `axisPct > 50 → ikkinchi qutb harfi`, `< 50 → birinchi qutb harfi`.

**Tie-break (`axisPct == 50`):** aniq qoida — `I`, `S`, `T`, `J` tanlanadi (statistik ko'proq tarqalgan qutb),
va `IsBorderline = true` bayrog'i qo'yiladi. Hisobotda "bu o'qda muvozanat" deb yoziladi.

**Borderline zona:** `45 ≤ axisPct ≤ 55` → `BorderlineAxes` ro'yxatiga qo'shiladi. AI promptiga
uzatiladi, chunki bu holatda tipni qat'iy aytish noto'g'ri.

### 2.3 Natija obyekti

```json
{
  "resultCode": "INTJ",
  "axes": {
    "EI": { "pct": 28.3, "letter": "I", "borderline": false },
    "SN": { "pct": 71.6, "letter": "N", "borderline": false },
    "TF": { "pct": 33.3, "letter": "T", "borderline": false },
    "JP": { "pct": 64.1, "letter": "J", "borderline": false }
  },
  "borderlineAxes": [],
  "typeName": "Strateg",
  "typeShort": "Uzoqni ko'zlaydigan, mustaqil rejalashtiruvchi"
}
```

16 tipning o'zbekcha nomlari `TypeCatalog` jadvalida seed qilinadi (INTJ — Strateg, ENFP — Ilhomlantiruvchi, ...).

### 2.4 Savol namunalari (seed uchun format)

| Kod | O'q | Yo'nalish | Matn (uz) |
|-----|-----|-----------|-----------|
| MB-Q01 | EI | +1 | Yangi odamlar bilan tanishish menga oson |
| MB-Q02 | EI | −1 | Ko'p odam orasida charchayman, yolg'iz tiklanaman |
| MB-Q11 | SN | +1 | Amaliy tafsilotdan ko'ra umumiy g'oya meni ko'proq qiziqtiradi |
| MB-Q12 | SN | −1 | Aniq faktlar va tajribaga tayanishni afzal ko'raman |
| MB-Q26 | TF | +1 | Qaror qabul qilishda odamlarning his-tuyg'usini birinchi o'ylayman |
| MB-Q27 | TF | −1 | Mantiq va adolat his-tuyg'udan ustun turadi |
| MB-Q41 | JP | +1 | Rejasiz kun menga noqulaylik tug'diradi |
| MB-Q42 | JP | −1 | Ishlarni oxirgi daqiqada qilishga odatlanganman |

> To'liq 60 savol `prompts/05-seed-mbti16.md` promptida generatsiya qilinadi.

---

## 3. Test 2 — Big Five / OCEAN (`BIG5`)

### 3.1 Tuzilishi
- **50 savol** (IPIP-50), 5 omil × 10 savol, `Likert5`.
- Omillar: `O` (Ochiqlik), `C` (Vijdonlilik), `E` (Ekstraversiya), `A` (Kelishuvchanlik),
  `N` (Emotsional beqarorlik / neyrotizm).

### 3.2 Scoring

```
factorRaw = Σ ( value_i  if direction=+1  else 6 − value_i )    // 10..50
factorPct = ( factorRaw − 10 ) / 40 × 100                        // 0..100
```

**Daraja tasnifi:**

| `factorPct` | Daraja |
|-------------|--------|
| 0–20 | Juda past |
| 21–40 | Past |
| 41–60 | O'rtacha |
| 61–80 | Yuqori |
| 81–100 | Juda yuqori |

> `N` omili hisobotda **"emotsional barqarorlik"** sifatida teskari ko'rsatiladi:
> `StabilityPct = 100 − N_pct`. O'quvchiga "neyrotizming yuqori" deyilmaydi.

### 3.3 Psixologik yetuklik indeksi (`MaturityIndex`)

Loyihaning o'ziga xos ko'rsatkichi — Big Five va Aktivlik testidan yig'iladi:

```
MaturityIndex = 0.30 × C_pct
              + 0.25 × StabilityPct
              + 0.20 × A_pct
              + 0.15 × SelfRegulationPct     // ACTIVITY testidan
              + 0.10 × O_pct
```

Natija 0–100. Talqin:

| Ball | Talqin |
|------|--------|
| 0–35 | Shakllanish bosqichida — qo'llab-quvvatlash kerak |
| 36–55 | O'rtacha — ayrim sohalarda mustahkamlash |
| 56–75 | Yaxshi — mustaqil ishlashga tayyor |
| 76–100 | Yuqori — yetakchilik va murakkab vazifalarga tayyor |

> **Muhim:** bu ko'rsatkich ilmiy standart emas, platformaning ichki agregat indeksi.
> Hisobotda shunday izohlanadi va hech qachon "aql darajasi" deb atalmaydi.

### 3.4 Natija obyekti

```json
{
  "factors": {
    "O": { "raw": 38, "pct": 70.0, "level": "Yuqori" },
    "C": { "raw": 41, "pct": 77.5, "level": "Yuqori" },
    "E": { "raw": 24, "pct": 35.0, "level": "Past" },
    "A": { "raw": 35, "pct": 62.5, "level": "Yuqori" },
    "N": { "raw": 22, "pct": 30.0, "level": "Past" }
  },
  "stabilityPct": 70.0,
  "maturityIndex": 68.4,
  "maturityLevel": "Yaxshi"
}
```

---

## 4. Test 3 — Kasb qiziqishlari, RIASEC (`RIASEC`)

### 4.1 Tuzilishi
- **48 savol**, 6 tip × 8 savol, `Likert5` ("Bu ish menga qiziq").
- Tiplar (qavsda `scale` kodi): Realistik `R` (amaliy/texnik), Tadqiqotchi `I`, Artistik `ART`,
  Ijtimoiy `SOC`, Tadbirkor `ENT`, Konvensional `CONV` (tartibli/hujjat).

> Matnda qisqalik uchun `R-I-A-S-E-C` harflari ishlatiladi, lekin **bazadagi `scale` qiymatlari**
> `R`, `I`, `ART`, `SOC`, `ENT`, `CONV` — `A`/`S`/`E`/`C` Big Five bilan to'qnashmasligi uchun.

### 4.2 Scoring

```
typeRaw = Σ value_i            // 8..40  (teskari savol yo'q)
typePct = (typeRaw − 8) / 32 × 100
```

**Holland kodi:** eng yuqori 3 tip, kamayish tartibida → masalan `IRA`.

**Tie-break:** teng bo'lsa `R → I → A → S → E → C` alifbo-tartib prioriteti.

**Differensiatsiya (`Differentiation`)** = `max(typePct) − min(typePct)`.
`< 20` bo'lsa — qiziqishlar hali aniq shakllanmagan, hisobotda shunday yoziladi.

**Konsistentlik:** Holland olti burchagida qo'shni tiplar (R-I-A-S-E-C aylana) mos keladi.
Birinchi ikki harf qo'shni bo'lsa `High`, bitta oralatib `Medium`, qarama-qarshi bo'lsa `Low`.

### 4.3 Kasb yo'nalishlari xaritasi

`CareerMap` jadvali: `HollandCode` (2 harf) → yo'nalishlar ro'yxati (O'zbekiston sharoitiga moslangan).

| Kod | Yo'nalishlar |
|-----|--------------|
| IR | Muhandislik, IT, ma'lumot tahlili, tibbiyot texnikasi |
| SA | Pedagogika, psixologiya, ijtimoiy ish, jurnalistika |
| EC | Biznes boshqaruv, moliya, logistika, savdo |
| AI | Dizayn, arxitektura, media, kontent ishlab chiqarish |
| ... | ... |

---

## 5. Test 4 — Aktivlik va motivatsiya (`ACTIVITY`)

### 5.1 Tuzilishi
- **32 savol**, 4 shkala × 8 savol, `Likert5`.

| Shkala | Kod | Nima o'lchaydi |
|--------|-----|----------------|
| Ta'lim motivatsiyasi | `MOT` | O'qishga ichki qiziqish, maqsad aniqligi |
| O'z-o'zini boshqarish | `SELF` | Vaqtni rejalash, intizom, e'tiborni ushlab turish |
| Ijtimoiy faollik | `SOCA` | Jamoa ishi, tashabbus, to'garak/tadbirlar |
| Band bo'lish darajasi | `ENG` | Darsdan tashqari mashg'ulot, kitob, sport, hobbi |

### 5.2 Scoring

```
scaleRaw = Σ ( value_i  if direction=+1  else 6 − value_i )   // 8..40
scalePct = (scaleRaw − 8) / 32 × 100

ActivityIndex = 0.30×MOT + 0.30×SELF + 0.20×SOCA + 0.20×ENG
```

**Aktivlik darajasi:**

| `ActivityIndex` | Daraja | Kod |
|-----------------|--------|-----|
| 0–30 | Passiv — diqqat talab qiladi | `Passive` |
| 31–50 | Past faol | `LowActive` |
| 51–70 | O'rtacha faol | `Moderate` |
| 71–85 | Faol | `Active` |
| 86–100 | Juda faol | `HighlyActive` |

> `ActivityIndex < 31` **avtomatik bayroq** (`NeedsAttention = true`) — superadmin ro'yxatida
> alohida belgi bilan chiqadi. Bu "muammo" emas, "suhbat kerak" signali.

---

## 6. Superadmin yaratgan anketalar — `SUM` strategiyasi

Superadmin panel orqali kod yozmasdan yangi anketa yaratishi mumkin (`TestDefinition.Kind = Custom`).
Bunday testlar uchun yagona universal ball hisobi ishlatiladi — `SUM`.

### 6.1 Tuzilishi

Superadmin belgilaydi:

| Nima | Misol |
|------|-------|
| Test nomi va tavsifi | "Stressga chidamlilik anketasi" |
| **Shkalalar** (1–8 ta) | `STRESS` — "Stressga munosabat", `SUPPORT` — "Ijtimoiy qo'llab-quvvatlanish" |
| Har shkala uchun talqin oraliqlari | 0–33 "Past", 34–66 "O'rtacha", 67–100 "Yuqori" |
| Savollar | matn, `Scale` (qaysi shkalaga), `Direction` (+1/−1), `Weight` |
| Javob turi | `Likert5` (default) yoki `Likert7` |

### 6.2 Ball hisobi

Har shkala uchun:

```
scaleRaw = Σ ( direction_i = +1 ? v_i : (max + min − v_i) ) × weight_i
scaleMin = Σ ( min × weight_i )
scaleMax = Σ ( max × weight_i )
scalePct = ( scaleRaw − scaleMin ) / ( scaleMax − scaleMin ) × 100
```

`Likert5` uchun `min = 1`, `max = 5`. Daraja `InterpretationBands` dan olinadi.
`ResultCode` bo'lmaydi (tip kodi yo'q) — natija faqat shkala ballari va darajalari.

### 6.3 Nashr qilish validatsiyasi

`Publish` bosilganda tekshiriladi:

- kamida 1 ta shkala;
- **har shkalada kamida 4 savol** (kamroqda ball ishonchsiz);
- har savolda `Scale` belgilangan va shkala shu testga tegishli;
- shkala oraliqlari 0–100 ni bo'shliqsiz qoplaydi va ustma-ust tushmaydi;
- test nomi va kodi unikal.

Shartlardan biri bajarilmasa nashr qilinmaydi va aniq xato ro'yxati qaytariladi.

### 6.4 Cheklovlar

- `Custom` testlar **AI tahliliga qo'shiladi**, lekin alohida bo'lim sifatida: prompt'ga
  `customTests: [{ name, scales: [{name, pct, level}] }]` ko'rinishida uzatiladi.
  AI ularni umumiy portretga bog'laydi, lekin tip yoki kasb xulosasi ilmiy metodikalarga tayanadi.
- `MaturityIndex` va `ActivityIndex` formulalariga `Custom` testlar **ta'sir qilmaydi**.
- `Custom` test o'chirilsa (arxivlansa), eski natijalar profilda ko'rinishda qoladi.

---

## 7. Ishonchlilik indeksi (`ReliabilityScore`) — barcha testlar uchun

Sessiya darajasida 0–100 ball, 100 dan quyidagi jarimalar ayriladi:

| Signal | Aniqlash | Jarima |
|--------|----------|--------|
| Juda tez javoblar | `DurationMs < 900` bo'lgan javoblar ulushi `p` | `min(40, p × 100 × 0.8)` |
| Straight-lining | Ketma-ket ≥ 12 ta bir xil qiymat | har bloki uchun 10, max 30 |
| Teskari savollar ziddiyati | `+1` va `−1` juftliklarida o'rtacha mos kelmaslik `d` (0..1) | `d × 25` |
| To'liq bir xil javob (barchasi 3) | Barcha javoblar bir xil | 50 |
| Umumiy vaqt juda qisqa | Sessiya < 6 daqiqa | 20 |

```
ReliabilityScore = clamp(0, 100, 100 − Σ penalties)
```

| Ball | Bayroq |
|------|--------|
| ≥ 70 | `Reliable` |
| 40–69 | `Questionable` — hisobotda ogohlantirish |
| < 40 | `Unreliable` — hisobot yuqorisida qizil banner, qayta topshirish tavsiyasi |

---

## 8. Scoring engine — texnik shartnoma

```csharp
public interface IScoringStrategy
{
    string StrategyCode { get; }                // "MBTI16", "BIG5", "RIASEC", "ACTIVITY", "SUM"
    ScoringResult Score(ScoringInput input);
}

public sealed record ScoringInput(
    IReadOnlyList<QuestionMeta> Questions,      // Scale, Direction, Weight
    IReadOnlyDictionary<Guid, int> Answers,     // QuestionId -> raw value
    IReadOnlyDictionary<Guid, int> Durations,   // QuestionId -> ms
    StudentContext Student);                    // yosh, sinf, jins

public sealed record ScoringResult(
    string ResultCode,                          // "INTJ" | "IRA" | null
    IReadOnlyDictionary<string, double> RawScores,
    IReadOnlyDictionary<string, double> NormalizedScores,
    IReadOnlyDictionary<string, string> Levels,
    double? CompositeIndex,                     // MaturityIndex / ActivityIndex
    IReadOnlyList<string> Flags,                // "Borderline:EI", "LowDifferentiation"
    string InterpretationKey);                  // lokalizatsiya kaliti
```

**Qoidalar:**
1. Strategiyalar **sof funksiya** — DB'ga murojaat qilmaydi, IO yo'q, DateTime.Now ishlatmaydi.
2. Barcha koeffitsiyentlar `ScoringConstants` da nomlangan konstantalar sifatida, sehrli raqam yo'q.
3. Har strategiya uchun **oltin test** (`golden test`): tayyor javoblar to'plami → kutilgan natija.
4. Formula o'zgarsa `TestDefinition.Version` oshiriladi; eski natijalar qayta hisoblanmaydi,
   `TestResult.ScoringVersion` da qaysi versiya ishlatilgani qoladi.

---

## 9. Umumiy vaqt byudjeti

| Test | Savol | Taxminiy vaqt |
|------|-------|---------------|
| Anketa | 8 maydon | 2 daq |
| 16 tip (`MBTI16`) | 60 | 9 daq |
| Big Five (`BIG5`) | 50 | 8 daq |
| RIASEC | 48 | 7 daq |
| Aktivlik (`ACTIVITY`) | 32 | 5 daq |
| **Jami** | **190** | **~31 daq** |

Superadmin qo'shgan har bir `Custom` anketa shu jadvalga qo'shiladi — nashr qilishda tizim
taxminiy vaqtni avtomatik hisoblaydi (`savol soni × 9 soniya`) va agar sessiyaning umumiy
vaqti **40 daqiqadan** oshsa ogohlantiradi.

Uzun sessiya — drop-off riski. Shuning uchun: progress bar, blok oxirida "tanaffus" ekrani,
autosave, istalgan vaqt qaytib davom ettirish.
