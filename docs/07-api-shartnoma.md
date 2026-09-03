# 07 — API shartnomasi

**Baza:** `/api` · **Format:** JSON (UTF-8) · **Xato:** `application/problem+json` (06-hujjat, 6-bo'lim)
**Versiya:** `/api/v1/...` (MVP'da `v1` majburiy emas, lekin marshrutlarga qo'shib qo'yiladi)

Ikkita autentifikatsiya sxemasi:

| Sxema | Header | Kim uchun |
|-------|--------|-----------|
| `Bearer` (JWT) | `Authorization: Bearer <access_token>` | Superadmin (`/api/admin/*`) |
| `SessionToken` | `X-Session-Token: <token>` | O'quvchi (`/api/public/*`) |

---

## 1. Ommaviy (o'quvchi) endpointlari

### 1.1 `GET /api/public/schools/{slug}?k={accessToken}`
Maktab havolasi to'g'riligini tekshirish va boshlanish ekranini to'ldirish.

**200**
```json
{
  "schoolId": "…", "name": "12-son umumiy o'rta ta'lim maktabi",
  "region": "Farg'ona", "district": "Qo'qon",
  "requiresAccessCode": false,
  "tests": [
    { "code": "MBTI16", "name": "16 tipli shaxsiyat modeli", "questionCount": 60, "estimatedMinutes": 9, "order": 1 },
    { "code": "BIG5", "name": "Shaxsiyatning 5 omili", "questionCount": 50, "estimatedMinutes": 8, "order": 2 },
    { "code": "RIASEC", "name": "Kasb qiziqishlari", "questionCount": 48, "estimatedMinutes": 7, "order": 3 },
    { "code": "ACTIVITY", "name": "Aktivlik va motivatsiya", "questionCount": 32, "estimatedMinutes": 5, "order": 4 }
  ],
  "totalEstimatedMinutes": 31,
  "consentText": "…",
  "programs": [
    { "code": "PERSONALITY_PROFILE", "nameUz": "Shaxsiyat profili", "descriptionUz": "…",
      "testCount": 4, "questionCount": 190, "estimatedMinutes": 31, "hasPersonalityBattery": true },
    { "code": "CAREER_SURVEY", "nameUz": "Kasb so'rovnomasi", "descriptionUz": null,
      "testCount": 1, "questionCount": 20, "estimatedMinutes": 5, "hasPersonalityBattery": false }
  ]
}
```
`programs[]` — maktab uchun mavjud dasturlar (`Visibility = Public` yoki biriktirilgan;
bir nechta bo'lsa o'quvchi tanlaydi, `code` → `POST /sessions` `programCode`).

**404** `NOT_FOUND` · **410** `SCHOOL_INACTIVE`

---

### 1.2 `POST /api/public/sessions`
Anketa + sessiya ochish.

```json
{
  "slug": "12-maktab-kokand",
  "accessToken": "…",
  "accessCode": "482913",
  "fullName": "Aliyev Sardor Bekzodovich",
  "birthDate": "2010-04-17",
  "gender": "Male",
  "grade": 9,
  "classLetter": "B",
  "phone": "+998901234567",
  "parentPhone": "+998911112233",
  "email": null,
  "consentAccepted": true,
  "languageCode": "uz"
}
```

**201**
```json
{
  "sessionToken": "8f3a…64",
  "assessmentId": "…",
  "status": "Draft",
  "expiresAt": "2026-09-07T10:12:00Z",
  "resumed": false,
  "tests": [ { "code": "MBTI16", "status": "NotStarted", "answered": 0, "total": 60, "order": 1 }, … ]
}
```

Qoidalar:
- Dublikat (maktab + normalizatsiyalangan FISH + tug'ilgan sana) topilsa:
  - tugallanmagan sessiya bor → **o'sha sessiya qaytariladi**, `resumed: true`;
  - 90 kun ichida yakunlangan sessiya bor → **409** `DUPLICATE_ASSESSMENT`.
- `consentAccepted != true` → 400.
- Rate limit: IP bo'yicha soatiga 10; maktab kunlik limiti.

**400** `VALIDATION_ERROR` · **409** `DUPLICATE_ASSESSMENT` · **429** `RATE_LIMITED`

---

### 1.3 `GET /api/public/sessions/me`
`X-Session-Token` bo'yicha holatni tiklash (sahifa yangilanganda).

**200**
```json
{
  "assessmentId": "…", "status": "InProgress",
  "student": { "firstNameShort": "Sardor", "grade": 9 },
  "expiresAt": "…",
  "currentTestCode": "BIG5",
  "tests": [
    { "code": "MBTI16", "status": "Completed", "answered": 60, "total": 60, "order": 1 },
    { "code": "BIG5",   "status": "InProgress","answered": 18, "total": 50, "order": 2 },
    { "code": "RIASEC", "status": "Locked",    "answered": 0,  "total": 48, "order": 3 },
    { "code": "ACTIVITY","status":"Locked",    "answered": 0,  "total": 32, "order": 4 }
  ],
  "progressPercent": 41,
  "hasPersonalityBattery": true
}
```
**410** `SESSION_EXPIRED`

#### `hasPersonalityBattery` (bool) — 1.1 va 1.3 da

Ushbu sessiyaning dasturida (1.1 da — ushbu dasturda) **ilmiy shaxsiyat batareyasi** bormi.
`false` bo'lsa shaxsiyat tipi HECH QACHON hisoblanmaydi — mijoz natija ekranini taklif
qilmasligi va shaxsiyat widget'larini render qilmasligi kerak (1.9 dagi kabi "tip aniqlanmadi"
holati; **bo'sh diagramma yoki `0` ball KO'RSATILMAYDI** — `docs/06` qarorlar jurnali,
2026-09-02: "ma'lumot yo'q" `0` bilan almashtirilmaydi).

Bayroq **domen qoidasi** bilan hisoblanadi — `Domain.Catalog.PersonalityBattery`:

> anketa `Kind = Standard` (ilmiy metodika, seed'dan keladi) **va** `ScoringMode = Scored`
> bo'lsa batareyaga kiradi; dastur/sessiyada bunday anketa kamida bittasi bo'lsa bayroq `true`.

Mijoz bu savolga **metodika kodi bo'yicha javob bermasligi kerak.** `"MBTI16"` kabi kodni
qidirish ikki holatda jimgina buziladi: (1) `Custom` dastur boshqa kodli metodika ishlatsa;
(2) kod o'zgarsa/versiyalansa. Ikkala javobda ham bayroq bir xil qoidadan hisoblanadi, ya'ni
ular hech qachon bir-biriga zid bo'lmaydi.

---

### 1.4 `POST /api/public/sessions/tests/{testCode}/start`
Testni boshlash (aralashtirish tartibi shu yerda qat'iylashadi).

**200** `{ "testCode": "BIG5", "status": "InProgress", "pageSize": 10, "totalPages": 5 }`
**409** `TEST_NOT_UNLOCKED`

---

### 1.5 `GET /api/public/sessions/tests/{testCode}/questions?page=1`

**200**
```json
{
  "testCode": "BIG5", "page": 1, "pageSize": 10, "totalPages": 5, "totalQuestions": 50,
  "scaleLabels": [
    { "value": 1, "label": "Umuman qo'shilmayman" },
    { "value": 2, "label": "Qo'shilmayman" },
    { "value": 3, "label": "Bilmadim" },
    { "value": 4, "label": "Qo'shilaman" },
    { "value": 5, "label": "To'liq qo'shilaman" }
  ],
  "questions": [
    { "id": "…", "code": "B5-Q01", "order": 1, "text": "Yangi g'oyalarni sinab ko'rishni yaxshi ko'raman",
      "type": "Likert5", "isRequired": true, "options": null, "currentValue": 4 }
  ]
}
```

> `scale` va `scaleDirection` **hech qachon** frontendga yuborilmaydi.

---

### 1.6 `POST /api/public/sessions/tests/{testCode}/answers`
Paketli, idempotent saqlash (autosave — har 5 s yoki sahifa almashganda).

```json
{ "answers": [
    { "questionId": "…", "value": 4, "durationMs": 3120 },
    { "questionId": "…", "value": 2, "durationMs": 1870 } ] }
```
**200** `{ "savedCount": 2, "answered": 20, "total": 50 }`
**410** `SESSION_EXPIRED`

---

### 1.7 `POST /api/public/sessions/tests/{testCode}/complete`
**200**
```json
{ "testCode": "BIG5", "status": "Completed",
  "nextTestCode": "RIASEC", "allTestsCompleted": false }
```
**400** `VALIDATION_ERROR` (`unansweredCount` bilan)

---

### 1.8 `POST /api/public/sessions/complete`
Barcha testlar tugagach yakuniy tasdiq. Ishonchlilik hisoblanadi, AI navbatga qo'yiladi.

**200**
```json
{ "status": "Analyzing", "message": "Natijalaringiz qayta ishlanmoqda.",
  "showResultToStudent": true, "resultAvailableAt": null }
```

---

### 1.9 `GET /api/public/sessions/result`
O'quvchiga **qisqartirilgan** natija (superadmin sozlamasi yoqilgan bo'lsa).

**200**
```json
{
  "personalityType": "INTJ", "typeName": "Loyihachi",
  "shortDescription": "Uzoqni ko'zlaydigan, mustaqil rejalashtiruvchi",
  "topStrengths": ["Tahliliy fikrlash", "Mustaqillik", "Maqsadga yo'nalganlik"],
  "careerFields": ["Muhandislik", "IT", "Ilmiy tadqiqot"],
  "note": "Bu natija tashxis emas — hozirgi holatingiz surati."
}
```
> Bu javobda **hech qachon** aktivlik darajasi, `NeedsAttention` bayrog'i, xom ballar yoki
> to'liq AI hisobot bo'lmaydi.

Dasturda shaxsiyat batareyasi bo'lmasa (1.3 dagi `hasPersonalityBattery: false`) tip
hisoblanmaydi: `personalityType` **bo'sh qator** qaytadi. Mijoz buni "tip aniqlanmadi"
holati sifatida ko'rsatadi va tip kartasini render qilmaydi — bo'sh karta yoki `0` ball
KO'RSATILMAYDI. Mijoz bu holatni bayroq bo'yicha OLDINDAN biladi (bo'sh qatorga tayanish —
ikkinchi himoya qatlami, birinchisi emas).

**202** — tahlil hali tayyor emas · **403** — o'quvchiga ko'rsatish o'chirilgan

---

## 2. Autentifikatsiya (superadmin)

| Metod | Yo'l | Izoh |
|-------|------|------|
| POST | `/api/auth/login` | `{username, password, totpCode?}` → `{accessToken, expiresIn, user}`; refresh token **`httpOnly` cookie** da qaytadi |
| POST | `/api/auth/refresh` | Tana **bo'sh**; refresh token `httpOnly` cookie'dan o'qiladi → yangi `accessToken` + rotatsiya qilingan cookie |
| POST | `/api/auth/logout` | Refresh tokenni bekor qiladi va cookie'ni tozalaydi |
| GET | `/api/auth/me` | Joriy foydalanuvchi |
| POST | `/api/auth/change-password` | `{currentPassword, newPassword}` |
| POST | `/api/auth/totp/enable` | 2FA yoqadi → `{secret, otpauthUri, backupCodes[8]}` (zaxira kodlar **faqat shu javobda bir marta** ko'rsatiladi) |
| POST | `/api/auth/totp/disable` | 2FA o'chiradi; tana: `{currentPassword}` — o'g'irlangan sessiya 2FA ni o'chira olmasligi uchun (P13) |

5 marta xato parol → 15 daqiqa blok (`LockedUntil`). Login urinishlari `AuditLog` da.

> **Eslatma (P19 da tuzatildi):** avval bu jadvalda `refreshToken` javob tanasida va so'rov
> tanasida ko'rsatilgan edi — bu `docs/08` bilan ziddiyatda edi (u yerda refresh token
> `httpOnly` cookie, JS uni hech qachon ko'rmaydi). Xavfsizlik hujjati ustun turadi:
> refresh token **faqat cookie** orqali yuradi, frontend `credentials: 'include'` bilan
> so'rov yuboradi. Bu XSS holatida refresh tokenning o'g'irlanishini oldini oladi.

---

## 3. Admin endpointlari (`Authorize(Roles = "SuperAdmin")`)

### 3.1 Maktablar
| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/schools?search=&region=&isActive=&page=1&pageSize=20&sort=name` | `PagedResult<SchoolListItemDto>` |
| GET | `/api/admin/schools/{id}` | Batafsil + statistika (o'quvchi soni, yakunlangan sessiyalar) |
| POST | `/api/admin/schools` | Yaratish; `slug` avtomatik (nom+tuman), band bo'lsa `-2` |
| PUT | `/api/admin/schools/{id}` | Yangilash |
| POST | `/api/admin/schools/{id}/regenerate-link` | Yangi `accessToken` → `{ publicUrl, qrCodeBase64 }` |
| POST | `/api/admin/schools/{id}/toggle-active` | Faol/nofaol |
| DELETE | `/api/admin/schools/{id}` | Soft delete (o'quvchisi bo'lsa 409) |

`SchoolListItemDto`: `id, name, region, district, slug, publicUrl, isActive, studentCount, completedCount, lastActivityAt`

**`GET /api/admin/schools/{id}` — 200** (`SchoolDetailDto`): `id, name, region, district,
schoolNumber, contactPerson, contactPhone, slug, publicUrl, qrCodeBase64, accessCode,
dailyRegistrationLimit, isActive, notes, createdAt, updatedAt, stats`

```json
{
  "stats": {
    "studentCount": 120,        // ro'yxatdan o'tgan o'quvchilar (soft-delete qilinganlar KIRMAYDI)
    "completedCount": 80,       // testni TO'LIQ yakunlagan o'quvchilar
    "inProgressCount": 7,       // hozir jarayonda bo'lgan sessiyalar (`status = InProgress`;
                                // `Draft` va `Abandoned` KIRMAYDI)
    "completionRate": 0.667,    // ULUSH (0..1), foiz emas — UI `× 100` qiladi
    "lastActivityAt": "2026-08-30T10:00:00Z"
  }
}
```

> **Birlik:** `stats.completionRate` — **ulush (0..1)**, `schoolBreakdown.completionRate`
> (3.6-bo'lim) bilan BIR XIL qoida va formula (`completedCount / studentCount`).
> `studentCount == 0` bo'lsa **`null`** (hech qachon `0` emas) — "hali hech kim ro'yxatdan
> o'tmagan" bilan "yakunlash ulushi HAQIQIY 0%" chalkashtirilmasin. `lastActivityAt` ham
> ma'lumot bo'lmaganda `null`. Qolgan uchta son esa **doim butun son** (`0` bo'lishi mumkin,
> `null` emas) — "hech kim topshirmagan" UI'da `0` deb ko'rsatiladi.

### 3.2 O'quvchilar
| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/students?schoolId=&grade=&status=&needsAttention=&personalityType=&activityLevel=&from=&to=&search=&page=&pageSize=&sort=` | Ro'yxat |
| GET | `/api/admin/students/{id}` | **Individual profil** (pastda) |
| DELETE | `/api/admin/students/{id}?hard=true` | O'chirish (`hard=true` — o'quvchi so'rovi bo'yicha) |
| GET | `/api/admin/students/export?…` | `.xlsx` (filtr saqlanadi) |

`StudentListItemDto`: `id, fullName, schoolName, grade, classLetter, phone, lastAssessmentStatus,
personalityType, maturityIndex, activityLevel, needsAttention, reliabilityFlag, lastAssessmentAt`

**`GET /api/admin/students/{id}` — 200**
```json
{
  "student": { "id":"…","fullName":"…","birthDate":"2010-04-17","age":16,"gender":"Male",
               "grade":9,"classLetter":"B","phone":"…","parentPhone":"…","email":null,
               "school":{"id":"…","name":"…"},"consentGivenAt":"…","createdAt":"…" },
  "assessments": [
    { "id":"…","status":"Analyzed","startedAt":"…","completedAt":"…",
      "durationMinutes":29,"reliabilityScore":82.5,"reliabilityFlag":"Reliable",
      "isLatest":true }
  ],
  "latestAssessment": {
    "id": "…",
    "results": {
      "MBTI16": { "resultCode":"INTJ","typeName":"Loyihachi",
        "axes": { "EI":{"pct":28.3,"letter":"I","borderline":false}, "SN":{…},"TF":{…},"JP":{…} },
        "borderlineAxes": [] },
      "BIG5": { "factors": { "O":{"raw":38,"pct":70,"level":"Yuqori"}, "C":{…},"E":{…},"A":{…},"N":{…} },
        "stabilityPct":70, "maturityIndex":68.4, "maturityLevel":"Yaxshi" },
      "RIASEC": { "resultCode":"IRA","types":{"R":62,"I":88,"A":71,"S":40,"E":35,"C":48},
        "differentiation":53,"consistency":"High",
        "careerFields":[{"name":"Muhandislik","professions":["Dasturchi","Mexatronika muhandisi"]}] },
      "ACTIVITY": { "scales":{"MOT":74,"SELF":68,"SOCA":52,"ENG":60},
        "activityIndex":65.2,"activityLevel":"Moderate","needsAttention":false }
    },
    "aiAnalysis": {
      "id":"…","status":"Succeeded","provider":"Gemini","model":"…","promptVersion":"v1.0",
      "createdAt":"…","isFallbackReport":false,"isModerated":false,"errorMessage":null,
      "summary":"…","personalityPortrait":"…",
      "strengths":[{"title":"…","description":"…","evidence":"…"}],
      "growthAreas":[{"title":"…","description":"…","actionStep":"…"}],
      "learningStyle":"…","motivationProfile":"…","activityAssessment":"…",
      "careerSuggestions":[{"field":"…","why":"…","exampleProfessions":["…"],"nextSteps":["…"]}],
      "studentRecommendations":["…"],"teacherNotes":["…"],"parentNotes":["…"],
      "attentionFlags":[{"code":"LOW_MOTIVATION","message":"…","severity":"attention"}],
      "reliabilityNote":null,"disclaimer":"…"
    },
    "aiHistory": [ { "id":"…","provider":"OpenAi","createdAt":"…","status":"Succeeded","isCurrent":false } ]
  }
}
```

#### `latestAssessment.results` — KALIT NOMLARI (shartnoma)

> **Bu bo'lim normativ.** Ilgari yozilmagan edi va backend bilan frontend ajralib ketgan edi
> (2026-09-03 tekshiruvi: backend `results.mbti16` chiqarardi, frontend `results.MBTI16` o'qirdi;
> RIASEC'da esa backend `types.A` chiqarardi, frontend `types.ART` o'qirdi). Natijada o'quvchi
> profilida shaxsiyat va kasb qiziqishlari bo'limlari **jimgina bo'sh yoki buzuq** chiqardi.

**1. `results` kalitlari — `TestDefinition.Code` bilan HARFMA-HARF bir xil:**

| Kalit | Tip | Izoh |
|-------|-----|------|
| `MBTI16` | `{resultCode, typeName, axes, borderlineAxes}` | camelCase (`mbti16`) EMAS |
| `BIG5` | `{factors, stabilityPct, maturityIndex, maturityLevel}` | |
| `RIASEC` | `{resultCode, types, differentiation, consistency, careerFields}` | |
| `ACTIVITY` | `{scales, activityIndex, activityLevel, needsAttention}` | |

Har biri **ixtiyoriy**: o'quvchi yechmagan blok kaliti javobda umuman bo'lmaydi (`null` emas).
API'ning qolgan barcha maydonlari camelCase, lekin bu TO'RTTASI — kod qiymatlari, shuning uchun
katta harfda qoladi (backend'da `[JsonPropertyName]` bilan qat'iylashtirilgan).

**2. Ichki lug'atlarning kalitlari — shkala kodlari, o'zgarmaydi:**

| Lug'at | Kalitlar |
|--------|----------|
| `MBTI16.axes` | `EI`, `SN`, `TF`, `JP` |
| `BIG5.factors` | `O`, `C`, `E`, `A`, `N` |
| `ACTIVITY.scales` | `MOT`, `SELF`, `SOCA`, `ENG` |
| **`RIASEC.types`** | **`R`, `I`, `A`, `S`, `E`, `C`** — Holland harflari |

**3. `RIASEC.types` kalitlari — Holland harflari, bazadagi `scale` kodlari EMAS.**
`docs/03` §4.1 da bazadagi `Scale` qiymatlari `R, I, ART, SOC, ENT, CONV` (Big Five'ning
`A`/`S`/`E`/`C` bilan to'qnashmasligi uchun), lekin **API'da har doim bitta harfli xalqaro
Holland mnemonikasi** (`R I A S E C`) qaytadi. Ikki sabab:

1. `scale` qiymati ommaviy/natija API'siga hech qachon chiqmaydi (`CLAUDE.md` 9-band);
2. `resultCode` (Holland kodi, masalan `"IRA"`) aynan shu harflardan iborat — mijoz kod
   harflarini `types` kalitlari bilan to'g'ridan-to'g'ri solishtira olishi kerak.

O'girish backend'da bir joyda: `StudentProfileMapping.RiasecScaleToLetter`
(`ART→A`, `SOC→S`, `ENT→E`, `CONV→C`). Saqlangan `jsonb` (`test_results.normalized_scores_json`)
**o'zgarmaydi** — u yerda `scale` kodlari qoladi, migratsiya kerak emas.

> Bu kalitlar shartnoma: o'zgarishi mijoz uchun **buzuvchi** (breaking). Tekshiradigan testlar —
> `tests/StudentRoadMap.Application.Tests/Admin/Students/AdminTestResultsJsonKeysTests.cs` va
> `frontend/src/features/students/pages/StudentProfileResultKeys.test.tsx`.


> **`aiAnalysis` shakli — haqiqat manbai `docs/09-ai-analiz-moduli.md` 5-bo'lim JSON sxemasi.**
> AI aynan o'sha sxema bo'yicha javob beradi (`AiAnalysis.ResponseJson`da xom holda saqlanadi)
> va post-filtr ham shunga tayanadi, shuning uchun DTO undan CHEKINMAYDI:
>
> | Maydon | Tip | Izoh |
> |--------|-----|------|
> | `summary`, `personalityPortrait` | `string?` | Sxemadagi bir xil nomli matnlar |
> | `strengths[]` | `{title, description?, evidence?}` | Satrlar massivi EMAS (`docs/09` §5) |
> | `growthAreas[]` | `{title, description?, actionStep?}` | |
> | `learningStyle`, `motivationProfile`, `activityAssessment` | `string?` | |
> | `careerSuggestions[]` | `{field, why, exampleProfessions[], nextSteps[]}` | Massivlar hech qachon `null` emas — bo'sh `[]` |
> | `studentRecommendations`, `teacherNotes`, `parentNotes` | `string[]` | `teacherNotes`/`parentNotes` — MASSIV (bitta satr emas) |
> | `attentionFlags[]` | `{code?, message, severity}` | `severity`: `info` \| `attention` \| `high` |
> | `reliabilityNote`, `disclaimer` | `string?` | **Hisobotning cheklovlari** — UI'da hech qachon yashirilmaydi (`docs/09` 4.2, 13-talab va "ISHONCHLILIK" bandi) |
>
> Ma'lumot bo'lmasa maydon `null` yoki bo'sh massiv bo'ladi — bo'sh satr (`""`) HECH QACHON
> qaytarilmaydi. `description`/`evidence`/`actionStep`/`code` — sxemada majburiy, lekin zaxira
> (shablon) hisobotda va AI modulidan oldin yaratilgan eski yozuvlarda `null` bo'lishi mumkin,
> chunki `AiAnalysis` ustunlari (`strengths_json` va h.k.) faqat YASSILANGAN satr nusxasini
> saqlaydi. UI bunday bo'limlarni bo'sh karta sifatida emas, umuman render qilmasligi kerak.

> **`isModerated`** (`docs/09` 6-bo'lim, 3-band) — `true` bo'lsa taqiqlangan atama IKKINCHI
> urinishda ham topilgan va javob "moderatsiya qilindi" belgisi bilan saqlangan
> (`attentionFlags` ichida `MODERATION_REQUIRED`, `severity: "high"`). Bu matn post-filtrdan
> TOZA holda o'tmagan — admin UI uni jimgina oddiy tahlil sifatida ko'rsatmasligi SHART
> (`CLAUDE.md` 6-qoida: "AI tashxis qo'ymaydi"), ochiq ogohlantirish chiqadi.

> **`isFallbackReport`** — `true` bo'lsa matnni AI YOZMAGAN: fallback zanjiridagi barcha
> provayder yiqilgach tizim avtomatik **shablon hisobot** yozgan (`docs/09` 11-bo'lim,
> `AiAnalysis.CreateFallbackReport`, `model: "template"`). Admin UI buni haqiqiy AI tahlilidan
> aniq ajratib ko'rsatishi SHART ("Avtomatik shablon hisobot" belgisi) — aks holda superadmin
> shablon matnni AI tahlili deb o'qiydi. Bunday yozuvlar `GET /api/admin/ai/usage`
> statistikasiga ham kirmaydi (token sarflanmagan).

### 3.3 Sessiyalar
| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/assessments?schoolId=&status=&from=&to=&page=&pageSize=` | Ro'yxat |
| GET | `/api/admin/assessments/{id}` | To'liq detal (`latestAssessment` yadrosi + sessiya sarlavhasi + `tests[]`) |
| GET | `/api/admin/assessments/{id}/answers?testCode=` | Xom javoblar (audit uchun) |
| POST | `/api/admin/assessments/{id}/rerun-analysis` | `{ "provider": "Anthropic", "promptVersion": "v1.1" }` → 202 |
| POST | `/api/admin/assessments/{id}/recalculate-scores` | Scoring versiyasi o'zgargan bo'lsa |
| GET | `/api/admin/assessments/{id}/report.pdf` | PDF hisobot |
| DELETE | `/api/admin/assessments/{id}` | Soft delete |

**`GET /api/admin/assessments` — ro'yxat elementi** (`AdminAssessmentListItemDto`)
```json
{ "id": "…", "studentId": "…", "studentName": "Nortoyev Aziz Shukurovich",
  "schoolId": "…", "schoolName": "12-son maktab", "status": "Analyzed",
  "startedAt": "2026-08-30T09:00:00Z", "completedAt": "2026-08-30T09:29:00Z",
  "durationMinutes": 29, "reliabilityScore": 85.5, "reliabilityFlag": "Reliable",
  "programId": "…", "programName": "Shaxsiyat profili" }
```

**`GET /api/admin/assessments/{id}` — javob** (`AdminAssessmentDetailDto`)
```json
{ "id": "…",
  "results": { "MBTI16": { … }, "BIG5": { … }, "RIASEC": { … }, "ACTIVITY": { … } },
  "aiAnalysis": { … }, "aiHistory": [ … ],

  "status": "Analyzed",
  "startedAt": "2026-08-30T09:00:00Z", "completedAt": "2026-08-30T09:29:00Z",
  "durationMinutes": 29, "reliabilityScore": 85.5, "reliabilityFlag": "Reliable",
  "student": { "id": "…", "fullName": "Nortoyev Aziz Shukurovich" },
  "school":  { "id": "…", "name": "12-son maktab" },
  "program": { "id": "…", "nameUz": "Shaxsiyat profili" },
  "tests": [
    { "testDefinitionId": "…", "testCode": "MBTI16", "nameUz": "Shaxsiyat tipi",
      "scoringMode": "Scored", "status": "Completed",
      "questionCount": 60, "answeredCount": 60 },
    { "testDefinitionId": "…", "testCode": "STRESS", "nameUz": "Stress so'rovnomasi",
      "scoringMode": "Survey", "status": "NotStarted",
      "questionCount": 8, "answeredCount": 0 }
  ] }
```

> **Birinchi to'rt maydon** (`id`, `results`, `aiAnalysis`, `aiHistory`) — 3.2-bo'limdagi
> `latestAssessment` bilan HARFMA-HARF bir xil, shu jumladan `results` kalitlari KATTA harfda
> (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`). Qolganlari — sessiyaning o'z "sarlavhasi": ular
> qo'shilgunga qadar (2026-09-03) admin detal sahifasi holat/vaqt/o'quvchi/maktabni faqat
> ro'yxatdan kelgan navigatsiya holatidan olardi, ya'ni havola NUSXALAB ochilganda sahifa
> yarim bo'sh qolardi.

> **`tests[]`** — sessiyaga biriktirilgan HAR bir test bloki (`assessment_tests`), `displayOrder`
> bo'yicha. `results` faqat 4 ta TIZIM blokini biladi, dastur esa `Custom` va `Survey`
> anketalarni ham o'z ichiga oladi — ular admin ekranida faqat shu ro'yxat orqali ko'rinadi.
> `scoringMode: "Survey"` — anketa **ballanmaydi** (`docs/06` 8-bo'lim, 2026-09-02 qarori):
> uning natijasi `results`da hech qachon bo'lmaydi va UI uni "0 ball" emas, "Ballanmaydi
> (so'rovnoma)" deb ko'rsatadi. `questionCount`/`answeredCount` — SESSIYA snapshoti
> (`AssessmentTest.TotalCount`/`AnsweredCount`), katalogdagi joriy savol soni emas.
> Maydon nomi `testCode` (`code` emas) — `GET /answers?testCode=` va `AdminAssessmentAnswerDto`
> bilan bir xil atama. `scale`/`scaleDirection` bu yerda ham YO'Q (`CLAUDE.md` 9-band).

> **Ma'lumot yo'q ≠ nol** (`docs/06` qarorlar jurnali, 2026-09-02): yakunlanmagan sessiyada
> `completedAt` va `durationMinutes` — `null`; ishonchlilik hisoblanmagan bo'lsa
> `reliabilityScore`/`reliabilityFlag` — `null`. `student`/`school`/`program` bog'liq yozuv
> topilmasa (masalan o'quvchi soft-delete qilingan) — `null`, bo'sh satrli soxta obyekt EMAS.
> Ro'yxatdagi `programName` ham shu qoidaga bo'ysunadi.

> Bu kalitlarni tekshiradigan testlar (XOM JSON ustidan — `ReadFromJsonAsync<Dto>` kalit
> farqini ko'rmaydi, 2026-09-03 qarori): `tests/StudentRoadMap.Api.IntegrationTests/Admin/
> AdminAssessmentsGetByIdEndpointTests.cs` va `…/AdminAssessmentsListEndpointTests.cs`.

### 3.4 Test katalogi va anketa konstruktori

**Testlar**

| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/catalog/tests` | Barchasi: `kind`, `isSystem`, `status`, savol soni, shkalalar soni |
| GET | `/api/admin/catalog/tests/{id}` | Batafsil (shkalalar bilan) |
| POST | `/api/admin/catalog/tests` | **Yangi `Custom` anketa** — `Draft` holatida yaratiladi |
| PUT | `/api/admin/catalog/tests/{id}` | Nom, tavsif, tartib, `pageSize`, `shuffleQuestions` |
| POST | `/api/admin/catalog/tests/{id}/publish` | Validatsiya + `Published` (xatolar ro'yxati bilan 400) |
| POST | `/api/admin/catalog/tests/{id}/duplicate` | Nusxa (`Draft`, yangi kod) |
| POST | `/api/admin/catalog/tests/{id}/toggle-active` | Yangi sessiyalarga kirish/kirmasligi (BR-10) |
| POST | `/api/admin/catalog/tests/{id}/archive` | `Custom` va ishlatilgan bo'lsa (BR-11) |
| DELETE | `/api/admin/catalog/tests/{id}` | Faqat `Custom` + hech qaysi sessiyada ishlatilmagan |
| GET | `/api/admin/catalog/tests/{id}/preview` | O'quvchi ko'radigan ko'rinish (savollar + shkala yorliqlari) |

**Shkalalar** (faqat `Custom`)

| Metod | Yo'l |
|-------|------|
| GET | `/api/admin/catalog/tests/{id}/scales` |
| POST | `/api/admin/catalog/tests/{id}/scales` (`code`, `nameUz`, `descriptionUz`, `interpretationBands`) |
| PUT | `/api/admin/catalog/scales/{scaleId}` |
| DELETE | `/api/admin/catalog/scales/{scaleId}` (savollari bo'lsa 409) |

**Savollar**

| Metod | Yo'l | Tizim testida |
|-------|------|---------------|
| GET | `/api/admin/catalog/tests/{id}/questions` | ✅ |
| POST | `/api/admin/catalog/tests/{id}/questions` | ❌ `409 SYSTEM_TEST_LOCKED` |
| PUT | `/api/admin/catalog/questions/{id}` | ⚠️ faqat `textUz`, `textRu`, `isActive` |
| DELETE | `/api/admin/catalog/questions/{id}` | ❌ `409 SYSTEM_TEST_LOCKED` |
| POST | `/api/admin/catalog/tests/{id}/questions/reorder` | ✅ (`[{id, displayOrder}]`) |
| POST | `/api/admin/catalog/tests/{id}/questions/import` | `Custom` — to'liq; tizim — faqat matn yangilash |

**Katalog ma'lumotlari**

| Metod | Yo'l |
|-------|------|
| GET/PUT | `/api/admin/catalog/type-catalog` · `/api/admin/catalog/type-catalog/{code}` |
| GET | `/api/admin/catalog/career-map` |

**`POST /api/admin/catalog/tests` — so'rov**
```json
{ "code": "STRESS", "nameUz": "Stressga chidamlilik anketasi",
  "descriptionUz": "…", "estimatedMinutes": 6, "pageSize": 10,
  "shuffleQuestions": false, "displayOrder": 5 }
```
**201** → `{ "id": "…", "status": "Draft", "kind": "Custom", "scoringStrategy": "SUM" }`

**`GET /api/admin/catalog/tests/{id}` — javob (batafsil)**
```json
{ "id": "…", "code": "STRESS", "nameUz": "Stressga chidamlilik anketasi",
  "kind": "Custom", "isSystem": false, "status": "Draft", "isActive": true,
  "scoringMode": "Scored", "questionCount": 8, "scaleCount": 2,
  "estimatedMinutes": 6, "version": 1, "usedInProgramCount": 0,
  "descriptionUz": "…", "pageSize": 10, "shuffleQuestions": false, "displayOrder": 5 }
```

> `displayOrder` ATAYLAB detal javobida ham bor: `PUT /tests/{id}` uni **majburiy** talab qiladi,
> shu sabab uni qaytarmaslik admin UI'ni "joriy tartibni bilmayman" holatiga tushirar va har
> saqlashda tartibni tasodifiy qiymatga o'zgartirar edi. Ro'yxat javobida (`GET /tests`) yo'q —
> u yerda tartib qatorlar ketma-ketligida ko'rinadi.

**`POST .../publish` — 400 (validatsiya yiqilganda)**
```json
{ "code": "TEST_NOT_PUBLISHABLE", "status": 400,
  "title": "Anketa nashr qilishga tayyor emas",
  "issues": [
    { "code": "SCALE_TOO_FEW_QUESTIONS", "scale": "SUPPORT", "message": "Kamida 4 savol kerak, hozir 2" },
    { "code": "QUESTION_WITHOUT_SCALE", "questionCode": "ST-Q07", "message": "Shkala tanlanmagan" }
  ] }
```

> **Cheklov:** `isSystem = true` bo'lgan test va savollarda `scale`, `scaleDirection`, `weight`
> o'zgartirilmaydi va savol qo'shilmaydi/o'chirilmaydi (BR-8) — API `409 SYSTEM_TEST_LOCKED` beradi.
> `Custom` testlarda bularning hammasi ochiq.

### 3.5 AI sozlamalari
| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/ai/providers` | Kalitlar maskalangan: `AIza••••••7f2b` |
| PUT | `/api/admin/ai/providers/{provider}` | `{ apiKey?, model, maxOutputTokens, temperature, isActive, fallbackOrder }` |
| POST | `/api/admin/ai/providers/{provider}/test` | Aloqa tekshiruvi → `{ ok, latencyMs, message }` |
| POST | `/api/admin/ai/providers/{provider}/set-default` | |
| GET | `/api/admin/ai/prompts` · POST `/api/admin/ai/prompts` | Prompt shablon versiyalari |
| GET | `/api/admin/ai/usage?from=&to=` | Token va taxminiy xarajat statistikasi |

> **`{provider}` — enum NOMI, `{id}` EMAS.** `Gemini` / `OpenAi` / `Anthropic`
> (`AiProviderConfig.Provider` ustida `ux_ai_provider_kind` unique indeksi bor, shu sabab
> provider turi tabiiy identifikator). Marshrut registrga sezgir emas; raqamli qiymat
> (`1`/`2`/`3`) ham qabul qilinadi, lekin **nom ishlatiladi**.

**`GET /api/admin/ai/providers` → `AdminAiProviderDto[]`** (jonli javob, 2026-09-02):

```json
[{
  "provider": "Gemini", "displayName": "Google Gemini", "model": "gemini-2.0-flash",
  "baseUrl": null, "maxOutputTokens": 4096, "temperature": 0.40,
  "isDefault": true, "isActive": true, "fallbackOrder": 1,
  "maskedApiKey": "AIza••••••cdef",
  "lastCheckedAt": "2026-09-02T18:25:30.878472+00:00", "lastCheckStatus": "Auth"
}]
```

- **Kalit hech qachon to'liq qaytmaydi** — faqat birinchi/oxirgi 4 belgi (`ApiKeyMasker`);
  kalit kiritilmagan bo'lsa `maskedApiKey: null`.
- Faqat DB'da mavjud (kamida bir marta `PUT` qilingan) provayderlar qaytadi.
- `lastCheckStatus` — `"ok"` yoki `AiErrorKind` nomi (`Auth`, `RateLimit`, `ModelNotFound`, …).

**`PUT /api/admin/ai/providers/{provider}`** — upsert. Tana: `{ apiKey?, model,
maxOutputTokens, temperature, isActive, fallbackOrder, baseUrl? }` → `AdminAiProviderDto`.

- `apiKey` bo'sh/yo'q bo'lsa **mavjud kalit saqlanib qoladi**.
- `baseUrl` bunday emas: to'liq `PUT` semantikasi — yuborilmasa mavjud qiymat **`null` ga
  yoziladi**. Klient joriy qiymatni qaytarib yuborishi shart.
- Kalit kiritilmagan providerni `isActive: true` qilish → `400 VALIDATION_ERROR`.

**`POST /api/admin/ai/providers/{provider}/test`** → `{ ok, latencyMs, message }`.
Muvaffaqiyatsiz tekshiruv **HTTP xatosi emas** — `200` + `ok:false`. `message` — provayderning
xom javobi EMAS, `AiErrorKind` bo'yicha oldindan yozilgan o'zbekcha matn (kalit noto'g'ri /
kvota tugagan / model topilmadi / tarmoq / …), shu sabab unda API kaliti bo'lishi mumkin emas.
Provayder umuman sozlanmagan bo'lsa → `404 NOT_FOUND`.

**`POST /api/admin/ai/providers/{provider}/set-default`** → `AdminAiProviderDto`.
Faqat faol va kaliti bor provider → aks holda `400 VALIDATION_ERROR`; sozlanmagan → `404`.

**`GET /api/admin/ai/prompts` → `AdminPromptTemplateDto[]`**:
`{ id, key, version, systemText, userText, jsonSchema, isActive, createdAt }` —
`version` **matn** (`"v1.0"`), raqam emas.

**`GET /api/admin/ai/usage?from=&to=` → `AdminAiUsageDto`**:

```json
{ "totalCalls": 0, "inputTokens": 0, "outputTokens": 0, "estimatedCostUsd": null, "byProvider": [] }
```

Javobda `from`/`to` **qaytmaydi** (faqat so'rov parametri), token maydonlarida `total`
prefiksi yo'q. Zaxira shablon hisobotlar (`IsFallbackReport = true`) statistikaga kirmaydi.

### 3.6 Dashboard va audit

> **P-dashboard da qo'shildi.** `GET /api/admin/dashboard/stats` javobiga ikki blok qo'shildi
> (mavjud maydonlar o'zgarmadi):
>
> ```jsonc
> "funnel": {                 // barchasi `[from,to]` oynasida
>   "linkViews":  1240,       // maktab havolasi ochilgan soni
>   "registered": 380,        // yaratilgan o'quvchilar
>   "started":    350,        // Draft dan o'tgan sessiyalar
>   "completed":  290,        // yakunlangan
>   "analyzed":   275         // AI tahlili tayyor
> },
> "schoolBreakdown": [        // maksimum 20 qator; registered DESC -> completed DESC -> linkViews DESC
>   { "schoolId": "…", "name": "12-son maktab", "region": "Farg'ona",
>     "linkViews": 210, "registered": 64, "completed": 51,
>     "completionRate": 0.797,         // ULUSH (0..1), foiz emas — UI `× 100` qiladi.
>                                      // registered = 0 bo'lsa `null` (0 EMAS)
>     "lastActivityAt": "2026-09-02T10:15:00Z" }
> ]
> ```
>
> **`null` qoidasi** (`docs/06` §8, 2026-09-02): `avgDurationMinutes`, `avgReliability`,
> `dropOffRate`, `completionRate` — ma'lumot bo'lmaganda **`null`**, hech qachon `0` emas.
>
> **Birlik:** `completionRate` va `dropOffRate` — **ulush (0..1)**; `avgReliability` — **0..100**;
> `avgDurationMinutes` — daqiqa. UI ulushlarni `× 100` qilib ko'rsatadi. Bu bir marta xato
> qilingan joy: `completionRate` uchun `× 100` unutilganda 50% yakunlagan maktab `1%` bo'lib
> ko'ringan edi.
> `0` "past ko'rsatkich" degan ma'noni beradi va adminni noto'g'ri xulosaga olib keladi.

| Metod | Yo'l |
|-------|------|
| GET | `/api/admin/dashboard/stats?from=&to=` |
| GET | `/api/admin/audit-logs?action=&entityType=&from=&to=&page=` |

**`dashboard/stats` — 200**
```json
{
  "totals": { "schools":42,"activeSchools":39,"students":5820,
              "completedAssessments":5104,"pendingAnalysis":12,"needsAttention":318 },
  "last30Days": { "newStudents":740, "completed":688, "avgDurationMinutes":28.4,
                  "avgReliability":79.2, "dropOffRate":0.17 },
  "personalityDistribution": [ { "type":"INTJ","count":184 }, … ],
  "activityDistribution": [ { "level":"Passive","count":410 }, … ],
  "hollandTop": [ { "code":"SA","count":621 }, … ],
  "recentAssessments": [ { "assessmentId":"…","studentName":"…","schoolName":"…",
                           "completedAt":"…","status":"Analyzed" } ]
}
```

---

## 4. Umumiy konvensiyalar

**Pagination** — barcha ro'yxatlar:
```json
{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 5820, "totalPages": 291,
  "hasNext": true, "hasPrevious": false }
```
`pageSize` max 100. `sort` formati: `name`, `-createdAt` (minus = kamayish).

**Sana/vaqt:** ISO-8601 UTC (`2026-08-31T10:12:00Z`). Tug'ilgan sana `date` (`2010-04-17`).

**Enum'lar** JSON'da **string** ko'rinishida (`"Analyzed"`, `"Male"`), DB'da raqam.

**Idempotentlik:** `POST /answers` — `(assessmentTestId, questionId)` bo'yicha upsert.
`POST /sessions` — dublikatda mavjud sessiyani qaytaradi.

**CORS:** faqat `App:FrontendUrl` va dev origin'lari.

**Rate limit** (ASP.NET Core `RateLimiter`):

| Endpoint | Limit |
|----------|-------|
| `POST /api/public/sessions` | IP bo'yicha 10/soat |
| `POST /api/public/.../answers` | sessiya bo'yicha 120/daqiqa |
| `GET /api/public/schools/{slug}` | IP bo'yicha 60/daqiqa |
| `POST /api/auth/login` | IP bo'yicha 10/15 daqiqa |
| Admin API (umumiy) | 300/daqiqa |

**Swagger:** `/swagger` faqat `Development` va `Staging` da.
