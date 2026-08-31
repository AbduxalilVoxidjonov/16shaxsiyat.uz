# P01 — Solution'ni Clean Architecture ga o'tkazish

## Kontekst
Papkada hozir bo'sh ASP.NET Core **MVC** shabloni bor (`StudentRoadMap/` — Views, wwwroot,
HomeController). Uni Clean Architecture yechimiga aylantiramiz. Hech qanday biznes kod hali yo'q.

## O'qish shart
- `docs/06-arxitektura.md` (2 va 3-bo'limlar — papka strukturasi va qatlam qoidalari)

## Vazifa
1. Mavjud `StudentRoadMap/` MVC loyihasini `src/StudentRoadMap.Api/` ga ko'chir va tozala:
   - o'chir: `Views/`, `wwwroot/lib/`, `Models/ErrorViewModel.cs`, `Controllers/HomeController.cs`
   - `Program.cs` ni API uchun qayta yoz (Controllers, Swagger, CORS, health)
2. Yangi loyihalar yarat va solution'ga qo'sh:
   - `src/StudentRoadMap.Domain` (classlib, net10.0, paketsiz)
   - `src/StudentRoadMap.Application` (classlib) — MediatR, FluentValidation
   - `src/StudentRoadMap.Infrastructure` (classlib) — Npgsql.EntityFrameworkCore.PostgreSQL,
     EFCore.NamingConventions, Microsoft.EntityFrameworkCore.Design
   - `tests/StudentRoadMap.Domain.Tests`, `tests/StudentRoadMap.Application.Tests`,
     `tests/StudentRoadMap.Api.IntegrationTests` (xUnit + FluentAssertions)
3. Bog'liqliklar: `Api → Application, Infrastructure` · `Infrastructure → Application → Domain`.
4. `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`,
   `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`.
5. `.editorconfig` (C# uslub qoidalari), `.gitignore` (bin/obj/node_modules/.env).
6. `Program.cs` da: Serilog, Swagger (faqat Development), CORS (`App:FrontendUrl`),
   `/health` va `/health/ready` endpointlari, `ProblemDetails` sozlamasi.
7. `README.md` — loyiha nima, qanday ishga tushiriladi (qisqa).

## Cheklovlar
- Hech qanday entity, controller yoki biznes mantiq **yozilmaydi** — faqat skelet.
- `Domain` loyihasida hech qanday NuGet paket bo'lmasin.
- Razor/MVC ga tegishli hech narsa qolmasin.

## DoD
- [ ] `dotnet build` 0 xato, 0 ogohlantirish
- [ ] `dotnet test` o'tadi (testlar bo'sh bo'lsa ham)
- [ ] `dotnet run --project src/StudentRoadMap.Api` → `/health` 200 qaytaradi
- [ ] Swagger `/swagger` Development'da ochiladi
- [ ] Qatlam bog'liqliklari `docs/06` ga mos (Domain hech nimaga bog'liq emas)

## Tekshiruv
```bash
dotnet build
dotnet test
dotnet run --project src/StudentRoadMap.Api &
curl -s localhost:5000/health
```
