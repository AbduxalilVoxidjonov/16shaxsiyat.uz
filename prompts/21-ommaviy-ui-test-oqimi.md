# P21 — Ommaviy UI: test yechish, autosave, yakun

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (E-3 … E-6)
- `docs/10-frontend-arxitektura.md` (4-bo'lim)

## Vazifa
1. **E-3 Test sahifasi** (`/t/:slug/test/:testCode`):
   - sticky header: test nomi, blok indikatori (●●○○), progress bar
   - sahifada 10 savol, `LikertQuestion` komponenti (5 katta tugma, ≥ 44px)
   - klaviatura: `1..5` javob, `Enter` keyingi savol
   - javob tanlanganda keyingi savolga yumshoq scroll
   - `durationMs` — savol ko'ringan vaqtdan javobgacha (`performance.now()`)
   - "Keyingi" — to'ldirilmagan savol bo'lsa birinchisiga scroll + qizil ramka
   - sticky footer: Orqaga / Keyingi
2. **Autosave (`useAutosave`)**:
   - javob darhol lokal store'ga (UI kutmaydi)
   - debounce 1.5 s + sahifa almashganda + har 10 s → `POST .../answers`
   - `pendingAnswers` navbati `localStorage` da
   - offline: banner + navbat to'planadi, online bo'lganda avtomatik yuboriladi
   - `beforeunload` → `sendBeacon`
   - "Saqlandi ✓" / "Saqlanmoqda…" indikatori
3. **E-4 Blok yakuni** — tabriknoma, qolgan bloklar, "Davom etish" / "Keyinroq".
4. **E-5 Yakuniy ekran** — `POST /sessions/complete`, "Natijang tahlil qilinmoqda",
   `showResultToStudent` bo'lsa 10 s dan keyin natija tugmasi faollashadi.
5. **E-6 Qisqa natija** — tip kartasi, 3 kuchli tomon, 3 yo'nalish, disclaimer.
   `202` kelsa "hali tayyor emas, keyinroq urin".
6. Sahifa yangilanganda `GET /sessions/me` → to'g'ri test va sahifadan davom etadi.

## Cheklovlar
- Javob yuborish xatosi foydalanuvchini bloklamaydi — navbatda qoladi, qayta uriniladi.
- `410` (sessiya tugagan) → landing'ga qaytarish + tushunarli xabar.
- Natija ekranida aktivlik ballari, bayroqlar, xom ballar **ko'rsatilmaydi**.

## DoD
- [ ] To'liq oqim boshidan oxirigacha ishlaydi (190 savol)
- [ ] Sahifa yangilansa o'sha joydan davom etadi, javoblar joyida
- [ ] Offline rejimda javoblar yo'qolmaydi (DevTools bilan sinaldi)
- [ ] `useAutosave` unit testlari (debounce, navbat, offline, retry)
- [ ] Mobil qurilmada real sinov: 30 daqiqada tugatish mumkin
- [ ] a11y: klaviatura bilan to'liq yechish mumkin, `axe` buzilishsiz

## Tekshiruv
```bash
cd frontend && npm run test -- useAutosave
# qo'lda: DevTools → Network offline → javob berish → online → yuborilishini kuzatish
```
