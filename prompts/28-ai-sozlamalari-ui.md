# P28 — AI sozlamalari interfeysi

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-7)
- `docs/07-api-shartnoma.md` (3.5)

## Vazifa
1. **`/admin/ai` sahifasi** — 3 ta provider kartasi (Gemini, OpenAI, Anthropic):
   - holat belgisi: "Kalit kiritilmagan" / "Faol" / "Default" / "Nofaol"
   - model nomi (matn maydoni), `maxOutputTokens`, `temperature`
   - API kalit maydoni: `password` tipi, saqlangandan keyin `AIza••••7f2b` ko'rinishida,
     "O'zgartirish" tugmasi bosilganda bo'sh maydon ochiladi
   - **"Aloqani tekshirish"** tugmasi → yashil "Ishlayapti · 640 ms" yoki qizil xato matni
   - "Default qilish" tugmasi
2. **Fallback tartibi** — drag-and-drop ro'yxat (yoki yuqoriga/pastga tugmalari),
   saqlanganda `fallback_order` yangilanadi.
3. **Foydalanish statistikasi** bloki: joriy oy chaqiruvlar soni, kirish/chiqish tokenlari,
   taxminiy xarajat, provider kesimidagi taqsimot (kichik bar chart).
4. **Promptlar** bo'limi: versiyalar ro'yxati (`key`, `version`, faol belgisi, yaratilgan sana),
   ko'rish dialogi (system + user matni, faqat o'qish uchun MVP'da).
5. Kalit saqlanganda ogohlantirish: "Kalit shifrlangan holda saqlanadi va boshqa hech qayerda
   ko'rsatilmaydi."

## Cheklovlar
- Kalit hech qachon to'liq ko'rinmasin (saqlangandan keyin ham).
- Kalitni brauzer konsoliga yoki log'ga chiqarish taqiqlanadi.
- Hech bir provider faol bo'lmasa — sahifa yuqorisida ogohlantirish banner:
  "AI tahlil ishlamaydi — kamida bitta provayder sozlanishi kerak."

## DoD
- [ ] Uch provider sozlanadi, kalit maskalangan qaytadi
- [ ] "Aloqani tekshirish" real natija beradi
- [ ] Default almashtirish ishlaydi
- [ ] Fallback tartibi saqlanadi va AI moduli unga amal qiladi
- [ ] Statistika to'g'ri hisoblanadi (DB bilan solishtirilgan)

## Tekshiruv
```bash
cd frontend && npm run build
# qo'lda: kalit kirit → tekshir → sessiya yakunlab tahlil o'sha provider bilan ketishini ko'r
```
