# StudentRoadMap (Salohiyat)

O'quvchining shaxsiyati, psixologik yetukligi, qiziqishlari va aktivligini onlayn testlar orqali
aniqlaydigan, natijalarni AI bilan tahlil qiladigan CRM platforma. Ommaviy nom — **Salohiyat**
(`salohiyat.uz`); `StudentRoadMap` — ichki kod nomi.

Maktablarga shaxsiy havola beriladi; o'quvchi anketa to'ldirib 4 blok test (16 Personality,
Big Five, Holland RIASEC, Aktivlik/motivatsiya) yechadi; bitta superadmin barcha natijalarni
va har o'quvchining individual profilini boshqaradi.

Hujjatlar — haqiqat manbai: [`docs/00-README.md`](docs/00-README.md).

## Stek

- **Backend:** ASP.NET Core (net10.0), Clean Architecture + CQRS (MediatR)
- **DB:** PostgreSQL 16 (EF Core + Npgsql)
- **Frontend:** React 19 + TypeScript + Vite + Tailwind + TanStack Query (`frontend/`, keyingi promptda)
- **AI:** provider-agnostik — Gemini / OpenAI / Anthropic

## Yechim strukturasi

```
src/
├── StudentRoadMap.Domain/          entity, VO, scoring qoidalari (paketsiz)
├── StudentRoadMap.Application/     CQRS use-case'lar (MediatR + FluentValidation)
├── StudentRoadMap.Infrastructure/  EF Core + Npgsql, AI provider'lar
└── StudentRoadMap.Api/             ASP.NET Core Web API (Controllers, Swagger, health)

tests/
├── StudentRoadMap.Domain.Tests/
├── StudentRoadMap.Application.Tests/
└── StudentRoadMap.Api.IntegrationTests/
```

Qatlam bog'liqligi: `Api → Application, Infrastructure` · `Infrastructure → Application → Domain`.
Batafsil: [`docs/06-arxitektura.md`](docs/06-arxitektura.md).

## Ishga tushirish

Talab: .NET SDK 10.0+.

```bash
dotnet build
dotnet test

dotnet run --project src/StudentRoadMap.Api
# yoki: dotnet run --project src/StudentRoadMap.Api --urls http://localhost:5299

curl http://localhost:5062/health
```

Development muhitida Swagger `/swagger` manzilida ochiladi.

Ma'lumotlar bazasi ulanish satri va boshqa sirlar `appsettings.json` da **saqlanmaydi** —
`dotnet user-secrets` (dev) yoki muhit o'zgaruvchilari (prod) orqali beriladi.

## Foydali buyruqlar

```bash
dotnet build && dotnet test
dotnet run --project src/StudentRoadMap.Api
dotnet run --project src/StudentRoadMap.Api -- --migrate
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet ef migrations add <Nom> -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api
```

## Ishlab chiqish tartibi

Kod `prompts/` papkasidagi ketma-ket promptlar bilan quriladi — `prompts/00-qollanma.md` dan
boshlab. Qat'iy qoidalar: [`CLAUDE.md`](CLAUDE.md).
