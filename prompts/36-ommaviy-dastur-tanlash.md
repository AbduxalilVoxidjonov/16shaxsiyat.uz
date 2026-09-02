# P36 — Ommaviy UI: dastur tanlash va so'rovnoma oqimi

## Kontekst
P34 (model) va P35 (admin) tayyor. Endi o'quvchi tomoni.
Qarorlar `docs/06` 8-bo'limida (2026-09-02).

## O'qish shart
- `docs/06-arxitektura.md` 8-bo'lim — 2026-09-02 qarorlari
- `docs/11-ux-va-ekranlar.md` (E-1 landing, E-3…E-6)
- `prompts/34` (model), `prompts/20`/`prompts/21` (mavjud oqim)

## Vazifa
1. **Landing (E-1) yangilanadi:** maktabda mavjud dasturlar ro'yxati.
   - **Aynan bitta dastur bo'lsa — tanlov ko'rsatilmaydi**, hozirgi oqim o'zgarmaydi
     (bu regressiya xavfi: bugungi foydalanuvchilar uchun hech narsa o'zgarmasligi kerak);
   - bir nechta bo'lsa — karta ko'rinishida tanlov: nomi, tavsifi, savol soni,
     taxminiy vaqt. Tanlov `sessionStore` da saqlanadi va anketaga uzatiladi.
2. **Anketa (E-2)** `programCode` bilan `POST /api/public/sessions` yuboradi.
3. **So'rovnoma (`ScoringMode = Survey`) oqimi:**
   - savollar test kabi ko'rsatiladi, autosave bir xil ishlaydi;
   - yakunda **ball ko'rsatilmaydi** — "Javoblaringiz saqlandi, rahmat" ko'rinishi;
   - natija ekrani (E-6) so'rovnoma uchun ochilmaydi.
4. **Qisqa natija (E-6) shartli bo'ladi:**
   - dasturda shaxsiyat batareyasi bo'lsa — hozirgidek tip kartasi va yo'nalishlar;
   - bo'lmasa — faqat "yakunlandi" holati va (agar `showResultToStudent` yoqilgan bo'lsa)
     dasturga tegishli qisqa xulosa. **Bo'sh joy yoki `0` ko'rsatilmaydi.**
5. **Xato holatlari:** maktabda dastur yo'q → tushunarli xabar
   ("Bu maktab uchun test hali tayyorlanmagan, maktabingizga murojaat qiling"),
   `400 PROGRAM_REQUIRED` → tanlov ekraniga qaytarish.

## Cheklovlar
- Javob yo'qolmasligi qoidasi kuchda (`prompts/21`): autosave, offline navbat,
  `fetch(keepalive)` — o'zgartirilmaydi.
- Natija ekranida aktivlik ballari, bayroqlar, xom ballar ko'rsatilmaydi.
- 360px da gorizontal scroll yo'q; klaviatura bilan to'liq ishlash.
- `dangerouslySetInnerHTML` taqiqlangan.

## DoD
- [ ] Bitta dasturli maktabda oqim **avvalgidek** (tanlov ekranisiz) — regressiya testi
- [ ] Ikki dasturli maktabda tanlov ishlaydi, tanlangan dastur sessiyaga o'tadi
- [ ] So'rovnoma yakunlanadi va ball ko'rsatilmaydi
- [ ] Shaxsiyat batareyasisiz dasturda natija ekrani bo'sh joy yoki `0` ko'rsatmaydi
- [ ] Dasturi yo'q maktabda tushunarli xabar
- [ ] Sahifa yangilanganda tanlangan dastur saqlanadi

## Tekshiruv
```bash
cd frontend && npm run typecheck && npm run lint && npm run test && npm run build
```
