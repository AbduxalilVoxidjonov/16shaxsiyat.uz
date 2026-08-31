# P30 — E2E testlar (Playwright)

## O'qish shart
- `docs/12-testlash-strategiyasi.md` (8-bo'lim — 10 stsenariy)

## Vazifa
1. `frontend/e2e/` da Playwright sozlamasi: `baseURL`, 3 brauzer (chromium majburiy),
   mobil viewport proyekti (`iPhone 13`), `trace: on-first-retry`.
2. **Test ma'lumoti:** `e2e/seed.ts` — har run oldidan API orqali alohida maktab yaratadi
   (unikal slug), oxirida tozalaydi. Testlar bir-biriga xalaqit bermasin.
3. `docs/12` 8-bo'limidagi **10 stsenariyni** yoz:
   E2E-1 to'liq oqim · E2E-2 sahifa yangilash · E2E-3 offline · E2E-4 nofaol maktab ·
   E2E-5 dublikat · E2E-6 admin maktab+QR · E2E-7 profil ko'rinishi · E2E-8 qayta tahlil ·
   E2E-9 Excel eksport · E2E-10 mobil oqim
4. **Yordamchi funksiyalar:** `fillTest(page, testCode)` — barcha savollarni tez to'ldiradi
   (190 savolni qo'lda bosmaslik uchun), `loginAsAdmin(page)`.
5. AI — `MockAiProvider` bilan (E2E'da real chaqiruv yo'q).
6. CI'da faqat `main` va release PR'larida ishlaydi; artefakt sifatida trace va screenshot saqlanadi.

## Cheklovlar
- `waitForTimeout` ishlatilmasin — faqat `expect(...).toBeVisible()` kabi kutishlar.
- Testlar parallel ishlashi kerak (har biri o'z maktabi bilan).
- Real AI provayderga chaqiruv qilinmasin.

## DoD
- [ ] 10 stsenariy yashil (lokal va CI'da)
- [ ] Mobil proyekt ham yashil
- [ ] Butun to'plam 10 daqiqadan kam vaqtda tugaydi
- [ ] Flaky test yo'q (3 marta ketma-ket ishga tushirilib tekshirilgan)

## Tekshiruv
```bash
cd frontend && npx playwright test
npx playwright test --repeat-each=3 --project=chromium
```
