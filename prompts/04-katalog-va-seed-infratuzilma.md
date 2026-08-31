# P04 — Test katalogi va seed infratuzilmasi

## Kontekst
DB tayyor (P03). Endi test bankini (metodikalar, savollar, tip katalogi, kasb xaritasi)
JSON fayllardan yuklaydigan idempotent seeder.

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (1-bo'lim — savol maydonlari)
- `docs/05-database-schema.md` (4-bo'lim — seed siyosati)

## Vazifa
1. `Infrastructure/Persistence/SeedData/` papkasi va JSON sxemasi:
```json
{
  "code": "BIG5", "nameUz": "Shaxsiyatning 5 omili", "descriptionUz": "...",
  "version": 1, "displayOrder": 2, "estimatedMinutes": 8, "pageSize": 10,
  "shuffleQuestions": false,
  "questions": [
    { "code": "B5-Q01", "order": 1, "textUz": "...", "type": "Likert5",
      "scale": "O", "direction": 1, "weight": 1.0, "isRequired": true }
  ]
}
```
2. `DbSeeder`:
   - `test-definitions/*.json` fayllarini o'qiydi
   - seed'dan kelgan testlarni `IsSystem = true`, `Kind = Standard`, `Status = Published` qilib
     belgilaydi; savollariga ham `IsSystem = true` qo'yadi (BR-8)
   - `ScoringStrategyCode` ni `Code` dan oladi (`MBTI16`, `BIG5`, `RIASEC`, `ACTIVITY`)
   - superadmin yaratgan (`Kind = Custom`) testlarga **tegmaydi**
   - **idempotent upsert**: `TestDefinition.Code` va `Question.Code` bo'yicha
     (mavjud bo'lsa matn/tartib yangilanadi, `scale`/`direction` **o'zgartirilmaydi** —
      agar farq bo'lsa xato bilan to'xtaydi va farqni log qiladi)
   - `type-catalog.json` (16 tip) va `career-map.json` ni ham yuklaydi
   - superadmin foydalanuvchini yaratadi (`ADMIN_USERNAME`, `ADMIN_PASSWORD` env; parol xeshlanadi)
   - `App:SeedOnStartup=true` bo'lsa start-upda ishlaydi; `--seed` argumenti bilan ham chaqiriladi
3. JSON fayllar `*.csproj` da `CopyToOutputDirectory=PreserveNewest`.
4. Bo'sh (skelet) fayllar yaratib qo'y: `mbti16.json`, `big5.json`, `riasec.json`, `activity.json`
   — savollar keyingi promptlarda to'ldiriladi. Har birida metadata to'g'ri bo'lsin.
5. `type-catalog.json` — 16 tipning **to'liq** o'zbekcha nomi, qisqa va uzun tavsifi,
   kuchli tomonlari (4–5), o'sish zonalari (3–4), kasb ishoralari (4–6).
6. `career-map.json` — kamida 15 ta eng keng tarqalgan Holland juftligi uchun yo'nalishlar
   (O'zbekiston sharoitiga moslashtirilgan: kasb nomlari mahalliy ta'lim tizimiga mos).
7. Seeder uchun integration test: ikki marta ishga tushirilsa dublikat yaratmaydi.

## Cheklovlar
- Seed migratsiyaga qo'yilmaydi.
- `scale` va `direction` qiymatlari faqat JSON'da — kodda qattiq yozilmaydi.
- Superadmin paroli kodda yoki JSON'da bo'lmasin.

## DoD
- [ ] `--seed` ikki marta ishlatilsa jadvaldagi qatorlar soni o'zgarmaydi
- [ ] `type_catalog` da 16 qator, `career_map` da ≥ 15 qator
- [ ] Superadmin yaratiladi, paroli xeshlangan
- [ ] Seeder integration testi yashil

## Tekshiruv
```bash
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet run --project src/StudentRoadMap.Api -- --seed
psql ... -c "select code, count(*) from questions q join test_definitions t on t.id=q.test_definition_id group by 1;"
psql ... -c "select count(*) from type_catalog;"
```
