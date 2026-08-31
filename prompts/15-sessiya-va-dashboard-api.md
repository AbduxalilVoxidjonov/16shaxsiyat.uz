# P15 — Sessiyalar va dashboard API

## O'qish shart
- `docs/07-api-shartnoma.md` (3.3, 3.6-bo'limlar)

## Vazifa
1. **Sessiyalar** (`AssessmentsController`):
   - `List` — maktab, holat, sana filtri, pagination
   - `GetById` — to'liq detal (o'quvchi, 4 natija, AI tahlil, tarix, ishonchlilik)
   - `GetAnswers` — xom javoblar (savol matni, javob, `durationMs`, `revisionCount`) — audit uchun
   - `RecalculateScores` — scoring versiyasi o'zgarganda qayta hisoblash (audit yoziladi)
   - `Delete` — soft
2. **Dashboard** (`DashboardController`):
   - `GetStats` — `docs/07` 3.6 dagi to'liq javob
   - Og'ir agregatlar bitta so'rovda (`GROUP BY`), `IMemoryCache` 60 soniya
   - `dropOffRate` = boshlangan lekin yakunlanmagan / jami boshlangan
3. **Audit** (`AuditController`) — `GET /api/admin/audit-logs` filtr va pagination bilan.

## Cheklovlar
- Dashboard so'rovi 1 soniyadan uzoq ketmasin — kerak bo'lsa indeks qo'sh
  (`docs/05` dagi mavjudlaridan foydalanib).
- Statistikada o'chirilgan (soft-deleted) yozuvlar hisobga olinmaydi.

## DoD
- [ ] Dashboard javobi to'liq va to'g'ri (qo'lda SQL bilan solishtirilgan)
- [ ] Kesh ishlaydi (ikkinchi so'rov sezilarli tez)
- [ ] Integration testlar: statistika hisob-kitobi, filtrlar, audit ro'yxati
- [ ] 5000 sessiyali ma'lumotda dashboard < 1 s

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Dashboard|Assessments"
curl -s localhost:5000/api/admin/dashboard/stats -H "Authorization: Bearer $T" | jq
```
