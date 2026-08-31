# P09 — Scoring engine va oltin testlar

## Kontekst
190 savol seed qilingan (P05–P08). Endi javoblardan natija chiqaruvchi **deterministik** yadro.
Bu loyihaning eng muhim va eng ko'p test qilinadigan qismi.

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (**to'liq** — barcha formulalar, `SUM` strategiyasi ham)
- `docs/12-testlash-strategiyasi.md` (2-bo'lim — oltin testlar)

## Vazifa
`Domain/Scoring/` ichida:

1. Shartnoma tiplari: `IScoringStrategy`, `ScoringInput`, `ScoringResult`, `QuestionMeta`,
   `StudentContext` — `docs/06` dagi imzolarga aniq mos.
2. `ScoringConstants` — barcha koeffitsiyent va chegaralar nomlangan konstanta sifatida
   (`MaturityWeightConscientiousness = 0.30` va h.k.). **Sehrli raqam yo'q.**
3. `Mbti16Strategy`:
   - o'q bo'yicha `axisRaw/axisMin/axisMax/axisPct`
   - harf tanlash, tie-break (`I/S/T/J`), `borderline` (45–55)
   - `ResultCode` = 4 harf
4. `BigFiveStrategy`: omil ballari, `factorPct`, daraja tasnifi, `StabilityPct`.
   `MaturityIndex` — `ACTIVITY` natijasi kerak bo'lgani uchun `CompositeScorer` da hisoblanadi
   (pastga qara).
5. `RiasecStrategy`: tip ballari, Holland kodi (top-3), tie-break `R→I→A→S→E→C`,
   `Differentiation`, `Consistency` (olti burchak qo'shnichiligi).
6. `ActivityStrategy`: 4 shkala, `ActivityIndex`, daraja, `NeedsAttention`.
7. `CompositeScorer` — barcha `TestResult` lar tayyor bo'lgach `MaturityIndex` ni hisoblaydi
   (Big Five + `SELF` shkalasi) va `BIG5` natijasiga yozadi.
8. `ReliabilityCalculator` — `docs/03` 7-bo'limidagi barcha jarimalar, `clamp(0,100)`, bayroq.
9. `SumStrategy` (`StrategyCode = "SUM"`) — superadmin anketalari uchun universal hisob
   (`docs/03` 6.2-bo'lim): shkala bo'yicha og'irlikli yig'indi, 0–100 normalizatsiya,
   `TestScale.InterpretationBands` dan daraja. `ResultCode` yo'q.
10. `ScoringEngine` — `TestDefinition.ScoringStrategyCode` bo'yicha strategiyani topadi
    (DI orqali `IEnumerable<IScoringStrategy>`; topilmasa aniq xato beradi).

**Qo'shimcha oltin test:** `SUM` uchun — 2 shkalali anketa, teskari savol to'g'ri aylanishi,
og'irlik 2.0 ning ta'siri, oraliqlardan to'g'ri daraja tanlanishi.

## Oltin testlar (majburiy)
`tests/StudentRoadMap.Domain.Tests/Scoring/GoldenCases/*.json` — `docs/12` 2-bo'limidagi
**barcha** holatlar. Har fayl: kirish javoblari + kutilgan natija + `tolerance`.
Test klassi barcha JSON fayllarni topib `[Theory]` bilan ishga tushiradi.

Kutilgan qiymatlar **qo'lda hisoblanadi** (kod natijasidan ko'chirilmaydi!) — aks holda test
xatoni ushlay olmaydi. Hisob-kitobni test faylida izoh sifatida yozib qo'y.

## Cheklovlar
- Strategiyalar **sof funksiya**: IO yo'q, `DateTime.Now` yo'q, DB yo'q, tashqi holat yo'q.
- Bir strategiya boshqasini chaqirmaydi (`CompositeScorer` — alohida qatlam).
- Float taqqoslash `tolerance` bilan (0.1).

## DoD
- [ ] 4 strategiya + reliability yozildi
- [ ] Oltin testlar 100% yashil, `Domain/Scoring` qoplamasi 100%
- [ ] Barcha chegaraviy holatlar (50/50, teng ball, hamma javob bir xil) qamralgan
- [ ] `ScoringVersion = 1` natijaga yoziladi

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Domain.Tests --filter "FullyQualifiedName~Scoring" \
  --collect:"XPlat Code Coverage"
```
