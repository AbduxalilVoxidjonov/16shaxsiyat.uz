# P06 — Big Five savol banki (50 savol)

## Kontekst
P05 bilan bir xil yondashuv, endi OCEAN modeli uchun.

## O'qish shart
- `docs/03-psixologik-metodikalar.md` (3-bo'lim)

## Vazifa
`SeedData/test-definitions/big5.json` — **50 savol**, 5 omil × 10 savol:
`O` (Ochiqlik), `C` (Vijdonlilik), `E` (Ekstraversiya), `A` (Kelishuvchanlik), `N` (Neyrotizm).

- Har omilda **5 ta to'g'ri + 5 ta teskari** savol (aniq muvozanat).
- Kodlar `B5-Q01` … `B5-Q50`, omillar navbatlashib keladi.
- Til talablari P05 dagi bilan bir xil (o'smir tili, maktab konteksti, bitta g'oya, ≤ 12 so'z).

**`N` (neyrotizm) omili uchun alohida ehtiyot:**
- Savollar **kundalik kayfiyat va stress** haqida bo'lsin ("Imtihon oldidan juda hayajonlanaman"),
  klinik alomatlar haqida **emas** (uyqu buzilishi, umidsizlik, o'z-o'ziga zarar — **qat'iy taqiq**).
- Ruhiy salomatlik bo'yicha skrining savollari bo'lmasin — bu test tashxis qo'ymaydi.

**Omillar qamrovi:**
- `O`: yangi g'oya, ijod, san'at, qiziquvchanlik, tasavvur, o'zgarishga ochiqlik
- `C`: reja, tartib, muddat, mas'uliyat, sinchkovlik, maqsad
- `E`: muloqot, tashabbus, energiya, guruh, gapirish
- `A`: yordam, ishonch, kechirimlilik, hamkorlik, boshqalarni o'ylash
- `N`: hayajon, xavotir, kayfiyat o'zgarishi, tanqidga sezgirlik, stressda tez asabiylashish

## Cheklovlar
- IPIP savollarini so'zma-so'z tarjima qilma — mazmunan asoslanib, original o'zbekcha yoz.
- Klinik/tibbiy mazmunli savollar taqiqlanadi.

## DoD
- [ ] 50 savol, har omilda 10 ta, direction 5/5
- [ ] `N` savollarida klinik alomat yo'q (qo'lda ko'rib chiqilgan)
- [ ] Seed ishlaydi, DB'da 50 qator
- [ ] JSON valid

## Tekshiruv
```bash
python3 -c "
import json,collections
d=json.load(open('.../big5.json')); q=d['questions']
print(collections.Counter(x['scale'] for x in q))
print(collections.Counter((x['scale'],x['direction']) for x in q))
"
```
