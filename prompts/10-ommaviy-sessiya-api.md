# P10 — Application skeleti va sessiya ochish API

## Kontekst
Domain va scoring tayyor. Endi Application qatlami va o'quvchi oqimining birinchi qadami.

## O'qish shart
- `docs/07-api-shartnoma.md` (1.1, 1.2, 1.3-bo'limlar)
- `docs/06-arxitektura.md` (4 va 6-bo'limlar)
- `docs/08-auth-va-xavfsizlik.md` (3 va 4-bo'limlar)

## Vazifa
1. **Application skeleti:**
   - MediatR ro'yxatga olish, `ValidationBehavior`, `LoggingBehavior`, `TransactionBehavior`
   - `Result<T>`, `PagedResult<T>`, `ProblemCodes` konstantalari
   - `ICurrentUser`, `IDateTime`, `ITokenGenerator` interfeyslari
2. **Api:**
   - `ExceptionHandlingMiddleware` → barcha xatolarni `ProblemDetails` (`docs/06` 6-bo'limi jadvali)
   - `SessionTokenAuthenticationHandler` (`X-Session-Token`) — sessiyani topadi, muddatini
     tekshiradi, `HttpContext.Items["AssessmentId"]` ga qo'yadi
   - Rate limiting siyosatlari (`docs/07` 4-bo'limidagi jadval)
3. **Use-case'lar:**
   - `GetSchoolInfoQuery` → `GET /api/public/schools/{slug}?k=`
     (token fixed-time compare; nofaol → 410)
   - `StartSessionCommand` → `POST /api/public/sessions`
     - anketa validatsiyasi (FluentValidation): FISH ≥ 5 belgi, tug'ilgan sana 6–20 yosh oralig'i,
       sinf 1–11, telefon `+998`, rozilik `true`
     - dublikat mantiqi (`docs/07` 1.2): mavjud tugallanmagan sessiya → `resumed: true`;
       90 kun ichida yakunlangan → `409 DUPLICATE_ASSESSMENT`
     - maktab kunlik limiti (`registration_counters`, atomik `INSERT ... ON CONFLICT DO UPDATE`)
     - `Assessment` + 4 ta `AssessmentTest` yaratiladi (`DisplayOrder` bo'yicha)
     - `SessionToken` — 32 bayt `RandomNumberGenerator`
   - `GetSessionStateQuery` → `GET /api/public/sessions/me`
     (`Locked` holati: oldingi test tugamagan bo'lsa)
4. `PublicSessionController` — yuqoridagi 3 endpoint.

## Cheklovlar
- URL yoki tanada `assessmentId` **qabul qilinmaydi** — faqat header token.
- Javobda o'quvchining to'liq ismi qaytmaydi (faqat ism, `firstNameShort`).
- IP xom saqlanmaydi — `SHA256(ip + salt)`.

## DoD
- [ ] 3 endpoint Swagger'da, qo'lda sinaldi
- [ ] Integration testlar: to'g'ri havola, noto'g'ri token (404), nofaol maktab (410),
      dublikat (`resumed` va `409`), kunlik limit (429), yaroqsiz anketa (400)
- [ ] Rate limit ishlaydi (11-so'rov 429)

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Public"
curl -s "localhost:5000/api/public/schools/test-maktab?k=$TOKEN" | jq
```
