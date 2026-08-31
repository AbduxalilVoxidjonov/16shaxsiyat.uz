# P26 — Diagrammalar va AI hisobot ko'rinishi

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (1-bo'lim — rang qoidasi, 4-bo'lim — a11y)
- `docs/03-psixologik-metodikalar.md` (natija obyektlari tuzilishi)

## Vazifa
`widgets/` papkasida qayta ishlatiladigan komponentlar:

1. **`AxisBar`** — 16 tip o'qlari: gorizontal bar, markazda 50 chizig'i, ikki tomonda harf va
   nom (masalan `I ← → E`), foiz raqami; `borderline` bo'lsa "muvozanat" belgisi.
2. **`PersonalityRadar`** — Big Five 5 burchakli radar (Recharts), har omil nomi o'zbekcha,
   yonida daraja matni. `N` o'rniga **"Emotsional barqarorlik"** ko'rsatiladi (`100 − N`).
3. **`RiasecChart`** — 6 ustunli bar, top-3 ajratib ko'rsatilgan, ostida Holland kodi va
   differensiatsiya izohi.
4. **`ActivityBars`** — 4 shkala bar + umumiy `ActivityIndex` gauge.
5. **`IndexGauge`** — yarim doira gauge (0–100), daraja matni va rangi (neytral palitra).
6. **`ReliabilityBadge`** — 3 holat, tooltip'da tushuntirish.
7. **`AiReportView`** — AI javobini bo'limlarga ajratib render qiladi; bo'sh massivlarga chidamli;
   `attentionFlags` alohida karta sifatida (severity bo'yicha belgisi).

**Umumiy talablar:**
- Har diagramma yonida **raqamli qiymat** — faqat rangga tayanmaydi.
- Har diagramma uchun `aria-label` va (screen reader uchun) yashirin jadval alternativi.
- Ranglar **darajani baholamaydi** — past ball "qizil" bo'lmasin.
- Responsive: 360px da ham o'qiladi (mobilda gorizontal scroll o'rniga stack).
- `prefers-reduced-motion` da animatsiya o'chadi.
- Storybook shart emas, lekin `widgets/__demo__/` sahifasida barcha widget namunaviy
  ma'lumot bilan ko'rsatilsin (dev rejimida `/admin/_widgets` route).

## Cheklovlar
- Recharts'dan tashqari diagramma kutubxonasi qo'shilmasin.
- Har widget **sof prezentatsion** — API chaqirmaydi, faqat props qabul qiladi.

## DoD
- [ ] Barcha 7 widget yozildi va demo sahifada ko'rinadi
- [ ] Komponent testlari: chegaraviy qiymatlar (0, 50, 100), bo'sh ma'lumot
- [ ] `axe` a11y tekshiruvi buzilishsiz
- [ ] Individual profil sahifasi shu widget'lardan foydalanadi
- [ ] Mobil ko'rinish tekshirilgan

## Tekshiruv
```bash
cd frontend && npm run test -- widgets && npm run dev   # /admin/_widgets ni och
```
