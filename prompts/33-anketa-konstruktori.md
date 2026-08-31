# P33 — Anketa konstruktori (superadmin o'z testini yaratadi)

## Kontekst
Katalog ko'rish/tahrirlash tayyor (P29). Endi superadmin **kod yozmasdan** o'z anketasini
yaratadi: shkalalar, savollar, talqin oraliqlari, preview va nashr qilish.
Ilmiy metodikalar (16 tip, Big Five, RIASEC, Aktivlik) himoyalangan qoladi.

## O'qish shart
- `docs/02-biznes-talablar.md` (SA-18…SA-22, FR-6.5…FR-6.9, BR-8…BR-11)
- `docs/03-psixologik-metodikalar.md` (6-bo'lim — `SUM` strategiyasi va nashr validatsiyasi)
- `docs/04-domain-model.md` (2.7 — `TestDefinition`, `TestScale`, `Question.IsSystem`)
- `docs/05-database-schema.md` (`test_definitions` yangi ustunlari, `test_scales`)
- `docs/07-api-shartnoma.md` (3.4 — to'liq katalog API)
- `docs/11-ux-va-ekranlar.md` (A-8 — konstruktor maketi)

## Vazifa

### A. Domain va DB
1. `TestDefinition` ga: `Kind`, `IsSystem`, `ScoringStrategyCode`, `Status`,
   `CreatedByAdminUserId`, `PublishedAt`. Metodlar: `AddQuestion`, `RemoveQuestion`,
   `AddScale`, `RemoveScale`, `Publish`, `Archive`, `Duplicate`, `BumpVersion`.
   `IsSystem = true` da qulflangan amallar `DomainException` beradi (BR-8).
2. Yangi entity `TestScale` (+ `InterpretationBand` value object).
3. `Question.IsSystem`; `TestResult.TestVersion`.
4. Migratsiya + mavjud 4 metodikani `IsSystem = true`, `Status = Published` qilib yangilash
   (data migration yoki seeder orqali — idempotent).

### B. Scoring
5. `SumStrategy` P09 da yozilgan bo'lsa — `TestScale` bilan ulash; yozilmagan bo'lsa shu yerda yoz.
6. `TestResult` ga `TestVersion` yoziladi (BR-9).

### C. Application va API
7. `docs/07` 3.4 dagi **barcha** endpointlar: test CRUD, publish, duplicate, archive,
   toggle-active, preview; shkala CRUD; savol CRUD va reorder; import.
8. `PublishTestCommand` validatsiyasi (`docs/03` 6.3) — xatolar ro'yxati bilan
   `400 TEST_NOT_PUBLISHABLE`, har xatoda `code`, `scale`/`questionCode`, `message`.
9. Qulf tekshiruvi: `IsSystem` testda taqiqlangan amal → `409 SYSTEM_TEST_LOCKED`.
   Ishlatilgan testni o'chirish → `409 TEST_IN_USE`.
10. Nashr etilgan testga savol qo'shilsa/olib tashlansa `Version++` va `TestVersionBumpedEvent`.
11. Katalog keshini nashr/o'zgarishda invalidatsiya qilish (P11 dagi kesh).
12. Yangi sessiya yaratilganda **faqat** `Status = Published` va `IsActive = true` testlar
    qo'shiladi (`StartSessionCommand` yangilanadi). Boshlangan sessiyalar o'zgarmaydi (BR-10).
13. Audit: `Catalog.TestCreated/Updated/Published/Archived/Deleted`, `Catalog.ScaleChanged`,
    `Catalog.QuestionAdded/Removed`, `Catalog.VersionBumped`.

### D. AI
14. `PromptBuilder` ga `customTests` bloki (`docs/09` 3-bo'lim) va promptga qo'shimcha
    ko'rsatma (`docs/09` 4.2 oxiri): custom natijalar portretga qo'shiladi, lekin tip va kasb
    xulosasi ilmiy metodikalarga tayanadi.

### E. Frontend
15. Katalog ro'yxati ikki guruhga bo'linadi: "Tizim metodikalari" (qulf ikonkasi) va
    "Mening anketalarim" (holat chipi bilan) + "Yangi anketa" tugmasi.
16. **Konstruktor sahifasi** (`/admin/catalog/tests/:id/edit`) — `docs/11` A-8 maketi:
    3 bo'lim (ma'lumot / shkalalar / savollar) + o'ngda jonli preview.
17. Savol qatori: matn, shkala select, yo'nalish toggle (tushuntirish tooltip bilan),
    og'irlik (kengaytirilgan rejim), sudrab tartiblash, o'chirish.
18. Shkala drawer'i: kod, nom, tavsif, talqin oraliqlari jadvali (bo'shliq/ustma-ustlik → xato).
19. Nashr paneli: validatsiya xatolari ro'yxati, har biriga bosilganda tegishli joyga scroll.
20. Tasdiq dialoglari: versiya oshishi, arxivlash, faollikni o'chirish.
21. Umumiy vaqt 40 daqiqadan oshsa ogohlantirish banneri.

## Cheklovlar
- Tizim metodikalarining savol soni, `Scale`, `Direction`, `Weight` — **hech qanday yo'l bilan**
  o'zgartirilmasin (domain + API + UI, uchala darajada).
- Eski natijalar **qayta hisoblanmaydi** — faqat `TestVersion` bilan belgilanadi.
- `Draft` anketa hech qachon o'quvchi sessiyasiga tushmasin.
- `MaturityIndex` va `ActivityIndex` formulalariga custom testlar ta'sir qilmasin.

## DoD
- [ ] Superadmin noldan anketa yaratadi → shkala qo'shadi → 8 savol kiritadi → preview ko'radi →
      nashr qiladi → **yangi** o'quvchi sessiyasida u chiqadi → yechiladi → natija profilda ko'rinadi
- [ ] Nashr validatsiyasi barcha xato turlarini aniq ko'rsatadi (test bilan qamralgan)
- [ ] Tizim metodikasiga savol qo'shishga urinish 409 beradi (API va UI'da)
- [ ] Nashr etilgan testga savol qo'shilsa versiya oshadi, eski natija o'zgarmaydi
- [ ] Faollikni o'chirish boshlangan sessiyalarga ta'sir qilmaydi
- [ ] `SUM` oltin testlari yashil
- [ ] AI hisobotida custom anketa natijalari eslatiladi, kasb xulosasi buzilmaydi
- [ ] Audit yozuvlari to'liq

## Tekshiruv
```bash
dotnet test --filter "Catalog|Publish|SumStrategy"
cd frontend && npm run test && npm run build
# qo'lda: yangi anketa → nashr → yangi sessiya ochib yechish → profilda natijani ko'rish
```
