# 13 — Deploy va infratuzilma

## 1. Muhitlar

| Muhit | Maqsad | Ma'lumot |
|-------|--------|----------|
| Local | Ishlab chiqish | Docker compose, seed data, AI mock |
| Staging | Sinov | Real AI kaliti (arzon model), anonimlashtirilgan ma'lumot |
| Production | Real foydalanish | Real ma'lumot, backup, monitoring |

---

## 2. Docker compose (local va staging)

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: studentroadmap
      POSTGRES_USER: srm
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes: [ pgdata:/var/lib/postgresql/data ]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U srm -d studentroadmap"]
      interval: 10s
      retries: 5

  api:
    build: { context: ., dockerfile: docker/Dockerfile.api }
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Postgres: Host=db;Database=studentroadmap;Username=srm;Password=${DB_PASSWORD}
      Jwt__Key: ${JWT_KEY}
      Security__EncryptionKey: ${ENCRYPTION_KEY}
      Security__IpHashSalt: ${IP_SALT}
      App__FrontendUrl: ${FRONTEND_URL}
    depends_on:
      db: { condition: service_healthy }
    ports: [ "5000:8080" ]
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s

  web:
    build:
      context: ./frontend
      dockerfile: ../docker/Dockerfile.web
      args:
        VITE_API_BASE_URL: ${API_URL}          # https://api.salohiyat.uz
        VITE_APP_NAME: Salohiyat
    depends_on: [ api ]
    ports: [ "80:80" ]

volumes: { pgdata: }
```

**Migratsiya** alohida qadamda (avtomatik emas):
```bash
docker compose run --rm api dotnet StudentRoadMap.Api.dll --migrate
```
`Program.cs` da `--migrate` argumenti bo'lsa migratsiya bajariladi va ilova to'xtaydi.

---

## 3. Dockerfile'lar

**`docker/Dockerfile.api`** — ko'p bosqichli:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.sln .
COPY src/ src/
RUN dotnet restore && dotnet publish src/StudentRoadMap.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StudentRoadMap.Api.dll"]
```

**`docker/Dockerfile.web`** — build + nginx:
```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
```

**`docker/nginx.conf`** muhim qismlari: SPA fallback (`try_files $uri /index.html`),
gzip/brotli, statik fayllarga `Cache-Control: max-age=31536000, immutable`,
`index.html` ga `no-cache`, xavfsizlik sarlavhalari (08-hujjat).

---

## 4. Production topologiya (minimal, bitta VPS)

```
Internet
   │
   ▼
Caddy / Nginx (TLS, Let's Encrypt)
   ├── salohiyat.uz        → web konteyner (SPA)
   └── api.salohiyat.uz    → api konteyner
                                     │
                                     ▼
                              PostgreSQL 16 (konteyner yoki managed)
```

Tavsiya: **4 vCPU / 8 GB RAM / 80 GB SSD** — 300 bir vaqtdagi foydalanuvchi uchun yetarli.
DB kritik bo'lgani uchun managed Postgres (avtomatik backup bilan) afzalroq.

### 4.1 DNS yozuvlari (`salohiyat.uz`)

| Turi | Nomi | Qiymati | Izoh |
|------|------|---------|------|
| A | `@` | server IP | SPA |
| A | `www` | server IP | `@` ga redirect |
| A | `api` | server IP | Backend |
| CAA | `@` | `0 issue "letsencrypt.org"` | Sertifikatni faqat LE bera oladi |
| TXT | `@` | `v=spf1 -all` | Domen nomidan spam yuborilishini to'sadi |
| TXT | `_dmarc` | `v=DMARC1; p=reject;` | Xuddi shu maqsadda |

> Pochta yuborish MVP'da yo'q. Keyin xabarnoma qo'shilsa, SPF/DKIM/DMARC qayta sozlanadi.

**TLS:** Caddy avtomatik Let's Encrypt oladi (`salohiyat.uz`, `www.salohiyat.uz`, `api.salohiyat.uz`).
Sertifikat muddati va avtomatik yangilanishi monitoringga qo'shiladi (30 kun qolganda alert).

**`.uz` domeni bo'yicha eslatma:** yillik uzaytirishni **avtomatik** rejimga qo'ying yoki
kalendarda eslatma qo'ying — domen muddati o'tsa barcha maktab havolalari bir vaqtda ishlamay qoladi.

---

## 5. CI/CD (GitHub Actions)

**`.github/workflows/ci.yml`** — har PR'da: build, test, lint, typecheck, migratsiya tekshiruvi
(12-hujjat, 10-bo'lim).

**`.github/workflows/deploy.yml`** — `main` ga merge bo'lganda:
```
1. Docker image build (api, web) → registry, tag: git sha + latest
2. SSH orqali serverga: docker compose pull
3. Migratsiya: docker compose run --rm api --migrate
4. docker compose up -d --no-deps api web
5. Health check: /health 200 kutiladi (60 s), aks holda oldingi tag'ga rollback
6. Telegram/email xabar: deploy natijasi
```

Sirlar GitHub Secrets'da: `DB_PASSWORD`, `JWT_KEY`, `ENCRYPTION_KEY`, `IP_SALT`,
`SSH_KEY`, `REGISTRY_TOKEN`. AI kalitlari **serverda emas, DB'da** (admin panel orqali kiritiladi).

---

## 6. Konfiguratsiya va sirlar

| O'zgaruvchi | Izoh |
|-------------|------|
| `ConnectionStrings__Postgres` | DB ulanishi |
| `Jwt__Key` | ≥ 32 bayt random |
| `Security__EncryptionKey` | 32 bayt base64 — AI kalitlarini shifrlash |
| `Security__IpHashSalt` | IP xeshlash uchun |
| `App__FrontendUrl` | CORS va havola generatsiyasi |
| `App__SeedOnStartup` | Faqat birinchi ishga tushirishda `true` |
| `Ai__TimeoutSeconds`, `Ai__MaxRetries` | AI siyosati |

Kalit almashtirish (`EncryptionKey` rotatsiyasi): admin panel orqali AI kalitlarini qayta kiritish
eng sodda yo'l; avtomatik re-encrypt skripti v2.

---

## 7. Backup va tiklanish

| Nima | Qanday | Muddat |
|------|--------|--------|
| DB to'liq | `pg_dump -Fc` har kuni 03:00 (UTC+5) | 30 kun |
| DB haftalik | Yakshanba, alohida saqlash (offsite) | 6 oy |
| Fayllar | MVP'da fayl saqlash yo'q (PDF on-the-fly) | — |

```bash
# backup.sh (cron)
pg_dump -Fc -h db -U srm studentroadmap > /backups/srm_$(date +%F).dump
find /backups -name 'srm_*.dump' -mtime +30 -delete
```

**Tiklanish sinovi:** har chorakda backupdan staging'ga tiklash va oqim tekshiruvi.
Tiklanmagan backup — backup emas.

RPO: 24 soat · RTO: 2 soat.

---

## 8. Monitoring va loglar

- **Serilog** → konsol (JSON) + fayl (kunlik rotatsiya, 14 kun). Ixtiyoriy Seq/Loki.
- **Health:** `/health` (jonli), `/health/ready` (DB ulanishi + migratsiya holati).
- **Metrikalar (v2):** OpenTelemetry → Prometheus: so'rovlar, xatolar, AI davomiyligi va narxi.
- **Alertlar (minimal):**
  - `/health` 3 marta ketma-ket yiqilsa → Telegram;
  - `AnalysisFailed` soni kuniga 10 dan oshsa → email;
  - disk > 85% → alert;
  - backup skripti xato bersa → alert.
- **Xato tracking:** Sentry (backend + frontend), shaxsiy ma'lumot filtrlangan holda.

---

## 9. Ishga tushirish tartibi (birinchi marta)

```bash
git clone <repo> && cd StudentRoadMap
cp .env.example .env            # sirlarni to'ldiring
docker compose up -d db
docker compose run --rm api dotnet StudentRoadMap.Api.dll --migrate
App__SeedOnStartup=true docker compose up -d api   # test banki + superadmin
docker compose up -d web
# superadmin: admin / <.env dagi boshlang'ich parol> — birinchi kirishda o'zgartiriladi
```

Lokal ishlab chiqish (Docker'siz):
```bash
docker compose up -d db
dotnet run --project src/StudentRoadMap.Api      # http://localhost:5000
cd frontend && npm install && npm run dev        # http://localhost:5173
```

---

## 10. Reliz siyosati

- Semantik versiya: `v1.0.0`, tag → deploy.
- `CHANGELOG.md` har relizda yangilanadi (o'zbekcha, foydalanuvchi tilida).
- DB migratsiyasi bo'lgan reliz — **oldindan backup**, keyin deploy.
- Rollback: oldingi image tag + (agar migratsiya destruktiv bo'lsa) backupdan tiklash.
  Shuning uchun destruktiv migratsiyalar ikki bosqichda (05-hujjat, 4-bo'lim).
