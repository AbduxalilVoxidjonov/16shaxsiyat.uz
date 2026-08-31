# P03 — EF Core, DbContext va birinchi migratsiya

## Kontekst
Domain tayyor (P02). Endi PostgreSQL ga saqlash qatlami.

## O'qish shart
- `docs/05-database-schema.md` (to'liq — DDL mos yozuvlar manbai)
- `docs/06-arxitektura.md` (7-bo'lim — konfiguratsiya)

## Vazifa
1. `Application/Common/Interfaces/IAppDbContext.cs` — barcha `DbSet<T>` lar va
   `SaveChangesAsync(CancellationToken)`.
2. `Infrastructure/Persistence/AppDbContext.cs`:
   - `IAppDbContext` ni amalga oshiradi
   - `UseSnakeCaseNamingConvention()`
   - `ApplyConfigurationsFromAssembly`
   - Soft delete global query filter (`School`, `Student`, `Assessment`)
   - `SaveChangesAsync` da: `CreatedAt/UpdatedAt` avtomatik, domen hodisalarini MediatR orqali
     publish qilish (`SaveChanges` dan **keyin**)
3. `Persistence/Configurations/` — har entity uchun `IEntityTypeConfiguration<T>`:
   ustun turlari, uzunliklar, indekslar va **`docs/05` dagi barcha unique/partial indekslar**
   (`HasFilter(...)` bilan), `jsonb` ustunlar (`.HasColumnType("jsonb")`).
4. Enum'lar `smallint` sifatida saqlanadi (`HasConversion<short>()`).
5. `AddInfrastructure(IConfiguration)` kengaytmasi — DbContext, `IAppDbContext`, `IDateTime`,
   `IEncryptionService` ro'yxatga olinadi.
6. Birinchi migratsiya: `InitialCreate`.
7. `Api/Program.cs` ga `--migrate` argumenti: berilsa migratsiya bajarilib, ilova to'xtaydi.
8. `/health/ready` — DB ulanishini tekshiradi.
9. `docker-compose.yml` (faqat `db` servisi hozircha) va `.env.example`.

## Cheklovlar
- Qo'lda SQL migratsiya yozilmaydi.
- `Application` va `Domain` da EF paketiga bog'liqlik yo'q (faqat `IAppDbContext` abstraksiyasi).
- Ulanish satri kodda emas — `ConnectionStrings:Postgres`.

## DoD
- [ ] `docker compose up -d db` → migratsiya bo'sh DB'da xatosiz o'tadi
- [ ] Yaratilgan jadval va indekslar `docs/05` bilan mos (nom va ustunlar bo'yicha)
- [ ] `dotnet ef migrations has-pending-model-changes` — o'zgarish yo'q
- [ ] `/health/ready` DB o'chirilganda 503 qaytaradi

## Tekshiruv
```bash
docker compose up -d db
dotnet ef database update -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api
psql -h localhost -U srm -d studentroadmap -c "\dt"
psql -h localhost -U srm -d studentroadmap -c "\di"
```
