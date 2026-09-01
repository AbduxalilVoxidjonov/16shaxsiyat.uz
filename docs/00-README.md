# StudentRoadMap — Loyiha hujjatlari

O'quvchining shaxsiyatini, psixologik yetukligini, qiziqishlari va aktivligini onlayn testlar
orqali aniqlaydigan, natijalarni AI yordamida to'liq tahlil qiladigan **CRM platforma**.

- **Kod nomi:** StudentRoadMap
- **Versiya:** MVP v1.0
- **Hujjat tili:** o'zbek
- **Stek:** ASP.NET Core (Clean Architecture) + PostgreSQL + React 19 + TypeScript + Vite
- **AI:** provider-agnostik (Gemini / OpenAI / Claude — superadmin qaysi API kalitini qo'ysa, o'sha ishlaydi)

---

## 1. Bir jumlada mahsulot

Har bir maktabga **shaxsiy URL** beriladi. O'quvchi shu URL orqali kiradi, FISH / tug'ilgan sana /
telefon kabi ma'lumotlarini to'ldiradi, 4 ta psixologik testni yechadi. Tizim ballarni hisoblaydi,
AI to'liq shaxsiy profil-tahlil yozadi. **Bitta superadmin** hamma maktab, o'quvchi va natijani
boshqaradi, har bir o'quvchining individual profilini ko'radi.

---

## 2. Hujjatlarni o'qish tartibi

| № | Fayl | Nima haqida | Kim o'qiydi |
|---|------|-------------|-------------|
| 01 | [01-vision-va-qamrov.md](01-vision-va-qamrov.md) | Maqsad, rollar, MVP chegarasi, muvaffaqiyat mezonlari | Hamma |
| 02 | [02-biznes-talablar.md](02-biznes-talablar.md) | Foydalanuvchi hikoyalari, funksional/nofunksional talablar | Hamma |
| 03 | [03-psixologik-metodikalar.md](03-psixologik-metodikalar.md) | 16P, Big Five, RIASEC, Aktivlik — savol banki va scoring formulalari | Backend, metodolog |
| 04 | [04-domain-model.md](04-domain-model.md) | Entity'lar, ER diagramma, invariantlar, holat mashinasi | Backend |
| 05 | [05-database-schema.md](05-database-schema.md) | PostgreSQL DDL, indekslar, JSONB, migratsiya siyosati | Backend |
| 06 | [06-arxitektura.md](06-arxitektura.md) | Clean Architecture qatlamlari, CQRS, papka strukturasi, texnik qarorlar | Backend |
| 07 | [07-api-shartnoma.md](07-api-shartnoma.md) | Barcha endpointlar, DTO'lar, xato formati, pagination | Backend + Frontend |
| 08 | [08-auth-va-xavfsizlik.md](08-auth-va-xavfsizlik.md) | Superadmin JWT, maktab URL tokenlari, rate limit, shaxsiy ma'lumot himoyasi | Backend, DevOps |
| 09 | [09-ai-analiz-moduli.md](09-ai-analiz-moduli.md) | Provider abstraksiya, promptlar, JSON schema, narx va fallback | Backend |
| 10 | [10-frontend-arxitektura.md](10-frontend-arxitektura.md) | React SPA tuzilishi, state, routing, API qatlam | Frontend |
| 11 | [11-ux-va-ekranlar.md](11-ux-va-ekranlar.md) | Ekranlar ro'yxati, oqimlar, wireframe tavsiflari | Frontend, dizayner |
| 12 | [12-testlash-strategiyasi.md](12-testlash-strategiyasi.md) | Unit / integration / E2E, scoring oltin testlari | Hamma |
| 13 | [13-deploy-va-infratuzilma.md](13-deploy-va-infratuzilma.md) | Docker, compose, CI/CD, backup, monitoring | DevOps |
| 14 | [14-yol-xaritasi.md](14-yol-xaritasi.md) | Sprintlar, bosqichlar, Definition of Done | Hamma |
| 15 | [15-glossariy.md](15-glossariy.md) | Atamalar lug'ati (uz/en) | Hamma |
| 16 | `16-foydalanuvchi-qollanmasi.md` | Superadmin qo'llanmasi — **P32 promptida yaratiladi** | Superadmin |
| 17 | [17-16personalities-tahlili.md](17-16personalities-tahlili.md) | 16Personalities tahlili: metodika, UX, biznes modeli, huquqiy chegara va bizga xulosalar | Mahsulot egasi, PM, psixometrika |

---

## 3. Ishlab chiqish promptlari

Kod yozish Claude Code orqali, **ketma-ket promptlar** bilan bajariladi.
Promptlar `../prompts/` papkasida, `00-qollanma.md` dan boshlang.

Har bir prompt: kontekst → vazifa → cheklovlar → Definition of Done → tekshiruv buyrug'i.
**Tartibni buzmang** — har bir prompt oldingisining natijasiga tayanadi.

---

## 4. Hujjatlarni yangilash qoidasi

1. Kod bilan hujjat farq qilsa — **hujjat yangilanadi**, kod emas (agar qaror ataylab o'zgargan bo'lsa).
2. Har bir arxitektura o'zgarishi `06-arxitektura.md` dagi "Qarorlar jurnali" ga yoziladi.
3. API o'zgarsa `07-api-shartnoma.md` va frontend tiplari bir vaqtda yangilanadi.
4. Yangi test metodikasi qo'shilsa `03-psixologik-metodikalar.md` ga to'liq scoring formulasi bilan kiritiladi.
