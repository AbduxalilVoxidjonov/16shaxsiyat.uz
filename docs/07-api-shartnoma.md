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
  "consentText": "…"
}
```
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
  "progressPercent": 41
}
```
**410** `SESSION_EXPIRED`

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
      "createdAt":"…","summary":"…","personalityPortrait":"…",
      "strengths":["…"],"growthAreas":["…"],"learningStyle":"…","motivationProfile":"…",
      "activityAssessment":"…","careerSuggestions":[{"field":"…","why":"…","nextSteps":["…"]}],
      "studentRecommendations":["…"],"teacherNotes":"…","parentNotes":"…",
      "attentionFlags":[],"disclaimer":"…"
    },
    "aiHistory": [ { "id":"…","provider":"OpenAi","createdAt":"…","status":"Succeeded","isCurrent":false } ]
  }
}
```

### 3.3 Sessiyalar
| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/assessments?schoolId=&status=&from=&to=&page=&pageSize=` | Ro'yxat |
| GET | `/api/admin/assessments/{id}` | To'liq detal (yuqoridagi `latestAssessment` shakli) |
| GET | `/api/admin/assessments/{id}/answers?testCode=` | Xom javoblar (audit uchun) |
| POST | `/api/admin/assessments/{id}/rerun-analysis` | `{ "provider": "Anthropic", "promptVersion": "v1.1" }` → 202 |
| POST | `/api/admin/assessments/{id}/recalculate-scores` | Scoring versiyasi o'zgargan bo'lsa |
| GET | `/api/admin/assessments/{id}/report.pdf` | PDF hisobot |
| DELETE | `/api/admin/assessments/{id}` | Soft delete |

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
