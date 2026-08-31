# P27 — Excel va PDF eksport

## O'qish shart
- `docs/02-biznes-talablar.md` (FR-7)
- `docs/07-api-shartnoma.md` (3.2, 3.3)

## Vazifa
1. **Excel** (`ClosedXML`): `GET /api/admin/students/export?<filtrlar>`
   - ustunlar: FISH, Maktab, Viloyat/Tuman, Sinf, Jins, Yosh, Telefon, Ota-ona telefoni,
     Sana, Holat, Tip, Tip nomi, Yetuklik, Aktivlik indeksi, Aktivlik darajasi, Holland kodi,
     Ishonchlilik, O'qish uchun izoh
   - sarlavha qatori muzlatilgan, avtofiltr, ustun kengliklari avtomatik
   - 10 000 qatorgacha ishlaydi (streaming yozish, xotira portlamasin)
   - fayl nomi: `oquvchilar_2026-08-31.xlsx`
   - `Export.StudentsDownloaded` audit yozuvi
2. **PDF** (`QuestPDF`): `GET /api/admin/assessments/{id}/report.pdf`
   - 1-sahifa: sarlavha, o'quvchi ma'lumoti, yig'ma kartalar (tip, indekslar, Holland)
   - 2-sahifa: diagrammalar (serverda `SkiaSharp`/QuestPDF vositalari bilan chiziladi —
     bar va radar; brauzer render qilinmaydi)
   - 3+ sahifa: AI hisobot bo'limlari
   - oxirgi sahifa: disclaimer va "AI tomonidan tayyorlangan, mutaxassis ko'rigi tavsiya etiladi"
   - kolontitul: platforma nomi, sahifa raqami, sana
   - o'zbek lotin harflari to'g'ri chiqadigan shrift (`DejaVu Sans` yoki `Inter`) embed qilinadi
3. Frontend: eksport tugmalari, yuklanish indikatori, xatoda toast.

## Cheklovlar
- PDF generatsiyasi 10 soniyadan uzoq ketmasin.
- Ma'lumot yo'q bo'lgan bo'limlar PDF'da bo'sh joy qoldirmasin (o'tkazib yuboriladi).
- Excel'da telefon raqamlari matn sifatida (formatlanmasin).
- Fayl nomlarida kirill yoki maxsus belgi bo'lmasin.

## DoD
- [ ] Excel Excel va LibreOffice'da to'g'ri ochiladi, o'zbek harflari buzilmaydi
- [ ] 5000 qatorli eksport 15 soniyada tugaydi
- [ ] PDF barcha bo'limlar bilan to'g'ri chiqadi, shrift buzilmaydi
- [ ] Diagrammalar PDF'da o'qiladi
- [ ] Audit yozuvi yaratiladi

## Tekshiruv
```bash
curl -s "localhost:5000/api/admin/students/export?schoolId=$ID" -H "Authorization: Bearer $T" -o test.xlsx
curl -s "localhost:5000/api/admin/assessments/$AID/report.pdf" -H "Authorization: Bearer $T" -o test.pdf
```
