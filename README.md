# StudentRoadMap (Shaxsiyat)

O'quvchining shaxsiyati, psixologik yetukligi, qiziqishlari va aktivligini onlayn testlar orqali
aniqlaydigan, natijalarni AI bilan tahlil qiladigan CRM platforma. Ommaviy nom — **Shaxsiyat**
(`16shaxsiyat.uz`); `StudentRoadMap` — ichki kod nomi (repo, solution, konteynerlar).

Maktablarga shaxsiy havola beriladi; o'quvchi anketa to'ldirib 4 blok test (16 Personality,
Big Five, Holland RIASEC, Aktivlik/motivatsiya) yechadi; **bitta superadmin** barcha natijalarni
va har o'quvchining individual profilini boshqaradi.

Hujjatlar — haqiqat manbai: [`docs/00-README.md`](docs/00-README.md).
Superadmin uchun ekran-ekran qo'llanma: [`docs/16-foydalanuvchi-qollanmasi.md`](docs/16-foydalanuvchi-qollanmasi.md).

## Stek

- **Backend:** ASP.NET Core (net10.0), Clean Architecture + CQRS (MediatR)
- **DB:** PostgreSQL 16 (EF Core + Npgsql, `jsonb` ballar va AI javoblari uchun)
- **Frontend:** React 19 + TypeScript (strict) + Vite + Tailwind + TanStack Query (`frontend/`)
- **AI:** provider-agnostik — Gemini / OpenAI / Anthropic (superadmin qaysi kalitni qo'ysa, o'sha ishlaydi)
- **Deploy:** Docker compose (`db` → `migrate` → `seed` → `api` → `app` → `tunnel`), Cloudflare Tunnel, GitHub Actions CI

## Arxitektura sxemasi

```
                         ┌────────────────────────── Docker compose ──────────────────────────┐
                         │                                                                       │
Internet ── Cloudflare ──┼──▶ tunnel (cloudflared) ──▶ app (nginx: SPA + /api proksi) ──▶ api ──┼──▶ db (Postgres 16)
  Tunnel (TLS, DDoS)     │                                        ▲                              │
                         │                              qurilgan React build                     │
                         └───────────────────────────────────────────────────────────────────────┘

src/
├── StudentRoadMap.Domain/          entity, VO, scoring qoidalari (paketsiz — EF/HTTP yo'q)
├── StudentRoadMap.Application/     CQRS use-case'lar (MediatR + FluentValidation)
├── StudentRoadMap.Infrastructure/  EF Core + Npgsql, AI provider'lar, seed
└── StudentRoadMap.Api/             ASP.NET Core Web API (Controllers, Swagger, health)

tests/
├── StudentRoadMap.Domain.Tests/
├── StudentRoadMap.Application.Tests/
├── StudentRoadMap.Infrastructure.Tests/
└── StudentRoadMap.Api.IntegrationTests/

frontend/src/   React SPA — routing, TanStack Query, Zustand (`docs/10-frontend-arxitektura.md`)
```

Qatlam bog'liqligi: `Api → Application, Infrastructure` · `Infrastructure → Application → Domain`.
`Domain` va `Application`da EF, HTTP, `DateTime.Now` yo'q — vaqt parametr sifatida uzatiladi.
Batafsil: [`docs/06-arxitektura.md`](docs/06-arxitektura.md).

Production'da bitta origin ishlatiladi: frontend `/api/...` ga nisbiy yo'l bilan so'rov
yuboradi, `app` (nginx) buni ichki tarmoqda `api`ga proksilaydi — alohida `api.` subdomeni
va CORS sozlash shart emas. Batafsil: [`docs/13-deploy-va-infratuzilma.md`](docs/13-deploy-va-infratuzilma.md).

## Tez boshlash (Docker compose — tavsiya etiladi)

Talab: Docker + Docker Compose v2.

```bash
git clone <repo-url> StudentRoadMap && cd StudentRoadMap
cp .env.example .env
```

`.env` faylini oching va **barcha `CHANGE_ME_...` qiymatlarni to'ldiring**:

| O'zgaruvchi | Qanday olinadi |
|-------------|-----------------|
| `DB_PASSWORD` | O'zingiz tanlagan kuchli parol |
| `Jwt__Key` | `openssl rand -base64 48` |
| `Security__EncryptionKey` | `openssl rand -base64 32` (aynan 32 bayt, AI kalitlarini shifrlash uchun) |
| `Security__IpHashSalt` | `openssl rand -base64 24` |
| `ADMIN_USERNAME` / `ADMIN_PASSWORD` / `ADMIN_EMAIL` | Yagona superadmin uchun o'zingiz tanlaysiz |
| `TUNNEL_TOKEN` | Faqat production'da kerak — Cloudflare Zero Trust > Tunnels > connector token. Faqat lokal sinov uchun `tunnel` xizmatini `docker compose up` buyrug'iga qo'shmang (pastga qarang) |
| `App__KnownProxies` | Standart qiymat (`172.26.0.0/16`) `docker-compose.yml`dagi qotirilgan subnet bilan bir xil — odatda o'zgartirish shart emas (`docs/13` §7) |

Keyin bitta buyruq bilan hamma narsa ko'tariladi:

```bash
docker compose up -d --build
```

Bu ketma-ket bajaradi: `db` (sog'lom bo'lguncha kutadi) → `migrate` (barcha migratsiya,
bir martalik) → `seed` (190 savol, 16 tip, kasb xaritasi, superadmin — idempotent) → `api` →
`app` (frontend + `/api` proksi) → `tunnel` (production'da Cloudflare'ga ulaydi).

Holatni kuzatish:
```bash
docker compose ps
docker compose logs -f migrate seed     # ikkalasi ham 0-kod bilan tugashi shart
```

**Faqat lokal sinov** (production tunnel kerak emas) — `app`ni xostga vaqtincha chiqarib
ko'rish uchun `docker-compose.yml`dagi izohlangan `ports:` qatorini oching yoki:
```bash
docker compose up -d --build db migrate seed api app   # tunnel'siz
docker compose port app 8080
curl http://localhost:<port>/health
```

Superadmin sifatida `.env`dagi `ADMIN_USERNAME`/`ADMIN_PASSWORD` bilan kiring — birinchi
kirishda parolni o'zgartirish tavsiya etiladi. Ekran-ekran qo'llanma:
[`docs/16-foydalanuvchi-qollanmasi.md`](docs/16-foydalanuvchi-qollanmasi.md).

To'liq deploy tafsilotlari, production topologiyasi, subnet/`App__KnownProxies` tuzog'i,
backup va tiklanish — [`docs/13-deploy-va-infratuzilma.md`](docs/13-deploy-va-infratuzilma.md).

## Tez boshlash (Docker'siz, kundalik ishlab chiqish)

Talab: .NET SDK 10.0+, Node.js 22+, Docker (faqat `db` uchun).

```bash
docker compose up -d db

dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=studentroadmap;Username=srm;Password=<.env dagi DB_PASSWORD>" \
  -p src/StudentRoadMap.Api

dotnet run --project src/StudentRoadMap.Api -- --migrate
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet run --project src/StudentRoadMap.Api      # http://localhost:5062, Swagger: /swagger

cd frontend && npm install && npm run dev        # http://localhost:5173
```

Ma'lumotlar bazasi ulanish satri va boshqa sirlar `appsettings.json`da **saqlanmaydi** —
`dotnet user-secrets` (dev) yoki muhit o'zgaruvchilari (prod, `.env`) orqali beriladi.

## Foydali buyruqlar

```bash
# Backend
dotnet build && dotnet test
dotnet run --project src/StudentRoadMap.Api
dotnet run --project src/StudentRoadMap.Api -- --migrate
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet ef migrations add <Nom> -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api
dotnet ef migrations has-pending-model-changes -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api

# Frontend
cd frontend
npm run dev
npm run typecheck && npm run lint && npm run test && npm run build
npm run generate:api          # swagger'dan TS tiplari (API_URL — standart http://localhost:5402)

# Infra
docker compose up -d --build     # to'liq stek
docker compose ps
docker compose logs -f <servis>
docker compose build api app     # image'larni qayta qurish (production'ga chiqarishdan oldin)

# Zaxira va tiklash (docs/13 §9)
./scripts/backup.sh                 # ./backups/ ga pg_dump
./scripts/restore-test.sh           # eng yangi zaxirani vaqtinchalik bazaga tiklab sinaydi
```

## Sifat darvozalari (CI, `.github/workflows/ci.yml`)

| Darvoza | Qanday tekshiriladi |
|---------|------------------------|
| Backend build/test | `dotnet build` (0 ogohlantirish) + `dotnet test` |
| Migratsiya sinxronligi | `dotnet ef migrations has-pending-model-changes` — bo'sh |
| Frontend | `npm run typecheck && npm run lint && npm run test && npm run build` |
| API kontrakt sinxronligi | `npm run generate:api` dan keyin `git diff` bo'sh (`schema.d.ts`) |
| Docker image | `docker compose build api app` — deploy'dan oldin buzilmasligi tekshiriladi |
| E2E (Playwright) | Faqat `main`ga push'da, konfiguratsiya tayyor bo'lgach (P30) |

Har PR va `main`ga push'da ishga tushadi. Batafsil: `docs/12-testlash-strategiyasi.md` §10.

## Ishlab chiqish tartibi

Kod `prompts/` papkasidagi ketma-ket promptlar bilan quriladi — `prompts/00-qollanma.md`dan
boshlab. Qat'iy qoidalar: [`CLAUDE.md`](CLAUDE.md). Joriy holat va qabul qilingan qarorlar:
[`PROGRESS.md`](PROGRESS.md).
