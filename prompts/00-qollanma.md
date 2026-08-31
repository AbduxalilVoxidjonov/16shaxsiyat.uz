# Promptlardan qanday foydalanish

Bu papkadagi fayllar — **Claude Code uchun ketma-ket topshiriqlar**. Har biri bitta mantiqiy
qadam. Tartibni buzmang: har prompt oldingisining natijasiga tayanadi.

## Ish tartibi

1. Loyiha papkasida Claude Code'ni oching.
2. Navbatdagi prompt faylini oching, **butun mazmunini** nusxalab Claude'ga bering
   (yoki `prompts/07-... faylini o'qi va bajar` deb ayting).
3. Claude ishlab bo'lgach, promptdagi **Tekshiruv** buyruqlarini o'zingiz ishga tushiring.
4. **DoD** ro'yxatidagi hamma band bajarilganini tasdiqlang.
5. Commit qiling, keyingi promptga o'ting.

## Har prompt tuzilishi

| Bo'lim | Ma'nosi |
|--------|---------|
| **Kontekst** | Nima allaqachon tayyor, bu qadam qayerga tushadi |
| **O'qish shart** | Qaysi `docs/` fayllarini avval o'qish kerak |
| **Vazifa** | Aniq nima qilinadi |
| **Cheklovlar** | Nima qilinmasligi kerak |
| **DoD** | Tugadi deyish uchun shartlar |
| **Tekshiruv** | Ishga tushiriladigan buyruqlar |

## Umumiy qoidalar (har promptda amal qiladi)

- **Til:** kod va identifikatorlar inglizcha, izohlar va foydalanuvchiga ko'rinadigan matn o'zbekcha.
- **Hujjat — haqiqat manbai.** Ikkilanish bo'lsa `docs/` dagi tegishli faylga qara,
  taxmin qilma. Hujjatda yo'q bo'lsa — **avval so'ra**, keyin qil.
- **Sirlar hech qachon kodda emas** (`appsettings.json` da ham). Faqat env yoki user-secrets.
- **Test yozilmagan kod tugallanmagan hisoblanadi** (scoring va biznes qoidalar uchun majburiy).
- **Migratsiya faqat EF Core orqali**, qo'lda SQL yozilmaydi.
- **Bir promptda bir maqsad.** Yo'l-yo'lakay boshqa narsani "yaxshilab qo'yish" taqiqlanadi —
  agar muammo ko'rsang, aytib qo'y, lekin tuzatma.
- Har qadam oxirida `dotnet build` va (frontend bo'lsa) `npm run build` **ogohlantirishsiz** o'tishi shart.

## Bosqichlar xaritasi

| Bosqich | Promptlar |
|---------|-----------|
| B0 Poydevor | 01 – 03 |
| B1 Katalog va scoring | 04 – 09 |
| B2 Ommaviy API | 10 – 12 |
| B3 Admin API | 13 – 15 |
| B4 AI modul | 16 – 18 |
| B5 Ommaviy UI | 19 – 21 |
| B6 Admin UI | 22 – 26 |
| B7 Eksport va sozlamalar | 27 – 29 |
| B7.5 Anketa konstruktori | 33 |
| B8 Sifat va chiqarish | 30 – 32 |
