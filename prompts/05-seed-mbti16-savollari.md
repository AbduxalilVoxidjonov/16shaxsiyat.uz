# P05 — 16 tipli model savol banki (60 savol)

## Kontekst
Seed infratuzilmasi tayyor (P04). Endi birinchi metodikaning savollarini yozamiz.

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (0 va 2-bo'limlar)

## Vazifa
`SeedData/test-definitions/mbti16.json` ni to'liq to'ldir:

- **60 savol**, 4 o'q × 15 savol: `EI`, `SN`, `TF`, `JP`.
- Har o'qda **teskari savollar muvozanati**: 8 ta `direction: 1`, 7 ta `direction: -1`
  (yoki teskarisi) — bir tomonga og'ish bo'lmasin.
- Savol tartibi **aralash** bo'lsin: o'qlar navbatlashib kelsin (EI, SN, TF, JP, EI, ...),
  ketma-ket bir o'qdan 2 tadan ortiq savol kelmasin.
- Kodlar: `MB-Q01` … `MB-Q60`.

**Savol matni talablari:**
- O'zbek tilida, **12–18 yoshli o'quvchi** tushunadigan sodda til.
- Bitta jumla, 12 so'zdan oshmasin.
- **Maktab va o'smir hayotidan** misollar (dars, sinfdosh, to'garak, uy vazifasi, do'stlar).
- "Men" shaklida, birinchi shaxsda.
- Ikki ma'noli, salbiy yorliqli yoki ijtimoiy bosim beradigan savollar bo'lmasin
  ("Men dangasaman" — noto'g'ri; "Ishni oxirgi kunga qoldirib qo'yaman" — to'g'ri).
- Bitta savolda faqat bitta g'oya ("va" bilan ikki narsa so'ralmasin).
- Inkor gaplardan qoch ("Men ... qilmayman" o'rniga ijobiy shakl + `direction: -1`).

**Har o'q uchun qamrov (kamida shu mavzular):**
- `EI`: yangi tanishuv, ko'p odamli joy, yolg'iz tiklanish, guruh ishi, jimlik, e'tibor markazi
- `SN`: tafsilot/umumiy manzara, hozir/kelajak, amaliy/g'oyaviy, aniq ko'rsatma/erkin izlanish
- `TF`: mantiq/his, adolat/hamdardlik, tanqid berish, qaror mezoni, nizoda yondashuv
- `JP`: rejalashtirish, muddat, tartib, o'zgarishga munosabat, ish boshlash vaqti

## Cheklovlar
- "MBTI" so'zi hech qayerda ishlatilmasin (`nameUz`: "16 tipli shaxsiyat modeli").
- Mavjud litsenziyalangan test savollarini nusxalash **taqiqlanadi** — savollar original yozilsin.
- **16Personalities (`16personalities.com`) sahifasini ochma va undan savol olma.** Uning savollari
  himoyalangan; tarjima yoki "qayta yozish" ham taqiqlanadi. Tahlil `docs/17` da bor — undan faqat
  UX va struktura darslari olinadi.
- `scale` va `direction` maydonlari to'g'ri va izchil bo'lishi kritik — ball shunga bog'liq.

## DoD
- [ ] JSON valid, 60 ta savol, kodlar takrorlanmaydi
- [ ] Har o'qda aniq 15 savol; `direction` taqsimoti 8/7 yoki 7/8
- [ ] Ketma-ket bir o'qdan ≤ 2 savol
- [ ] Seed ishlaydi, DB'da 60 qator paydo bo'ladi
- [ ] Barcha matn o'zbek lotin alifbosida, 12 so'zdan qisqa

## Tekshiruv
```bash
python3 -c "
import json,collections
d=json.load(open('src/StudentRoadMap.Infrastructure/Persistence/SeedData/test-definitions/mbti16.json'))
q=d['questions']; print('jami',len(q))
print(collections.Counter(x['scale'] for x in q))
print(collections.Counter((x['scale'],x['direction']) for x in q))
print('uzun savollar:',[x['code'] for x in q if len(x['textUz'].split())>12])
"
```
