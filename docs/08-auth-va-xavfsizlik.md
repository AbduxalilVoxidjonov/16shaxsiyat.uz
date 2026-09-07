# 08 — Autentifikatsiya, avtorizatsiya va xavfsizlik

## 1. Uch xil kirish modeli

| | Superadmin | O'quvchi (maktab havolasi) | Ommaviy foydalanuvchi (P47) |
|---|---|---|---|
| Kirish | Username + parol (+ TOTP) | Maktab havolasi (+ ixtiyoriy kod) | Telegram Login Widget |
| Token | JWT access (30 daq, `aud=…-admin`) + refresh (14 kun) | `SessionToken` (opaque, 7 kun) | JWT access (30 daq, `aud=…-public`) + refresh (14 kun) |
| Saqlash | access — xotirada, refresh — `httpOnly` cookie (`Path=/api/auth`) | `localStorage` | access — xotirada, refresh — `httpOnly` cookie (`Path=/api/auth/telegram`) |
| Ruxsat | Hamma admin endpoint | Faqat o'z sessiyasi | Faqat o'z kabineti (`/api/me/*`) va o'z sessiyasi |

> **Uch model bir-biriga O'TMAYDI.** Superadmin va ommaviy JWT'lar alohida `aud` bilan
> imzolanadi va API'da alohida sxemalar (`Bearer` / `PublicBearer`) tekshiradi — ya'ni
> ommaviy token superadmin endpointida ROLGA umuman yetib bormaydi (`401`), aksi ham
> shunday. Maktab havolasi oqimi esa ikkalasidan ham mustaqil (`X-Session-Token`) va P47da
> **hech qanday o'zgarish ko'rmadi**.

---

## 2. Superadmin JWT

**Access token** claim'lari: `sub` (AdminUserId), `name`, `role`, `jti`, `iat`, `exp`.
HS256, kalit `Jwt:Key` (≥ 32 bayt, env orqali). Muddat 30 daqiqa.

**Refresh token:** 64 baytli random, DB'da **faqat SHA-256 xeshi** saqlanadi.
- Rotatsiya: har `refresh` da eskisi bekor qilinadi, yangisi beriladi.
- Qayta ishlatish aniqlansa (bekor qilingan token bilan so'rov) — foydalanuvchining
  **barcha** refresh tokenlari bekor qilinadi + `AuditLog` ga `Security.RefreshReuse`.
- `httpOnly; Secure; SameSite=Strict` cookie'da.

**Parol:** PBKDF2-HMACSHA256, **210 000 iteratsiya**, 16 baytli tasodifiy tuz, 32 baytli xesh
(OWASP 2023 tavsiyasi). Solishtirish `CryptographicOperations.FixedTimeEquals` bilan.
Iteratsiya soni xesh ichida saqlanadi (kelajakda oshirish uchun), lekin o'qishda
`100 000 … 1 000 000` oralig'ida bo'lishi tekshiriladi — aks holda xesh yaroqsiz deb qaraladi
(downgrade hujumining oldini olish).
Minimal talab: 10 belgi, harf+raqam. Parol o'zgarganda barcha refresh tokenlar bekor qilinadi.

**Blokirovka:** 5 ta noto'g'ri urinish → 15 daqiqa (`LockedUntil`). Muvaffaqiyatli kirishda `FailedLoginCount = 0`.

**TOTP (2FA, ixtiyoriy):** RFC 6238, 30 s oyna ±1, sekret AES-256-GCM bilan shifrlangan.
Yoqilganda 8 ta bir martalik zaxira kod beriladi (xeshlangan holda saqlanadi).

**O'rnatish ikki bosqichli (P46):**

1. `POST /api/auth/totp/enable` — yangi sekret generatsiya qilinadi va `admin_users.
   pending_totp_secret_encrypted` ga (shifrlangan) yoziladi. `totp_enabled` **`false` qoladi**,
   login oqimi o'zgarmaydi. Javobda `otpauthUri` ning QR kodi (xom base64 PNG) va sirning
   o'zi (qo'lda kiritish uchun) qaytadi. Kutish holatidagi sir **10 daqiqa** amal qiladi
   (`AdminUser.PendingTotpEnrollmentLifetime`); takroriy `enable` uni almashtiradi.
2. `POST /api/auth/totp/confirm` — foydalanuvchi ilovadagi 6 xonali kodni yuboradi. Kod
   kutish holatidagi sirga mos kelsagina sekret asosiy maydonga ko'chadi, `totp_enabled`
   `true` bo'ladi va 8 ta zaxira kod generatsiya qilinib javobda **bir marta** ko'rsatiladi.
   Tasdiqlashda ishlatilgan vaqt qadami `totp_last_used_step` ga yoziladi — xuddi shu kod
   bilan darhol login qilib bo'lmaydi.

Sabab: tasdiqlashsiz yoqish — hisobni bloklab qo'yish yo'li. Agar autentifikator ilovasi
noto'g'ri sozlangan bo'lsa (yoki sir qo'lda xato ko'chirilgan bo'lsa), foydalanuvchi keyingi
kirishda hech qachon to'g'ri kod bera olmaydi. Sekret hech qachon logga yoki `AuditLog` ga
yozilmaydi — audit faqat `Auth.TotpEnrollmentStarted` / `Auth.TotpEnabled` faktini qayd etadi.

---

## 2a. Ommaviy foydalanuvchi JWT (Telegram) — P47

**Kirish oqimi (redirect).** Frontend Telegram Login Widget'ini `data-auth-url` bilan
chizadi — callback manzili `/kirish` ning O'ZI (mutlaq, `window.location.origin` dan
quriladi; Telegram nisbiy yo'lni qabul qilmaydi). Foydalanuvchi tasdiqlagach Telegram
brauzerni o'sha manzilga QAYTARADI va ma'lumotni query parametrlarida beradi:
`id`, `first_name`, `last_name?`, `username?`, `photo_url?`, `auth_date`, `hash`. Sahifa
ochilganda ular o'qiladi va **o'zgartirilmasdan** `POST /api/auth/telegram` tanasiga
yuboriladi (`credentials: 'include'`). Telegram bermagan maydon **umuman yuborilmaydi** —
bo'sh satr `data_check_string` ni buzadi.

**Nega `data-onauth` (JS callback) EMAS.** `data-onauth` atributining qiymatini Telegram
widget'i **matn sifatida `eval` qiladi**. CSP'da `unsafe-eval` ATAYLAB yo'q (7-bo'lim),
shu sabab widget skripti `200` bilan yuklansa ham iframe'ni umuman chiza olmasdi
(`Evaluating a string as JavaScript violates the following Content Security Policy
directive…`) — `/kirish` sahifasida hech qanday tugma ko'rinmasdi. Redirect oqimi `eval`
talab qilmaydi, ya'ni CSP bo'shatilmasdan muammo hal bo'ladi.

**Query parametrlari qanday tozalanadi.** Ma'lumot manzil satrida keladi, ya'ni ikki joyda
qolib ketishi mumkin edi:

- *brauzer tarixi va manzil satri* — so'rov yuborilishi bilan DARHOL (javobni kutmasdan)
  `history.replaceState` chaqiriladi va Telegram parametrlari olib tashlanadi (`pushState`
  EMAS: yangi yozuv "orqaga" tugmasi bilan qaytib kelardi). Telegram bilan aloqasi yo'q
  parametrlar (`returnUrl`) saqlanadi.
- *nginx access log* — `docker/web-nginx.conf` da `location = /kirish` uchun alohida
  `no_query` log formati: `$request` (query bilan) o'rniga `$safe_uri` (faqat yo'l).
  `access_log off` qilinmadi — status kod va so'rovlar soni kuzatuv uchun kerak.

**Imzoni tekshirish (Telegram rasmiy algoritmi, `TelegramLoginVerifier`):**

1. `data_check_string` — `hash` dan tashqari barcha maydonlar `key=value` ko'rinishida,
   kalit bo'yicha **alifbo (ordinal) tartibida** saralanib `\n` bilan birlashtiriladi.
   Telegram yubormagan maydon (masalan `last_name`) **umuman qo'shilmaydi** — bo'sh satr
   bilan qo'shish imzoni buzadi.
2. `secret_key = SHA256(bot_token)`.
3. `HMAC_SHA256(data_check_string, secret_key)` ning hex ko'rinishi kelgan `hash` bilan
   **doimiy vaqtda** (`CryptographicOperations.FixedTimeEquals`) solishtiriladi — baytma-bayt
   erta chiqadigan solishtiruv xeshni bosqichma-bosqich tiklashga (timing oracle) imkon berardi.
4. `auth_date` yangiligi: **24 soatdan** eski bo'lmasligi (Telegram namunasidagi qiymat) va
   5 daqiqadan ko'p kelajakda bo'lmasligi (soat farqi uchun). Bu tekshiruv imzodan OLDIN —
   eskirgan ma'lumotni HMAC hisoblashiga olib bormaslik arzonroq, `auth_date` esa imzoning
   bir qismi, ya'ni uni o'zgartirish imzoni ham buzadi.

**Bot tokeni** — `Telegram:BotToken` (env `Telegram__BotToken`), kodda/`appsettings.json`da
YO'Q (`CLAUDE.md` 4-qoida). Berilmasa ilova ishga tushaveradi, lekin
`POST /api/auth/telegram` `503 TELEGRAM_AUTH_NOT_CONFIGURED` qaytaradi.

**Access token** claim'lari: `sub` (`PublicUser.Id`), `role = PublicUser`, `jti`, `iat`, `exp`,
`aud = Jwt:PublicAudience` (standart `studentroadmap-public`). **`name`/ism/username claim'i
ATAYLAB YO'Q** — token log, proxy va brauzer tarixida qolishi mumkin (`CLAUDE.md` 5-qoida
ruhida); profil faqat `GET /api/me` orqali beriladi.

**Refresh token** — superadmin naqshining aynan o'zi: 64 baytli random, DB'da faqat SHA-256
xeshi (`public_refresh_tokens`), rotatsiya + **qayta ishlatishni aniqlash** (bekor qilingan
token bilan urinish → foydalanuvchining BARCHA tokenlari bekor + `PublicSecurity.RefreshReuse`
audit). Cookie: `srm_public_refresh_token`, `HttpOnly; Secure; SameSite=Strict;
Path=/api/auth/telegram` — nom ham, yo'l ham superadminникidan farq qiladi, shu sabab bitta
brauzerda ikkala sessiya bir-birini buzmaydi va ommaviy cookie admin endpointlariga umuman
yuborilmaydi.

**Akkauntni o'chirish** (`DELETE /api/me`): yozuv **anonimlashtiriladi**
(`telegram_id`/`username`/ism/avatar tozalanadi, `deleted_at` qo'yiladi), barcha refresh
tokenlar bekor qilinadi, `PublicUser.Deleted` audit yoziladi. Qattiq o'chirish emas — test
natijalari arxivi (§5: 5 yil) va `students.public_user_id` FK saqlanishi kerak. Global query
filtr (`deleted_at IS NULL`) o'chirilgan akkauntni barcha so'rovlardan yashiradi, shu sabab
hali amal qilayotgan access token ham `401` oladi.

---

## 3. Maktab havolasi

**Havola:** `https://16shaxsiyat.uz/t/{slug}?k={accessToken}`

- `slug` — o'qiladigan, unikal (`12-maktab-kokand`).
- `accessToken` — 32 bayt `RandomNumberGenerator` → Base64Url (43 belgi).
- Tekshirish: `slug` bo'yicha maktab topiladi, `accessToken` **fixed-time compare** bilan solishtiriladi.
- `IsActive = false` → `410 SCHOOL_INACTIVE`.
- Qayta generatsiya (`regenerate-link`) — eski token darhol yaroqsiz; hodisa audit'ga yoziladi.
- Ixtiyoriy `AccessCode` (6 raqam) — sinfda o'qituvchi doskaga yozib beradi; 5 marta xato kod →
  o'sha IP uchun 10 daqiqa blok. **Eskirgan (2026-09-07):** admin UI'dan olib tashlangan, forma
  uni yubormaydi (backend `null`); qiymatli eski maktabda tekshiruv ishlaydi. O'rnini `EntryCode` egalladi.

**Suiiste'molga qarshi:**
- Maktab kunlik ro'yxatdan o'tish limiti (`registration_counters` jadvali, atomik `INSERT … ON CONFLICT … +1`).
- IP bo'yicha soatiga 10 ta yangi sessiya.
- Bir xil `(school, normalizedName, birthDate)` — yangi o'quvchi yaratmaydi.
- Bot himoyasi: MVP'da vaqt asosidagi honeypot maydon; kerak bo'lsa v2'da hCaptcha.

### 3a. Maktab kodi (`School.EntryCode`) — 2026-09-07

Havolaga MUQOBIL kirish eshigi: `/kirish` → "Maktab uchun" → kod → `POST /api/public/schools/resolve-code`
→ `{ slug, accessToken }` → brauzer `/t/{slug}?k=` ga o'tadi (3-bo'limdagi oqim o'zgarmaydi).

- **Format:** 8 belgi, alifbo `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (31 belgi; `0 O 1 I L` yo'q — kod
  doskaga yoziladi/telefonda aytiladi). Ko'rsatishda `XXXX-XXXX`, bazada defissiz. Entropiya 31^8 ≈ 8.5·10^11.
- **Generatsiya:** `RandomNumberGenerator.GetInt32` (`Infrastructure.Security.EntryCodeGenerator`),
  maktab yaratilganda AVTOMATIK; admin kiritmaydi. Unikal: `ux_schools_entry_code`.
- **Ommaviy makon** (`Kind = PublicSpace`) kodga EGA EMAS (`null`) — u faqat Telegram orqali.
- **Tekshirish:** normalizatsiya (katta harf, defis/bo'shliqsiz) → `Kind = School AND IsActive AND
  NOT IsDeleted` bo'yicha qidiruv. Har qanday rad — BITTA `404 SCHOOL_CODE_INVALID` (nofaol ≠ yo'q
  farqi oshkor qilinmaydi — kod yagona sir, `410 SCHOOL_INACTIVE` bu yerda yo'q).
- **Kod tanada**, URL'da emas — server loglari va brauzer tarixiga tushmaydi.
- **Rate limit:** IP bo'yicha 10 / 5 daqiqa (`RateLimitSetup.PublicResolveSchoolCode`, `AdminLogin` bilan
  bir xil). Brute-force 31^8 fazoda amalda imkonsiz, lekin kod qisqa va qo'lda kiritiladigan sir —
  siyosat shart.
- **Audit:** faqat muvaffaqiyatsiz urinish — `SchoolCode.ResolveFailed` (IP xeshi, User-Agent;
  kiritilgan kod QIYMATI yozilmaydi, `EntityId` yo'q). Muvaffaqiyat yozilmaydi (havola ochilishi
  bilan bir ma'noda — keyingi qadam `school_link_views` hisoblagichiga tushadi).
- **Qayta generatsiya:** `POST /api/admin/schools/{id}/regenerate-entry-code` — eski kod darhol
  yaroqsiz; audit `School.EntryCodeRegenerated` (kod qiymatisiz, `School.LinkRegenerated` kabi).
  Havola tokeni va kod MUSTAQIL: biri yangilansa ikkinchisi ishlashda davom etadi.
- **`AccessCode` bilan farqi:** `AccessCode` — ixtiyoriy 6 raqamli sinf kodi (yuqorida; admin UI'dan
  olib tashlangan, eskirgan), havola orqali
  kirganlardan anketada so'raladi, maktabni aniqlamaydi. `EntryCode` — maktabni aniqlaydigan eshik.
  Kod bilan kirgan o'quvchidan ham (maktabda `AccessCode` bo'lsa) anketada u so'raladi — oqim bir xil.

---

## 4. Sessiya tokeni (o'quvchi)

- 32 bayt random → Base64Url. **P47dan buyon DB'da SHA-256 XESHI bo'yicha qidiriladi**
  (`assessments.session_token_hash`, `TokenHash.Compute`) — refresh tokenlar bilan bir xil
  himoya darajasi: DB nusxasi sizib chiqsa ham faol sessiyalarni bevosita ochib bo'lmaydi.
  ⚠️ Ochiq `session_token` ustuni hozircha SAQLANIB turibdi (ikki bosqichli destruktiv
  o'zgarish, `CLAUDE.md` 7-qoida) — endi u hech qayerda O'QILMAYDI va keyingi migratsiyada
  o'chiriladi.
- Header: `X-Session-Token`. Custom `AuthenticationHandler` (`SessionTokenScheme`) tokenni tekshiradi,
  `HttpContext.Items["AssessmentId"]` ga qo'yadi.
- Muddati: `ExpiresAt` (7 kun) — o'tsa `410 SESSION_EXPIRED`.
- Sessiya **faqat o'z** `AssessmentId` ostidagi resurslarga kiradi; hech qanday `assessmentId`
  URL'da qabul qilinmaydi (IDOR imkoniyati yopiladi).
- Yakunlangan sessiyaga javob yozib bo'lmaydi.

---

## 5. Shaxsiy ma'lumotlarni himoya qilish

| Ma'lumot | Yondashuv |
|----------|-----------|
| FISH, telefon, tug'ilgan sana | Zarur minimum. Faqat superadmin ko'radi |
| IP manzil | Xom saqlanmaydi — `SHA256(ip + Security:IpHashSalt)` |
| AI provider API kalitlari | AES-256-GCM shifrlangan, o'qishda maskalangan (`AIza••••7f2b`) |
| TOTP sekret | AES-256-GCM |
| Parol | PBKDF2-HMACSHA256 210k (qaytarilmas) |
| Test javoblari | Xom javoblar faqat audit uchun; AI'ga **javoblar emas, ballar** yuboriladi |

**AI provayderga yuboriladigan ma'lumot (muhim):**
- ✅ Yosh (raqam), sinf, jins, ball va indekslar, tip kodlari, ishonchlilik.
- ❌ FISH, telefon, email, maktab nomi, tug'ilgan sana (aniq sana), o'quvchi ID.
- Hisobotda ism kerak bo'lsa — **frontend** o'z DB'sidan qo'yadi, promptga qo'shilmaydi.

**Saqlash muddati:** test natijalari 5 yil (ta'lim yo'nalishini kuzatish), audit log 1 yil,
xom `answers` 1 yil (keyin arxivga yoki o'chiriladi).

**O'chirish huquqi:** `DELETE /api/admin/students/{id}?hard=true` — student, sessiyalar, javoblar,
AI tahlillar butunlay o'chadi; audit'da faqat `{studentId, deletedAt, adminId}` qoladi.

**O'chirish huquqi (ommaviy foydalanuvchi, P47):** `DELETE /api/me` — foydalanuvchi
o'zi, adminga murojaat qilmasdan. `public_users` yozuvi anonimlashtiriladi (§2a).
⚠️ **Ochiq savol (egasiga):** ommaviy makonda yaratilgan `Student` yozuvidagi F.I.Sh./telefon
bu bosqichda anonimlashtirilmaydi — u BR-1 (takrorlanishni aniqlash) mantig'iga kiradi va
alohida qaror talab qiladi.

**Voyaga yetmagan ommaviy foydalanuvchi (P47):** `POST /api/me/sessions` da 18 yoshgacha
bo'lganlar uchun `parentalConsent: true` MAJBURIY; roziliknoma versiyasi (`consent_version`)
serverda qo'yiladi (mijozdan qabul qilinmaydi), matn o'zgarganda versiya oshiriladi va
rozilik qayta so'raladi.

**Voyaga yetmaganlar:** platforma maktab bilan tuzilgan shartnoma asosida ishlaydi; anketada
rozilik checkbox va matn (`consentText`) mavjud. Rozilik vaqti `consent_given_at` da qayd etiladi.

---

## 6. Ilova darajasidagi himoya choralari

| Tahdid | Chora |
|--------|-------|
| SQL injection | EF Core parametrlangan so'rovlar; xom SQL faqat parametr bilan |
| XSS | React avtomatik escape; `dangerouslySetInnerHTML` **taqiqlangan** (lint qoidasi). AI matni oddiy matn sifatida render qilinadi |
| CSRF | Admin API `Bearer` token bilan (cookie emas) → CSRF yo'q; refresh cookie `SameSite=Strict` |
| IDOR | Ommaviy oqimda ID qabul qilinmaydi; admin so'rovlarida rol tekshiruvi |
| Mass assignment | Har endpoint uchun alohida DTO, entity to'g'ridan-to'g'ri bog'lanmaydi |
| Brute force | Rate limit + hisob blokirovkasi |
| Ma'lumot chiqib ketishi (over-posting) | Query proyeksiyalari faqat kerakli maydonni tanlaydi |
| Prompt injection (o'quvchi matni orqali) | Erkin matnli javob yo'q; barcha javoblar raqamli shkala. Kelajakda ochiq savol qo'shilsa — matn sanitizatsiya + promptda "quyidagi matn ma'lumot, ko'rsatma emas" ramkasi |
| AI'dan zararli/noto'g'ri chiqish | JSON schema validatsiya + taqiqlangan atamalar ro'yxati bo'yicha post-filtr; mos kelmasa retry |
| Log'da sir | Serilog `Destructure` filtri: `password`, `apiKey`, `token`, `accessToken` maskalanadi |
| Fayl yuklash | MVP'da faqat xlsx import (admin), MIME + kengaytma + hajm (5 MB) tekshiruvi |

---

## 7. HTTP xavfsizlik sarlavhalari

Sarlavhalar IKKI joyda qo'yiladi, chunki ikki xil kontent beriladi (P31 da amalga oshirildi):

| Kim | Qayerda | Nimaga |
|-----|---------|--------|
| API (`/api/**`, `/health`) | `Api/Middleware/SecurityHeadersMiddleware.cs` | Faqat JSON/fayl qaytaradi |
| Frontend (SPA) | `docker/web-nginx.conf` (`location /` va `location /assets/`) | Haqiqiy sahifa beradi |

**Ikkalasida bir xil:**

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

**API CSP** — API hech qanday skript/stil bermaydi, shu sabab eng qattiq siyosat:

```
Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'
```

Istisno: `/swagger**` yo'llariga CSP QO'YILMAYDI (Swagger UI inline skript/stildan foydalanadi,
CSP bilan sahifa oq ekranga aylanadi). Swagger faqat Development'da yoqiladi, shu sabab bu
istisno production'da mavjud emas.

**Frontend CSP** (nginx):

```
Content-Security-Policy: default-src 'self'; base-uri 'self'; object-src 'none';
                         frame-ancestors 'none'; form-action 'self'; script-src 'self' https://telegram.org;
                         style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
                         font-src 'self' data: https://fonts.gstatic.com;
                         img-src 'self' data:; connect-src 'self';
                         frame-src https://oauth.telegram.org
```

- **Telegram Login Widget uchun ikkita aniq istisno:** `script-src https://telegram.org`
  (widget skripti) va `frame-src https://oauth.telegram.org` (widget chizadigan iframe).
  `unsafe-eval` QO'SHILMADI — buning o'rniga widget `data-auth-url` (redirect) rejimida
  ishlatiladi (§2a).
  Ular butun sayt uchun ochiq, chunki bu SPA — foydalanuvchi `/kirish` ga klient tomonda
  o'tsa brauzer DASTLAB yuklangan sahifaning CSP'sini qo'llaydi, ya'ni faqat bitta yo'l
  uchun alohida siyosat yozib bo'lmaydi. `connect-src` ga Telegram qo'shilmagan: widget
  bergan ma'lumot bizning o'z API'mizga boradi va imzo serverda HMAC-SHA256 bilan
  tekshiriladi — iframe'ga ishonilmaydi.
- `unsafe-eval` YO'Q (`prompts/31` cheklovi).
- `unsafe-inline` faqat STIL uchun: React `style={{…}}` atributlari va grafik kutubxonalari
  inline stil qo'yadi. Skript uchun kerak emas — Vite build'i faqat tashqi
  `<script type="module">` chiqaradi (`index.html`da inline skript yo'q).
- Google Fonts `index.html`da ishlatiladi → `fonts.googleapis.com` (stil) +
  `fonts.gstatic.com` (shrift fayllari).
- `connect-src 'self'` yetarli: API ayni origin ostida (`/api/` nginx proksisi), tashqi
  `https://api.16shaxsiyat.uz` ga to'g'ridan-to'g'ri murojaat qilinmaydi.

> **nginx tuzog'i.** `add_header` MEROS OLINMAYDI: `location` blokida bitta `add_header`
> bo'lsa, `server` darajasidagi barchasi bekor bo'ladi. `location /` va `location /assets/`
> da `Cache-Control` qo'yilgani uchun xavfsizlik sarlavhalari HAR IKKALA blokda ataylab
> takrorlangan. `/api/`, `/swagger`, `/health` bloklariga qo'yilmagan — u yerda API o'z
> sarlavhalarini beradi (takroriy/zid CSP bo'lmasligi uchun).

HTTP → HTTPS redirect; production'da HSTS yoqiladi:
`Strict-Transport-Security: max-age=31536000; includeSubDomains` (`AddHsts` + `UseHsts`,
faqat Production muhitida).

---

## 8. Audit qilinadigan harakatlar

`Auth.LoginSucceeded`, `Auth.LoginFailed`, `Auth.PasswordChanged`, `Auth.TotpEnrollmentStarted`
(P46 — 2FA o'rnatish boshlandi, hali yoqilmagan), `Auth.TotpEnabled`,
`Auth.TotpDisabled` (P13 da qo'shildi — 2FA o'chirilishi yoqilishidan ko'ra muhimroq hodisa,
chunki u himoyani pasaytiradi),
`Security.RefreshReuse`, `School.Created/Updated/Deleted`, `School.LinkRegenerated`,
`School.EntryCodeRegenerated` (2026-09-07, kod qiymatisiz), `SchoolCode.ResolveFailed` (2026-09-07 —
ommaviy `resolve-code` rad etildi: `admin_user_id` va `entity_id` `null`, IP xeshi bor, kod qiymati yo'q),
`School.ToggledActive`, `Student.Deleted`, `Assessment.Deleted`, `Assessment.AnalysisRerun`,
`Assessment.ScoresRecalculated`, `AiConfig.Updated`, `AiConfig.KeyChanged` (kalit qiymati emas!),
`Catalog.QuestionUpdated`, `Export.StudentsDownloaded`.

**Ommaviy foydalanuvchi (P47):** `PublicAuth.LoginSucceeded`, `PublicAuth.LoginFailed`
(`{"reason":"hash"|"auth_date"}`, `EntityId` YOZILMAYDI — imzo tasdiqlanmagan `id` ga
ishonib bo'lmaydi), `PublicUser.Deleted`, `PublicSecurity.RefreshReuse`.
Bu yozuvlarda `admin_user_id` HAR DOIM `null` (u `admin_users` ga FK) — kim ekanligi
`entity_type = "PublicUser"` + `entity_id = public_users.id` orqali qayd etiladi.

Har yozuvda: kim, qachon, qaysi obyekt, `before/after` (sirlarsiz), IP xeshi, user-agent.

---

## 9. Chiqarishdan oldingi xavfsizlik ro'yxati

- [x] `Telegram:BotToken` — faqat env (`Telegram__BotToken`), repo'da yo'q; berilmasa
      Telegram kirishi o'chiq (`503`), qolgan oqimlar ishlaydi (P47)
- [x] `Jwt:Key`, `Security:EncryptionKey`, DB paroli — faqat env, repo'da yo'q
      (`docker-compose.yml` `${...:?}` bilan majburlaydi; `appsettings.json`da sir yo'q)
- [ ] Default superadmin paroli birinchi kirishda **majburiy** o'zgartiriladi
      (`MustChangePassword` — P31 doirasida BAJARILMADI, alohida vazifa)
- [x] Swagger production'da o'chirilgan — `Program.cs` uni faqat `IsDevelopment()`da
      ro'yxatga oladi; `docker-compose.yml` standarti `ASPNETCORE_ENVIRONMENT=Production`,
      ya'ni `/swagger` 404. Sabab: sxema butun ichki API yuzasini (admin endpointlari,
      maydon nomlari, enum qiymatlari) autentifikatsiyasiz ochib beradi
- [x] `ASPNETCORE_ENVIRONMENT=Production`, batafsil xato sahifalari o'chiq — barcha xatolar
      `ProblemDetails`, stack trace/SQL/fayl yo'li/CLR tip nomi chiqmaydi (`docs/06` §6)
- [x] CORS faqat real domen — `App:FrontendUrl` berilmasa hech qanday origin ochilmaydi;
      production'da SPA va API bitta origin ostida (nginx proksisi), ya'ni CORS umuman ishlamaydi
- [x] Rate limitlar yoqilgan va tekshirilgan — `docs/07` §4 dagi 5 siyosat, integratsiya
      testlari bilan (`AdminApiRateLimitTests`, `AuthLoginRateLimitTests`,
      `PublicSessionRateLimitTests`, `RateLimitRetryAfterTests`); `429` `Retry-After` bilan
- [x] HTTPS + HSTS + xavfsizlik sarlavhalari — 7-bo'limga qarang; `SecurityHeadersTests`
- [ ] DB foydalanuvchisi superuser emas, faqat kerakli huquqlar (infra vazifasi)
- [ ] Backup yoqilgan va **tiklanishi sinovdan o'tgan** (infra vazifasi)
- [x] Log'larda shaxsiy ma'lumot yo'qligi tekshirilgan — Serilog `Destructure.ByTransforming`
      (`Program.cs`), audit yozuvlarida xom IP emas `IpHash`, eksport auditida qidiruv matni
      o'rniga `HasSearch: true/false`
- [x] AI promptiga shaxsiy ma'lumot ketmasligi test bilan qamrab olingan (P16–P18)
