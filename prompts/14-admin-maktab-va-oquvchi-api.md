# P14 — Maktablar va o'quvchilar admin API

## O'qish shart
- `docs/07-api-shartnoma.md` (3.1, 3.2-bo'limlar)
- `docs/04-domain-model.md` (2.1, 2.2-bo'limlar)

## Vazifa
1. **Maktablar** (`SchoolsController`):
   - `List` — qidiruv (`pg_trgm`), viloyat/faollik filtri, pagination, sort
   - `GetById` — statistika bilan (o'quvchi soni, yakunlangan sessiyalar, oxirgi faollik)
   - `Create` — `slug` avtomatik generatsiya (nom + tuman → translit), band bo'lsa `-2`, `-3`
   - `Update`, `ToggleActive`, `Delete` (soft; o'quvchisi bo'lsa `409`)
   - `RegenerateLink` → yangi token + `publicUrl` + **QR kod** (`QRCoder`, PNG base64)
   - Barcha o'zgartirish `AuditLog` ga (`before/after`)
2. **O'quvchilar** (`StudentsController`):
   - `List` — filtrlar: `schoolId`, `grade`, `status`, `needsAttention`, `personalityType`,
     `activityLevel`, sana oralig'i, qidiruv; **snapshot ustunlardan** o'qiladi (JOIN'siz)
   - `GetById` — `docs/07` 3.2 dagi to'liq profil javobi (sessiyalar, natijalar, AI tahlil,
     AI tarixi). AI qismi hozircha bo'sh bo'lishi mumkin (P16–P18 dan keyin to'ladi)
   - `Delete` — `?hard=true` bo'lsa to'liq o'chirish (student + sessiyalar + javoblar + AI),
     audit'da faqat `{studentId, deletedAt, adminId}`
3. Query'lar `AsNoTracking()` + to'g'ridan-to'g'ri DTO proyeksiyasi.

## Cheklovlar
- `PageSize` maksimum 100 (kattaroq so'ralsa 100 ga tushiriladi).
- Sort maydonlari oq ro'yxatda (SQL injection'ga yo'l qo'ymaslik uchun).
- Barcha ro'yxat javoblari `PagedResult<T>` formatida.

## DoD
- [ ] Barcha endpointlar Swagger'da, `[Authorize]` bilan himoyalangan
- [ ] Integration testlar: pagination, filtr kombinatsiyalari, slug dublikati,
      `regenerate-link` dan keyin eski havola 404, o'quvchisi bor maktabni o'chirish → 409
- [ ] `hard=true` o'chirish barcha bog'liq yozuvlarni tozalaydi
- [ ] 1000 o'quvchida ro'yxat so'rovi < 300 ms (lokal)

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Schools|Students"
```
