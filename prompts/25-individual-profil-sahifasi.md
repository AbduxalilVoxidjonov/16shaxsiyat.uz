# P25 — Individual o'quvchi profili (asosiy ekran)

## Kontekst
Bu — superadmin eng ko'p ishlatadigan ekran. Loyihaning "mahsuloti" shu yerda ko'rinadi.

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-5 — to'liq maket)
- `docs/07-api-shartnoma.md` (3.2 — `GET /api/admin/students/{id}` javobi)

## Vazifa
`/admin/students/:id` sahifasi, `docs/11` A-5 dagi tartibda:

1. **Sarlavha bloki:** FISH, maktab, sinf, yosh, jins, holat `Badge`, `ReliabilityBadge`,
   amal tugmalari: PDF yuklab olish, Qayta tahlil, ⋯ (o'chirish, xom javoblar).
2. **Yig'ma kartalar (4 ta):** shaxsiyat tipi (harflar + o'zbekcha nom), Yetuklik indeksi,
   Aktivlik indeksi, Holland kodi. Har biri katta raqam + qisqa izoh.
3. **Diagrammalar bloki** (P26 da yoziladigan widget'lardan foydalanadi):
   16 tip o'qlari, Big Five radar, RIASEC bar, Aktivlik shkalalari.
4. **AI hisobot bloki:** provider/versiya/sana, bo'limlar (akkordeon yoki ketma-ket kartalar):
   umumiy xulosa, shaxsiyat portreti, kuchli tomonlar, o'sish zonalari, o'quv uslubi,
   motivatsiya, aktivlik baholovi, kasb yo'nalishlari, tavsiyalar (3 auditoriya), disclaimer.
5. **Holatlar:**
   - `Analyzing` → skeleton + "Tahlil tayyorlanmoqda (odatda 1 daqiqa)" + `refetchInterval: 5000`
   - `AnalysisFailed` → qizil karta: sabab + provider tanlab "Qayta urinish"
   - zaxira hisobot (`isFallbackReport`) → "Avtomatik shablon hisobot" belgisi
   - `Unreliable` → hisobot ustida sariq banner (matn `docs/11` A-5 dan)
6. **Tarix:** oldingi sessiyalar jadvali va AI tahlillar ro'yxati (provider, sana, ochish).
7. **Xom javoblar** dialogi: savol matni, javob, `durationMs`, `revisionCount`.
8. **Qayta tahlil** dialogi: provider tanlash (faqat mavjudlari), tasdiq → `202` → holat `Analyzing`.
9. Chop etish uslubi (`@media print`): sidebar va tugmalar yashiriladi, diagrammalar sig'adi.

## Cheklovlar
- Sahifa 3 soniyadan uzoq yuklanmasin (skeleton bilan bosqichma-bosqich ko'rsatish).
- AI matni **oddiy matn** sifatida render qilinadi (`dangerouslySetInnerHTML` taqiqlangan).
- `refetchInterval` faqat `Analyzing` holatida yoqiladi, keyin o'chadi.

## DoD
- [ ] Real ma'lumotli o'quvchida barcha bloklar to'g'ri ko'rinadi
- [ ] 4 ta holat (Analyzed, Analyzing, Failed, Unreliable) qo'lda tekshirilgan
- [ ] Qayta tahlil ishlaydi va holat avtomatik yangilanadi
- [ ] Chop etish ko'rinishi toza (Ctrl+P)
- [ ] Bo'sh AI massivlarida sahifa yiqilmaydi

## Tekshiruv
```bash
cd frontend && npm run test -- StudentProfile && npm run build
```
