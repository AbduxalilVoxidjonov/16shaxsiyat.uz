# P31 — Xavfsizlik va mustahkamlash

## O'qish shart
- `docs/08-auth-va-xavfsizlik.md` (**to'liq**, ayniqsa 6, 7, 9-bo'limlar)

## Vazifa
1. **Xavfsizlik sarlavhalari** middleware (`docs/08` 7-bo'limi): HSTS, `X-Content-Type-Options`,
   `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, CSP.
2. **Rate limiting** — `docs/07` 4-bo'limidagi barcha siyosatlar yoqilgan va integration test bilan
   qamralgan.
3. **Serilog destructure filtri**: `password`, `apiKey`, `accessToken`, `refreshToken`,
   `sessionToken`, `phone` maskalanadi. Log'da shaxsiy ma'lumot yo'qligini tekshiruvchi test.
4. **Maxfiylik testlari (majburiy):**
   - `PromptBuilder` chiqishida ism/telefon/email/aniq tug'ilgan sana yo'q
   - o'quvchi natijasi javobida maxfiy maydonlar yo'q
   - DB'da `api_key_encrypted` ochiq matn emas
   - IP xom saqlanmaydi
5. **IDOR testlari:** boshqa sessiya tokeni bilan har bir ommaviy endpointga urinish → rad etiladi.
6. **Kirish tekshiruvi:** barcha `POST/PUT` uchun oshirib yuborilgan hajm (413), noto'g'ri tip,
   ortiqcha maydon (`additionalProperties`) holatlari.
7. **Bog'liqliklar auditi:** `dotnet list package --vulnerable`, `npm audit` — kritik zaifliklar
   tuzatiladi.
8. **`docs/08` 9-bo'limidagi chiqarish ro'yxatini** to'liq bajarib, har bandni belgilash.
9. Boshlang'ich superadmin paroli birinchi kirishda **majburiy** o'zgartiriladi
   (`MustChangePassword` bayrog'i + frontend yo'naltirish).

## Cheklovlar
- Xavfsizlik uchun funksionallikni buzish mumkin emas — har o'zgarishdan keyin E2E qayta ishlaydi.
- CSP `unsafe-eval` ni **o'z ichiga olmasin**.

## DoD
- [ ] Barcha xavfsizlik testlari yashil
- [ ] `docs/08` 9-bo'lim ro'yxati to'liq belgilangan
- [ ] `npm audit` va `dotnet list package --vulnerable` — kritik zaiflik yo'q
- [ ] Sarlavhalar brauzerda tekshirilgan (DevTools → Network → Headers)
- [ ] E2E to'plami hali ham yashil

## Tekshiruv
```bash
dotnet test --filter "Security|Privacy|RateLimit"
dotnet list package --vulnerable --include-transitive
cd frontend && npm audit --audit-level=high
curl -sI https://localhost:5001/health | grep -i "strict-transport\|x-frame\|content-security"
```
