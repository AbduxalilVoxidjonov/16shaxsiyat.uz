# P08 — Aktivlik va motivatsiya anketasi (32 savol)

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (5-bo'lim)

## Vazifa
`SeedData/test-definitions/activity.json` — **32 savol**, 4 shkala × 8 savol:
`MOT` (ta'lim motivatsiyasi), `SELF` (o'z-o'zini boshqarish), `SOCA` (ijtimoiy faollik),
`ENG` (band bo'lish darajasi).

- Har shkalada 5 ta to'g'ri + 3 ta teskari savol.
- Kodlar `AC-Q01` … `AC-Q32`.
- Bu — loyihaning **o'z anketasi**, shuning uchun mazmunini biz belgilaymiz.

**Shkalalar qamrovi:**
- `MOT`: o'qish sababi (ichki qiziqish/tashqi bosim), maqsad aniqligi, qiyin fanga munosabat,
  yangi mavzuga qiziqish, kelajak bilan bog'lash, muvaffaqiyatga intilish
- `SELF`: vaqtni rejalash, uy vazifasini o'z vaqtida qilish, chalg'itadigan narsalarni boshqarish
  (telefon), boshlangan ishni tugatish, e'tiborni ushlab turish, xatodan xulosa chiqarish
- `SOCA`: sinf tadbirlarida ishtirok, tashabbus ko'rsatish, guruh ishida rol, yangi tanishuv,
  yordam so'rash va berish, jamoa oldida gapirish
- `ENG`: to'garak/sport, kitob o'qish, kurs, hobbi, loyiha/ijod, bo'sh vaqtni o'tkazish tarzi

## Cheklovlar
- **Ayblovchi til taqiqlanadi.** "Men dangasaman", "Men hech narsa qilmayman" — yo'q.
  O'rniga xolis xatti-harakat: "Uy vazifasini odatda oxirgi kunga qoldiraman" (`direction: -1`).
- Oilaviy sharoit, moliyaviy holat, sog'liq haqida savol **yo'q**.
- Baholar (5, 4, 3) haqida to'g'ridan-to'g'ri so'ralmaydi.

## DoD
- [ ] 32 savol, har shkalada 8 ta, direction 5/3
- [ ] Ayblovchi yoki maxfiy mavzuli savol yo'q
- [ ] Seed ishlaydi; jami 4 metodikada **190 savol** bo'ladi

## Tekshiruv
```bash
psql ... -c "select t.code, count(*) from questions q join test_definitions t on t.id=q.test_definition_id group by 1 order by 1;"
# kutilgan: ACTIVITY 32, BIG5 50, MBTI16 60, RIASEC 48  → jami 190
```
