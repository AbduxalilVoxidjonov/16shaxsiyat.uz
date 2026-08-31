# P07 — RIASEC kasb qiziqishlari banki (48 savol)

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (4-bo'lim)

## Vazifa
`SeedData/test-definitions/riasec.json` — **48 savol**, 6 tip × 8 savol:
`R`, `I`, `ART`, `SOC`, `ENT`, `CONV` (kodlar `docs/05` dagi `scale` qiymatlariga mos).

- Format: `Likert5`, savol ramkasi — **"Bu ish menga qiziq"** ("Men buni yaxshi bajaraman" emas).
- **Teskari savol yo'q** — barchasi `direction: 1`.
- Kodlar `RS-Q01` … `RS-Q48`, tiplar navbatlashib keladi.
- Har savol **aniq faoliyat** bo'lsin, mavhum emas:
  - ❌ "Men texnikani yaxshi ko'raman"
  - ✅ "Buzilgan velosipedni o'zim ta'mirlashga urinaman"

**Tiplar bo'yicha faoliyat namunalari (har biriga 8 xil):**
- `R`: ta'mirlash, qurish, texnika, dala/bog', sport, asbob bilan ishlash, mashina, elektronika
- `I`: tajriba, kuzatish, sabab izlash, matematik masala, ilmiy maqola, tahlil, dastur mantiqi
- `ART`: rasm, musiqa, yozish, dizayn, video, sahna, fotografiya, bezash
- `SOC`: tushuntirish, yordam, tinglash, o'rgatish, ko'ngilli ish, guruhni birlashtirish
- `ENT`: sotish, boshqarish, ishontirish, tashkil qilish, tadbir, tanlov, jamoa yetakchisi
- `CONV`: ro'yxat, jadval, tartib, hisob-kitob, hujjat, arxiv, aniq qoidaga rioya

## Cheklovlar
- Kasblarni gender bilan bog'lash **taqiqlanadi** ("qizlar uchun", "erkak ishi").
- Faoliyatlar O'zbekiston o'smiri hayotida uchraydigan bo'lsin.

## DoD
- [ ] 48 savol, har tipda 8 ta, hammasi `direction: 1`
- [ ] Har savol aniq faoliyatni tasvirlaydi
- [ ] Gender stereotipi yo'q (qo'lda ko'rib chiqilgan)
- [ ] Seed ishlaydi

## Tekshiruv
```bash
python3 -c "import json,collections;q=json.load(open('.../riasec.json'))['questions'];print(len(q),collections.Counter(x['scale'] for x in q))"
```
