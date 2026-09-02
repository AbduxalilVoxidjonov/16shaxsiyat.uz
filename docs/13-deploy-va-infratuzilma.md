# 13 — Deploy va infratuzilma

> Bu hujjat **hozirgi ishlab turgan compose tuzilishiga** mos (P32 yakuniy tekshiruvi,
> 2026-09-02). Haqiqat manbai — repo ildizidagi `docker-compose.yml`, `docker/*` va
> `.env.example`; bu yerda faqat ular tushuntiriladi. Ular bilan farq chiqsa — fayllar
> to'g'ri, shu hujjat yangilanadi (`docs/00-README.md` §4-qoida).

## 1. Muhitlar

| Muhit | Maqsad | Ma'lumot |
|-------|--------|----------|
| Local (Docker'siz) | Kundalik ishlab chiqish | `dotnet run` + `npm run dev`, `db` faqat Docker'da |
| Local (Docker compose) | To'liq stekni bir joyda sinash | Compose barcha servislarni ko'taradi, `MockAiProvider` |
| Production | Real foydalanish | `16shaxsiyat.uz`, Cloudflare Tunnel, real ma'lumot, backup |

Hozircha alohida "staging" muhiti yo'q — repo `origin`ga ulanmagan (egasining qarori,
`PROGRESS.md`), shuning uchun 5-bo'limdagi CD (avtomatik deploy) ham hali ishga tushirilmagan.
Production serveri **qo'lda**, shu repo nusxasidan `docker compose up -d --build` bilan
boshqariladi.

---

## 2. Docker compose tuzilishi

To'liq va aniq mazmun — `docker-compose.yml`. Bu yerda faqat **nima uchun shunday** ekani
tushuntiriladi.

```
docker compose up -d --build
        │
        ▼
   db (postgres:16-alpine, healthcheck: pg_isready)
        │  depends_on: condition: service_healthy
        ▼
   migrate (bir martalik: `dotnet StudentRoadMap.Api.dll --migrate`, restart: "no")
        │  depends_on: condition: service_completed_successfully
        ▼
   seed (bir martalik: `--seed`, idempotent — 190 savol, 16 tip, kasb xaritasi, superadmin)
        │  depends_on: condition: service_completed_successfully
        ▼
   api (doimiy, restart: unless-stopped)
        │  depends_on
        ▼
   app (nginx: qurilgan frontend + `/api` teskari proksi — bitta origin)
        │  depends_on
        ▼
   tunnel (cloudflared — `app:8080`ni `16shaxsiyat.uz`ga ulaydi, TOKEN orqali)
```

**Bitta buyruq — butun oqim.** `docker compose up -d --build` shu zanjirni ketma-ket
bajaradi va to'xtaydi; qadamlardan biri muvaffaqiyatsiz bo'lsa, keyingisi ishga tushmaydi
(`condition: service_completed_successfully`).

Muhim tafsilotlar:

- **`migrate` va `seed` — alohida, bir martalik konteynerlar**, `api`ning o'zi ichida
  avtomatik `Database.Migrate()` chaqirmaydi (`docs/05` §4 band 5: production'da migratsiya
  alohida qadam bo'lishi shart — avtomatik ishga tushsa, noto'g'ri versiyaga tasodifan
  migratsiya qilib qo'yish xavfi bor). `restart: "no"` — muvaffaqiyatli tugagach o'chib
  qoladi, qayta ishga tushmaydi.
- **`app` xost'ga port CHIQARMAYDI.** Ilova faqat Cloudflare Tunnel orqali, o'z domenida
  ochiladi — mashinada tashqi tarmoqdan kirsa bo'ladigan hech qanday port yo'q (`api` ham
  xost portiga chiqmagan; faqat ichki compose tarmog'ida `app` unga murojaat qiladi).
  Lokal tekshiruv kerak bo'lsa: `docker compose port app 8080` yoki `docker-compose.yml`
  dagi izohlangan `ports:` qatorini vaqtincha oching.
- **`app` — "same-origin" arxitektura**: frontend `/api/...` ga NISBIY yo'l bilan so'rov
  yuboradi (alohida `api.16shaxsiyat.uz` subdomeni YO'Q — dastlabki reja shu edi, lekin
  amalda soddalashtirildi: bitta domen, bitta sertifikat, CORS umuman kerak emas).
  `web-nginx.conf` `/api/`, `/swagger`, `/health` yo'llarini `api:8080`ga proksilaydi.
- **`db` porti xostga chiqarilgan** (`5432:5432`) — admin vositalari (`psql`, backup
  skripti) bilan to'g'ridan-to'g'ri ulanish uchun qulay, lekin production serverida bu
  portni **firewall bilan tashqi dunyodan yopish shart** (faqat localhost/VPN'dan kirish).
  Aks holda Postgres internetga ochiq qoladi. Tekshirish: `sudo ufw status` yoki
  bulut provayderining xavfsizlik guruhi qoidalarida `5432` faqat ishonchli manzillarga
  ochilganini tasdiqlang.
- **Tarmoq subneti QAT'IY belgilangan** — 5-bo'lim, MAXSUS DIQQAT.

---

## 3. Dockerfile'lar

To'liq mazmun — `docker/Dockerfile.api`, `docker/Dockerfile.web`, `docker/web-nginx.conf`.
Qisqacha:

**`docker/Dockerfile.api`** — ko'p bosqichli (`sdk:10.0` → `aspnet:10.0`):
- Avval faqat `.csproj` fayllar nusxalanadi va `dotnet restore` qilinadi — NuGet keshi
  manba kodi o'zgarganda ham saqlanadi (qatlam keshlash).
- Ishga tushirish bosqichida **root emas** — rasmiy image'dagi tayyor `app` foydalanuvchisi
  bilan ishlaydi (`USER app`).
- `ENTRYPOINT ["dotnet", "StudentRoadMap.Api.dll"]` — `docker-compose.yml`dagi `command:`
  (`["--migrate"]` / `["--seed"]`) shu ENTRYPOINT'ga argument sifatida qo'shiladi.

**`docker/Dockerfile.web`** — `node:22-alpine` (build) → `nginx:alpine` (ishga tushirish):
- `VITE_API_BASE_URL` ATAYLAB berilmaydi — build "same-origin" rejimida bo'ladi
  (`frontend/src/shared/config/env.ts`: bo'sh qiymat → `/api/...` nisbiy yo'l).
- **MUHIM (`.dockerignore`, repo ildizida):** `COPY frontend/ ./` `npm ci`dan KEYIN keladi.
  Agar dasturchi mashinasida `frontend/node_modules`/`dist` mavjud bo'lsa va `.dockerignore`
  ularni chetlab o'tmasa, bu buyruq konteyner ichida to'g'ri o'rnatilgan (Linux) modullarni
  host'nikilar (masalan, macOS'da `@rollup/rollup-darwin-arm64`) bilan USTIDAN YOZIB, image
  qurilishini buzadi — xato **faqat** shunday mashinada chiqadi, toza CI muhitida sezilmaydi
  va keyin "birdan" production build'da paydo bo'ladi. P32 tekshiruvida aynan shu holat
  qo'lda takrorlanib topildi va `.dockerignore` bilan yopildi.

**`docker/web-nginx.conf`** (8080-portda tinglaydi — `EXPOSE 8080`, host portiga emas,
compose ichidagi `app:8080`ga bog'lanadi):
- `/api/` → `api:8080`ga proksi (Host/X-Forwarded-* header'lari bilan).
- `/swagger`, `/health` → shuningdek `api:8080`ga proksi (diagnostika uchun).
- `/assets/` — 1 yil kesh (`immutable`, fayl nomida xesh bor); `index.html` — `no-cache`.
- SPA fallback: noma'lum yo'llar `index.html`ga (`try_files`).

`docker/app-proxy.conf` — compose'ga ULANMAGAN, ixtiyoriy yordamchi: lokal Docker
tarmog'idan `host.docker.internal:5173`(Vite dev server)ga proksi qilish kerak bo'lganda
qo'lda ishlatiladigan namuna (masalan, boshqa konteynerdan frontend dev serverini sinash).
Kundalik oqimda kerak emas.

---

## 4. Production topologiya (Cloudflare Tunnel — haqiqiy holat)

Dastlabki reja (Caddy/Nginx + Let's Encrypt + alohida `api.` subdomeni) **amalda
soddalashtirildi**. Hozirgi haqiqiy topologiya:

```
Internet
   │
   ▼
Cloudflare edge (TLS, DDoS himoyasi — Let's Encrypt/Caddy KERAK EMAS)
   │  Cloudflare Tunnel (chiquvchi ulanish, kiruvchi port ochilmaydi)
   ▼
tunnel konteyneri (cloudflared) ── http://app:8080
                                        │
                                        ▼
                                  app (nginx, SPA + /api proksi)
                                        │
                                        ▼
                                  api (ASP.NET Core, 8080)
                                        │
                                        ▼
                                  db (PostgreSQL 16)
```

Farqlar sababi va oqibati:

- **TLS/sertifikat**: Cloudflare boshqaradi — serverda sertifikat yangilash vazifasi yo'q,
  lekin **`TUNNEL_TOKEN` muddati/holatini** Cloudflare Zero Trust panelida kuzatish kerak
  (token bekor qilinsa, `tunnel` konteyneri jimgina ulanolmay qoladi — loglarga qarash
  shart: `docker compose logs tunnel`).
- **DNS**: A/CAA/SPF/DMARC yozuvlari MVP uchun shart emas — domen Cloudflare'da bo'lsa,
  Tunnel ulanishi CNAME orqali avtomatik sozlanadi (Zero Trust > Tunnels > Public Hostname).
- **Kiruvchi port yo'q**: serverning firewall'ida hech qanday portni ochish shart emas
  (80/443 ham) — bu klassik reverse-proksi sxemasidan ko'ra kichikroq hujum yuzasi.
  Faqat `db`ning `5432` porti host'ga chiqqan (§2) — buni administrativ maqsadda ochiq
  qoldirish yoki firewall bilan cheklash tanlovi qoladi.
- **`.uz` domeni**: yillik uzaytirish eslatmasi hali kuchda — muddat o'tsa Tunnel'ning o'zi
  ishlaydi, lekin domen hech kimga ko'rsatmaydi.

---

## 5. CI/CD (GitHub Actions)

### 5.1 CI — `.github/workflows/ci.yml` (tayyor, har PR va `main`ga push'da)

| Job | Nima tekshiradi |
|-----|------------------|
| `backend` | `dotnet build` (0 ogohlantirish — `TreatWarningsAsErrors`), `dotnet test`, `dotnet ef migrations has-pending-model-changes` |
| `frontend` | `npm run typecheck`, `npm run lint`, `npm run test`, `npm run build` |
| `api-contract-sync` | API'ni DB'siz ko'taradi → `npm run generate:api` → `git diff` **bo'sh** bo'lishi shart (`schema.d.ts` kontraktdan chetlashmagan) |
| `docker-build` | `docker compose build api app` — production image'lari qurilishi (deploy'dan oldin buzilishning oldi olinadi) |
| `e2e` | Playwright — `frontend/playwright.config.*` mavjud bo'lgandagina ishga tushadi (P30 tugagach); hozircha avtomatik o'tkazib yuboriladi |

**Muhim texnik topilma (P32):** `dotnet ef migrations has-pending-model-changes` va
`npm run generate:api` uchun Swagger o'qish — ikkalasi ham **haqiqiy Postgres talab
qilmaydi**. Sababi tekshirildi va tasdiqlandi:
- EF modelni oxirgi migratsiya snapshotiga kod darajasida solishtiradi, DB ulanishini
  ochmaydi — soxta (mavjud bo'lmagan) `Host=` bilan ham to'g'ri natija beradi.
- `Program.cs` faqat `--migrate` / `--seed` argumenti yoki `App:SeedOnStartup=true`
  bo'lgandagina DB'ga murojaat qiladi; oddiy ishga tushishda (Swagger o'qish uchun kerak
  bo'lgani) DB'ga umuman tegmaydi.

Shu sabab CI'da bu ikki qadam uchun **Postgres xizmat konteyneri kerak emas** — faqat
`Jwt__Key`/`Security__EncryptionKey` uchun 32 baytlik DUMMY qiymat yetarli (haqiqiy sir
emas, faqat ilova ishga tushishi uchun fail-fast tekshiruvni qondiradi).

`dotnet test` ham Postgres talab qilmaydi — integratsiya testlari `WebApplicationFactory`
orqali o'z `Jwt`/`Encryption` qiymatlarini xotirada beradi, EF esa SQLite bilan ishlaydi
(bilinign cheklov: `EnsureCreated()` ishlatgani uchun haqiqiy migratsiya yo'li testlarda
sinalmaydi — `PROGRESS.md`dagi ochiq risk, P30 Testcontainers bilan yopiladi).

**Ochiq qolgan narsa:** `docs/12` §1 dagi granulyar qoplama chegarasi (Domain/Scoring
100%, Application ≥70%, umumiy ≥60%) hozircha CI'da faqat **hisobot** sifatida yig'iladi
(`--collect:"XPlat Code Coverage"`, artifact'ga yuklanadi), lekin qattiq gate emas — buning
uchun `tests/**` ichidagi test loyihalariga `coverlet` threshold sozlamasi (yoki alohida
`.runsettings`) qo'shish kerak. Bu P32 doirasida **fayl egaligi** sababli qilinmadi
(`tests/**` — backend agentining zonasi); keyingi qadam sifatida PM'ga qoldirilgan.

### 5.2 CD — hali yo'q

`docs/13` avvalgi versiyasida rejalashtirilgan `.github/workflows/deploy.yml` (SSH orqali
serverga push-to-deploy) **hali yaratilmagan** — sabab: repo hozircha GitHub `origin`ga
ulanmagan (egasining ataylab qilingan qarori, `PROGRESS.md` oxirgi qatori). Push
qilinmaguncha GitHub Actions umuman ishga tushmaydi — deploy workflow yozib qo'yish
sinovdan o'tkazib bo'lmaydigan kod bo'lardi.

Hozircha production **qo'lda** yangilanadi, to'g'ridan-to'g'ri serverda:
```bash
cd /opt/16shaxsiyat            # yoki repo qayerda joylashgan bo'lsa
git pull
docker compose build api app
docker compose run --rm migrate     # yangi migratsiya bo'lsa
docker compose up -d --no-deps api app
curl -sf http://localhost/health || echo "OLDINGI IMAGE'GA QAYTARILSIN"
```

Egasi `origin`ni ulashga qaror qilganda, ushbu qo'lda qadamlar `.github/workflows/deploy.yml`
ga ko'chiriladi (SSH + health check + rollback, sirlar GitHub Secrets'da:
`DB_PASSWORD`, `Jwt__Key`, `Security__EncryptionKey`, `Security__IpHashSalt`, `TUNNEL_TOKEN`,
`SSH_KEY`, `REGISTRY_TOKEN`). AI provider kalitlari **serverda emas, DB'da** (admin panel
orqali shifrlangan holda kiritiladi) — bu allaqachon amalga oshirilgan.

---

## 6. Konfiguratsiya va sirlar

To'liq va izohli ro'yxat — `.env.example` (repo ildizida). Qisqacha jadval:

| O'zgaruvchi | Qaysi servis | Izoh |
|-------------|---------------|------|
| `DB_PASSWORD` | `db`, `api`/`migrate`/`seed` | Postgres paroli |
| `Jwt__Key` | `api`/`migrate`/`seed` | ≥ 32 bayt tasodifiy (`openssl rand -base64 48`) |
| `Security__EncryptionKey` | `api`/`migrate`/`seed` | Aynan 32 bayt, base64 (`openssl rand -base64 32`) — AI kalitlarini shifrlaydi |
| `Security__IpHashSalt` | `api` | IP xeshlash tuzi (audit/rate-limit) |
| `App__KnownProxies` | `api` | Ishonchli proksi subneti — §7 MAXSUS DIQQAT |
| `App__FrontendUrl` | `api` | CORS va havola generatsiyasi |
| `App__SeedOnStartup` | `api` | Production'da `false` — seed alohida `seed` konteyneri bilan |
| `ADMIN_USERNAME` / `ADMIN_PASSWORD` / `ADMIN_EMAIL` | `seed` | Yagona superadmin — bo'lmasa seed uni yaratmaydi |
| `FRONTEND_URL` | (hujjat/eslatma) | `docs`/skriptlarda ishlatiladigan haqiqiy domen |
| `API_URL` | (hujjat/eslatma) | `frontend/package.json` dagi `generate:api` standart manzili |
| `TUNNEL_TOKEN` | `tunnel` | Cloudflare Zero Trust > Tunnels > connector token |
| `WEB_PORT` | (ixtiyoriy, lokal) | `app` xizmatini vaqtincha xostga chiqarish uchun |

Kalit almashtirish (`Security__EncryptionKey` rotatsiyasi): admin panel orqali AI
kalitlarini qayta kiritish eng sodda yo'l; avtomatik re-encrypt skripti v2.

---

## 7. MAXSUS DIQQAT — `App__KnownProxies` va tarmoq subneti (P32 topilmasi)

`App__KnownProxies` (`.env`) `ForwardedHeadersSetup`ga qaysi manzillardan kelgan
`X-Forwarded-For`/`X-Forwarded-Proto` header'iga ishonish kerakligini aytadi — bu manzil
compose tarmog'idagi `app` (nginx) konteynerining IP'si (aniqrog'i, shu konteyner
joylashgan subnet).

**Xavf:** agar Docker tarmog'i (`16shaxsiyat_default`) biror sababdan qayta yaratilsa
(masalan, `docker compose down` + `up`, yoki compose loyihasi qayta nomlansa), Docker
avtomatik ravishda **boshqa** subnet tanlashi mumkin edi — bu holda `.env`dagi qattiq
yozilgan `App__KnownProxies` eskirib qoladi va `ForwardedHeadersSetup` header'ga ishonishni
**to'xtatadi, lekin xato bermaydi** (jim otkazib yuboradi). Natija: barcha so'rovlar bitta
"manzil" (proksi konteynerining o'zi) ostida ko'rinadi — rate limiting amalda ishlamay
qoladi (barcha o'quvchi bitta limitni bo'lishadi) va IP audit foydasiz bo'lib qoladi.

**Yechim — subnet `docker-compose.yml`da QAT'IY belgilangan** (P32'da qo'shildi):

```yaml
networks:
  default:
    ipam:
      config:
        - subnet: 172.26.0.0/16
```

Bu qiymat joriy production tarmog'i bilan bir xil (`docker network inspect
16shaxsiyat_default` bilan tasdiqlangan) — shuning uchun uni qo'llash konteynerlarni
qayta yaratishga majbur qilmaydi. Bundan buyon tarmoq qayta yaratilsa ham subnet
o'zgarmaydi, `.env`dagi `App__KnownProxies=172.26.0.0/16` doim to'g'ri qoladi.

**Tekshirish buyrug'i** (deploy'dan keyin har safar ishlatilsin — ikkalasi bir xil chiqishi
shart):
```bash
docker network inspect 16shaxsiyat_default --format '{{range .IPAM.Config}}{{.Subnet}}{{end}}'
grep -A2 "subnet:" docker-compose.yml
```

Agar kimdir kelajakda `docker-compose.yml`dagi subnetni o'zgartirsa — `.env.example` VA
serverdagi haqiqiy `.env` shu qatorni birga yangilashi SHART, aks holda yuqoridagi jim
buzilish qaytadan yuz beradi.

---

## 8. MAXSUS DIQQAT — migratsiya va ma'lumot backfill'i (P34 QA topilmasi)

`docker-compose.yml`dagi `migrate` konteyneri `dotnet StudentRoadMap.Api.dll --migrate`ni
ishga tushiradi — bu **bitta chaqiruvda BARCHA kutilayotgan migratsiyalarni** ketma-ket
bajaradi (`Database.MigrateAsync()`), keyin `seed` alohida, FAQAT migratsiya muvaffaqiyatli
tugagach ishga tushadi (`condition: service_completed_successfully`).

**Bundan kelib chiqadigan qat'iy qoida:** agar yangi migratsiya mavjud jadvalga `NOT NULL`
ustun yoki yangi `FOREIGN KEY` qo'shsa va mavjud qatorlarda qiymat to'ldirilishi kerak
bo'lsa (backfill) — bu to'ldirish **migratsiyaning o'zi ichida** (`Up()` metodida, xom SQL
yoki `migrationBuilder.Sql(...)` bilan) bajarilishi SHART. Uni `seed`ga (yoki ilovaning
boshqa keyingi bosqichiga) qoldirib bo'lmaydi.

**Sabab — real voqea (P34):** dastur (`Program`) modeli qo'shilganda `assessments`
jadvaliga `program_id` NOT NULL ustun kerak bo'ldi. Backfill dastlab `seed` bosqichiga
rejalashtirilgan edi, lekin bu amalda ikki muammoga olib keldi: (1) migratsiya `SET NOT
NULL` qo'llagan payt mavjud qatorlar hali `program_id = NULL` bo'lib, cheklov buzilib
**deploy to'xtardi**; (2) integratsiya testlari `EnsureCreated()` ishlatgani uchun bu
ketma-ketlik muammosini umuman ushlay olmasdi. Tuzatildi: tizim dasturi **deterministik
GUID bilan migratsiya ichida** yaratiladi, backfill `SET NOT NULL`dan OLDIN, o'sha
migratsiya ichida bajariladi. Haqiqiy Postgres'da (`program_id IS NULL` qatorlar bilan)
qo'lda sinaldi.

**Xulosa deploy nuqtai nazaridan:** yangi migratsiya PR'ida backend agent (yoki QA)
tekshirishi kerak bo'lgan savol — "agar bu migratsiya BO'SH BO'LMAGAN production bazasida
ishga tushsa, oraliq holatda cheklov buzilmaydimi?" Javob "ha, buzilishi mumkin" bo'lsa,
backfill o'sha migratsiya ichiga ko'chiriladi, `seed`ga emas.

---

## 9. Backup va tiklanish

| Nima | Qanday | Muddat |
|------|--------|--------|
| DB to'liq | `./scripts/backup.sh` (cron, har kuni 03:00 UTC+5) | 30 kun (skript avtomatik tozalaydi) |
| DB haftalik | Yakshanba, alohida saqlash (offsite — masalan S3/Backblaze'ga nusxa) | 6 oy |
| Fayllar | MVP'da fayl saqlash yo'q (PDF on-the-fly generatsiya qilinadi) | — |

### 9.1 Zaxira olish

```bash
./scripts/backup.sh                       # ./backups/srm_<sana>_<vaqt>.dump
BACKUP_DIR=/mnt/backups ./scripts/backup.sh   # boshqa joyga
```

`db` konteyneri ichidagi `pg_dump -Fc` (maxsus format) orqali ishlaydi — host mashinada
Postgres mijoz vositalari shart emas. `backups/` katalogi `.gitignore`ga qo'shilgan
(shaxsiy ma'lumot — o'quvchi FISH, tug'ilgan sana — hech qachon repo'ga tushmasin).

Cron namunasi:
```cron
0 3 * * * cd /opt/16shaxsiyat && ./scripts/backup.sh >> /var/log/srm-backup.log 2>&1
```

### 9.2 Tiklanish — SINOVDAN O'TGAN (P32, 2026-09-02)

`./scripts/restore-test.sh` — production bazasiga **tegmasdan**, vaqtinchalik
`srm_restore_test` bazasiga tiklaydi, jadval/migratsiya sonini tekshiradi, so'ng
vaqtinchalik bazani o'chiradi:

```bash
./scripts/restore-test.sh                              # eng yangi zaxirani sinaydi
./scripts/restore-test.sh backups/srm_2026-09-02.dump   # aniq faylni sinaydi
```

**P32'da bajarilgan haqiqiy sinov natijasi:** joriy production bazasidan zaxira olindi
(`./scripts/backup.sh`), so'ng `./scripts/restore-test.sh` bilan tiklandi va tekshirildi —
**24 jadval, 6 qo'llangan migratsiya** to'g'ri tiklandi (`PASS`), production baza
(`studentroadmap`) sinov davomida o'zgarishsiz qoldi (`schools`/`students` qatorlari soni
oldin va keyin bir xil).

Haqiqiy falokatdan keyingi tiklash (bu skript buni AVTOMATIK QILMAYDI — qo'lda,
diqqat bilan):
```bash
docker compose up -d db
docker compose exec -T db dropdb -U srm studentroadmap      # ESKI ma'lumot shu yerda o'chadi
docker compose exec -T db createdb -U srm studentroadmap
docker compose exec -T db pg_restore -U srm -d studentroadmap --no-owner < backups/srm_XXXX.dump
docker compose up -d api      # --migrate SHART EMAS: pg_restore sxema+ma'lumotni birga tiklaydi
```

**Tiklanish sinovi jadvali:** har chorakda `./scripts/restore-test.sh` qayta ishga
tushirilsin (eng so'nggi zaxira bilan). Tiklanmagan backup — backup emas.

RPO: 24 soat · RTO: 2 soat.

---

## 10. Monitoring va loglar

- **Serilog** → konsol (JSON) + fayl (kunlik rotatsiya, 14 kun). Ixtiyoriy Seq/Loki.
- **Health:** `/health` — jonlik, HECH QANDAY tekshiruv bajarmaydi (`Predicate = _ =>
  false`), DB holatidan qat'i nazar doim `200` qaytaradi (konteyner ishga tushganini
  bildiradi). `/health/ready` — FAQAT `ready` tegli tekshiruv, hozircha bitta: DB ulanishi
  (`AddDbContextCheck<AppDbContext>`). **Aniqlik uchun:** bu migratsiya holatini
  TEKSHIRMAYDI — faqat DB'ga ulanish mumkinligini. Web'da: `web-nginx.conf` orqali
  `location = /health` `api:8080/health`ga proksilanadi (nginx darajasida esa alohida
  health check yo'q — `docker-compose.yml`da hozircha faqat `db`/`api` uchun bor).
- **Metrikalar (v2):** OpenTelemetry → Prometheus: so'rovlar, xatolar, AI davomiyligi va narxi.
- **Alertlar (minimal, hali qo'lda kuzatiladi — avtomatlashtirish v2):**
  - `/health` 3 marta ketma-ket yiqilsa → Telegram;
  - `AnalysisFailed` soni kuniga 10 dan oshsa → email;
  - disk > 85% → alert;
  - `./scripts/backup.sh` xato bersa (cron log'ida `set -e` tufayli nolinchi bo'lmagan
    chiqish kodi) → alert.
- **Xato tracking:** Sentry (backend + frontend), shaxsiy ma'lumot filtrlangan holda — v2.

---

## 11. Ishga tushirish tartibi

### 11.1 Docker compose bilan (tavsiya etiladi — bitta buyruq)

```bash
git clone <repo> && cd StudentRoadMap
cp .env.example .env            # BARCHA CHANGE_ME qiymatlarni to'ldiring
docker compose up -d --build    # db → migrate → seed → api → app → tunnel — ketma-ket
docker compose logs -f migrate seed   # ikkalasi ham "0" kod bilan tugashini kuzating
curl -sf http://localhost/health   # agar `app` porti vaqtincha ochilgan bo'lsa
# tunnel ishlayotgan bo'lsa: https://<sizning domeningiz>/health
```

Superadmin: `.env`dagi `ADMIN_USERNAME`/`ADMIN_PASSWORD` — birinchi kirishda parolni
o'zgartirish tavsiya etiladi (`docs/16-foydalanuvchi-qollanmasi.md`).

Tekshirish (subnet nomuvofiqligi bo'lmasligi uchun, §7):
```bash
docker network inspect 16shaxsiyat_default --format '{{range .IPAM.Config}}{{.Subnet}}{{end}}'
```

### 11.2 Lokal ishlab chiqish (Docker'siz backend/frontend, faqat `db` Docker'da)

```bash
docker compose up -d db
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=studentroadmap;Username=srm;Password=<.env dagi DB_PASSWORD>" \
  -p src/StudentRoadMap.Api
dotnet run --project src/StudentRoadMap.Api -- --migrate
dotnet run --project src/StudentRoadMap.Api -- --seed
dotnet run --project src/StudentRoadMap.Api      # http://localhost:5062 (launchSettings.json)

cd frontend && npm install && npm run dev        # http://localhost:5173
```

---

## 12. Reliz siyosati

- Semantik versiya: `v1.0.0`, tag → deploy (CD ulangach avtomatik, hozircha qo'lda — §5.2).
- `CHANGELOG.md` har relizda yangilanadi (o'zbekcha, foydalanuvchi tilida).
- DB migratsiyasi bo'lgan reliz — **oldindan backup** (`./scripts/backup.sh`), keyin deploy.
- Rollback: oldingi image tag + (agar migratsiya destruktiv bo'lsa) backupdan tiklash
  (§9.2). Shuning uchun destruktiv migratsiyalar ikki bosqichda (`docs/05` §4).
