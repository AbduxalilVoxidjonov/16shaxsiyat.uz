# P23 — Admin UI: maktablar va havolalar

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-3)
- `docs/07-api-shartnoma.md` (3.1)

## Vazifa
1. **Maktablar sahifasi** (`/admin/schools`):
   - `DataTable`: Nomi · Viloyat/Tuman · Havola · O'quvchi · Yakunlangan · Holat · amallar
   - qidiruv (400 ms debounce), viloyat va faollik filtri — hammasi URL query'da
2. **Havola ustuni**: qisqartirilgan URL + nusxalash tugmasi (toast: "Havola nusxalandi") +
   QR ikonkasi.
3. **QR modal**: katta QR (backenddan base64), maktab nomi, havola matni,
   "PNG yuklab olish" va "Chop etish" (A5 formatga mos chop etish uslubi).
4. **Yaratish/tahrirlash** — o'ng tomondan drawer: nomi, viloyat, tuman, raqami, mas'ul shaxs,
   telefon, kunlik limit, kirish kodi (ixtiyoriy), izoh. Zod validatsiya.
5. **Havolani yangilash** — tasdiq dialogi: "Eski havola darhol ishlamay qoladi. Maktabga
   yangi havolani yuborishni unutmang." → muvaffaqiyatda yangi havola ko'rsatiladi.
6. **Faol/nofaol** toggle va **o'chirish** (o'quvchisi bo'lsa 409 → tushunarli xabar).
7. Barcha mutation'lardan keyin tegishli query invalidatsiya + toast.

## Cheklovlar
- Havola to'liq matni jadvalda ko'rinmasin (uzun) — qisqartirilgan + nusxalash.
- Xavfli amallar (havola yangilash, o'chirish) har doim tasdiq dialogi bilan.

## DoD
- [ ] CRUD to'liq ishlaydi
- [ ] QR ochiladi, yuklab olinadi, chop etishga tayyor
- [ ] Havola yangilangach eski havola brauzerda 404 beradi (qo'lda tekshirilgan)
- [ ] Filtrlar URL'da saqlanadi (sahifa yangilansa qoladi)
- [ ] Bo'sh holat va xato holatlari chiroyli ko'rinadi

## Tekshiruv
```bash
cd frontend && npm run build
# qo'lda: maktab yarat → havolani nusxala → yangi tabda och → test boshlanishini ko'r
```
