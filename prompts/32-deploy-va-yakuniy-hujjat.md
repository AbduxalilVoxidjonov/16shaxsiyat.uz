# P32 — Docker, CI/CD va yakuniy hujjat

## O'qish shart
- `docs/13-deploy-va-infratuzilma.md` (**to'liq**)

## Vazifa
1. **Docker:** `docker/Dockerfile.api`, `docker/Dockerfile.web`, `docker/nginx.conf`,
   to'liq `docker-compose.yml` (`db`, `api`, `web`) — `docs/13` 2 va 3-bo'limlariga mos.
   - API konteyneri root emas foydalanuvchi bilan ishlaydi
   - health check'lar sozlangan
2. **`.env.example`** — barcha kerakli o'zgaruvchilar izohlar bilan (qiymatlarsiz).
3. **CI** (`.github/workflows/ci.yml`) — `docs/12` 10-bo'limidagi 7 qadam.
4. **CD** (`.github/workflows/deploy.yml`) — `docs/13` 5-bo'limidagi 6 qadam,
   health check va rollback bilan.
5. **Backup skripti** (`scripts/backup.sh`) va cron namunasi.
6. **Yakuniy hujjatlar:**
   - `README.md` — loyiha haqida, tez boshlash, arxitektura sxemasi, `docs/` ga havolalar
   - `CHANGELOG.md` — `v1.0.0` yozuvi
   - `docs/00-README.md` dagi jadval haqiqatga mos ekanini tekshirish
   - `docs/06-arxitektura.md` qarorlar jurnaliga ishlab chiqish davomida qabul qilingan
     qo'shimcha qarorlarni yozish
   - **Superadmin qo'llanmasi** (`docs/16-foydalanuvchi-qollanmasi.md`, o'zbekcha):
     maktab qo'shish, havola berish, natijalarni ko'rish, hisobotni chiqarish, AI sozlash —
     skrinshotlar uchun joy qoldirilgan holda
7. **Chiqarishdan oldingi to'liq tekshiruv:** toza muhitda (yangi klon, bo'sh DB) `docs/13`
   9-bo'limidagi buyruqlar bilan ishga tushirish va to'liq oqimni qo'lda o'tish.

## Cheklovlar
- `.env` va sirlar repo'ga tushmasin (`.gitignore` tekshirilgan).
- Production image'da dev paketlar va manba kodi bo'lmasin.
- Migratsiya avtomatik ishga tushmasin — alohida qadam.

## DoD
- [ ] `docker compose up` toza mashinada ishlaydi va to'liq oqim o'tadi
- [ ] CI barcha qadamlar bilan yashil
- [ ] Deploy workflow staging'da sinovdan o'tgan (rollback ham)
- [ ] Backup olinadi va **tiklash sinovdan o'tgan**
- [ ] Barcha hujjatlar yangilangan, `docs/00-README.md` jadvali to'g'ri
- [ ] Superadmin qo'llanmasi tayyor

## Tekshiruv
```bash
git clone <repo> /tmp/srm-clean && cd /tmp/srm-clean
cp .env.example .env   # qiymatlarni to'ldir
docker compose up -d db && docker compose run --rm api dotnet StudentRoadMap.Api.dll --migrate
docker compose up -d
curl -s localhost/health && curl -s localhost:5000/health
```
