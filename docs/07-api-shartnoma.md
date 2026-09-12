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
      "testCount": 4, "questionCount": 190, "estimatedMinutes": 31, "hasPersonalityBattery": true,
      "registrationMode": "Full",
      "registrationFields": { "birthDate": "Required", "gender": "Required", "grade": "Required",
        "classLetter": "Optional", "phone": "Required", "parentPhone": "Optional", "email": "Optional" },
      "registrationForm": {
        "coreFields": {
          "fullName": { "requirement": "Required", "labelUz": "F.I.Sh.", "placeholderUz": null, "order": 1 },
          "birthDate": { "requirement": "Required", "labelUz": "Tug'ilgan sana", "placeholderUz": null, "order": 2 },
          "gender": { "requirement": "Required", "labelUz": "Jins", "placeholderUz": null, "order": 3 },
          "grade": { "requirement": "Required", "labelUz": "Sinf", "placeholderUz": null, "order": 4 },
          "classLetter": { "requirement": "Optional", "labelUz": "Sinf harfi", "placeholderUz": null, "order": 5 },
          "phone": { "requirement": "Required", "labelUz": "Telefon raqami", "placeholderUz": null, "order": 6 },
          "parentPhone": { "requirement": "Optional", "labelUz": "Ota-ona telefoni", "placeholderUz": null, "order": 7 },
          "email": { "requirement": "Optional", "labelUz": "Email", "placeholderUz": null, "order": 8 }
        },
        "customFields": [
          { "code": "PARENT_JOB", "type": "ShortText", "labelUz": "Ota-onangiz kasbi",
            "placeholderUz": "Masalan: o'qituvchi", "requirement": "Optional", "maxLength": 200,
            "inputPattern": null, "options": null, "order": 9 }
        ]
      },
      "tests": [
        { "code": "MBTI16", "name": "16 tipli shaxsiyat modeli", "questionCount": 60, "estimatedMinutes": 9, "order": 1 },
        { "code": "BIG5", "name": "Shaxsiyatning 5 omili", "questionCount": 50, "estimatedMinutes": 8, "order": 2 },
        { "code": "RIASEC", "name": "Kasb qiziqishlari", "questionCount": 48, "estimatedMinutes": 7, "order": 3 },
        { "code": "ACTIVITY", "name": "Aktivlik va motivatsiya", "questionCount": 32, "estimatedMinutes": 5, "order": 4 }
      ] },
    { "code": "CAREER_SURVEY", "nameUz": "Kasb so'rovnomasi", "descriptionUz": null,
      "testCount": 1, "questionCount": 20, "estimatedMinutes": 5, "hasPersonalityBattery": false,
      "registrationMode": "None",
      "registrationFields": { "birthDate": "Required", "gender": "Required", "grade": "Required",
        "classLetter": "Optional", "phone": "Required", "parentPhone": "Optional", "email": "Optional" },
      "tests": [
        { "code": "CAREER_SURVEY_Q", "name": "Kasb so'rovnomasi savollari", "questionCount": 20, "estimatedMinutes": 5, "order": 1 }
      ] }
  ]
}
```
`programs[]` — maktab uchun mavjud dasturlar (`Visibility = Public` yoki biriktirilgan;
bir nechta bo'lsa o'quvchi tanlaydi, `code` → `POST /sessions` `programCode`).
`programs[].tests` — AYNAN shu dasturning test bloklari (`ProgramTest.DisplayOrder` bo'yicha),
sessiya boshlanganda (`POST /sessions`) biriktiriladigan ro'yxat bilan BIR XIL manbadan keladi.

**`programs[].registrationMode`** (P52, 2026-09-11) — `"Full"`/`"None"` (`AssessmentProgram.RegistrationMode`).
`Full` bo'lsa mijoz registratsiya ekranini (F.I.Sh./tug'ilgan sana/jins/sinf/telefon) ko'rsatadi
va shu maydonlarni `POST /sessions` ga yuboradi (1.2 — mavjud xatti-harakat, o'zgarmagan).
`None` bo'lsa mijoz registratsiya ekranini UMUMAN KO'RSATMAYDI — `POST /sessions` shaxs
maydonlarisiz (faqat `slug`/`accessToken`/`consentAccepted`/ixtiyoriy `programCode`) chaqiriladi,
server anonim `Student` yaratadi (1.2 pastga qarang). Qat'iy invariant: `hasPersonalityBattery: true`
bo'lgan dasturda `registrationMode` DOIM `"Full"` — bu ikkala maydon hech qachon
`{ true, "None" }` kombinatsiyasida bo'lmaydi (domen darajasida qulflangan).

**`programs[].registrationFields`** (P52 kengaytmasi, 2026-09-11, `docs/18` §9.5) — `registrationMode
= "Full"` bo'lganda registratsiya ekranidagi HAR BIR maydonning holati: `"Hidden"` (ko'rsatilmaydi,
kelsa ham `POST /sessions` da e'tiborsiz qoldiriladi), `"Optional"` (ko'rsatiladi, bo'sh
qoldirish mumkin) yoki `"Required"` (bo'sh bo'lsa `400`). `fullName` bu obyektda YO'Q — `Full`
rejimida u har doim majburiy. Dastur bazada sozlamani saqlamagan (`NULL`) bo'lsa ham bu yerda
HAR DOIM standart qiymatlar (yuqoridagi misoldagidek) bilan keladi. `registrationMode = "None"`
dasturda ham shakl beriladi, lekin mijoz uni E'TIBORGA OLMAYDI (registratsiya ekrani umuman
ko'rsatilmaydi). **Qat'iy invariant:** `hasPersonalityBattery: true` bo'lgan dasturda
`birthDate`/`grade` DOIM `"Required"` (`REGISTRATION_FIELD_REQUIRED_FOR_BATTERY`, 400,
`docs/06` §6) — `gender` bundan mustasno, erkin sozlanadi.

**⚠️ P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2):** `registrationFields` qiymati ENDI
`AssessmentProgram.RegistrationFields` (§9.5, eskirgan, o'lik ustun)dan EMAS, GLOBAL
`RegistrationFormSettings` sozlamasidan (dastur ustunligi — yuqoridagi batareya invarianti —
QO'LLANGAN holda) hisoblanadi. Shakl (mijoz uchun) o'zgarmadi — frontend hozircha shu maydonga
tayanadi, `registrationForm` migratsiyasi keyingi to'lqinda.

**`programs[].registrationForm`** (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) — TO'LIQ GLOBAL
ro'yxatdan o'tish formasi ta'rifi, superadmin qo'shgan `customFields[]` bilan birga (`GET`/`PUT
/api/admin/settings/registration-form` — §3.8 bilan BIR XIL shakl), dastur ustunligi (batareya
invarianti) QO'LLANGAN holda. `registrationFields` — shu obyektning `coreFields`ga mos
qisqartirilgan (eski, `fullName`siz) proyeksiyasi; ikkalasi BIR XIL manbadan hisoblanadi, hech
qachon bir-biriga zid bo'lmaydi. `customFields[].code` → qiymat juftliklari `POST /sessions`
tanasidagi `customFields` obyektiga mos keladi (pastga qarang). `scale`/`scaleDirection` bu
yerda YO'Q (`CLAUDE.md` 8-qoida — bu forma ta'rifi, savol emas, lekin qoidaning ruhi bir xil:
faqat mijozga kerakli maydonlar chiqadi).

**`tests[]` (yuqori daraja, P52 — 2026-09-11 jonli hodisadan keyin tuzatildi):** endi BUTUN
katalogdan EMAS, FAQAT `programs[]` ichidagi MAVJUD dasturlar asosida hisoblanadi — bir nechta
dastur bo'lsa BIRLASHMA (union), bitta test bir nechta dasturda bo'lsa ham TAKRORLANMAYDI.
`totalEstimatedMinutes` ham shu (birlashtirilgan) ro'yxatdan hisoblanadi. Ilgari (2026-09-11
gacha) bu maydon "orqaga moslik uchun, dasturdan qat'i nazar butun nashr qilingan/faol
katalog" edi — bu XATO edi: dastur arxivlansa ham uning testlari bu yerda ko'rinishda davom
etardi (mijoz `programs.length === 1` bo'lganda aynan `tests[]`ga tayanadi, `programs[].tests`
EMAS — jonli hodisa shundan kelib chiqqan).

> **2026-09-03 dan boshlab `programs[]` `200` javobda HECH QACHON bo'sh emas** — bo'sh bo'lsa
> javob `409 NO_PROGRAM_AVAILABLE` (pastga qarang). Ilgari bu holat `200` + `"programs": []`
> qaytarardi; mijoz bo'sh massivni "test yo'q" deb TALQIN QILISHI kerak edi, ya'ni xatolik
> holati javob TANASIGA yashiringan edi.

**Havola ochilishi hisoblagichi (`school_link_views`):** `200` va `409 NO_PROGRAM_AVAILABLE`
holatlarida OSHADI (o'quvchi havolani chindan ochgan), `404`/`410` da OSHMAYDI.

**404** `NOT_FOUND` · **409** `NO_PROGRAM_AVAILABLE` · **410** `SCHOOL_INACTIVE`

#### `409 NO_PROGRAM_AVAILABLE` — "havola to'g'ri, lekin test tayyor emas" (2026-09-03)

Havolasi to'g'ri, tokeni to'g'ri, maktabi faol — LEKIN bu maktab uchun bironta **mavjud
dastur** yo'q. Bu holat `404 NOT_FOUND` dan **atayin ajratilgan**: ikkisi o'quvchidan
BUTUNLAY boshqa harakat talab qiladi.

| Holat | Kod | O'quvchiga xabar | O'quvchi nima qiladi |
|-------|-----|------------------|----------------------|
| Noma'lum `slug` yoki noto'g'ri `k` | `404 NOT_FOUND` | "Havola topilmadi" | maktabdan TO'G'RI havolani so'raydi |
| `slug` bor, dastur yo'q | `409 NO_PROGRAM_AVAILABLE` | "Hozircha test mavjud emas — maktabingizga murojaat qiling." | kutadi; maktab admini dasturni yoqadi |

```json
{ "type": "…", "title": "…", "status": 409, "code": "NO_PROGRAM_AVAILABLE",
  "detail": "Hozircha test mavjud emas — maktabingizga murojaat qiling." }
```

**Mavjud dastur mezoni** (`ProgramAvailability` — ommaviy va admin tomonda BITTA manba):

> `Status = Published` **va** `IsActive = true` **va** (`Visibility = Public` **yoki**
> `school_programs` orqali shu maktabga biriktirilgan)

**Javobda maktab haqida qo'shimcha ma'lumot YO'Q** — faqat holat va umumiy xabar (nom,
viloyat, testlar ro'yxati chiqarilmaydi).

**Xavfsizlik mulohazasi.** Bu javob `slug` MAVJUDLIGINI tasdiqlaydi (noma'lum slug baribir
`404` oladi), ya'ni `slug` bo'yicha sanash mumkin. Bu QABUL QILINADI: maktab havolasi
(`/t/{slug}?k=…`) o'quvchilarga ommaviy tarqatiladi — QR kod, e'lon, guruh xabari —
demak `slug` maxfiy emas. Haqiqiy sir — `accessToken` (`k`), u BU YERDA HAM tekshiriladi:
noto'g'ri token `409` emas, `404` beradi. Ya'ni yangi kod **tokenni bilmagan** kishiga
hech qanday yangi ma'lumot bermaydi.


---

### 1.1a `POST /api/public/schools/resolve-code` — maktab kodi (2026-09-07)
`/kirish` sahifasidagi **"Maktab uchun"** yo'li: o'quvchi maktab bergan 8 belgili kodni kiritadi,
server uni maktab havolasining tarkibiy qismlariga aylantiradi. Bu — MAVJUD oqimga (1.1 → 1.2)
kirish eshigi: mijoz javobdan `/t/{slug}?k={accessToken}` quradi va o'sha yerdan davom etadi.
Kod bilan kirgan o'quvchi havola bilan kirgan bilan BIR XIL huquqga ega (ikkalasi ham maktab
tarqatadigan sir). Auth yo'q. Kod **tanada** (URL/loglarga tushmasin).

```json
{ "code": "7K3M-9XQ2" }
```
`code` — xom matn: kichik harf, defis, bo'shliq qabul qilinadi (server normalizatsiya qiladi:
katta harf, defissiz, 8 belgi, alifbo `ABCDEFGHJKMNPQRSTUVWXYZ23456789`).

**200**
```json
{ "slug": "12-maktab-kokand", "accessToken": "…" }
```
ID YO'Q (`CLAUDE.md` 8-qoida). Maktab nomi ham yo'q — u 1.1 da keladi.

**404** `SCHOOL_CODE_INVALID` — **BITTA umumiy javob** quyidagi hammasi uchun: format noto'g'ri,
bunday kod yo'q, maktab nofaol, o'chirilgan, ommaviy makon (unda kod umuman yo'q). 1.1 dagi
`410 SCHOOL_INACTIVE` bu yerda ATAYLAB qaytarilmaydi — kod yagona sir, "nofaol" ≠ "yo'q"
farqi kodni sanab chiqayotganga tasdiq bo'lardi. Matn: "Kod topilmadi. Maktabingizdan tekshiring."
· **400** `VALIDATION_ERROR` (`code` maydoni yo'q) · **429** `RATE_LIMITED` (IP bo'yicha 10/5 daqiqa).

Muvaffaqiyatsiz urinish audit'ga yoziladi (`SchoolCode.ResolveFailed`, IP xeshi, kod qiymatisiz —
`docs/08` 8-bo'lim); muvaffaqiyat yozilmaydi (keyingi qadam `school_link_views` ga tushadi).

---

### 1.2 `POST /api/public/sessions`
Anketa + sessiya ochish.

**P52 kengaytmasi (2026-09-11, `docs/18` §9.5; 2-to'lqin 2026-09-12, §9.6.2):** `fullName`
bundan mustasno, quyidagi so'rov maydonlarining har biri qaysi tanlangan dasturning
`registrationFields` sozlamasiga qarab majburiy/ixtiyoriy/kerak emas bo'lishi mumkin — mijoz
`GET /api/public/schools/{slug}` javobidagi `programs[].registrationFields` ga qarab formani
chizadi. `"Hidden"` maydon uchun yuborilgan qiymat serverda E'TIBORSIZ qoldiriladi (saqlanmaydi).

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
  "languageCode": "uz",
  "customFields": { "PARENT_JOB": "O'qituvchi" }
}
```

**`customFields`** (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) — ixtiyoriy, `{ "KOD": qiymat }`
(kod → qiymat; kod — `programs[].registrationForm.customFields[].code`). Matn turlarida
(`ShortText`/`LongText`/`Phone`) satr, `SingleChoice`da butun son, `MultiChoice`da butun sonlar
massivi (son — variantning `order`i, `value`si EMAS). Qoidalar: `Hidden` maydon uchun kelgan
qiymat E'TIBORSIZ qoldiriladi; `Required` maydon uchun qiymat kelmasa `400 VALIDATION_ERROR`
(`errors{KOD:[…]}`, MAKTAB oqimida HAR SAFAR tekshiriladi — boshqa asosiy maydonlar kabi,
mavjud o'quvchi topilgan taqdirda ham); tur bo'yicha noto'g'ri shakl/qiymat ham
`400 VALIDATION_ERROR` beradi. Tasdiqlangan javoblar `Student.ProfileExtra`ga yoziladi.

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
- Avval dastur tanlanadi (`programCode`; bitta dastur bo'lsa avtomatik, bir nechta bo'lsa
  **400** `PROGRAM_REQUIRED`, mavjud bo'lmasa **404**). Keyingi tekshiruvlar **shu dastur**
  bo'yicha (BR-1, `(o'quvchi, dastur)` juftligi).
- Dublikat (maktab + normalizatsiyalangan FISH + tug'ilgan sana) topilsa, **tanlangan dasturda**:
  - tugallanmagan sessiya bor → **o'sha sessiya qaytariladi**, `200`, `resumed: true`;
  - 90 kun ichida yakunlangan sessiya bor → **409** `DUPLICATE_ASSESSMENT`;
  - hech biri yo'q → yangi sessiya, `201`.
- Boshqa dasturdagi sessiyalar (yarim qolgan yoki yakunlangan) ta'sir qilmaydi: A dasturni
  yakunlagan (yoki A da yarim qolgan) o'quvchi B dasturni yangi sessiya bilan boshlaydi.
  Natijada bir o'quvchida bir vaqtda bir nechta (har dasturda ko'pi bilan bitta) tugallanmagan
  sessiya bo'lishi mumkin — har biri o'z `sessionToken`i bilan.
- `consentAccepted != true` → 400.
- Rate limit: IP bo'yicha soatiga 10; maktab kunlik limiti.

**400** `VALIDATION_ERROR` · **409** `DUPLICATE_ASSESSMENT` · **429** `RATE_LIMITED`

#### Anonim oqim — `registrationMode: "None"` dastur (P52, 2026-09-11)

Tanlangan dastur (`programCode` orqali aniqlangan yoki maktabda yagona) `registrationMode
= "None"` bo'lsa, yuqoridagi ODATIY (`Full`) oqim **umuman ishlamaydi** — mijoz registratsiya
ekranini ko'rsatmaydi va so'rovni shaxs maydonlarisiz yuboradi:

```json
{
  "slug": "12-maktab-kokand",
  "accessToken": "…",
  "consentAccepted": true,
  "languageCode": "uz",
  "programCode": "CAREER_SURVEY"
}
```

- `fullName`/`birthDate`/`gender`/`grade`/`phone`/`parentPhone`/`email` **talab qilinmaydi** —
  yuborilsa ham **e'tiborsiz qoldiriladi** (saqlanmaydi). `consentAccepted` HAMON majburiy —
  bu huquqiy rozilik, rejimdan qat'i nazar.
- Server o'quvchini **ANONIM** yaratadi (`Student.CreateAnonymous`) — `fullName` PII EMAS
  (`"Anonim ishtirokchi #A1B2C3"` shaklida), `birthDate`/`phone` bazada `null`. Agar
  so'rovnomaning O'ZIDA shaxs savollari bo'lsa (masalan `Survey` rejimdagi anketa `Q1_1`
  kabi savol kodlari bilan), shaxs ma'lumoti O'SHA javoblarda qoladi — eksportda ko'rinadi,
  lekin `Student` yozuvida YO'Q.
- **BR-1 (90 kunlik takror topshirish) va "davom ettirish" (`resumed: true`) ISHLAMAYDI** —
  identifikator (FISH+tug'ilgan sana) yo'q. HAR so'rov yangi anonim `Student` va yangi
  `Assessment` yaratadi (`201`, `resumed: false`) — bir xil brauzerdan bir necha marta kirish
  mumkin. Bu qabul qilingan cheklov (egasining qarori).
- Qolgan qoidalar (`consentAccepted`, kunlik ro'yxatdan o'tish limiti, `accessCode`, rate limit)
  `Full` rejimi bilan BIR XIL.

**Qat'iy invariant:** `hasPersonalityBattery: true` bo'lgan dasturda `registrationMode` hech
qachon `"None"` bo'lmaydi (domen darajasida qulflangan, `docs/06` §6
`REGISTRATION_REQUIRED_FOR_BATTERY`) — ya'ni bu anonim oqim orqali shaxsiyat tipi/Holland
kodi/`MaturityIndex` HECH QACHON hisoblanmaydi.

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
  "sections": null,
  "questions": [
    { "id": "…", "code": "B5-Q01", "order": 1, "text": "Yangi g'oyalarni sinab ko'rishni yaxshi ko'raman",
      "type": "Likert5", "isRequired": true, "options": null, "currentValue": 4,
      "sectionId": null, "placeholder": null, "inputPattern": null, "maxLength": null,
      "minSelections": null, "maxSelections": null, "visibility": null,
      "currentText": null, "currentValues": null }
  ]
}
```

> `scale` va `scaleDirection` **hech qachon** frontendga yuborilmaydi.

> **P52 tarmoqlanuvchi so'rovnoma** (`docs/18` §4.1) — to'liq shakl, `sections[]` va yangi
> savol maydonlari (`sectionId`/`placeholder`/`inputPattern`/`maxLength`/`minSelections`/
> `maxSelections`/`visibility`/`currentText`/`currentValues`) o'sha yerda. **Qisqa qoida:**
> anketada bo'lim bo'lsa sahifalash O'CHADI (`page=1`, `totalPages=1`, BARCHA faol savollar) —
> tarmoqlanishni mijoz o'zi (`shared/lib/visibility.ts`) hisoblaydi. Yuqoridagi 4 ta tizim
> metodikasida (`sections: null`) va bo'limsiz `Custom` anketalarda hech narsa o'zgarmagan.

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

> **P52** (`docs/18` §4.2) — `value` endi `int?`; yangi `text`/`selectedValues` maydonlari
> qo'shildi, uchtadan AYNAN bittasi savol turiga mos to'ldiriladi (`400 ANSWER_SHAPE_INVALID`).
> Turga xos mazmun tekshiruvi (matn uzunligi/shablon, `MultiChoice` tanlovlari) va
> **`400 QUESTION_NOT_VISIBLE`** qo'riqchisi (ko'rinmaydigan savolga yozishga urinish) —
> to'liq jadval va ReDoS himoyasi `docs/18` §4.2 da. Likert/Binary/SingleChoice/ForcedChoice
> uchun `value` — mavjudidek o'zgarishsiz.

---

### 1.7 `POST /api/public/sessions/tests/{testCode}/complete`
**200**
```json
{ "testCode": "BIG5", "status": "Completed",
  "nextTestCode": "RIASEC", "allTestsCompleted": false }
```
**400** `VALIDATION_ERROR` (`unansweredCount` bilan)

> **P52** (`docs/18` §4.3) — majburiy savol tekshiruvi FAQAT ko'rinadigan savollar bo'yicha;
> yashirilgan savollarning javoblari yakunlashda bazadan o'chiriladi. Tizim metodikalarida
> (shart/bo'lim yo'q) xatti-harakat o'zgarishsiz.

---

### 1.8 `POST /api/public/sessions/complete`
Barcha testlar tugagach yakuniy tasdiq. Ballar, ishonchlilik indeksi va o'quvchi
snapshoti **har doim** shu yerda hisoblanadi.

**AI tahlili esa standart holatda navbatga QO'YILMAYDI** — u admin panelidagi
"AI tahlil qilish" tugmasi bilan ishga tushiriladi (2026-09-03, egasining qarori:
AI xarajati nazorati). Avtomatik navbat `Ai:AutoAnalyzeOnCompletion` bayrog'i bilan
qaytariladi (`docs/09` §8.0, `docs/13` §6.1).

**200** — standart konfiguratsiya (bayroq `false`):
```json
{ "status": "Completed", "message": "Javoblaringiz saqlandi. Rahmat!",
  "showResultToStudent": true, "resultAvailableAt": null }
```

`Ai:AutoAnalyzeOnCompletion = true` bo'lsa `status` — `"Analyzing"`, `message` esa
"Natijalaringiz qayta ishlanmoqda." Xabar holatga qarab o'zgaradi: tahlil navbatga
qo'yilmagan bo'lsa "qayta ishlanmoqda" deyish o'quvchiga yolg'on bo'lardi.

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

### 1.10 `GET /api/public/type-catalog`
Ochiq tanishtiruv (marketing) kontenti — 16 ta shaxsiyat tipining tavsifi. **Autentifikatsiya
YO'Q**: na `X-Session-Token`, na maktab havolasi kerak. Mijozda `/metodika` sahifasidagi
tiplar bo'limi va `/metodika/:kod` sahifalari shu javobdan quriladi.

**200**
```json
{
  "types": [
    {
      "code": "ENFJ",
      "name": "Ilhomlantiruvchi",
      "shortDescription": "Odamlarni birlashtiradigan, ularning o'sishiga e'tibor qaratadigan yetakchi.",
      "longDescription": "ENFJ — ...",
      "strengths": ["..."],
      "growthAreas": ["..."],
      "careerHints": ["..."]
    }
  ]
}
```

- Tartib — `code` bo'yicha alifbo tartibida (`type_catalog` da ko'rsatish tartibi ustuni yo'q;
  mijozning "oldingi/keyingi tip" navigatsiyasi shu barqaror ketma-ketlikka tayanadi).
- Matnning hammasi `SeedData/type-catalog.json` dan keladi (loyihaning o'z o'zbekcha kontenti,
  `CLAUDE.md` 6a-qoida). Tiplar guruhlarga bo'linmaydi — guruh nomlari raqobatchi tasnifi.
- Javobda **ball, shkala yoki yo'nalish yo'q** (`CLAUDE.md` 9-qoida) — kontent butunlay tavsifiy.
- Baza seed qilinmagan bo'lsa `{"types": []}` qaytadi (xato emas) — mijoz "bo'sh holat" ko'rsatadi.
- Kesh: `PublicCatalogCache` da 1 soat (jadval faqat seed orqali o'zgaradi).
- Tezlik cheklovi: IP bo'yicha 60/daqiqa (1.1 bilan bir xil `PublicSchoolInfo` siyosati).

**429** — limitdan oshdi

---

## 2. Autentifikatsiya (superadmin)

| Metod | Yo'l | Izoh |
|-------|------|------|
| POST | `/api/auth/login` | `{username, password, totpCode?}` → `{accessToken, expiresIn, user}`; refresh token **`httpOnly` cookie** da qaytadi |
| POST | `/api/auth/refresh` | Tana **bo'sh**; refresh token `httpOnly` cookie'dan o'qiladi → yangi `accessToken` + rotatsiya qilingan cookie |
| POST | `/api/auth/logout` | Refresh tokenni bekor qiladi va cookie'ni tozalaydi |
| GET | `/api/auth/me` | Joriy foydalanuvchi |
| POST | `/api/auth/change-password` | `{currentPassword, newPassword}` |
| POST | `/api/auth/totp/enable` | 2FA o'rnatishni **boshlaydi** (hali yoqmaydi) → `{secret, otpauthUri, qrCodePngBase64, expiresAt}`; sir 10 daqiqa kutish holatida turadi |
| POST | `/api/auth/totp/confirm` | Tana: `{code}` (6 xonali) → `{backupCodes[8]}`. **2FA aynan shu yerda yoqiladi**; zaxira kodlar **faqat shu javobda bir marta** ko'rsatiladi |
| POST | `/api/auth/totp/disable` | 2FA o'chiradi; tana: `{currentPassword}` — o'g'irlangan sessiya 2FA ni o'chira olmasligi uchun (P13) |

5 marta xato parol → 15 daqiqa blok (`LockedUntil`). Login urinishlari `AuditLog` da.

> **2FA ikki bosqichli o'rnatish (P46 da tuzatildi).** Avval `totp/enable` 2FA'ni DARHOL
> yoqar edi va QR kod umuman qaytmasdi — foydalanuvchi 32 belgili sirni qo'lda ko'chirishga
> majbur bo'lar, xato qilsa keyingi kirishda hisob butunlay bloklanardi. Endi:
> `enable` sirni faqat **kutish holatida** saqlaydi (`TotpEnabled` `false` qoladi) va
> `otpauthUri` ning QR kodini xom base64 PNG sifatida qaytaradi (maktab QR bilan bir xil
> format); 2FA esa `confirm` ga ilovadan olingan to'g'ri kod kelgandagina yoqiladi.
> Tasdiqlanmagan sir 10 daqiqadan keyin yaroqsiz bo'ladi yoki yangi `enable` uni almashtiradi.
> Zaxira kodlar ham `confirm` javobida beriladi — tasdiqlanmagan o'rnatish DB'da xeshlangan
> kod qoldirmasligi uchun.

> **Eslatma (P19 da tuzatildi):** avval bu jadvalda `refreshToken` javob tanasida va so'rov
> tanasida ko'rsatilgan edi — bu `docs/08` bilan ziddiyatda edi (u yerda refresh token
> `httpOnly` cookie, JS uni hech qachon ko'rmaydi). Xavfsizlik hujjati ustun turadi:
> refresh token **faqat cookie** orqali yuradi, frontend `credentials: 'include'` bilan
> so'rov yuboradi. Bu XSS holatida refresh tokenning o'g'irlanishini oldini oladi.

---

## 2a. Ommaviy foydalanuvchi autentifikatsiyasi (Telegram) — P47

> **Maktab havolasi oqimi (`/t/{slug}?k=…`) BU BO'LIMDAN MUSTAQIL va O'ZGARMAGAN** — u yerda
> ro'yxatdan o'tish shart emas, o'quvchi maktab nomidan kiraveradi (§1.1–1.10).

| Metod | Yo'l | Auth | Izoh |
|-------|------|------|------|
| POST | `/api/auth/telegram` | yo'q | Telegram Login Widget obyekti → `{accessToken, expiresIn, isNewUser, user}`; refresh token **`httpOnly` cookie** da |
| POST | `/api/auth/telegram/refresh` | cookie | Tana **bo'sh** → yangi `accessToken` + rotatsiya qilingan cookie |
| POST | `/api/auth/telegram/logout` | cookie | Refresh tokenni bekor qiladi, cookie'ni tozalaydi (idempotent, `204`) |

### 2a.1 `POST /api/auth/telegram`

So'rov — Telegram Login Widget `onauth` callback'i bergan obyekt **o'zgartirilmasdan**
(kalitlar `snake_case`, chunki imzo aynan shu nomlardan hisoblangan):

```json
{
  "id": 123456789,
  "first_name": "Ali",
  "last_name": "Valiyev",
  "username": "alivali",
  "photo_url": "https://t.me/i/userpic/320/abc.jpg",
  "auth_date": 1767225600,
  "hash": "6f1a…64 belgili hex"
}
```

`last_name`/`username`/`photo_url` — ixtiyoriy; Telegram bermasa **umuman yubormaslik** kerak
(bo'sh satr yuborilsa imzo mos kelmaydi).

`200 OK`:

```json
{
  "accessToken": "eyJhbGciOi…",
  "expiresIn": 1800,
  "isNewUser": true,
  "user": {
    "id": "8f14e45f-ceea-467a-9e6b-1a2b3c4d5e6f",
    "username": "alivali",
    "firstName": "Ali",
    "lastName": "Valiyev",
    "photoUrl": "https://t.me/i/userpic/320/abc.jpg",
    "createdAt": "2026-09-05T10:12:00Z",
    "lastLoginAt": "2026-09-05T10:12:00Z"
  }
}
```

`refreshToken` javob tanasida **HECH QACHON** bo'lmaydi — faqat
`Set-Cookie: srm_public_refresh_token=…; HttpOnly; Secure; SameSite=Strict; Path=/api/auth/telegram`.
Frontend `credentials: 'include'` bilan so'rov yuboradi.
`user.telegramId` ham **qaytarilmaydi** (minimallik prinsipi, `docs/08` §5).

Xatolar:

| Kod | HTTP | Qachon |
|-----|------|--------|
| `VALIDATION_ERROR` | 400 | `id ≤ 0`, `auth_date ≤ 0`, `hash` 64 belgili hex emas |
| `TELEGRAM_AUTH_INVALID` | 401 | Imzo mos kelmadi (maydon o'zgartirilgan yoki boshqa bot tokeni) |
| `TELEGRAM_AUTH_EXPIRED` | 401 | `auth_date` 24 soatdan eski (yoki 5 daqiqadan ko'p kelajakda) |
| `RATE_LIMITED` | 429 | IP bo'yicha 10/5 daqiqa |
| `TELEGRAM_AUTH_NOT_CONFIGURED` | 503 | Serverda `Telegram:BotToken` berilmagan |

### 2a.2 `POST /api/auth/telegram/refresh`

Tana bo'sh. `200 OK` → `{ "accessToken": "…", "expiresIn": 1800 }` + rotatsiya qilingan cookie.
`401 UNAUTHORIZED` — token yo'q / yaroqsiz / muddati o'tgan / **qayta ishlatilgan**.
Qayta ishlatish aniqlanganda foydalanuvchining BARCHA refresh tokenlari bekor qilinadi va
`PublicSecurity.RefreshReuse` audit yozuvi qoldiriladi (superadmin oqimidagi bilan aynan bir xil).

### 2a.3 `POST /api/auth/telegram/logout`

Tana bo'sh. Har doim `204 No Content` (idempotent), cookie tozalanadi.

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
| POST | `/api/admin/schools/{id}/regenerate-entry-code` | Yangi maktab kodi → `{ entryCode }` (`XXXX-XXXX`); eski kod darhol yaroqsiz, audit `School.EntryCodeRegenerated` (2026-09-07) |
| POST | `/api/admin/schools/{id}/toggle-active` | Faol/nofaol |
| DELETE | `/api/admin/schools/{id}` | Soft delete (o'quvchisi bo'lsa 409) |
| GET | `/api/admin/schools/link-health` | Tizim bo'yicha "nechta maktab havolasi ishlamaydi" (dashboard banneri) |

`SchoolListItemDto`: `id, name, region, district, slug, publicUrl, isActive, studentCount, completedCount, lastActivityAt, linkHealth, entryCode`

#### `linkHealth` — "bu havola ishlaydimi" (2026-09-03)

Ro'yxatda ham, `GET /api/admin/schools/{id}` javobida ham bor. Maqsad: admin dasturni
o'chirganda maktab havolasi jimgina o'lib qolmasin — panel buni AYTSIN.

```json
{ "linkHealth": { "status": "NoProgramAssigned", "availableProgramCount": 0, "usableProgramCount": 0 } }
```

| `status` | Ma'nosi | Ommaviy javob |
|----------|---------|---------------|
| `Ok` | mavjud dastur bor va unda yaroqli test bor | `200` |
| `NoProgramsAtAll` | tizimda umuman dastur yo'q | `409 NO_PROGRAM_AVAILABLE` |
| `NoProgramAssigned` | dasturlar bor, lekin hech biri `Public` emas va bu maktabga biriktirilmagan | `409 NO_PROGRAM_AVAILABLE` |
| `ProgramsDeactivated` | mos dastur bor, lekin o'chirilgan/arxivlangan | `409 NO_PROGRAM_AVAILABLE` |
| `ProgramsWithoutTests` | mavjud dastur bor, lekin unda nashr qilingan/faol va savoli bor test yo'q | `200`, lekin sessiyada test bo'lmaydi |

`availableProgramCount` — 1.1-bo'limdagi **aynan bir xil** mezon bo'yicha hisoblanadi.
`availableProgramCount == 0` ⟺ ommaviy javob `409 NO_PROGRAM_AVAILABLE`. Ikkisi ajralib
ketmasligi test bilan qulflangan (`SchoolLinkHealthCriterionTests`) — aks holda panel
"hammasi joyida" deb yolg'on aytardi.

**`GET /api/admin/schools/link-health` — 200**

```json
{
  "activeSchoolCount": 42,
  "brokenSchoolCount": 3,
  "schools": [
    { "id": "…", "name": "12-son maktab",
      "linkHealth": { "status": "NoProgramAssigned", "availableProgramCount": 0, "usableProgramCount": 0 } }
  ]
}
```

FAQAT **faol** maktablar hisoblanadi (nofaol maktab havolasi `410 SCHOOL_INACTIVE` bilan
ATAYIN ishlamaydi — u yolg'on ogohlantirish bermasligi kerak). `schools[]` — ko'pi bilan
10 ta namuna; qolgani `brokenSchoolCount - schools.length`.

#### Dastur holati — `state` (2026-09-06)

Dasturlar admin API'si (`/api/admin/programs`, P34/P35) bu hujjatda to'liq yozilmagan; quyida
faqat 2026-09-06 da o'zgargan qism.

`AdminProgramListItemDto` va `AdminProgramDetailDto` javoblarida ilgari `status`
(`Draft`/`Published`/`Archived`) va `isActive` (bool) maydonlari **alohida** kelardi. Endi
ular OLIB TASHLANDI, o'rniga bitta maydon beriladi:

```json
{ "state": "Paused" }   // "Draft" | "Active" | "Paused" | "Archived"
```

Ma'nosi va (`Status`, `IsActive`) juftligi bilan bog'liqligi — `docs/04` 2.13-bo'lim.
`AdminPublicSpaceProgramDto` (`GET /api/admin/public-space`) ham AYNAN shu `state` maydonini
qaytaradi — ikki bo'lim bitta manbadan (`AssessmentProgram.State`) o'qiydi.

**Ro'yxat filtri:** `GET /api/admin/programs?state=Paused`. Eski `?status=` va `?isActive=`
parametrlari OLIB TASHLANDI (orqaga moslik saqlanmadi: bu ichki admin API va uning yagona
mijozi — shu repodagi frontend). Noma'lum yoki raqamli qiymat jimgina e'tiborsiz qoladi —
ro'yxat filtrsiz qaytadi.

**`POST /api/admin/programs/{id}/toggle-active`** — `Active ⇄ Paused`. Faqat nashr qilingan
dasturda ishlaydi: `Draft` yoki `Archived` da **`409 PROGRAM_INVALID_TRANSITION`**.

**`POST /api/admin/programs/{id}/restore`** — `Archived ──▶ Paused` (2026-09-06, arxivdan
tiklash). Tanasiz. Javob `200` + `AdminProgramDetailDto` (`"state": "Paused"`). Tiklangan dastur
o'quvchiga DARHOL ko'rinmaydi — keyin `toggle-active` bilan aniq faollashtiriladi (sabab:
`Archive()` maktab biriktirishlarini saqlab qoladi; `docs/04` 2.13). Xatolar: `404 NOT_FOUND`;
arxivda bo'lmagan (`Draft`/`Active`/`Paused`) dasturda **`409 PROGRAM_INVALID_TRANSITION`**.
Audit: `Program.Restored`.

#### Dastur detalida ommaviy makon — `isAssignedToPublicSpace` (2026-09-07)

`GET /api/admin/programs/{id}` (`AdminProgramDetailDto`, shuningdek dastur mutatsiyalarining
javobi) ommaviy makon biriktirmasini **alohida maydonda** beradi:

```json
{ "assignedSchoolIds": ["…"], "isAssignedToPublicSpace": true }
```

`school_programs` da ommaviy makon ham oddiy qator (`schools.kind = PublicSpace`), lekin u
"maktab" emas: `assignedSchoolIds` da FAQAT `kind = School` makonlar qoladi, ommaviy makon
esa bayroqqa o'tadi. Aks holda UI uni maktab chipi deb chizardi va
`GET /api/admin/schools/{id}` (`AdminSchoolScope.SchoolsOnly`) unga `404` qaytarardi.

Dastur tomonidan biriktirish/olib tashlash uchun **yangi endpoint yo'q** — mavjud
`POST | DELETE /api/admin/public-space/programs/{programId}` ishlatiladi (3.7-bo'lim). Bu
endpoint dastur holatini **tekshirmaydi** (`Draft` ham `200`, idempotent) — "faqat faol
dastur biriktiriladi" cheklovi UI tomonida: `Active` bo'lmagan dastur ommaviy foydalanuvchiga
baribir ko'rinmaydi (`ProgramAvailability`: `Published && IsActive`).

#### Dastur `registrationMode` — admin CRUD (P52, 2026-09-11)

`AdminProgramListItemDto`/`AdminProgramDetailDto` javoblarida (yuqoridagi `state` bilan bir
qatorda) endi `registrationMode` (`"Full"`/`"None"`) ham keladi:

```json
{ "state": "Active", "registrationMode": "Full" }
```

`POST /api/admin/programs` va `PUT /api/admin/programs/{id}` so'rov tanasida ixtiyoriy
`registrationMode` (satr, standart `"Full"`):

```json
{ "code": "CAREER_SURVEY", "nameUz": "Kasb so'rovnomasi", "descriptionUz": null,
  "displayOrder": 5, "visibility": "Public", "registrationMode": "None" }
```

**Qat'iy invariant** (`docs/04` §2.13, `docs/06` §6): dasturda ilmiy shaxsiyat batareyasi
bo'lsa (`Standard` + `Scored` metodika — `PersonalityBattery.ContainedIn`) `registrationMode`
`"None"`ga o'rnatib bo'lmaydi — `PUT`da **`400 REGISTRATION_REQUIRED_FOR_BATTERY`**. Tekshiruv
IKKI nazorat nuqtasida: `PUT` (rejim o'zgartirilganda) va `POST /publish` (nashr qilinganda —
masalan `None` dasturga keyinroq batareya testi biriktirilib nashr qilinsa).

`Code`/`Kind`/`IsSystem` bilan bir xil qoida: yangi (`Create`) dasturda hali test yo'q, shu
sabab `registrationMode` bu bosqichda invariantni buza olmaydi — tekshiruv faqat testlar
biriktirilgach (`PUT`/`publish`) ishlaydi.

#### Dastur `registrationFields` — admin CRUD (P52 kengaytmasi, 2026-09-11)

`AdminProgramDetailDto` javobida (`registrationMode` bilan bir qatorda) endi `registrationFields`
ham keladi — HAR DOIM yechilgan (resolved) qiymatlar bilan (dastur `NULL` saqlagan bo'lsa ham):

```json
{ "state": "Active", "registrationMode": "Full",
  "registrationFields": { "birthDate": "Required", "gender": "Required", "grade": "Required",
    "classLetter": "Optional", "phone": "Required", "parentPhone": "Optional", "email": "Optional" } }
```

`POST /api/admin/programs` va `PUT /api/admin/programs/{id}` so'rov tanasida ixtiyoriy
`registrationFields` obyekti — HAR ICHKI maydon ham mustaqil ixtiyoriy (berilmagan maydon
standart qiymatga tushadi):

```json
{ "code": "CAREER_SURVEY", "nameUz": "Kasb so'rovnomasi", "displayOrder": 5,
  "visibility": "Public", "registrationFields": { "phone": "Hidden", "email": "Required" } }
```

`PUT` — `registrationMode` bilan bir xil TO'LIQ ALMASHTIRISH naqshi: `registrationFields`
obyektining o'zi umuman berilmasa, standart qiymatlarga qaytadi (mijoz doim joriy holatni
qayta yuborishi kerak — ichki maydonlar esa alohida-alohida ixtiyoriy).

**Qat'iy invariant** (`docs/04` §2.13, `docs/06` §6): dasturda ilmiy shaxsiyat batareyasi
bo'lsa `birthDate`/`grade` `"Required"`dan boshqasiga o'rnatib bo'lmaydi — `PUT`da
**`400 REGISTRATION_FIELD_REQUIRED_FOR_BATTERY`**. Tekshiruv IKKI nazorat nuqtasida:
`PUT` va `POST /publish` — `registrationMode`dagi bilan AYNAN bir xil naqsh. `gender` bu
invariantga KIRMAYDI, erkin sozlanadi (batareyali dasturda ham).

#### Dastur `hasPersonalityBattery` — admin ham (P52, 2026-09-11)

`AdminProgramListItemDto`/`AdminProgramDetailDto` javoblarida (yuqoridagi `state`/
`registrationMode` bilan bir qatorda) endi `hasPersonalityBattery` (bool) ham keladi:

```json
{ "state": "Active", "registrationMode": "Full", "hasPersonalityBattery": true }
```

**Texnik qarz yopildi** (`PROGRESS.md` risklar jadvali): ommaviy API'da bu bayroq
(`PublicProgramSummaryDto.HasPersonalityBattery`, 1.1-bo'lim) ALLAQACHON bor edi, lekin admin
javobida yo'q edi — frontend `programComputations.ts` da qattiq kod ro'yxati
(`"MBTI16"`/`"BIG5"`/`"RIASEC"`/`"ACTIVITY"`) bilan taxmin qilardi. Bu ikkinchi "sehrli satr"
edi (birinchisi — natija ekranidagi `"MBTI16"` qidiruvi, allaqachon domenga ko'chirilgan).

Mezon — AYNAN bir xil domen qoidasi: `Domain.Catalog.PersonalityBattery` (`Kind ==
TestKind.Standard && ScoringMode == TestScoringMode.Scored`), kod ro'yxati EMAS.
Dastur tarkibida kamida bitta shunday anketa bo'lsa — `true`. Ro'yxat endpointi
(`GET /api/admin/programs`) bayroqni BATCH so'rov bilan hisoblaydi — sahifadagi dasturlar soniga
qarab N+1 so'rov YO'Q (`ListProgramsQueryHandler`, `testCount` bilan bir xil naqsh).

#### `GET /api/admin/programs/{id}/impact?action=…` — amaldan OLDIN oqibat (2026-09-03)

Quyidagi endpoint ayni shu hodisa uchun qo'shildi va shu yerda hujjatlashtiriladi.

`action`: `deactivate` (`POST /toggle-active` bilan o'chirish) · `archive` · `makeAssigned`
(`PUT` orqali `Visibility: Public → Assigned`). Boshqa qiymat — `400 VALIDATION_ERROR`.

**200**
```json
{ "action": "deactivate", "affectedSchoolCount": 12,
  "schools": [ { "id": "…", "name": "12-son maktab" } ] }
```

`affectedSchoolCount` — amaldan keyin **umuman dastursiz qoladigan faol maktablar** soni
(hozir dasturi bor, keyin bo'lmaydi; ilgari ham dastursiz bo'lganlar HISOBGA OLINMAYDI).
`schools[]` — ko'pi bilan 20 ta namuna. Endpoint **read-only** va amalni **taqiqlamaydi** —
faqat tasdiq oynasi oqibatni ko'rsatishi uchun.

**`GET /api/admin/schools/{id}` — 200** (`SchoolDetailDto`): `id, name, region, district,
schoolNumber, contactPerson, contactPhone, slug, publicUrl, qrCodeBase64, accessCode, entryCode,
dailyRegistrationLimit, isActive, notes, createdAt, updatedAt, stats`

`entryCode` (2026-09-07) — maktab kodi, KO'RSATISH shaklida (`"7K3M-9XQ2"`); yaratish (`POST`)
javobida ham bor — admin uni havola/QR bilan birga maktabga beradi. `accessCode` (ixtiyoriy sinf
kodi) bilan ALOQASIZ. Ommaviy makon admin maktab endpointlariga kirmaydi, shu sabab amalda doim
to'ldirilgan (tip `string | null` — ustun nullable).

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
| GET | `/api/admin/students?schoolId=&grade=&status=&needsAttention=&personalityType=&activityLevel=&gender=&ageMin=&ageMax=&from=&to=&search=&page=&pageSize=&sort=` | Ro'yxat — **faqat maktab o'quvchilari** (pastda) |
| GET | `/api/admin/students/{id}` | **Individual profil** (pastda) |
| DELETE | `/api/admin/students/{id}?hard=true` | O'chirish (`hard=true` — o'quvchi so'rovi bo'yicha) |
| GET | `/api/admin/students/export?…` | `.xlsx` (filtr saqlanadi — ro'yxat bilan AYNAN bir xil parametrlar, `page`/`pageSize`/`sort`siz) |

> **Faqat maktab** (2026-09-07, egasining talabi): ro'yxat va eksport ommaviy makon
> (`SchoolKind.PublicSpace`) foydalanuvchilarini **shartsiz** chiqarib tashlaydi — ular o'z
> bo'limida, `GET /api/admin/public-space/users` (3.7). Ilgari bu yerda bo'lgan `?source=`
> parametri va `source` ustuni **olib tashlandi** (sessiyalar 3.3 da ham shu qoida; faqat
> boshqaruv paneli 3.6 da `source` qoladi). Yuborilgan `?source=` jimgina e'tiborsiz qoldiriladi.

**Ro'yxat filtrlari** (barchasi ixtiyoriy; enum qiymatlari — nomi, `docs/05` 3-bo'lim):

| Parametr | Tip / qiymatlar | Izoh |
|----------|-----------------|------|
| `schoolId` | `uuid` | Maktab |
| `grade` | `1..11` | Sinf |
| `status` | `Draft` \| `InProgress` \| `Completed` \| `Analyzing` \| `Analyzed` \| `AnalysisFailed` \| `Abandoned` | Shu holatdagi sessiyasi BOR o'quvchilar (`EXISTS`) |
| `needsAttention` | `true`/`false` | Snapshot `needsAttention` |
| `personalityType` | `INTJ`… | Snapshot `lastPersonalityType` |
| `activityLevel` | `Passive` \| `LowActive` \| `Moderate` \| `Active` \| `HighlyActive` | Snapshot `lastActivityLevel` |
| `gender` | `Male` \| `Female` | Boshqa qiymat (`Unspecified`, raqam) → **400** `VALIDATION_ERROR` |
| `ageMin`, `ageMax` | butun son, `6..99`, `ageMin <= ageMax` | To'liq yosh; aks holda **400** `VALIDATION_ERROR` |
| `from`, `to` | ISO sana-vaqt | `lastAssessmentAt` oralig'i |
| `search` | matn | F.I.Sh. bo'yicha (`ix_students_name_trgm`) |

> **Yosh → `birthDate`** (DB darajasida, bugungi sana serverning UTC sanasi):
> `age >= ageMin` ⇔ `birthDate <= today − ageMin yil`; `age <= ageMax` ⇔
> `birthDate > today − (ageMax + 1) yil`. Bugun tug'ilgan kuni bo'lgan o'quvchi `ageMin`ga
> kiradi, bugun `ageMax + 1` yoshga to'lgani kirmaydi (`Student.CalculateAge` bilan bir xil).
> `status`/`activityLevel` noma'lum qiymatda filtr jimgina qo'llanmaydi (tarixiy xatti-harakat).

`StudentListItemDto`: `id, fullName, schoolName, grade, classLetter, phone, lastAssessmentStatus,
personalityType, personalityTypeName, maturityIndex, activityLevel, needsAttention, reliabilityFlag,
lastAssessmentAt`

> **`personalityTypeName`** (2026-09-03) — `personalityType` KODIGA (`"INTJ"`) mos to'liq
> o'zbekcha nom (`"Loyihachi"`), manba `type_catalog.name_uz` (seed: `type-catalog.json`,
> mustaqil yozilgan — `CLAUDE.md` 6a-band). Katalogda yozuv topilmasa `null`: mijoz shunda
> FAQAT kodni ko'rsatadi, soxta nom o'ylab topilmaydi. Sabab: ro'yxatda yolg'iz `INTJ`
> tushunarsiz — nom asosiy, kod ikkinchi darajali (`ART`/`Artistik` naqshi bilan bir xil).

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
| GET | `/api/admin/assessments?schoolId=&status=&from=&to=&page=&pageSize=&sort=` | Ro'yxat — **faqat maktab sessiyalari** (pastda) |
| GET | `/api/admin/assessments/{id}` | To'liq detal (`latestAssessment` yadrosi + sessiya sarlavhasi + `tests[]`) — ommaviy sessiya uchun ham ochiq (pastda) |
| GET | `/api/admin/assessments/{id}/answers?testCode=` | Savolma-savol javoblar va tahlili (audit uchun) |
| POST | `/api/admin/assessments/{id}/rerun-analysis` | `{ "provider": "Anthropic", "promptVersion": "v1.1" }` → 202 |
| POST | `/api/admin/assessments/{id}/recalculate-scores` | Scoring versiyasi o'zgargan bo'lsa |
| GET | `/api/admin/assessments/{id}/report.pdf` | PDF hisobot |
| DELETE | `/api/admin/assessments/{id}` | Soft delete |

> **Faqat maktab** (2026-09-07, egasining qarori — 3.2 bilan bir xil): ro'yxat ommaviy makon
> (`SchoolKind.PublicSpace`) sessiyalarini **shartsiz** chiqarib tashlaydi — ular
> `GET /api/admin/public-space/users` (3.7) → foydalanuvchi profili (3.2 `{id}`) orqali
> ko'rinadi. 2026-09-06 da qo'shilgan `?source=` parametri va `source` ustuni **olib tashlandi**;
> yuborilgan `?source=` jimgina e'tiborsiz qoldiriladi. Boshqaruv paneli (3.6) ataylab ikki
> kesimli — u yerda `source` qoladi.
>
> **`{id}` amallari 404 QILINMAYDI.** Ro'yxatdan yashirish ≠ yozuvni yo'q qilish: ommaviy
> foydalanuvchi profili (`/admin/students/{id}`) `{id}/answers`, `rerun-analysis`,
> `report.pdf` endpointlarini ommaviy sessiya uchun ishlatadi; detal ham shu qatorda. Qulf:
> `AdminSourceFilterEndpointTests.Assessments_Royxat_OmmaviyMakonSessiyasiniHechQachonQaytarmaydi`.

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

**`GET /api/admin/assessments/{id}/answers?testCode=` — javob** (`AdminAssessmentAnswersDto`)
```json
{
  "answers": [
    { "questionId": "…", "questionCode": "BIG5-Q17", "testCode": "BIG5",
      "questionText": "Rejalarimni oxirigacha yetkazaman",
      "rawValue": 5, "textValue": null, "selectedValues": null, "selectedOptionText": null,
      "durationMs": 820, "revisionCount": 0, "answeredAt": "2026-08-30T09:11:02Z",
      "questionType": "Likert5",
      "scale": "C", "scaleNameUz": "Vijdonlilik", "scaleDirection": -1, "weight": 1.0,
      "effectiveValue": 1, "isFastAnswer": true, "straightLiningBlockIndex": null }
  ],
  "session": { "answeredCount": 190, "fastAnswerCount": 12, "straightLiningBlockCount": 1,
               "allSameAnswer": false, "shortSession": false, "totalDurationSeconds": 1740,
               "reliabilityScore": 62.0, "reliabilityFlag": "Questionable" },
  "scales": [ { "scale": "C", "scaleNameUz": "Vijdonlilik",
                "forwardCount": 6, "reverseCount": 4,
                "forwardAvgPct": 72.5, "reverseAvgPct": 31.25, "mismatchPct": 41.25 } ],
  "thresholds": { "fastAnswerDurationMs": 900, "straightLiningMinRunLength": 12,
                  "shortSessionMinutes": 6.0 }
}
```

> **Nega massiv emas, konvert** (2026-09-03, egasining talabi: "har bir savol uchun qanday
> javob bergani va tahlili"). Jadvalning o'zi savolma-savol ma'noni beradi, lekin
> `reliabilityScore` SESSIYA darajasida hisoblanadi (`docs/03` §7) — "nega 62" degan savolga
> javob berish uchun sessiya signallari, shkala darajasidagi teskari savol ziddiyati va
> `ScoringConstants` chegaralari ham kerak.

> ⚠️ **`effectiveValue` — jadvaldagi eng muhim maydon.** Teskari savolga
> (`scaleDirection = -1`) berilgan `5` shkalaga `1` bo'lib tushadi (`docs/03` §1:
> `v' = (max + min) − v`). Xom `5` ni ko'rgan psixolog javobni BUTUNLAY teskari o'qirdi.
> Qiymat `Domain/Scoring/ScoringMath.ApplyDirection` — strategiyalar ishlatadigan AYNAN o'sha
> funksiya — orqali hisoblanadi, `Application`da formula qayta yozilmaydi.

> **P52** (`docs/18` §2.1/§2.7): `rawValue` endi `int?` — `ShortText`/`LongText`/`Phone`
> javoblari `textValue`da, `MultiChoice` javoblari `selectedValues[]`da (bo'sh bo'lsa
> `null`). Bitta savolda uchtadan FAQAT bittasi to'ldirilgan bo'ladi. `Survey` (matn/ko'p
> tanlov) javoblarida `effectiveValue` shunchaki `0` (ma'nosiz — `scale = "SURVEY"`).

> ⚠️ **`scale`/`scaleNameUz`/`scaleDirection`/`effectiveValue` FAQAT ADMIN javobida.**
> `CLAUDE.md` 9-bandi O'QUVCHI API'siga tegishli: u yerda bu maydonlar o'lchanayotgan
> konstruktni va savolning teskari ekanini ochib berardi, ya'ni o'quvchi javobini
> moslashtirib natijani buzishi mumkin edi. Ommaviy javobda va swagger sxemasida
> yo'qligi `PublicTestQuestionsEndpointTests` da XOM JSON ustidan qulflangan.

> **`session`/`scales` filtrdan qat'i nazar BUTUN sessiya bo'yicha.** `testCode` faqat
> `answers` ro'yxatini toraytiradi. Sabab: `ReliabilityCalculator` ham sessiya darajasida
> ishlaydi (straight-lining 4 blok bo'ylab uzluksiz sanaladi, `docs/03` §7.1 band 3) —
> bitta blok ichida qayta hisoblansa BOSHQA, yolg'on qiymat chiqardi. `Survey`
> (ballanmaydigan) bloklar signallarga kirmaydi — `recalculate-scores` ham ularni
> `ReliabilityCalculator`ga bermaydi.

> **`straightLiningBlockIndex`** — javob TO'LIQ `straightLiningMinRunLength` (12) talik
> bir xil qiymat blokiga tushsa uning tartib raqami (1 dan), aks holda `null`. Barcha javob
> bir xil bo'lsa (`allSameAnswer: true`) bloklar BELGILANMAYDI — `docs/03` §7.1 band 1
> bo'yicha bu holatda faqat `AllSameAnswer` jarimasi qo'llanadi.

> **`thresholds`** — `Domain/Scoring/ScoringConstants` dagi qiymatlar. Mijoz ularni QO'LDA
> TAKRORLAMAYDI: konstanta o'zgarsa UI avtomatik ergashadi.

> **`scales[]`** — `docs/03` §7.1 band 4 dagi `d_shkala`: faqat IKKALA yo'nalish ham
> mavjud bo'lgan shkalalar; `mismatchPct = |forwardAvgPct − reverseAvgPct|`. Jarimaning
> o'zi (`d × 25`) bu yerda ko'rsatilmaydi — u sessiya darajasidagi o'rtachadan chiqadi.

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

**Bo'limlar** (faqat `Custom`, tarmoqlanuvchi so'rovnoma — `docs/18` §5)

| Metod | Yo'l | Tizim testida |
|-------|------|---------------|
| GET | `/api/admin/catalog/tests/{id}/sections` | ✅ (har doim bo'sh ro'yxat, B-3) |
| POST | `/api/admin/catalog/tests/{id}/sections` — `{ code, titleUz, descriptionUz, displayOrder, visibility }` | ❌ `409 SYSTEM_TEST_LOCKED` |
| PUT | `/api/admin/catalog/sections/{sectionId}` — `{ titleUz, descriptionUz, visibility }` (`code` o'zgarmaydi) | ❌ `409 SYSTEM_TEST_LOCKED` |
| DELETE | `/api/admin/catalog/sections/{sectionId}` | savollari bo'lsa `409 SECTION_IN_USE` |
| POST | `/api/admin/catalog/tests/{id}/sections/reorder` — `[{id, displayOrder}]` | ❌ `409 SYSTEM_TEST_LOCKED` |

Kod takrorlansa `409 SECTION_CODE_DUPLICATE`. `visibility` berilib anketa `ScoringMode = Scored`
bo'lsa `400 BRANCHING_NOT_ALLOWED_IN_SCORED` (B-2). To'liq shartnoma — `docs/18` §5.

**Savollar**

| Metod | Yo'l | Tizim testida |
|-------|------|---------------|
| GET | `/api/admin/catalog/tests/{id}/questions` | ✅ |
| POST | `/api/admin/catalog/tests/{id}/questions` | ❌ `409 SYSTEM_TEST_LOCKED` |
| PUT | `/api/admin/catalog/questions/{id}` | ⚠️ faqat `textUz`, `textRu`, `isActive` |
| DELETE | `/api/admin/catalog/questions/{id}` | ❌ `409 SYSTEM_TEST_LOCKED` |
| POST | `/api/admin/catalog/tests/{id}/questions/reorder` | ✅ (`[{id, displayOrder}]`) |
| POST | `/api/admin/catalog/tests/{id}/questions/import` | `Custom` — to'liq; tizim — faqat matn yangilash |

**`DELETE .../questions/{id}` — qo'shimcha xatolar** (P52, 2026-09-11 QA topilmasi: ilgari
FK buzilishi jimgina `500 INTERNAL_ERROR` bo'lib chiqardi):

| Holat | Javob |
|-------|-------|
| Savolga allaqachon javob berilgan (`Answers` jadvalida yozuv bor) | `409 QUESTION_IN_USE` — xabar harakatga yo'naltiruvchi: "Faol emas" holatiga o'tkazish tavsiya etiladi |
| Boshqa savol/bo'limning ko'rsatish sharti (`docs/18` B-5) shu savol kodiga tayanadi | `409 QUESTION_REFERENCED_BY_VISIBILITY` — xabarda havola qiluvchi savol/bo'lim kodi |

`POST .../questions` va `PUT .../questions/{id}` (`docs/18` §5) qo'shimcha maydonlarni qabul
qiladi: `sectionCode` (yoki `null` — bo'limsiz), `placeholder`, `inputPattern`, `maxLength`,
`minSelections`/`maxSelections` (`MultiChoice`), `visibility`, `options[]`
(`{ textUz, value, displayOrder }` — `SingleChoice`/`ForcedChoice`/`MultiChoice` uchun,
tahrirlashda **to'liq almashtiriladi**). Tizim savolida bu maydonlarning HAMMASI e'tiborsiz
(faqat `textUz`/`textRu`/`isActive`). `inputPattern` kompilyatsiya qilinmasa `400
INPUT_PATTERN_INVALID`. `ShortText`/`LongText`/`Phone`/`MultiChoice` yoki `visibility` `Scored`
anketada berilsa mos ravishda `400 QUESTION_TYPE_NOT_SCORABLE`/`BRANCHING_NOT_ALLOWED_IN_SCORED`
(B-1/B-2). `POST .../questions/import` savol elementlari ham xuddi shu qo'shimcha maydonlarni
qabul qiladi — `sectionCode` ko'rsatilgan bo'lim OLDIN import qilingan bo'lishi shart (aks holda
`404 NOT_FOUND`).

**Nashr validatsiyasi** (`CatalogPublishValidator`) — bo'lim/tarmoqlanish uchun yangi
`issues[]` kodlari (`docs/18` §5): `VISIBILITY_UNKNOWN_QUESTION`, `VISIBILITY_FORWARD_REFERENCE`
(B-4), `VISIBILITY_OPERATOR_MISMATCH`, `VISIBILITY_VALUE_UNKNOWN`, `QUESTION_OPTIONS_REQUIRED`
(`SingleChoice`/`MultiChoice` da 2 tadan kam variant), `QUESTION_OPTION_VALUE_DUPLICATE`,
`SECTION_EMPTY` (bo'limda faol savol yo'q — issue'da `sectionCode` to'ldiriladi), va
`INPUT_PATTERN_INVALID` (himoya sifatida, odatda saqlashda allaqachon ushlanadi).

**Import (JSON) va Excel chegarasi.** "Katalog → Import (JSON)" oqimi (`POST /tests` →
`POST .../sections` → `POST .../questions/import`) `sections[]`/`sectionCode`/`visibility`/
`options[]`ni to'liq qo'llab-quvvatlaydi — sxema namunasi:
`src/StudentRoadMap.Infrastructure/Persistence/SeedData/surveys/intellect-survey.json`
(egasining haqiqiy so'rovnomasi, endi bu fayl SEED — `docs/18` §7).
**Excel yo'li (`export.xlsx`/`import-template.xlsx`/`parse-excel`) KENGAYTIRILMAGAN** — jadval
shakli tarmoqlanishni (shartli ko'rinish) ifodalay olmaydi. Bo'limi yoki `visibility`si bor
anketani Excel'ga eksport qilish HOZIRCHA bu ma'lumotlarni TASHLAB YUBORADI (faqat tekis
savollar ro'yxati chiqadi) — superadmin buni Excel orqali TAHRIRLAB qaytarib import qilsa,
bo'lim/shart yo'qolgan holda saqlanadi. Tarmoqlanuvchi anketalar uchun faqat JSON yo'li
ishlatilishi kerak.

**Excel shablon, eksport va yuklash** (P39)

| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/catalog/tests/{id}/export.xlsx` | Mavjud anketani Excel'ga chiqaradi. **Tizim metodikasi uchun ham ochiq** — bu o'qish amali, BR-8 faqat o'zgartirishni qulflaydi |
| GET | `/api/admin/catalog/import-template.xlsx` | Bo'sh shablon: ko'rsatma varag'i + bitta namunaviy to'ldirilgan qator |
| POST | `/api/admin/catalog/import/parse-excel` | `multipart/form-data`, maydon nomi `file`. `.xlsx` ni o'qib **JSON import sxemasidagi obyektni** + `issues[]` qaytaradi. **Hech narsa saqlanmaydi** |

Uchala endpoint ham `SuperAdminPolicy` ostida.

**Varaqlar tuzilishi** — eksport va shablon uchun BIR XIL, ya'ni eksport ↔ import aylanma:

| Varaq | Ustunlar |
|-------|----------|
| `Anketa` | `Kod`, `Nomi`, `Tavsifi`, `Taxminiy daqiqa`, `Sahifa hajmi`, `Ballash rejimi` (`Scored`/`Survey`) — bitta qator |
| `Shkalalar` | `Shkala kodi`, `Nomi`, `Tavsifi` |
| `Oraliqlar` | `Shkala kodi`, `Dan`, `Gacha`, `Yorliq` |
| `Savollar` | `Savol kodi`, `Tartib`, `Matn`, `Shkala`, `Yo'nalish` (`1`/`-1`), `Og'irlik`, `Majburiy` (`Ha`/`Yo'q`), `Javob turi` (ixtiyoriy: `Likert5`/`Likert7`) |
| `Ko'rsatma` | Har ustun izohi, yo'nalish ma'nosi va **talqin oraliqlari qoidasi** (`docs/03` §6.3) |

> `Javob turi` — egasining ustun ro'yxatida yo'q, shu sabab OXIRGI ustun va IXTIYORIY (bo'sh
> bo'lsa `Likert5`). Usiz `Likert7` anketa aylanmada jimgina `Likert5`ga aylanib, ballash
> formulasini buzardi. Ustunlar TARTIBI ahamiyatsiz — moslashtirish sarlavha NOMI bo'yicha.

> `Ko'rsatma` varag'ida oraliq qoidasi ATAYLAB batafsil yozilgan: usiz superadmin faylni
> muvaffaqiyatli import qilib bo'lgach NASHRDA to'siqqa uriladi (`SCALE_BAND_*`) va sababini
> xato paydo bo'lgan joydan uzoqda qidiradi.

**`POST /api/admin/catalog/import/parse-excel` — javob (200)**
```json
{ "data": { "code": "STRESS", "nameUz": "…", "descriptionUz": null,
            "estimatedMinutes": 6, "pageSize": 10, "scoringMode": "Scored",
            "scales": [ { "code": "STRESS", "nameUz": "Stressga munosabat",
                          "descriptionUz": null,
                          "interpretationBands": [ { "from": 0, "to": 33, "label": "Past" } ] } ],
            "questions": [ { "code": "ST-Q01", "order": 1, "textUz": "…", "type": "Likert5",
                             "scale": "STRESS", "direction": 1, "weight": 1.0, "isRequired": true } ] },
  "issues": [ { "code": "QUESTION_DIRECTION_INVALID", "message": "5-qatorda yo'nalish faqat 1 yoki -1 bo'lishi mumkin.",
                "sheet": "Savollar", "row": 5, "questionCode": "ST-Q04", "scale": null } ] }
```

`data` shakli ATAYLAB frontend'dagi MAVJUD JSON import sxemasi bilan bir xil
(`features/catalog/model/importSchema.ts`) — u yerdagi oldindan ko'rish, validatsiya va
yaratish yo'li O'ZGARISHSIZ ishlatiladi, ya'ni ikkita parallel import mantiqi yo'q va
serverda vaqtinchalik holat saqlanmaydi. `issues[]` — qator darajasidagi muammolar; ular
javobni yiqitmaydi (`200`), faqat superadminga nimani tuzatishni aytadi. Talqin oraliqlari
QOIDASI bu yerda TAKRORLANMAYDI — u `CatalogPublishValidator` va uning frontend egizagi
(`interpretationBands.ts`) da, yagona nusxada.

**Fayl darajasidagi xatolar** (`data` qaytmaydi):

| Holat | Javob |
|-------|-------|
| ZIP/Open XML emas, buzilgan, bo'sh | `400 IMPORT_FILE_INVALID` |
| Eski `.xls` (ikkilik) | `400 IMPORT_FILE_INVALID` — alohida xabar: `.xlsx` sifatida saqlash kerak |
| Makroli `.xlsm` (`xl/vbaProject.bin` yoki `macroEnabled` content-type) | `400 IMPORT_FILE_INVALID` |
| Bitta varaqda 500 dan ortiq qator | `400 IMPORT_FILE_INVALID` |
| Fayl 2 MB dan katta yoki ZIP yoyilganda 20 MB dan oshadi | `413 PAYLOAD_TOO_LARGE` |

> `.xlsx` — ZIP arxiv, shu sabab tekshiruv KENGAYTMAGA emas, MAZMUNGA tayanadi: sehrli
> baytlar, arxiv yozuvlari soni, yoyilgan umumiy hajm (zip bomba) va `xl/workbook.xml`
> mavjudligi ClosedXML'ga berishdan OLDIN tekshiriladi. Formulalar HISOBLANMAYDI —
> Excel saqlagan keshlangan qiymat o'qiladi. Xato matnlari o'zgarmas o'zbekcha satrlardan:
> istisno xabari, kutubxona nomi yoki fayl yo'li javobga HECH QACHON tushmaydi (P31).

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

**`GET /api/admin/catalog/tests/{id}/questions` — savol qatori (`scaleNameUz`)**

```json
{ "id": "…", "code": "RS-Q03", "order": 3, "textUz": "Rasm chizishni yoqtiraman.",
  "textRu": null, "textEn": null, "type": "Likert5",
  "scale": "ART", "scaleNameUz": "Artistik", "scaleDescriptionUz": null,
  "direction": 1, "weight": 1.0, "isRequired": true, "isActive": true, "isSystem": true,
  "hasAnswers": false }
```

> **`hasAnswers`** (P52, 2026-09-11 QA topilmasi) — savolga allaqachon javob berilganmi
> (`QUESTION_IN_USE` sababi). Frontend o'chirish tugmasini shu maydonga qarab OLDINDAN
> o'chirib qo'yadi. Ro'yxat javobida (shu yerda va `POST .../questions/reorder`) BATCH so'rov
> bilan hisoblanadi — savollar soniga bog'liq bo'lmagan doimiy so'rovlar soni (N+1 emas,
> ADR-11); `POST .../questions` javobida yangi savol uchun har doim `false`.

`scaleNameUz` — shkalaning o'zbekcha nomi. Backend uni quyidagi tartibda aniqlaydi:

1. `TestScale.NameUz` — anketaning O'Z shkalasi bo'lsa (amalda `Custom`);
2. tizim shkalalari katalogi (`Domain/Catalog/SystemScaleCatalog.cs`) — tizim metodikasi bo'lsa,
   kalit `TestDefinition.ScoringStrategyCode` (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`), nomlar
   `docs/03` §2.1/§3.1/§4.1/§5.1 dan;
3. `null` — noma'lum kod.

`scaleDescriptionUz` — o'sha manbadan keladigan qisqa izoh (`docs/03` da bo'lsa yoki
`TestScale.DescriptionUz`); ko'pincha `null`. Nom va tavsif HAR DOIM bitta manbadan olinadi.

`null` **xato emas**, degradatsiya: noma'lum shkala jimgina noto'g'ri nom olmaydi, admin faqat
kodni ko'radi. `scale` (xom kod) baribir qaytadi — import formati va savol tahrirlash kod
bo'yicha ishlaydi.

> **Nima uchun nom backend'dan keladi.** Aks holda bitta ustun ikki manbadan to'lardi:
> `Custom` uchun bazadan, tizim uchun frontend i18n jadvalidan. Ikki manba vaqt o'tib
> bir-biridan uziladi va farq jimgina yuzaga chiqadi. Katalog seed JSON'idagi shkala kodlarini
> aynan qoplashi test bilan qulflangan (`SystemScaleCatalogDriftTests`).

> ⚠️ `scaleNameUz`/`scaleDescriptionUz` **o'quvchi API'siga hech qachon chiqmaydi** (`CLAUDE.md` 9-qoida). U
> `scale`dan ham xavfliroq: o'lchanayotgan konstruktni ochiq aytadi ("Artistik"), ya'ni o'quvchi
> javobini moslashtirib natijani buzishi mumkin. Yo'qligi
> `PublicTestQuestionsEndpointTests.GetTestQuestions_JavobVaSwaggerda_ScaleMaydoniYoq` da xom
> JSON ustidan tekshiriladi.

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

### 3.7 Ommaviy makon (`/api/admin/public-space`) — 2026-09-06/07

Ommaviy makon (`SchoolKind.PublicSpace`, bazada AYNAN BITTA yozuv) maktablar bo'limidan
TO'LIQ ajratilgan: `GET /api/admin/schools` uni qaytarmaydi, boshqarishning yagona joyi shu
bo'lim. `id` hech qayerda qabul qilinmaydi — makon bitta, handler uni o'zi topadi. Makonni
o'chirish/faolsizlantirish endpointi YO'Q (domen taqiqlaydi).

| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/public-space` | holat, biriktirilgan dasturlar, sozlamalar, statistika |
| GET | `/api/admin/public-space/users?search=&status=&page=&pageSize=&sort=` | ro'yxatdan o'tgan foydalanuvchilar |
| POST | `/api/admin/public-space/programs/{programId}` | dastur biriktirish (idempotent) |
| DELETE | `/api/admin/public-space/programs/{programId}` | biriktirishni olib tashlash (idempotent) |
| PUT | `/api/admin/public-space/show-result` | `{ "enabled": true }` — natija foydalanuvchiga ko'rinadimi |

Xato: makon seed qilinmagan bo'lsa barcha endpointlar `409 PUBLIC_SPACE_NOT_CONFIGURED`.

**`GET /api/admin/public-space` — `stats` bloki**
```jsonc
"stats": {
  "userCount": 128,          // ro'yxatdan o'tgan FAOL Telegram akkauntlari (`public_users`,
                             // `Student` EMAS — anketa to'ldirmaganlar ham kiradi; O'CHIRILGANLAR
                             // KIRMAYDI — `userCount` ≠ ro'yxat `totalCount`i, chunki 3.7dagi
                             // ro'yxat 2026-09-08dan o'chirilganlarni HAM qamraydi)
  "deletedUserCount": 3,     // "ma'lumotimni o'chiring" qilgan (anonimlashtirilgan) akkauntlar —
                             // 2026-09-08dan RO'YXATDA HAM ko'rinadi (`?status=deleted`), bu — shu son
  "totalAssessments": 96, "inProgressCount": 7, "completedCount": 74, "analyzedCount": 61,
  "lastActivityAt": "2026-09-04T12:00:00Z"   // sessiya bo'lmasa `null`
}
```

**`GET /api/admin/public-space/users` — 200**

Manba — `public_users` (LEFT JOIN `students` orqali `public_user_id`; bitta akkaunt → 0..1 profil).
Ro'yxatdan o'tgan, lekin hali anketa to'ldirmagan/test boshlamagan foydalanuvchi ham chiqadi.

Parametrlar:
- `search` — F.I.Sh. (anketa), Telegram ism/familiya/`username` bo'yicha, katta-kichik harf farqsiz
  (≤200 belgi); **TO'LIQ telefon raqami** bo'yicha ham (`+998XXXXXXXXX` yoki `998XXXXXXXXX` yoki
  mahalliy 9 xonali `XXXXXXXXX`) — faqat ANIQ TENGLIK, qisman raqam (masalan `"90123"`) hech
  narsaga mos kelmaydi (2026-09-08).
- `status` — OXIRGI sessiya bo'yicha: `all` (standart, o'chirilganlar HAM kiradi) · `never_started`
  (sessiya yo'q — anketa bo'lmasa ham) · `in_progress` (oxirgi sessiya yakunlanmagan:
  `Draft`/`InProgress`/`Abandoned`) · `completed` (`completedAt != null`:
  `Completed`/`Analyzing`/`Analyzed`/`AnalysisFailed`) · `deleted` (2026-09-08 — FAQAT
  "ma'lumotimni o'chiring" qilgan akkauntlar, sessiya holatidan mustaqil).
  Noma'lum qiymat → `400 VALIDATION_ERROR` (jimgina "hammasi"ga tushmaydi).
- `sort` — `registeredAt` (standart `-registeredAt`) yoki `lastLoginAt`; boshqa maydon → standart.
- `page`/`pageSize` — 4-bo'lim konvensiyasi (`pageSize` ≤ 100).

```jsonc
{
  "items": [
    {
      "publicUserId": "…",
      "telegram": { "firstName": "Bobur", "lastName": "Toshev", "username": "bobur_t" },   // hammasi nullable
      "registeredAt": "2026-09-01T09:00:00Z",   // `public_users.created_at`
      "lastLoginAt":  "2026-09-06T18:20:00Z",
      "studentId": "…",                // anketa to'ldirilmagan bo'lsa `null` (fullName/phone/age/grade ham)
      "fullName": "Toshev Bobur",
      "phone": "+998901234567",        // manba — `students.phone` (anketa); `public_users`da telefon
                                        // YO'Q (Telegram Login Widget bermaydi); anketa yo'q bo'lsa `null`
      "age": 17,
      "grade": 9,                      // `Student.NoGrade` (0 — "sinf yo'q") bo'lsa `null`
      "assessments": { "total": 3, "completed": 2, "inProgress": 1 },   // inProgress = Draft|InProgress;
                                                                        // total − completed − inProgress = Abandoned
      "deletedAt": null,               // "ma'lumotimni o'chiring" qilingan bo'lsa vaqt, aks holda `null`
      "deletionReason": null,          // `PublicUserDeletionReason` nomi (masalan `"PrivacyConcern"`), aks holda `null`
      "deletionComment": null,         // erkin matnli izoh, aks holda `null`
      "lastAssessment": {              // sessiya bo'lmasa `null`; oxirgisi — `startedAt` bo'yicha
        "id": "…", "status": "InProgress",
        "startedAt": "2026-09-06T18:21:00Z", "completedAt": null,
        "progress": {                  // FAQAT yakunlanmagan sessiyada (`completedAt == null`), aks holda `null`
          "testsTotal": 4, "testsCompleted": 1,
          "currentTestNumber": 2,      // 1 dan — "4 dan 2-blok"; hamma blok yakunlangan bo'lsa `null`
          "currentTestCode": "BIG5", "currentTestName": "Katta beshlik",
          "answered": 17, "questionsTotal": 44   // JORIY blokdagi javoblar
        }
      }
    }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1, "hasNext": false, "hasPrevious": false
}
```

"Qayerda to'xtagan" qoidasi ommaviy `GET /api/public/sessions/me` (1.3) bilan BITTA manbadan
hisoblanadi (`Application/Public/GetSession/SessionProgressCalculator`): `displayOrder` bo'yicha
birinchi `Completed` bo'lmagan blok — joriy blok. Bu ADMIN API — `id`lar qaytariladi
(8-qoida faqat ommaviy API uchun).

**O'chirilgan (anonimlashtirilgan) akkauntlar ENDI RO'YXATGA KIRADI (2026-09-08, egasining
qarori)** — ilgari ular butunlay yashirilardi va faqat `stats.deletedUserCount`da sanalardi.
Endi `deletedAt`/`deletionReason`/`deletionComment` orqali "nega o'chirilgani" ko'rinadi;
`telegram.*` bunday qatorda `null` (anonimlashtirish), `fullName`/`phone` esa `Student` yozuvi
alohida o'chirilmagani uchun odatda saqlanib qoladi. `?status=deleted` — faqat shular.
`stats.userCount` esa avvalgidek FAQAT faol akkauntlarni sanaydi — shu sabab `userCount` bu
ro'yxatning `totalCount`iga TENG BO'LMASLIGI mumkin (farqi taxminan `deletedUserCount`).

**Ish unumi:** N+1 yo'q — jami ≤5 so'rov, sahifa hajmiga bog'liq emas: `COUNT`, sahifa
(`ORDER BY … OFFSET/LIMIT`, holat filtri — oxirgi sessiya bo'yicha korrelyatsiyalangan
sub-so'rov), sahifadagi o'quvchilarning sessiyalari (`student_id IN`), yakunlanmagan oxirgi
sessiyalarning bloklari (`assessment_id IN`), anketa nomlari (`id IN`).

---

### 3.8 Sozlamalar — ro'yxatdan o'tish formasi (`/api/admin/settings`) — 2026-09-11/12

Egasining talabi: "Sozlamalar" sahifasidan ro'yxatdan o'tish formasini GLOBAL boshqarish
(`docs/18-tarmoqlanuvchi-sorovnoma.md` §9.6) — `AssessmentProgram.RegistrationFields`
(har dasturda alohida, `docs/18` §9.5) O'RNIGA. Faqat superadmin.

| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/admin/settings/registration-form` | Sozlama yo'q bo'lsa STANDART qaytadi (`null` emas) |
| PUT | `/api/admin/settings/registration-form` | To'liq almashtirish — `docs/05` jsonb shakli |

**`GET`/`PUT ... → RegistrationFormDefinitionDto`** (ikkalasi ham BIR XIL shakl, round-trip):

```json
{
  "coreFields": {
    "fullName":    { "requirement": "Required", "labelUz": "F.I.Sh.", "placeholderUz": null, "order": 1 },
    "birthDate":   { "requirement": "Required", "labelUz": "Tug'ilgan sana", "placeholderUz": null, "order": 2 },
    "gender":      { "requirement": "Required", "labelUz": "Jins", "placeholderUz": null, "order": 3 },
    "grade":       { "requirement": "Required", "labelUz": "Sinf", "placeholderUz": null, "order": 4 },
    "classLetter": { "requirement": "Optional", "labelUz": "Sinf harfi", "placeholderUz": null, "order": 5 },
    "phone":       { "requirement": "Required", "labelUz": "Telefon raqami", "placeholderUz": null, "order": 6 },
    "parentPhone": { "requirement": "Optional", "labelUz": "Ota-ona telefoni", "placeholderUz": null, "order": 7 },
    "email":       { "requirement": "Optional", "labelUz": "Email", "placeholderUz": null, "order": 8 }
  },
  "customFields": [
    { "code": "PARENT_JOB", "type": "ShortText", "labelUz": "Ota-onangiz kasbi",
      "placeholderUz": "Masalan: o'qituvchi", "requirement": "Optional",
      "maxLength": 200, "inputPattern": null, "options": null, "order": 9 }
  ]
}
```

- `coreFields` — sakkizta QATTIQ KODLANGAN maydon. `fullName.requirement` faqat `Required`
  bo'lishi mumkin (`PUT` boshqacha yuborsa `400 REGISTRATION_FORM_FULL_NAME_LOCKED`); yorlig'i/
  placeholder'i erkin tahrirlanadi.
- `customFields[].type` — `ShortText`/`LongText`/`Phone`/`SingleChoice`/`MultiChoice` (mavjud
  `QuestionType` nomlari — yangi atama yo'q). `SingleChoice`/`MultiChoice` uchun `options[]`
  (`{ textUz, value, order }`, kamida 2 ta, `value` unikal).
- `customFields[].code` — `^[A-Za-z0-9_-]{1,20}$`, sozlama ichida unikal, `coreFields` kalitlari
  (`fullName`, `birthDate`, ...) bilan to'qnashmaydi.
- `PUT` — TO'LIQ almashtirish (`registrationFields` PUT bilan bir xil semantika):
  `customFields` ro'yxati har safar to'liq beriladi, mijoz `GET`dan olgan obyektni tahrirlab
  qaytarishi kutiladi. Muvaffaqiyatli bo'lsa audit: `Settings.RegistrationFormUpdated`.
- Validatsiya xatolari — `docs/06` §6: `REGISTRATION_FORM_FULL_NAME_LOCKED` (400),
  `REGISTRATION_FORM_FIELD_CODE_INVALID` (400), `REGISTRATION_FORM_FIELD_CODE_DUPLICATE` (409),
  `REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT` (400), `REGISTRATION_FORM_OPTION_VALUE_DUPLICATE`
  (409), `INPUT_PATTERN_INVALID` (400).

> **2-to'lqin (2026-09-12) — ULANDI.** `POST /api/public/sessions` (§1.2) va `POST`/`PUT
> /api/me/sessions`/`profile` (§5.1b/§5.4) endi AYNAN shu sozlamadan o'qiydi
> (`RegistrationFormResolver`) — eski `AssessmentProgram.RegistrationFields` (`docs/18` §9.5)
> BOSHQA O'QILMAYDI. Superadmin qo'shgan `customFields[]` javoblari `Student.ProfileExtra`ga
> yoziladi (`docs/04` §2.2, `docs/05` `AddStudentProfileExtra` migratsiyasi).

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
| `POST /api/public/schools/resolve-code` | IP bo'yicha 10/5 daqiqa (2026-09-07, `AdminLogin` bilan bir xil qattiqlik) |
| `POST /api/auth/login` | IP bo'yicha 10/15 daqiqa |
| `POST /api/auth/telegram` | IP bo'yicha 10/5 daqiqa (P47) |
| `POST /api/me/sessions` | IP bo'yicha 10/soat (P47, ommaviy sessiya bilan bir xil) |
| Ommaviy kabinet (`/api/me/*`, `telegram/refresh`, `telegram/logout`) | IP bo'yicha 120/daqiqa (P47) |
| Admin API (umumiy) | 300/daqiqa |

**Swagger:** `/swagger` faqat `Development` va `Staging` da.

---

## 5. Ommaviy foydalanuvchi kabineti (`Authorize(Policy = "PublicUser")`) — P47

Barcha endpointlar `Authorization: Bearer <accessToken>` talab qiladi (Telegram kirishidan
olingan token). **Superadmin tokeni bu yerda ishlamaydi va aksincha** — ikki auditoriya
alohida JWT `aud` bilan ajratilgan (`docs/08` §2a).

| Metod | Yo'l | Izoh |
|-------|------|------|
| GET | `/api/me` | Profil (Telegram akkaunti) |
| GET | `/api/me/profile` | Saqlangan test anketasi (F.I.Sh., sana, telefon, rozilik holati) |
| PUT | `/api/me/profile` | Anketani FAQAT saqlash — sessiya ochilmaydi |
| GET | `/api/me/assessments` | Test sessiyalari tarixi |
| GET | `/api/me/assessments/{id}/result` | Bitta sessiyaning qisqartirilgan natijasi |
| POST | `/api/me/sessions` | Ommaviy (maktabsiz) sessiya ochish |
| DELETE | `/api/me` | O'z ma'lumotini o'chirish (anonimlashtirish) |

Umumiy xatolar: `401 UNAUTHORIZED` (token yo'q/yaroqsiz/akkaunt o'chirilgan),
`429 RATE_LIMITED` (IP bo'yicha 120/daqiqa; `POST /api/me/sessions` uchun 10/soat).

### 5.1 `GET /api/me`

`200 OK` — `2a.1` dagi `user` obyektining aynan o'zi.

### 5.1a `GET /api/me/profile` — saqlangan anketa (2026-09-07)

`GET /api/me` dan FARQ QILADI: u Telegram akkaunti (ism, rasm), bu — test uchun berilgan
rasmiy anketa (`Student`, `students.public_user_id` bo'yicha; bitta akkaunt → bitta profil).
Frontend `/kabinet/test` ni ochishda avval shuni o'qiydi va anketani **qayta so'ramaydi**.

```json
{
  "hasProfile": true,
  "fullName": "Karimov Sardor Alisherovich",
  "birthDate": "1995-04-12",
  "gender": "Male",
  "phone": "+998901234567",
  "grade": null,
  "email": null,
  "consentVersion": "1.0",
  "consentCurrent": true,
  "parentalConsent": false,
  "isMinor": false,
  "suggestedFullName": "Valiyev Ali",
  "registrationForm": {
    "coreFields": { "fullName": { "requirement": "Required", "labelUz": "F.I.Sh.", "placeholderUz": null, "order": 1 }, "…": "…" },
    "customFields": [
      { "code": "PARENT_JOB", "type": "ShortText", "labelUz": "Ota-onangiz kasbi",
        "placeholderUz": "Masalan: o'qituvchi", "requirement": "Optional", "maxLength": 200,
        "inputPattern": null, "options": null, "order": 9 }
    ]
  }
}
```

| Maydon | Izoh |
|--------|------|
| `hasProfile` | `false` — anketa hali to'ldirilmagan (**`200`**, `404` EMAS: bu oddiy holat). Shaxsiy maydonlar `null`. |
| `grade` | `null` — maktabda o'qimaydi (`Student.NoGrade` mijozga chiqmaydi) |
| `consentCurrent` | `consentVersion` joriy roziliknoma versiyasiga tengmi; `false` — anketa rozilikni qayta so'raydi |
| `isMinor` | yosh < 18 (`Student.CalculateAge`) — `parentalConsent` shu holatda talab qilinadi |
| `suggestedFullName` | Telegram `LastName + FirstName` (ikkalasi bo'lsa; bo'lmasa bori; hech biri yo'q — `null`). Bu **taklif**: yangi anketada F.I.Sh. maydoni shu bilan oldindan to'ldiriladi, foydalanuvchi tahrirlaydi (Telegram ismi ko'pincha rasmiy F.I.Sh. emas). AI'ga tushmaydi. |
| `registrationForm` | **P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2)** — GLOBAL ro'yxatdan o'tish formasi ta'rifi (`§3.8` bilan BIR XIL shakl), dastur ustunligi QO'LLANMAYDI (bu yerda dastur hali tanlanmagan). Mijoz shundan `customFields` formasini chizadi. Oldin to'ldirilgan `ProfileExtra` QIYMATLARI bu javobda YO'Q (faqat forma TA'RIFI) — keyingi to'lqinda kerak bo'lsa qo'shiladi. |

Hech qanday identifikator (`Student.Id`, `TelegramId`) qaytarilmaydi — egalik JWT bilan.

### 5.1b `PUT /api/me/profile` — anketani FAQAT saqlash (2026-09-07)

**Nima uchun alohida.** §5.4 (`POST /api/me/sessions`) profilni yangilaydi **va** sessiya
ochadi. Kabinetdagi "O'zgartirish" ilgari shu endpoint orqali ishlardi — foydalanuvchi faqat
telefonini to'g'rilamoqchi bo'lsa ham test boshlanib ketardi (egasi ko'rgan xato). Bu endpoint
o'sha anketa qoidalarini **aynan** qayta ishlatadi (backendda bitta umumiy modul), lekin
`Assessment` yaratmaydi, kunlik hisoblagichni oshirmaydi, dastur tanlamaydi.

So'rov tanasi — §5.4 dagi anketa maydonlari, **`languageCode`/`programCode` YO'Q**:

```json
{
  "fullName": "Karimova Malika Alisherovna",
  "birthDate": "1995-04-12",
  "gender": "Female",
  "phone": "+998911112233",
  "consentAccepted": true,
  "parentalConsent": null,
  "grade": 0,
  "email": "",
  "customFields": { "PARENT_JOB": "Dizayner" }
}
```

Majburiylik va tahrir semantikasi §5.4 jadvali bilan **bir xil**:

| Holat | Talab qilinadi | Natija |
|-------|----------------|--------|
| Profil YO'Q | `fullName`, `birthDate`, `gender`, `phone`, `consentAccepted: true`; 18 yoshgacha `parentalConsent: true`; `Required` `customFields` | ommaviy makonda yangi `Student` (`public_user_id` bilan), **sessiya yo'q** |
| Profil BOR, rozilik joriy | hech narsa — `{}` ham `200` | kelgan maydon tahrir, `null` o'zgarmaydi; `grade: 0` — sinf yo'q, `email: ""` — tozalash; kelgan `customFields` KODLARI mavjud `ProfileExtra`ga USTIDAN yoziladi (kelmagan kod o'zgarmaydi) |
| Profil BOR, rozilik eskirgan | `consentAccepted: true` | rozilik joriy versiya bilan qayta yoziladi |
| Profil BOR, voyaga yetmagan, bazada `parentalConsent: false` | `parentalConsent: true` | — |

**`customFields`** (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) — ixtiyoriy, §1.2 bilan BIR XIL
shakl/qoidalar, LEKIN majburiylik FAQAT profil YO'Q holatida tekshiriladi (`RequireFields`
bilan bir xil "bir marta so'raladi" naqshi) — profil BOR bo'lsa `Required` maydon ham qayta
so'RALMAYDI, faqat kelgan kod tahrirlanadi.

`200 OK` — yangilangan anketa, §5.1a bilan **aynan bir xil** shakl (`hasProfile: true`).
Frontend javobni `GET /api/me/profile` keshiga to'g'ridan-to'g'ri yozadi.

Xatolar:

| Kod | HTTP | Qachon |
|-----|------|--------|
| `VALIDATION_ERROR` | 400 | Format yoki profil holatiga ko'ra yetishmagan maydon — `errors{maydon:[xabar]}` (§5.4 bilan bir shakl) |
| `PUBLIC_SPACE_NOT_CONFIGURED` | 409 | Ommaviy makon seed qilinmagan |
| `RATE_LIMITED` | 429 | Umumiy `PublicUserApi` kvotasi (120/daqiqa); §5.4 dagi 10/soat bu yerga tegmaydi |

Makon faol emasligi (`SCHOOL_INACTIVE`) bu yerda **tekshirilmaydi**: o'z ma'lumotini
to'g'rilash test o'tkazish emas.

**Frontend qoidasi (`/kabinet/test`, `features/public-account/lib/profileState.ts`):**

| Rejim | Qachon | Tugma | Submit |
|-------|--------|-------|--------|
| `new` | profil yo'q | "Testni boshlash" (ustida: "Ma'lumotlar saqlanadi va test boshlanadi") | `POST /api/me/sessions` |
| `ready` | profil to'liq, rozilik joriy | "Testni boshlash" | `POST /api/me/sessions` `{}` |
| `consent` | profil bor, rozilik eskirgan / ota-ona roziligi yo'q | "Testni boshlash" | `POST /api/me/sessions` |
| `edit` | foydalanuvchi "O'zgartirish" bosdi (`?edit=1` ham) | **"Saqlash"** | **`PUT /api/me/profile`** — test boshlanmaydi; `/kabinet` dan kelgan bo'lsa u yerga qaytadi, aks holda `ready` kartaga |

### 5.2 `GET /api/me/assessments`

```json
{
  "items": [
    {
      "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
      "status": "Analyzed",
      "startedAt": "2026-09-01T09:00:00Z",
      "completedAt": "2026-09-01T09:48:00Z",
      "programCode": "PERSONALITY_PROFILE",
      "programName": "Shaxsiyat profili",
      "resultAvailable": true
    }
  ]
}
```

Sahifalash YO'Q (bitta foydalanuvchida sessiyalar soni kichik). Tartib — `startedAt` bo'yicha
kamayish. `resultAvailable` — natija HOZIR ochilishi mumkinmi (`status = Analyzed` **va**
natija ko'rsatish yoqilgan); frontend tugmani shu bo'yicha ko'rsatadi.
Ball/indeks/bayroq maydonlari bu yerda **hech qachon** bo'lmaydi.

### 5.3 `GET /api/me/assessments/{id}/result`

`200 OK` — §1.9 bilan **aynan bir xil** shakl:

```json
{
  "personalityType": "INTJ",
  "typeName": "Strateg",
  "shortDescription": "…",
  "topStrengths": ["…", "…", "…"],
  "careerFields": ["…"],
  "note": "Bu natija tashxis emas — hozirgi holatingiz surati."
}
```

| Javob | Qachon |
|-------|--------|
| `202 Accepted` (tanasiz) | Sessiya hali `Analyzed` emas |
| `403 FORBIDDEN` | Natija ko'rsatish o'chirilgan (§5.5) |
| `404 NOT_FOUND` | `id` mavjud emas **yoki boshqa foydalanuvchiga tegishli** |

> **Nega `403` emas `404`.** Begona sessiya uchun `403` qaytarish "bunday sessiya bor"
> ma'lumotini oshkor qilardi (mavjudlik oracle'i). `CLAUDE.md` 8-qoidasi buzilmaydi: egalik
> `id` bilan emas, JWT bilan aniqlanadi; `id` faqat SHU foydalanuvchining sessiyalari
> ichidan tanlash kaliti.

> **Muddat tekshirilmaydi.** §1.9 dan farqli (u yerda `410 SESSION_EXPIRED` bor): kabinetda
> natija — arxiv, egalik esa doimiy. 7 kundan keyin o'z natijasini ko'ra olmaslik shaxsiy
> kabinetning ma'nosini yo'qotardi.

### 5.4 `POST /api/me/sessions`

**Barcha shaxsiy maydonlar IXTIYORIY (2026-09-07).** Qaysi biri majburiy — profil (§5.1a)
holatiga bog'liq va server `Student` topilganidan KEYIN hal qiladi (validator faqat format):

| Holat | Talab qilinadi | Ixtiyoriy |
|-------|----------------|-----------|
| Profil YO'Q (birinchi sessiya) | `fullName`, `birthDate`, `gender`, `phone`, `consentAccepted: true`; 18 yoshgacha `parentalConsent: true`; `Required` `customFields` | `grade` (`null` = maktabda o'qimaydi), `email`, `programCode` |
| Profil BOR, rozilik joriy | **hech narsa** — `{}` yoki `{ "programCode": "…" }` yetarli | kelgan shaxsiy maydon TAHRIR sifatida qo'llanadi, kelmagani (`null`) o'zgarmaydi; `customFields` ham ixtiyoriy tahrir (pastga qarang) |
| Profil BOR, rozilik eskirgan (`consentCurrent: false`) | `consentAccepted: true` | qolgani yuqoridagidek |
| Profil BOR, voyaga yetmagan, bazada `parentalConsent: false` | `parentalConsent: true` | — |

Tahrir semantikasi (`null` = "o'zgarmasin" bo'lgani uchun bo'sh qiymat ANIQ yuboriladi):
`grade: 0` — sinfni "yo'q" qilish (`Student.NoGrade`), `email: ""` — emailni tozalash.
`consentAccepted: true` kelsa rozilik joriy versiya va hozirgi vaqt bilan qayta yoziladi;
kelmasa `consentGivenAt` (rozilik isboti sanasi) tegilmaydi. Tahrir tugallanmagan sessiya
davom ettirilganda (`200`) ham saqlanadi.

**`customFields`** (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) — §1.2 bilan BIR XIL shakl.
**MUHIM (egasining alohida ta'kidlagan talabi):** profil BOR bo'lsa `Required` o'z maydoni ham
QAYTA SO'RALMAYDI — majburiylik FAQAT profil YO'Q holatida (birinchi sessiya) tekshiriladi,
boshqa shaxsiy maydonlar bilan bir xil "bir marta so'raladi" naqshi. Kelgan kodlar mavjud
`ProfileExtra`ga USTIDAN yoziladi, kelmagan kod o'zgarmaydi.

To'liq so'rov (profil yo'q holati):

```json
{
  "fullName": "Karimov Sardor Alisherovich",
  "birthDate": "1995-04-12",
  "gender": "Male",
  "phone": "+998901234567",
  "consentAccepted": true,
  "parentalConsent": false,
  "grade": null,
  "email": null,
  "languageCode": "uz",
  "programCode": null,
  "customFields": { "PARENT_JOB": "O'qituvchi" }
}
```

Maktab oqimidagi §1.2 dan farqlari:

| Maydon | Maktab (§1.2) | Ommaviy (§5.4) |
|--------|---------------|----------------|
| `slug`/`accessToken`/`accessCode` | majburiy | **yo'q** (egalik JWT bilan) |
| yosh (`birthDate`) | 6–20 | **6–99** |
| `grade` | majburiy 1–11 | **ixtiyoriy** (`null` = maktabda o'qimaydi) |
| `classLetter`/`parentPhone` | bor | **yo'q** |
| `parentalConsent` | yo'q | **18 yoshgacha `true` bo'lishi SHART** (bazada bor bo'lsa qayta so'ralmaydi) |
| `consentVersion` | — | server qo'yadi, mijozdan qabul qilinmaydi |
| shaxsiy maydonlar | har safar majburiy | **profil bor bo'lsa ixtiyoriy** (yuqoridagi jadval) |

Javob — §1.2 bilan **aynan bir xil** (`StartSessionResult`):

```json
{
  "sessionToken": "…",
  "assessmentId": "…",
  "status": "Draft",
  "expiresAt": "2026-09-12T10:12:00Z",
  "resumed": false,
  "tests": [
    { "code": "MBTI16", "name": "…", "status": "NotStarted", "answeredCount": 0,
      "totalCount": 60, "displayOrder": 1, "estimatedMinutes": 15 }
  ]
}
```

`201 Created` — yangi sessiya; `200 OK` — tugallanmagan sessiya davom ettirildi (`resumed: true`).

Shundan keyin foydalanuvchi **maktab oqimidagi bilan bir xil** endpointlardan foydalanadi
(`X-Session-Token` bilan `/api/public/sessions/*`, §1.3–1.9) — hech qanday parallel oqim yo'q.

Xatolar:

| Kod | HTTP | Qachon |
|-----|------|--------|
| `VALIDATION_ERROR` | 400 | Format (yosh/telefon/sinf/email) yoki profil holatiga ko'ra yetishmagan maydon — `errors{maydon:[xabar]}` bilan (`fullName`, `birthDate`, `gender`, `phone`, `consentAccepted`, `parentalConsent`) |
| `PROGRAM_REQUIRED` | 400 | Bir nechta dastur mavjud, `programCode` berilmagan |
| `NOT_FOUND` | 404 | Berilgan `programCode` ommaviy makonda mavjud emas |
| `DUPLICATE_ASSESSMENT` | 409 | So'nggi 90 kunda **shu dasturni** allaqachon yakunlagan (BR-1, `(o'quvchi, dastur)` bo'yicha). Boshqa dasturni yakunlagani to'sqinlik qilmaydi — u `201` bilan yangi sessiya oladi; shu dasturda yarim qolgan sessiya bo'lsa `200` `resumed: true` |
| `NO_PROGRAM_AVAILABLE` | 409 | Ommaviy makonga birorta dastur biriktirilmagan |
| `PUBLIC_SPACE_NOT_CONFIGURED` | 409 | Ommaviy makon seed qilinmagan (`--seed` bajarilmagan) |
| `SCHOOL_INACTIVE` | 410 | Ommaviy makon o'chirilgan |
| `RATE_LIMITED` | 429 | IP bo'yicha 10/soat yoki kunlik limit |

### 5.5 `DELETE /api/me`

**Tana MAJBURIY (2026-09-08, egasining qarori — o'chirishdan oldin sabab so'raladi):**

```jsonc
{ "reason": "NoLongerNeeded", "comment": "Ixtiyoriy erkin matn" }
```

- `reason` — MAJBURIY, quyidagi qiymatlardan biri (`PublicUserDeletionReason`):
  `NoLongerNeeded` ("Endi kerak emas"), `NotUseful` ("Natijalar foydali bo'lmadi"),
  `PrivacyConcern` ("Ma'lumotlarim saqlanishini xohlamayman"), `CreatedByMistake`
  ("Xato bilan ro'yxatdan o'tganman"), `Other` ("Boshqa sabab"). Berilmasa yoki noma'lum
  qiymat bo'lsa `400 VALIDATION_ERROR`.
- `comment` — ixtiyoriy, ≤500 belgi; `reason == "Other"` bo'lsa MAJBURIY (bo'sh bo'lmasin),
  aks holda `400 VALIDATION_ERROR`.

Muvaffaqiyatda har doim `204 No Content` — **idempotent**: akkaunt allaqachon o'chirilgan
bo'lsa ham `204` qaytadi, lekin sabab/izoh QAYTA YOZILMAYDI (birinchi o'chirishdagi qiymat
saqlanadi).

Nima bo'ladi: `public_users` yozuvi **anonimlashtiriladi** (`telegram_id`, `username`,
`first_name`, `last_name`, `photo_url` tozalanadi, `deleted_at` qo'yiladi), barcha refresh
tokenlar bekor qilinadi, `PublicUser.Deleted` audit yoziladi (audit `afterJson`da FAQAT sabab
KODI — erkin matnli izoh audit logga yozilmaydi). `reason`/`comment` esa anonimlashtirish
bilan TOZALANMAYDI — superadmin panelida ("nega o'chirilgani") ko'rinadi (§3.7). Yozuvning
O'ZI qoladi — test natijalari arxivi (`docs/08` §5: 5 yil) va `students.public_user_id`
FK buzilmasligi uchun. Shu Telegram akkaunti bilan qayta kirilsa **YANGI** akkaunt yaratiladi.

Access token 30 daqiqagacha "tirik" qolishi mumkin, lekin `/api/me/*` darhol `401` qaytaradi.

### 5.6 Natija ko'rsatish qoidasi (`showResultToStudent`)

Natija (§1.9 va §5.3) IKKI bayroqning **VA** birlashmasida ochiladi:

1. `App:ShowResultToStudent` — **global avariya rubilnigi**, standart `true`;
2. `schools.show_result_to_student` — makon qarori: maktab uchun standart `false`
   (natija psixolog orqali beriladi), **ommaviy makon uchun `true`**.

Ya'ni ommaviy foydalanuvchi o'z natijasini ko'radi, maktab o'quvchisi esa — faqat maktab
o'z makonida yoqib qo'ysa. Global `false` ikkalasini ham darhol yopadi.

---
