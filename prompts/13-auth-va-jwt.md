# P13 — Superadmin autentifikatsiyasi

## O'qish shart
- `docs/08-auth-va-xavfsizlik.md` (2-bo'lim)
- `docs/07-api-shartnoma.md` (2-bo'lim)

## Vazifa
1. `Infrastructure/Identity/`:
   - `JwtTokenService` — access token (30 daq), claim'lar: `sub`, `name`, `role`, `jti`
   - `PasswordHasher` — BCrypt (work factor 12)
   - `RefreshTokenService` — 64 bayt random, DB'da SHA-256 xesh, rotatsiya,
     **qayta ishlatish aniqlansa barcha tokenlarni bekor qilish** + `Security.RefreshReuse` audit
   - `TotpService` — RFC 6238, sekret AES-256-GCM shifrlangan, 8 ta zaxira kod
2. `Application/Identity/` use-case'lar: `Login`, `Refresh`, `Logout`, `Me`, `ChangePassword`,
   `EnableTotp`, `DisableTotp`.
3. `AuthController` — `docs/07` 2-bo'limidagi endpointlar.
4. Blokirovka: 5 xato urinish → 15 daqiqa (`LockedUntil`); muvaffaqiyatda hisoblagich nolga tushadi.
5. Refresh token `httpOnly; Secure; SameSite=Strict` cookie'da; access token javob tanasida.
6. `[Authorize(Roles = "SuperAdmin")]` — admin controller'lar uchun asos policy.
7. `AuditLog` yozuvlari: `Auth.LoginSucceeded`, `Auth.LoginFailed`, `Auth.PasswordChanged`,
   `Auth.TotpEnabled`, `Security.RefreshReuse`.
8. Parol o'zgarganda barcha refresh tokenlar bekor qilinadi.

## Cheklovlar
- Xato xabari qaysi maydon noto'g'ri ekanini oshkor qilmaydi ("Login yoki parol noto'g'ri").
- `Jwt:Key` konfiguratsiyadan; kalit 32 baytdan qisqa bo'lsa ilova start-upda **xato bilan to'xtaydi**.
- Parol hech qachon log'ga tushmasin (Serilog destructure filtri).

## DoD
- [ ] Login → access + refresh; admin endpoint token bilan 200, tokensiz 401
- [ ] Refresh rotatsiyasi ishlaydi; eski token bilan urinish → barcha tokenlar bekor
- [ ] 5 xato urinish → 423/401 + blokirovka; 15 daqiqadan keyin ochiladi
- [ ] Integration testlar: 401, 403, blokirovka, refresh reuse
- [ ] TOTP yoqilganda kod talab qilinadi

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Auth"
curl -s -X POST localhost:5000/api/auth/login -d '{"username":"admin","password":"..."}' -H 'Content-Type: application/json' | jq
```
