# P29 — Test katalogi va audit jurnali interfeysi

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-8, A-9)
- `docs/07-api-shartnoma.md` (3.4, 3.6)

## Vazifa
1. **`/admin/catalog`**:
   - 4 ta test kartasi: nomi, kodi, versiyasi, savol soni, taxminiy vaqt, faollik
   - kartaga bosilganda savollar jadvali: kod, matn, tartib, shkala, yo'nalish, faollik
   - savol matnini **inline tahrirlash** (`PUT /api/admin/catalog/questions/{id}`)
   - tartibni o'zgartirish (yuqoriga/pastga)
   - faol/nofaol toggle
   - `scale` va `direction` ustunlari **faqat ko'rish** — tahrirlashga urinishda tooltip:
     "Bu qiymatlar ball hisobiga ta'sir qiladi va o'zgartirilmaydi"
   - qidiruv (savol matni bo'yicha)

> Bu promptda **faqat tizim metodikalarini ko'rish va matnini tahrirlash** qilinadi.
> Superadminning o'z anketasini yaratishi — alohida **P33** promptida.
2. **Import** (ixtiyoriy, [S]): xlsx/json yuklash dialogi, oldindan ko'rish (nechta yangi,
   nechta yangilanadi), tasdiq.
3. **Tip katalogi** bo'limi: 16 tip, o'zbekcha nom va tavsiflarni tahrirlash.
4. **`/admin/audit`**:
   - jadval: sana, foydalanuvchi, harakat, obyekt turi, obyekt
   - filtr: harakat turi (select), obyekt turi, sana oralig'i
   - qator kengaytirilsa `before/after` JSON farqi ko'rinadi (oddiy diff ko'rinishi)
   - sirlar maskalangan holda ko'rsatiladi

## Cheklovlar
- Savol matnini o'zgartirish **mavjud natijalarni qayta hisoblamaydi** — bu ogohlantirish
  sifatida ko'rsatiladi.
- Faol testdagi savolni nofaol qilish ogohlantirish bilan (savol soni o'zgaradi).
- Audit yozuvlari faqat o'qish uchun — o'chirish yoki tahrirlash imkoni yo'q.

## DoD
- [ ] Savol matni tahrirlanadi va darhol ommaviy testda ko'rinadi
- [ ] `scale`/`direction` tahrirlanmaydi (UI va API darajasida)
- [ ] Audit filtrlari ishlaydi, diff o'qiladi
- [ ] Tip katalogi tahrirlanadi va profil sahifasida yangilangan matn chiqadi

## Tekshiruv
```bash
cd frontend && npm run build
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Catalog|Audit"
```
