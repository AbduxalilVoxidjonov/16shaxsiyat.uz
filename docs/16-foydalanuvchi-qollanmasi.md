# 16 — Superadmin uchun foydalanuvchi qo'llanmasi

Bu qo'llanma **Shaxsiyat** boshqaruv panelidan qanday foydalanish haqida — texnik bilim
talab qilmaydi. Har bo'lim bitta ekranga bag'ishlangan; ekran nomi sarlavhada yozilgan
(boshqaruv panelidagi chap menyudagi nom bilan bir xil).

> **Skrinshot uchun joy:** har bo'limda `📷 [Skrinshot: ...]` belgisi qo'yilgan — real
> skrinshotlar tayyor bo'lganda shu joylarga qo'yiladi.

---

## 1. Tizimga kirish

Manzil: `https://16shaxsiyat.uz/admin/login` (yoki serveringiz domeni + `/admin/login`).

📷 *[Skrinshot: "Boshqaruv paneliga kirish" oynasi]*

1. **Login** va **Parol** maydonlariga sizga berilgan ma'lumotlarni kiriting.
2. Agar ikki bosqichli tasdiqlash (2FA) yoqilgan bo'lsa, **"Tasdiqlash kodi (2FA)"**
   maydoniga telefoningizdagi autentifikatsiya ilovasi (masalan Google Authenticator)
   ko'rsatayotgan 6 xonali kodni kiriting.
3. **"Kirish"** tugmasini bosing.

**Eslatma:**
- Login va boshlang'ich parol tizimni o'rnatgan mutaxassis tomonidan beriladi (server
  sozlamalarida ko'rsatilgan) — birinchi kirishdan so'ng ularni **"Sozlamalar"**
  bo'limidan (7-bo'lim) darhol o'zgartiring.
- Parolni 5 marta ketma-ket noto'g'ri kiritsangiz, hisob 15 daqiqaga vaqtincha bloklanadi
  — bu begonalarning parolni "taxmin qilishga urinishi"dan himoya qiladi.

---

## 2. Boshqaruv paneli (bosh sahifa)

Kirgandan so'ng ochiladigan birinchi sahifa — umumiy holat bir qarashda.

📷 *[Skrinshot: Boshqaruv paneli — KPI kartochkalari, voronka, jadvallar]*

Yuqorida **sana oralig'i** filtri bor ("Sanadan" / "Sanagacha" yoki "Oxirgi 7 kun" /
"Oxirgi 30 kun" tugmalari) — barcha ko'rsatkichlar shu davr uchun hisoblanadi.

**Asosiy kartochkalar:**
- **Maktablar** — nechta maktab qo'shilgan, nechtasi faol.
- **O'quvchilar** — jami ro'yxatdan o'tganlar soni.
- **Yakunlangan sessiyalar** — testni to'liq topshirganlar soni.
- **Tahlil navbatida** — AI hisobot tayyorlanayotgan o'quvchilar. Bosilsa, ularning
  ro'yxatiga o'tkazadi.
- **E'tibor talab qiladi** — javoblari "shubhali"/"ishonchsiz" chiqqan yoki alohida
  e'tibor kerak bo'lgan o'quvchilar. Bosilsa, ularning ro'yxatiga o'tkazadi.

**"Voronka: havoladan tahlilgacha"** — o'quvchi havolani ochishidan tortib AI tahlil
tayyor bo'lishigacha bo'lgan yo'lni bosqichma-bosqich ko'rsatadi (havola ochilgan →
ro'yxatdan o'tgan → boshlagan → yakunlagan → tahlil tayyor). Har bosqichda qancha
o'quvchi "tushib qolgani" ko'rinadi — masalan, ko'p o'quvchi ro'yxatdan o'tib, lekin
testni boshlamasa, bu muammoning qayerdaligini ko'rsatadi.

Pastda **"Maktablar kesimi"** jadvali (har maktab bo'yicha alohida statistika) va
**diagrammalar** (shaxsiyat tiplari, aktivlik darajalari, kasb qiziqishlari taqsimoti)
joylashgan.

---

## 3. Maktab qo'shish va havola berish

Chap menyu → **"Maktablar"**.

📷 *[Skrinshot: Maktablar ro'yxati jadvali]*

### 3.1 Yangi maktab qo'shish

1. Yuqori o'ng burchakdagi **"Yangi maktab"** tugmasini bosing.

   📷 *[Skrinshot: "Yangi maktab" formasi]*

2. Formani to'ldiring:
   - **Nomi** — maktabning to'liq nomi (majburiy).
   - **Viloyat**, **Tuman** — ro'yxatdan tanlanadi.
   - **Maktab raqami**, **Mas'ul shaxs (F.I.Sh.)**, **Telefon raqami**, **Izoh** —
     ixtiyoriy, lekin to'ldirilsa bog'lanish osonlashadi.
   - **Kunlik ro'yxatdan o'tish limiti** — bir kunda nechta o'quvchi ro'yxatdan o'ta
     olishini cheklaydi (ortiqcha yuklanishning oldini olish uchun).
   - **Kirish kodi** — ixtiyoriy. To'ldirilsa, o'quvchilar havolaga qo'shimcha ravishda
     shu kodni ham bilishlari kerak bo'ladi (masalan, faqat maktab ichida e'lon
     qilingan bo'lsa). Bo'sh qoldirilsa, havolani bilgan har kim ro'yxatdan o'ta oladi.
3. **"Yaratish"** tugmasini bosing — maktab ro'yxatga qo'shiladi va unga avtomatik
   **o'ziga xos havola** yaratiladi.

### 3.2 Havolani olish va ulashish

Har maktab qatorida amallar tugmalari bor:

- **Havolani nusxalash** — maktabning shaxsiy havolasini buferga nusxalaydi
  (`https://16shaxsiyat.uz/t/<maktab-kodi>` ko'rinishida). Shu havolani maktab
  ma'muriyatiga yuboring — ular o'quvchilarga ulashadi.
- **QR kodni ko'rish** → ochilgan oynada:

  📷 *[Skrinshot: QR kod oynasi]*

  - **"PNG yuklab olish"** — QR kodni rasm sifatida saqlaydi (masalan, e'lon
    doskasiga chop etish uchun).
  - **"Chop etish"** — to'g'ridan-to'g'ri chop etish oynasini ochadi.
- **"Havolani yangilash"** — eski havolani DARHOL ishlamay qo'yadi va yangisini
  yaratadi. Faqat havola "chiqib ketgan" yoki xavfsizlik sababli almashtirish kerak
  bo'lganda ishlatiladi — maktabga yangi havolani qayta yuborishni unutmang.
- **"Faollashtirish" / "Nofaol qilish"** — maktabni vaqtincha yopish/ochish (havola
  ishlaydi, lekin nofaol maktabda o'quvchi "test vaqtincha yopilgan" xabarini ko'radi).
- **"Tahrirlash"** — maktab ma'lumotlarini o'zgartirish.
- **"O'chirish"** — maktabda hali o'quvchi yo'q bo'lsagina ishlaydi (aks holda avval
  o'quvchilarni boshqa maktabga ko'chiring yoki maktabni shunchaki nofaol qiling).

---

## 4. O'quvchilar ro'yxati

Chap menyu → **"O'quvchilar"**.

📷 *[Skrinshot: O'quvchilar jadvali, filtrlar]*

Jadval har bir o'quvchi bo'yicha qisqacha ma'lumot beradi: **F.I.Sh.**, **Maktab**,
**Sinf**, **Holat**, **Shaxsiyat tipi**, **Yetuklik**, **Aktivlik**, **Ishonchlilik**,
**Sana**.

**Filtrlar** (jadval ustida): qidiruv (ism/telefon bo'yicha), maktab, sinf, holat,
shaxsiyat tipi, aktivlik darajasi, "faqat e'tibor talab qiladiganlar", sana oralig'i.
Tanlangan filtrlar pastda "chip" sifatida ko'rinadi — har birini alohida yoki
**"Hammasini tozalash"** bilan bekor qilish mumkin.

**"Excel'ga eksport"** tugmasi — joriy filtrlarga mos o'quvchilar ro'yxatini jadval
fayl (`.xlsx`) sifatida yuklab beradi.

Qatorni bosish o'sha o'quvchining **individual profiliga** o'tkazadi (5-bo'lim).

**Ustunlar ma'nosi (oddiy tilda):**
- **Ishonchlilik** — o'quvchi javoblari qanchalik "diqqat bilan" berilganini ko'rsatadi
  (masalan, hamma savolga bir xil javob bergan yoki juda tez javob bergan bo'lsa,
  "Shubhali"/"Ishonchsiz" belgisi qo'yiladi — natijaga to'liq ishonmaslik kerak
  bo'lishi mumkin).
- **Yetuklik** — psixologik yetuklik ko'rsatkichi (0–100 oraliqda).
- **Aktivlik** — o'quvchining o'quv/ijtimoiy faolligi darajasi (Passiv → Juda faol).

---

## 5. O'quvchi profili (individual profil)

O'quvchilar jadvalidan istalgan qatorni bosing.

📷 *[Skrinshot: O'quvchi profili sahifasi — yuqori qism]*

Sahifa yuqorisida asosiy amallar:

- **"PDF yuklab olish"** — to'liq hisobotni PDF fayl sifatida yuklab beradi (maktabga
  yoki ota-onaga berish uchun qulay).
- **"Qayta tahlil"** — agar AI hisobot qoniqarli chiqmagan yoki boshqa AI provider
  bilan qayta tekshirish kerak bo'lsa, yangi tahlil buyurtma qilinadi. Ochilgan oynada
  provayderni tanlab, **"Boshlash"**ni bosing. Eski hisobot yangisi tayyor bo'lguncha
  ko'rinishda qoladi.
- **"Boshqa amallar"** ostida: **"Xom javoblar"** (o'quvchi har savolga qanday javob
  berganini, qancha vaqt sarflaganini ko'rsatadi — audit uchun) va **"O'chirish"**
  (o'quvchi va uning barcha natijalarini butunlay o'chiradi — bekor qilib bo'lmaydi).

**"Yig'ma ko'rsatkichlar"** — 4 ta kartochka:
- **Shaxsiyat tipi** (16 tipdan biri),
- **Yetuklik indeksi**,
- **Aktivlik indeksi**,
- **Kasb qiziqishlari** (Holland kodi — masalan, "IRA" — qaysi kasb yo'nalishlari
  o'quvchiga ko'proq mos kelishini ko'rsatadi).

📷 *[Skrinshot: Diagrammalar bo'limi]*

**"Diagrammalar"** — har test bo'yicha vizual grafik (16 tip o'qlari, 5 omil radar
diagrammasi, kasb qiziqishlari, aktivlik va motivatsiya).

📷 *[Skrinshot: "AI tahlil" bo'limi]*

**"AI tahlil"** — sun'iy intellekt tomonidan yozilgan to'liq matnli hisobot (kuchli
tomonlar, tavsiya etiladigan yo'nalishlar va h.k.). Agar tahlil hali tayyorlanayotgan
bo'lsa — "Tahlil tayyorlanmoqda" xabari ko'rinadi (odatda ~1 daqiqa, sahifa o'zi
yangilanadi). **"Tarix"** tugmasi — oldingi AI tahlillarni (agar bir necha marta qayta
tahlil qilingan bo'lsa) ko'rish imkonini beradi.

Pastda **"Sessiyalar tarixi"** — o'quvchi bir necha marta test topshirgan bo'lsa (masalan,
bir yildan keyin qayta), barcha urinishlar shu yerda ko'rinadi.

---

## 6. Dastur (test to'plami) biriktirish

Chap menyu → **"Dasturlar"**. Dastur — bu maktabga biriktiriladigan testlar to'plami
(masalan, standart 4 blok yoki qisqartirilgan variant).

📷 *[Skrinshot: Dasturlar ro'yxati]*

- **"Tizim"** dasturlari — tayyor, o'zgartirib bo'lmaydigan standart to'plamlar.
- **"Mening"** dasturlarim — siz yaratgan, moslashtirilgan to'plamlar.

### 6.1 Yangi dastur yaratish va unga test qo'shish

1. **"Yangi dastur"** → kod (masalan `PROFILE_9`), nomi, tavsifi, ko'rinishi
   ("Ommaviy" — barcha maktabda ko'rinadi, yoki "Biriktirilgan" — faqat tanlangan
   maktablarda) kiritiladi → **"Yaratish"**.
2. Dastur ochilgan sahifada **"Testlar"** bo'limida **"Test qo'shish"** tugmasi bilan
   kerakli testlarni (masalan, 16 tipli shaxsiyat modeli, RIASEC) tanlab qo'shing.
   Testlarni **"Yuqoriga"/"Pastga"** tugmalari bilan tartiblash mumkin.
3. Dastur tayyor bo'lgach **"Nashr qilish"** tugmasini bosing — shundan keyingina u
   maktablarga biriktirilishi mumkin bo'ladi. (Kamida bitta test bo'lishi shart;
   umumiy vaqt juda uzun bo'lsa, tizim ogohlantiradi.)

### 6.2 Dasturni maktabga biriktirish

Dastur sahifasida **"Biriktirilgan maktablar"** bo'limi:

📷 *[Skrinshot: Dasturga maktab biriktirish paneli]*

1. Qidiruv maydoniga maktab nomini yozing.
2. Ro'yxatdan maktabni tanlab **"Biriktirish"**ni bosing.
3. Endi shu maktab havolasidan kirgan o'quvchilar aynan shu dasturdagi testlarni
   ko'radi. Biriktirilgan maktabni ro'yxatdan olib tashlash — maktab nomi yonidagi
   olib tashlash belgisi orqali.

**Eslatma:** tizim dasturlarining tarkibi (qaysi testlar borligi) himoyalangan —
o'zgartirib bo'lmaydi, faqat maktabga biriktirish/olib tashlash mumkin.

---

## 7. Test yuklash (testlar katalogi)

Chap menyu → **"Testlar katalogi"**.

📷 *[Skrinshot: Testlar katalogi — "Tizim metodikalari" va "Mening testlarim"]*

- **"Tizim metodikalari"** — tayyor, o'zgartirib bo'lmaydigan 4 asosiy test
  (savollari/shkalasi himoyalangan — faqat ko'rish uchun ochiladi).
- **"Mening testlarim"** — siz JSON fayldan yuklagan qo'shimcha testlar.

### Yangi test yuklash

1. **"Test yuklash"** tugmasini bosing.
2. Ochilgan oynada tayyor JSON faylni tanlang (yoki shu yerga tashlang). Fayl tuzilishi
   metodolog/dasturchi tomonidan tayyorlanadi (`code`, `nameUz`, savollar ro'yxati va
   h.k. — aniq format uchun `docs/03-psixologik-metodikalar.md`ga qarang).
3. Fayl to'g'ri bo'lsa, tizim savollar sonini va taxminiy davomiylikni ko'rsatadi
   ("Fayl to'g'ri — yuklashga tayyor"). Xato bo'lsa, aniq xatolar ro'yxati chiqadi —
   ularni tuzatib qayta urinib ko'ring.
4. **"Yuklash"** — test **"Qoralama"** holatida yaratiladi (hali hech qayerda
   ishlatilmaydi). Uni dasturga qo'shish uchun avval kerakli dasturda **"Test
   qo'shish"** orqali biriktiring (6-bo'lim).

---

## 8. Sozlamalar (parol va 2FA)

Chap menyu → **"Sozlamalar"**.

📷 *[Skrinshot: Sozlamalar sahifasi]*

### 8.1 Parolni o'zgartirish

**"Joriy parol"**, **"Yangi parol"**, **"Yangi parolni tasdiqlang"** maydonlarini
to'ldirib **"Saqlash"**ni bosing. Yangi parol kamida 10 belgi, kamida bitta harf va
bitta raqamdan iborat bo'lishi shart.

### 8.2 Ikki bosqichli autentifikatsiya (2FA) — tavsiya etiladi

2FA hisobingizni parol o'g'irlansa ham himoya qiladi (kiruvchi kod telefoningizda
bo'ladi).

1. **"Yoqish"** tugmasini bosing.
2. Ekranda chiqqan QR kodni (yoki "Maxfiy kalit"ni qo'lda) autentifikatsiya ilovangizga
   (Google Authenticator, Authy va h.k.) qo'shing.
3. Ilova ko'rsatgan 6 xonali kodni kiritib tasdiqlang.
4. Tizim bir martalik **"Zaxira kodlar"** ro'yxatini ko'rsatadi — **ularni xavfsiz
   joyda saqlang** (masalan, qog'ozga yozib seyfda). Telefon yo'qolsa, shu kodlar
   orqali kirish mumkin bo'ladi.

2FA'ni o'chirish uchun **"O'chirish"** → joriy parolingizni tasdiqlang.

---

## 9. Tez orada qo'shiladigan bo'limlar

Quyidagi menyu bandlari boshqaruv panelida ko'rinadi, lekin hozircha **qurilish
jarayonida** ("Bu sahifa hali qurilmoqda" xabari chiqadi):

| Bo'lim | Nima uchun kerak bo'ladi |
|--------|---------------------------|
| **Sessiyalar** | Barcha test sessiyalarini (yakunlangan, davom etayotgan, muvaffaqiyatsiz) bitta ro'yxatda ko'rish va filtrlash — hozircha bu ma'lumotlar Boshqaruv paneli va O'quvchilar orqali qisman ko'rinadi |
| **AI sozlamalari** | Gemini/OpenAI/Anthropic kalitlarini kiritish, qaysi provayder standart ekanini tanlash, ulanishni sinash. Hozircha bu sozlamalar to'g'ridan-to'g'ri serverdagi ma'lumotlar bazasiga (texnik mutaxassis yordamida) kiritiladi |
| **Audit jurnali** | Kim, qachon, qaysi amalni bajarganini (login, o'chirish, sozlama o'zgartirish) ko'rish — xavfsizlik nazorati uchun |

Bu bo'limlar tayyor bo'lgach, shu qo'llanmaga yangi bo'lim sifatida qo'shiladi.

---

## 10. Muammo yuzaga kelsa

- **Havola ishlamayapti** ("Havola ishlamayapti, maktabingizdan yangisini so'rang" xabari
  o'quvchiga chiqsa) — maktab **"Nofaol"** holatda emasligini va havola oxirgi marta
  **"Havolani yangilash"** bilan almashtirilmaganini tekshiring (3-bo'lim).
- **O'quvchi "test vaqtincha yopilgan" degan xabar ko'rmoqda** — maktab holatini
  **"Faollashtirish"**ga o'zgartiring.
- **AI tahlil uzoq vaqt "tayyorlanmoqda" holatida qolib ketsa** — bir necha daqiqadan
  so'ng ham o'zgarmasa, o'quvchi profilida **"Qayta urinish"** tugmasini bosing yoki
  texnik mutaxassisga murojaat qiling.
- **Boshqa texnik muammolar** — texnik mutaxassisga murojaat qiling; ular uchun
  batafsil qo'llanma [`README.md`](../README.md) va [`docs/13-deploy-va-infratuzilma.md`](13-deploy-va-infratuzilma.md)da.
