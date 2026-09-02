# P35 — Admin UI: savollar bo'limi, dasturlar va biriktirish

## Kontekst
P34 da dastur modeli, biriktirish va shartli scoring backend'da tayyor bo'ldi.
Endi superadmin uchun ko'rinadigan qism. Qarorlar `docs/06` 8-bo'limida (2026-09-02).

## O'qish shart
- `docs/06-arxitektura.md` 8-bo'lim — 2026-09-02 qarorlari
- `docs/11-ux-va-ekranlar.md` (A-6 katalog, A-8 konstruktor maketi)
- `docs/07-api-shartnoma.md` (3.4 katalog API + P34 qo'shgan dastur endpointlari)
- `prompts/34` (model va terminologiya)

## Vazifa

### A. Savollar/testlar bo'limi (`/admin/catalog`)
1. Ikki guruh: **"Tizim metodikalari"** (qulf ikonkasi, tahrirlanmaydi) va
   **"Mening testlarim"** (holat chipi: Draft/Published/Archived).
   Har qatorda: nomi, turi (`Ballanadi`/`So'rovnoma`), savol soni, taxminiy vaqt,
   holat, qaysi dasturlarda ishlatilishi.
2. Test ko'rish sahifasi: savollar ro'yxati (matn, shkala, yo'nalish, og'irlik).
   Tizim metodikalarida hamma narsa **faqat o'qish** uchun.

### B. Test yuklash (import)
3. **"Test yuklash"** tugmasi — JSON fayl yuklanadi (seed fayllari bilan **bir xil sxema**:
   `code`, `nameUz`, `questions[]` va h.k. — `prompts/04` dagi sxema).
4. Yuklashdan oldin **validatsiya va preview**: nechta savol, qaysi shkalalar,
   topilgan xatolar ro'yxati (qator raqami bilan). Xato bo'lsa yuklash tugmasi o'chiq.
5. Muvaffaqiyatda test **`Draft` holatida** yaratiladi — hech qachon to'g'ridan-to'g'ri
   o'quvchiga chiqmaydi.
6. Dublikat `code` bo'lsa aniq xabar: "Bu kod bilan test allaqachon bor."

### C. Dasturlar (`/admin/programs`)
7. Ro'yxat: nomi, turi (Tizim/Mening), ko'rinishi (Ommaviy/Biriktirilgan),
   testlar soni, jami savol va vaqt, holat, biriktirilgan maktablar soni.
8. Dastur yaratish/tahrirlash: nomi, tavsifi, ko'rinishi,
   **testlarni tanlash va sudrab tartiblash** (jami savol soni va vaqt jonli hisoblanadi).
9. **Maktablarga biriktirish** — `Visibility = Assigned` bo'lganda: qidiruvli ko'p tanlovli
   ro'yxat, biriktirilgan maktablar chipi bilan.
10. `Publish` — tasdiq dialogi bilan; nashr qilingandan keyin dastur o'quvchilarga ko'rinadi.
11. Tizim dasturi (`Shaxsiyat profili`) tarkibi tahrirlanmaydi — qulf va tushuntirish
    ("Ilmiy metodikalar himoyalangan"), lekin **ko'rinishi va biriktirishi o'zgartiriladi**.

### D. Ogohlantirishlar (jimgina xato bo'lmasin)
12. Dastur nashr qilinayotganda **jami vaqt 40 daqiqadan oshsa** — ogohlantirish banneri.
13. Maktabga **birorta dastur biriktirilmagan** bo'lsa, maktablar ro'yxatida va maktab
    sahifasida aniq belgi: "Dastur biriktirilmagan — o'quvchilar test yecha olmaydi."
    Bu jimgina buzilish, foydalanuvchi buni o'zi payqamaydi.
14. Dasturda ilmiy batareya (BIG5 + ACTIVITY) **yo'q** bo'lsa, tahrirlash sahifasida
    izoh: "Bu dasturda yetuklik va aktivlik indekslari hisoblanmaydi, AI hisoboti
    qisqartirilgan bo'ladi." — admin nima yo'qotayotganini bilishi kerak.

## Cheklovlar
- Faqat `frontend/`. `DataTable`/`useServerTableState` mavjudini ishlat.
- `dangerouslySetInnerHTML` taqiqlangan; `any` yo'q; hardcode matn yo'q (i18n).
- `shared/` `features/` dan import qilmaydi.
- Xavfli amallar (nashr, arxivlash, biriktirishni olib tashlash) tasdiq dialogi bilan.

## DoD
- [ ] JSON test yuklanadi, xatolar aniq ko'rsatiladi, `Draft` holatida yaratiladi
- [ ] Dastur yaratiladi, testlar qo'shiladi va tartiblanadi, maktabga biriktiriladi, nashr qilinadi
- [ ] Biriktirilgandan keyin o'sha maktab havolasida dastur ko'rinadi (qo'lda tekshirilgan)
- [ ] Dasturi yo'q maktab aniq belgilanadi
- [ ] Tizim metodikasi va tizim dasturi tarkibini o'zgartirib bo'lmaydi (UI'da ham)
- [ ] Filtrlar URL'da saqlanadi

## Tekshiruv
```bash
cd frontend && npm run typecheck && npm run lint && npm run test && npm run build
```
