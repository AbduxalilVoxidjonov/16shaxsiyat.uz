# P24 — Admin UI: o'quvchilar ro'yxati va filtrlar

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-4)
- `docs/07-api-shartnoma.md` (3.2)

## Vazifa
1. **O'quvchilar sahifasi** (`/admin/students`):
   - filtr paneli: maktab (searchable select), sinf, holat, shaxsiyat tipi, aktivlik darajasi,
     "faqat e'tibor talab qiladiganlar" toggle, sana oralig'i, qidiruv
   - `DataTable`: FISH · Maktab · Sinf · Tip · Yetuklik · Aktivlik · Ishonchlilik · Sana
   - `needsAttention` qatorlarida chap chekkada yupqa rangli chiziq (butun qator bo'yalmaydi)
   - holat va ishonchlilik `Badge` sifatida
   - qatorga bosish → `/admin/students/:id`
2. **Faol filtrlar chipi** — tanlangan filtrlar tepada chip sifatida, har birini alohida
   olib tashlash mumkin, "Hammasini tozalash".
3. **Excel eksport** tugmasi — joriy filtr bilan `GET /api/admin/students/export`,
   yuklanish indikatori bilan.
4. Barcha filtr holati URL query'da (`useSearchParams`) — ulashiladigan havola.
5. Skeleton loading, bo'sh holat ("Hali o'quvchi yo'q — maktab havolasini ulashing"),
   xato holati (qayta urinish tugmasi bilan).

## Cheklovlar
- Server-side pagination — barcha ma'lumotni bir marta yuklash taqiqlanadi.
- Qidiruv debounce'siz so'rov yubormasin.
- 100 dan ortiq `pageSize` so'ralmasin.

## DoD
- [ ] Barcha filtrlar backend bilan to'g'ri ishlaydi
- [ ] URL ulashilsa boshqa brauzerda bir xil ko'rinish
- [ ] Eksport fayli ochiladi, qatorlar soni filtrga mos
- [ ] 1000+ o'quvchida sahifa tez ochiladi
- [ ] Mobil ko'rinishda jadval gorizontal scroll bilan o'qiladi

## Tekshiruv
```bash
cd frontend && npm run test && npm run build
```
