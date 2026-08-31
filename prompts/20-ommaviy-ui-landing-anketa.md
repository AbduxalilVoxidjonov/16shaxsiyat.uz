# P20 — Ommaviy UI: landing va anketa

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (2-bo'lim, E-1 va E-2)
- `docs/07-api-shartnoma.md` (1.1, 1.2)

## Vazifa
1. `PublicLayout` — sodda, markazlashtirilgan, mobil-birinchi; brend nomi va minimal footer.
2. **E-1 Landing** (`/t/:slug`):
   - `useSchoolInfo(slug, k)` — `GET /api/public/schools/{slug}?k=`
   - maktab nomi, "Sen haqingdagi test", 3 jumlalik tushuntirish
   - 4 ta test kartasi (nomi, savol soni, vaqti, nima o'lchaydi)
   - "To'g'ri yoki noto'g'ri javob yo'q" izohi
   - **Boshlash** tugmasi → `/t/:slug/register`
   - xato holatlari: 404 → "Havola ishlamayapti…", 410 → "Test vaqtincha yopilgan"
   - mavjud sessiya bo'lsa (store'da token) → "Davom ettirish" tugmasi
3. **E-2 Anketa** (`/t/:slug/register`):
   - React Hook Form + Zod, maydonlar tartibi `docs/11` E-2 bo'yicha
   - telefon maskasi `+998 (__) ___-__-__`
   - tug'ilgan sana — 3 ta select (kun/oy/yil)
   - rozilik checkbox (matn API'dan `consentText`)
   - `requiresAccessCode` bo'lsa kod maydoni
   - `POST /api/public/sessions` → token store'ga, `/t/:slug/test/{firstTest}` ga o'tish
   - `409 DUPLICATE_ASSESSMENT` → tushunarli xabar
   - `resumed: true` → "Boshlagan testingni davom ettiramiz"
4. `sessionStore` (Zustand + persist) — `docs/10` 4.1.
5. Ilova yuklanganda `GET /sessions/me` bilan holat tiklash; `410` da store tozalanadi.

## Cheklovlar
- Rozilik belgilanmagunicha "Boshlash" o'chiq.
- Validatsiya xatolari maydon ostida, o'zbekcha, do'stona tilda.
- 360px kenglikda gorizontal scroll bo'lmasin.

## DoD
- [ ] Ikkala ekran real backend bilan ishlaydi
- [ ] Barcha xato holatlari tekshirilgan (404, 410, 409, 400, 429)
- [ ] Mobil (390px) va desktop (1440px) da tekshirilgan
- [ ] Komponent testlari: forma validatsiyasi, telefon maskasi
- [ ] Klaviatura bilan to'liq to'ldirish mumkin

## Tekshiruv
```bash
cd frontend && npm run test && npm run build
# qo'lda: dev serverda to'liq anketani to'ldirib sessiya ochish
```
