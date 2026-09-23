# 16 — Foydalanuvchi qo'llanmasi (Shaxsiyat)

**Shaxsiyat** — o'quvchining shaxsiyati, psixologik yetukligi, qiziqishlari va aktivligini
onlayn testlar orqali aniqlaydigan platforma. Bu qo'llanma texnik bilim talab qilmaydi.

Qo'llanma ikki xil odam uchun:

| Kim | Nima qiladi | Qaysi bo'limlar |
|-----|-------------|-----------------|
| **Superadmin** — psixolog yoki koordinator, boshqaruv paneliga kiradi | Maktab qo'shadi, havola beradi, natijalarni ko'radi, sozlaydi | 1–8 |
| **Maktab mas'uli** — maktab direktori, o'rinbosari yoki psixolog | Havolani o'quvchilarga yetkazadi, jarayonni kuzatadi | 9 (qisqa varaqa) |

> **Muhim:** tizimda **bitta rol** bor — superadmin. Maktab mas'uli uchun alohida login
> **yo'q**: u faqat maktabning shaxsiy havolasini oladi va o'quvchilarga tarqatadi.
> Natijalarni faqat superadmin ko'radi.

Bu hujjatda yozilgan har bir ekran, tugma va maydon nomi haqiqiy dasturdan olingan.
Hali qurilmagan bo'limlar **8.4-bo'limda** ochiq ro'yxat qilingan.

---

## 1. Boshlash

### 1.1 Tizimga kirish

Manzil: `https://16shaxsiyat.uz/admin/login`

Ekran sarlavhasi — **"Boshqaruv paneliga kirish"**.

1. **"Login"** va **"Parol"** maydonlarini to'ldiring.
2. **"Kirish"** tugmasini bosing.
3. Agar hisobingizda 2FA yoqilgan bo'lsa, birinchi urinishdan keyin ekranda qo'shimcha
   **"Tasdiqlash kodi (2FA)"** maydoni paydo bo'ladi va "Davom etish uchun autentifikatsiya
   ilovasidagi kodni kiriting" xabari chiqadi. Telefoningizdagi ilova ko'rsatayotgan
   6 xonali kodni kiriting va yana **"Kirish"**ni bosing.

> 2FA maydoni **doim ko'rinmaydi** — u faqat tizim kod so'raganda chiqadi. Bu normal holat.

**Bilib qo'ying:**
- Login va boshlang'ich parolni tizimni o'rnatgan texnik mutaxassis beradi. Ular server
  sozlamalarida saqlanadi, bu hujjatda **yozilmaydi**.
- Parol 5 marta ketma-ket noto'g'ri kiritilsa, hisob **15 daqiqaga bloklanadi**
  ("Hisob 15 daqiqaga bloklandi" xabari). Bu parolni taxmin qilishga urinishdan himoya.

### 1.2 Parolni o'zgartirish

Birinchi kirishdan keyin **darhol** parolni almashtiring.

Chap menyu → **"Sozlamalar"** → **"Parolni o'zgartirish"** kartasi:

1. **"Joriy parol"** — hozirgi parolingiz.
2. **"Yangi parol"** — kamida **10 belgi**, ichida kamida bitta **harf** va bitta **raqam**
   bo'lishi shart.
3. **"Yangi parolni tasdiqlang"** — yangi parolni yana bir marta yozing.
4. **"Saqlash"**.

Muvaffaqiyatli bo'lsa "Parol muvaffaqiyatli o'zgartirildi" xabari chiqadi.

### 1.3 Ikki bosqichli tasdiqlash (2FA) va zaxira kodlar

**"Sozlamalar"** → **"Ikki bosqichli autentifikatsiya (2FA)"** kartasi. Holat yozuvi
**"Yoqilgan"** yoki **"Yoqilmagan"** deb ko'rsatiladi.

**Yoqish:**

1. **"Yoqish"** tugmasini bosing. 2FA **shu zahoti yoqiladi** — qo'shimcha tasdiqlash
   qadami yo'q.
2. Ochilgan oynada ikki narsa ko'rsatiladi:
   - **"Maxfiy kalit"** — uzun harf-raqamli matn. Uni autentifikatsiya ilovangizga
     (Google Authenticator, Authy va shunga o'xshash) **qo'lda kiriting**.
   - **"Zaxira kodlar (har biri bir marta ishlatiladi)"** — bir nechta qisqa kod.
3. Oynani yopishdan oldin ikkalasini ham ko'chirib oling.

> **Bu ekranda QR kod ko'rsatilmaydi** — faqat matn ko'rinishidagi maxfiy kalit bor.
> Ilovada "kalitni qo'lda kiritish" (`Enter a setup key`) variantini tanlang.

**⚠️ Zaxira kodlar haqida — diqqat bilan o'qing:**

- Zaxira kodlar **faqat shu bir marta**, shu oynada ko'rsatiladi. Oynani yopganingizdan
  keyin ularni **hech qanday yo'l bilan qayta ko'rib bo'lmaydi**.
- Telefoningiz yo'qolsa yoki ilova o'chib ketsa, tizimga **faqat shu kodlar orqali**
  kirasiz. Kodlarsiz va telefonsiz hisobingiz yopiladi va tiklash uchun serverga
  kirish huquqiga ega texnik mutaxassis kerak bo'ladi.
- **Qayerda saqlash kerak:** qog'ozga yozib seyfga yoki qulflanadigan javonga qo'ying.
  Ikkinchi nusxani boshqa joyda saqlang. Ish kompyuteringizning ish stolida, elektron
  pochtangizda yoki telefoningizdagi eslatmalar ilovasida **saqlamang** —
  parol o'g'irlansa, ular bilan birga zaxira kodlar ham o'g'irlanadi.
- Har bir kod **bir martalik** — ishlatilgani ikkinchi marta ishlamaydi.

> **Yoqishdan oldin bir daqiqa tekshiring.** "Yoqish"ni bosganingizda oynada zaxira
> kodlar **ko'rinmasa** yoki ekranda "Sahifada xatolik yuz berdi" degan xabar chiqsa —
> 2FA server tomonda **allaqachon yoqilgan**, lekin sizda kod yo'q. Bunday holda
> **darhol**, tizimdan chiqmasdan turib, **"O'chirish"** tugmasi bilan 2FA'ni o'chiring
> (parolingiz so'raladi) va texnik mutaxassisga xabar bering. Muammo tuzatilgandan keyin
> qayta yoqasiz. Sessiyani yopib yuborsangiz va telefoningizda kod bo'lmasa, hisobingizga
> kira olmay qolasiz.

**O'chirish:** **"O'chirish"** → ochilgan oynada joriy **"Parol"**ingizni kiriting va
tasdiqlang. Parol so'ralishi ataylab qilingan: agar kimdir sizning ochiq sessiyangizni
o'g'irlasa ham, parolni bilmasdan 2FA'ni o'chira olmaydi.

**Chiqish:** chap menyu pastidagi **"Chiqish"**.

---

## 2. Maktab qo'shish va havola berish

Chap menyu → **"Maktablar"**.

### 2.1 Yangi maktab yaratish

Yuqoridagi **"Yangi maktab"** tugmasini bosing. Ochilgan formada:

| Maydon | Majburiymi | Izoh |
|--------|-----------|------|
| **"Nomi"** | Ha | Maktabning to'liq nomi |
| **"Viloyat"** | Ha | Ro'yxatdan tanlanadi |
| **"Tuman"** | Ha | Qo'lda yoziladi |
| **"Maktab raqami"** | Yo'q | |
| **"Mas'ul shaxs (F.I.Sh.)"** | Yo'q | Maktabdagi bog'lanadigan odam |
| **"Telefon raqami"** | Yo'q | |
| **"Kunlik ro'yxatdan o'tish limiti"** | Ha | Standart qiymat — **500** |
| **"Kirish kodi"** | Yo'q | **6 xonali raqam**; bo'sh qoldirilsa hamma ro'yxatdan o'ta oladi |
| **"Izoh"** | Yo'q | O'zingiz uchun eslatma |
| **"Testlar"** | Yo'q | Shu maktab havolasida ochiladigan testlar (faqat nashr qilingan va faol testlar taklif qilinadi). "Barcha maktablarga" ochiq testlarni belgilash shart emas (3.2) |

**"Yaratish"** tugmasini bosing. Maktab ro'yxatga qo'shiladi va unga avtomatik
**shaxsiy havola** yaratiladi.

### 2.2 Kunlik ro'yxatdan o'tish limiti nima uchun kerak

Bu — bir kunda shu maktab havolasi orqali **nechta o'quvchi ro'yxatdan o'ta olishi**.
Standart qiymat 500.

Nima uchun kerak:

- **Havola tarqab ketishidan himoya.** Maktab havolasi ochiq internetga (masalan,
  ommaviy Telegram kanaliga) tushib qolsa, begonalar yuzlab soxta anketa to'ldirishi
  mumkin. Limit shu oqimni bir kunda to'xtatadi va siz muammoni sezib ulgurasiz.
- **Yuklamani boshqarish.** Butun maktab bir vaqtda kirsa, server ham, siz ham ortiqcha
  yuklanadi. Limit testni bir necha kunga yoyishga majbur qiladi.
- **Xarajat nazorati.** Har bir yakunlangan test uchun AI tahlil buyurtma qilinadi —
  bu pul turadi. Limit kutilmagan xarajatdan saqlaydi.

Limitni maktab hajmiga qarab tanlang: sinf bo'yicha bosqichma-bosqich testlaydigan bo'lsangiz
30–60 yetadi; bir kunda butun maktabni qamramoqchi bo'lsangiz — o'quvchilar sonidan
biroz ko'proq qiling. Limitni istalgan vaqtda **"Tahrirlash"** orqali o'zgartirish mumkin.

**"Kirish kodi"** — qo'shimcha himoya qatlami. To'ldirilsa, o'quvchi havolani bilishining
o'zi kifoya qilmaydi: ro'yxatdan o'tish formasidagi **"Kirish kodi"** maydoniga
6 xonali kodni ham kiritishi kerak bo'ladi. Kodni faqat maktab ichida (sinfda, doskada)
e'lon qiling.

### 2.3 Havola va QR kodni olish

Maktablar jadvalidagi har bir qatorda amallar bor:

- **"Havolani nusxalash"** — maktabning shaxsiy havolasini buferga oladi
  (`https://16shaxsiyat.uz/t/<maktab-kodi>` ko'rinishida). "Havola nusxalandi" xabari chiqadi.
- **"QR kodni ko'rish"** — **"QR kod"** oynasi ochiladi. Unda:
  - **"Havola"** — matn ko'rinishidagi havola;
  - **"PNG yuklab olish"** — QR kodni rasm sifatida saqlaydi;
  - **"Chop etish"** — chop etish oynasini ochadi.

### 2.4 Havolani maktabga qanday yetkazish

1. **Mas'ul shaxsni aniqlang** — direktor o'rinbosari yoki maktab psixologi. Havolani
   shaxsan unga yuboring, umumiy guruhga tashlamang.
2. **QR kodni chop eting** va sinf doskasiga, koridorga yoki e'lon taxtasiga osing —
   o'quvchi telefon kamerasi bilan ochadi, havolani terib o'tirmaydi.
3. **Kirish kodi bo'lsa**, uni havoladan **alohida** kanal orqali ayting (masalan, sinfda
   og'zaki). Ikkalasini bitta xabarda yubormang — aks holda himoyaning ma'nosi qolmaydi.
4. **Nima qilish kerakligini tushuntiring:** o'quvchi telefon yoki kompyuterda ochadi,
   qisqa anketani to'ldiradi, keyin savollarga javob beradi. Javoblar avtomatik saqlanadi,
   internet uzilsa ham yo'qolmaydi.
5. **9-bo'limdagi qisqa varaqani** maktab mas'uliga yuboring — unda hamma narsa
   yozilgan.

### 2.5 Maktab bo'yicha boshqa amallar

- **"Tahrirlash"** — ma'lumotlarni o'zgartirish.
- **"Faollashtirish" / "Nofaol qilish"** — maktabni vaqtincha yopish yoki ochish.
  Nofaol maktabda havolani ochgan o'quvchi **"Test vaqtincha yopilgan"** xabarini ko'radi.
- **"Havolani yangilash"** — ochilgan oynada ogohlantirish chiqadi: *"Eski havola darhol
  ishlamay qoladi. Maktabga yangi havolani yuborishni unutmang."* Faqat havola tashqariga
  chiqib ketgan bo'lsa ishlating va yangisini maktabga qayta yuboring.
- **"O'chirish"** — maktabda hali o'quvchi bo'lmagandagina ishlaydi. Aks holda
  *"Bu maktabda o'quvchilar bor"* xatosi chiqadi — bunday holda maktabni shunchaki
  **"Nofaol qilish"** yetarli.

### 2.6 Maktab sahifasi

Jadvaldagi maktab nomini bosing (yoki **"Maktabni ochish"**) — alohida sahifa ochiladi:

- **"Ishtirok statistikasi"** — "Ro'yxatdan o'tgan", "Testni yakunlagan",
  "Yakunlash ulushi", "Jarayonda", "Oxirgi topshirilgan".
- **"Maktab ma'lumotlari"** — barcha kiritilgan maydonlar, shu jumladan "Havola".
- **"Biriktirilgan testlar"** — shu maktabga alohida biriktirilgan testlar nomi (bosilsa
  test sahifasi ochiladi). "Barcha maktablarga" ochiq testlar bu ro'yxatda ko'rsatilmaydi.
- **"O'quvchilar"** → **"Shu maktab o'quvchilarini ko'rish"** tugmasi.

### 2.7 Boshqaruv paneli (umumiy holat)

Chap menyu → **"Boshqaruv paneli"**. Yuqorida sana filtri: **"Sanadan"**, **"Sanagacha"**,
**"Oxirgi 7 kun"**, **"Oxirgi 30 kun"**, **"Filtrni tozalash"**.

Kartochkalar: **"Maktablar"**, **"O'quvchilar"**, **"Yakunlangan sessiyalar"**,
**"Tahlil navbatida"**, **"E'tibor talab qiladi"** (oxirgisi bosilsa, e'tibor talab
qiladigan o'quvchilar ro'yxatiga o'tkazadi).

Pastda: **"Oxirgi 30 kun"** (Yangi o'quvchilar, Yakunlangan, O'rtacha davomiylik,
O'rtacha ishonchlilik, Tashlab ketish darajasi), **"Voronka: havoladan tahlilgacha"**
(Havola ochilgan → Ro'yxatdan o'tgan → Boshlagan → Yakunlagan → Tahlil tayyor),
**"Maktablar kesimi"** jadvali, **"Shaxsiyat tiplari taqsimoti"**,
**"Aktivlik darajalari"**, **"Kasb qiziqishlari (Holland) — eng ko'p uchraydiganlar"**
va **"So'nggi sessiyalar"**.

Voronka eng foydali vosita: qaysi bosqichda o'quvchilar tushib qolayotganini ko'rsatadi.
Masalan, "Havola ochilgan" katta, "Ro'yxatdan o'tgan" kichik bo'lsa — anketa yoki kirish
kodi to'sqinlik qilyapti; "Boshlagan" katta, "Yakunlagan" kichik bo'lsa — test juda uzun.

---

## 3. Testni o'quvchilarga ochish (Biriktirish)

> **2026-09-23 egasi qarori:** ilgari alohida **"Dasturlar"** bo'limi bor edi, lekin u
> **"Testlar katalogi"** bilan amalda bir xil narsa edi (deyarli har dastur bitta testdan
> iborat edi). Endi "Dasturlar" bo'limi **yo'q** — testni kimga ochish **test sahifasining
> o'zida**, **"Biriktirish"** bo'limida sozlanadi. Eski `/admin/programs` havolalari
> avtomatik **"Testlar katalogi"**ga olib boradi.

Chap menyu → **"Testlar katalogi"** → kerakli testni oching → **"Biriktirish"** bo'limi
(test sarlavhasi ostida).

### 3.1 Holat — test hozir kimga ko'rinadi

Bo'lim tepasida rangli belgi va bir jumla:

| Belgi | Ma'nosi |
|-------|---------|
| **"Faol"** | *"Faol — o'quvchilarga ko'rinadi."* |
| **"Qoralama"** | *"Qoralama — test nashr qilinganda o'quvchilarga ochiladi."* Biriktirishni oldindan tayyorlab qo'yish mumkin |
| **"To'xtatilgan"** | Test nofaol — tepadagi **"Faollashtirish"** tugmasi bilan yoqing |
| **"Biriktirilmagan"** | *"Hech kimga biriktirilmagan — o'quvchilar bu testni ko'rmaydi."* |
| **"Arxiv"** | Test arxivlangan — biriktirishni o'zgartirib bo'lmaydi |

Yonida — shu biriktirish orqali nechta sessiya ochilgani. Holat **testga ergashadi**:
testni nashr qilish, nofaol qilish yoki arxivlash (sahifa tepasidagi tugmalar) uni o'zi
o'zgartiradi — alohida "dastur holati" yo'q.

### 3.2 "Kimga ochiq" — barcha yoki tanlangan maktablar

- **"Barcha maktablarga"** — test **har bir** maktab havolasida (keyin qo'shiladigan
  maktablarda ham) va ommaviy kabinetda ko'rinadi. Ehtiyot bo'ling: saqlashingiz bilanoq
  test hamma joyda paydo bo'ladi. Sinov uchun **"Tanlangan maktablarga"**ni ishlating.
- **"Tanlangan maktablarga"** — faqat siz tanlagan maktablarda:
  1. **"Maktab qidirish"** maydoniga maktab nomini yozing;
  2. ro'yxatda kerakli maktablarni belgilang (bir nechtasini birdan);
  3. tanlanganlar yuqorida "chip" ko'rinishida chiqadi — **×** bilan olib tashlanadi.

### 3.3 "Ommaviy (kabinet orqali hamma uchun)"

Bu belgi qo'yilsa, test **maktabsiz** foydalanuvchilarga ham ochiladi — Telegram orqali
`/kirish` sahifasidan kirgan har kim uni kabinetda ko'radi (**"Ommaviy makon"** bo'limi).
**"Barcha maktablarga"** tanlanganda bu belgi avtomatik qo'yilgan bo'ladi.

Agar *"Ommaviy makon hali sozlanmagan"* xabari chiqsa — ommaviy makon tizimda hali ishga
tushirilmagan; belgini olib, qolgan sozlamalarni saqlashingiz mumkin (texnik mutaxassisga
xabar bering).

### 3.4 Ro'yxatdan o'tish

- **"To'liq ro'yxatdan o'tish"** — o'quvchi test oldidan anketani to'ldiradi (maydonlar
  **"Sozlamalar"** → ro'yxatdan o'tish formasida sozlanadi).
- **"Ro'yxatdan o'tmasdan (anonim)"** — anketa so'ralmaydi; kerakli ma'lumotlar
  so'rovnoma savollari ichida so'raladi.

Shaxsiyat batareyasi (16 tip, Big Five, RIASEC, Aktivlik) bor testda anonim rejim
**tanlanmaydi** — natija yosh, sinf va jinsga tayanadi (tugma o'chiq, sababi yozilgan).

### 3.5 Saqlash

O'zgartirishdan keyin **"Biriktirishni saqlash"**ni bosing (**"O'zgarishlarni bekor
qilish"** — saqlanmagan tanlovni qaytaradi). Muvaffaqiyatli bo'lsa *"Biriktirish saqlandi"*.

Maktabga testni **maktab formasidan** ham berish mumkin — **"Maktablar"** → **"Tahrirlash"**
→ **"Testlar"** (2.1-bo'lim). Ikkalasi bir xil biriktirmani o'zgartiradi.

### 3.6 ⚠️ Bir maktabda bir nechta test ko'rinsa, o'quvchi TANLAYDI

Maktab havolasini ochgan o'quvchi ko'radigan narsa — shu maktabga ochiq testlar soniga
bog'liq:

| Maktabga ochiq testlar soni | O'quvchi nima ko'radi |
|-----------------------------|------------------------|
| **0 ta** | **"Test hali tayyor emas"** — *"Bu maktab uchun test hali tayyorlanmagan, maktabingizga murojaat qiling."* |
| **1 ta** | To'g'ridan-to'g'ri test boshlash sahifasi. Tanlov so'ralmaydi |
| **2 va undan ko'p** | Tanlov ekrani — har bir test alohida karta ko'rinishida (nomi, tavsifi, savol soni, vaqti). O'quvchi **o'zi** birini tanlaydi |

Bir xil to'plamni hamma topshirishi kerak bo'lsa, maktabda bir vaqtda **faqat bitta**
test ochiq turishiga ishonch hosil qiling (ortiqchasini nofaol qiling yoki
"Tanlangan maktablarga" qilib, kerakli maktablardan olib tashlang).

### 3.7 Endi yo'q imkoniyatlar

"Dasturlar" bo'limi bilan birga quyidagilar olib tashlandi: bir nechta testni bitta
"dastur"ga yig'ish va ularning tartibini belgilash, dasturga alohida kod/nom/tavsif/tartib
raqami berish, dasturni alohida nashr qilish/to'xtatish/arxivlash/tiklash, dasturni
o'chirishdan oldingi "nechta maktab havolasiz qoladi" ogohlantirishi. Endi har test o'zi
alohida ochiladi; landing va kabinetda **test nomi** ko'rinadi. Eski (ko'p testli) dastur
ommaviy makonda qolgan bo'lsa, u yerda **"Eski dastur"** belgisi bilan ko'rinadi va faqat
olib tashlanadi.

---

## 4. Savollar va anketalar (Testlar katalogi)

Chap menyu → **"Testlar katalogi"**. Sahifa ikki bo'limga bo'lingan:

- **"Tizim metodikalari"** — platforma bilan birga keladigan ilmiy metodikalar.
- **"Mening testlarim"** — siz yaratgan yoki yuklagan anketalar.

Har bir kartada: nomi, **"Faol"/"Nofaol"** belgisi, **"Ballanadi"** yoki
**"So'rovnoma"** belgisi va "N ta savol · ~M daqiqa".

### 4.1 Tizim metodikasini tahrirlash — nima mumkin, nima mumkin emas

Tizim metodikasini ochsangiz, sahifada shu izoh turadi:

> *"Bu tizim metodikasi. Nomi, tavsifi va savol matnlarini tahrirlash mumkin; shkala,
> yo'nalish va og'irlik qulflangan, savol qo'shib yoki o'chirib bo'lmaydi — ular ballash
> formulalariga bog'liq. Metodikaning o'zini o'zgartirmoqchi bo'lsangiz, "Nusxa olish"
> bilan erkin tahrirlanadigan nusxa yarating."*

| ✅ O'zgartirsa bo'ladi | ❌ Qulflangan |
|----------------------|--------------|
| Anketaning **nomi** va **tavsifi** | Savolning **shkalasi** |
| **Savol matni** (o'zbekcha, ruscha, inglizcha) | Savolning **yo'nalishi** ("To'g'ri" / "Teskari") |
| **Taxminiy vaqt**, **bir sahifadagi savollar soni**, aralashtirish | Savolning **og'irligi** |
| **Katalogdagi tartib raqami** | Savol **qo'shish** va **o'chirish** |
| **Faollashtirish / Faollikni to'xtatish**, **Arxivlash** | **O'chirish** (tugma umuman ko'rinmaydi) |

### 4.2 Nega aynan shular qulflangan

Sabab bitta: **ball shu uch narsaga tayanadi.**

Har bir savol biror **shkalaga** (masalan "Ekstraversiya") ball qo'shadi. **Yo'nalish**
javobni qanday sanashni aytadi: "To'g'ri" savolda yuqori javob shkalani oshiradi,
"Teskari" savolda esa qiymat aylantiriladi (5 → 1). **Og'irlik** esa savolning ulushini
belgilaydi.

Agar bir savolning shkalasini almashtirsangiz yoki savol qo'shsangiz:

- **eski natijalar buziladi** — kechagi o'quvchining bali boshqa formula bilan
  hisoblangan bo'ladi va bugungi bilan solishtirib bo'lmaydi;
- **metodika ilmiyligini yo'qotadi** — bu testlar ma'lum savollar to'plami bilan
  tekshirilgan; savol qo'shilishi bilan tekshiruv natijasi kuchini yo'qotadi;
- **AI tahlil ham noto'g'ri chiqadi** — u ballarga tayanadi.

**Savol matnini** o'zgartirishga esa ruxsat bor: matnni tushunarliroq qilish yoki
imlo xatosini tuzatish ballni o'zgartirmaydi.

> **Nashr etilgan anketani tahrirlasangiz** qo'shimcha ogohlantirish chiqadi:
> *"Bu anketa nashr etilgan. Savol matnini o'zgartirsangiz, u hozir test yechayotgan
> o'quvchilarga ham darhol ko'rinadi. Ballar o'zgarmaydi — hisoblash shkala va og'irlikka
> tayanadi, ular bu yerda o'zgartirilmaydi."* Shuning uchun matnni tahrirlashni test
> yechilmayotgan vaqtda qiling.

**Metodikaning o'zini o'zgartirmoqchi bo'lsangiz** — **"Nusxa olish"** tugmasini bosing.
Ochilgan oynada yangi kod bering (katta lotin harflari, raqam, `-` va `_`, 20 belgigacha).
Nusxa **"Qoralama"** holatida yaratiladi va unda **hamma narsa tahrirlanadi**.

### 4.3 O'z anketangizni yaratish

O'z anketangizni yaratishning uch yo'li bor.

**A) Tayyor metodikadan nusxa olish** — yuqoridagi **"Nusxa olish"**. Eng oson yo'l.

**B) Panelda yaratish** — **"Yangi anketa"** tugmasi. Fayl umuman kerak emas:
kod (katta lotin harflari, raqam, `-`, `_`), nom, tavsif, ballash rejimi
(**"Ballanadi"** yoki **"So'rovnoma"**), taxminiy vaqt va bir sahifadagi savollar sonini
kiritasiz → anketa **"Qoralama"** holatida yaratiladi va tahrirlash sahifasi ochiladi.
Savollar va shkalalarni o'sha yerda birma-bir qo'shasiz.

**C) Excel fayldan yuklash** — **"Anketa yuklash"** tugmasi:

1. Avval **namunani yuklab oling**. Ikki variant bor:
   - **bo'sh shablon** — ko'rsatma varag'i va bitta to'ldirilgan misol qatori bilan;
   - **mavjud anketani Excel'ga chiqarish** — masalan "16 tipli shaxsiyat modeli"ni
     yuklab olsangiz, 60 ta savol, shkalalar va og'irliklar bilan to'ldirilgan haqiqiy
     fayl chiqadi. Uni nusxalab o'zingiznikini yozish eng oson yo'l.
2. Fayl **beshta varaqdan** iborat: **Anketa** (kod, nom, tavsif), **Shkalalar**,
   **Oraliqlar** (talqin oraliqlari), **Savollar**, **Ko'rsatma**. Ustunlar tartibi
   ahamiyatsiz — sarlavha nomi bo'yicha topiladi.
3. To'ldirilgan faylni tanlang yoki oynaga tashlang (`.xlsx`). Eski `.xls` va makrosli
   `.xlsm` qabul qilinmaydi — Excel'da **"Farqli saqlash" → `.xlsx`** qiling.
4. Fayl o'qilgach savollar soni, shkalalar va topilgan xatolar ko'rsatiladi. Xato bo'lsa
   ro'yxat chiqadi — tuzatib qayta urinib ko'ring. **Hech narsa saqlanmaydi**, to siz
   tasdiqlamaguningizcha.
5. **"Yuklash"** — anketa **"Qoralama"** holatida yaratiladi.

> **Talqin oraliqlarini to'ldirishni unutmang.** Ularsiz anketa yaratiladi, lekin
> **nashr qilinmaydi**: har bir shkalada oraliqlar 0 dan 100 gacha bo'shliqsiz qoplashi
> va butun son bo'lishi shart (`0–33`, `34–66`, `67–100`). Bu qoida **Ko'rsatma**
> varag'ida ham yozilgan.

Eski `.json` format ham ishlaydi (seed fayllari bilan bir xil sxema) — u metodolog yoki
texnik mutaxassis uchun.

**Anketani to'ldirish** (o'z anketangizda, Qoralama holatida):

1. **"Tahrirlash"** — **"Anketa ma'lumotlarini tahrirlash"** oynasi: **"Nomi"**,
   **"Tavsifi"**, **"Katalogdagi tartib raqami"**, **"Taxminiy vaqt (daqiqa)"**,
   **"Bir sahifadagi savollar soni"**, **"Savollar tartibi aralashtirilsin"**.
2. **"Shkalalar"** bo'limi → **"Shkala qo'shish"**. Har bir shkala — o'lchanadigan bitta
   sifat (masalan "Stressga chidamlilik"). Maydonlari: **"Shkala kodi"** (katta lotin
   harflari), **"Nomi"**, **"Tavsifi"**, **"Tartib raqami"** va **"Talqin oraliqlari"**
   (4.4-bo'lim).
3. **"Savollar"** bo'limi → **"Savol qo'shish"**. Maydonlari: **"Savol kodi"**,
   **"Savol turi"** (Likert (5 ball), Likert (7 ball), Ha / Yo'q, Bitta variant,
   Majburiy tanlov), **"Matni (o'zbekcha)"** va ixtiyoriy ruscha/inglizcha matn,
   **"Tartib raqami"**, shkala, yo'nalish, og'irlik, **"Savol faol"**,
   **"Javob berish majburiy"**.

> **"Shkalalar"** bo'limi **faqat o'z anketalaringizda** ko'rinadi — tizim metodikasida
> u umuman ko'rsatilmaydi.

### 4.4 Talqin oraliqlari — qat'iy qoida

**Talqin oraliqlari** — bu 0 dan 100 gacha bo'lgan ballni odam tushunadigan darajaga
aylantirish jadvali. Masalan: 0–33 "Past", 34–66 "O'rtacha", 67–100 "Yuqori".

Shkala oynasidagi **"Talqin oraliqlari"** bo'limida qoida shunday yozilgan:

> *"Chegaralar butun son bo'lsin; "gacha" qiymati oraliqqa kiradi; keyingi oraliq
> oldingisidan roppa-rosa 1 ga katta sondan boshlanadi; birinchisi 0 dan boshlanib,
> oxirgisi 100 da tugaydi."*

Ya'ni to'rt shart:

1. **Butun son** — 33.5 kabi kasrli chegara mumkin emas.
2. **Bo'shliqsiz** — har bir ball biror oraliqqa tushishi shart.
3. **Ustma-ust tushmasin** — bitta ball ikki oraliqqa tegishli bo'lmasin.
4. **0 dan 100 gacha to'liq qoplansin.**

Har bir qatorda: **"Dan"**, **"Gacha"**, **"Daraja nomi"**. Qator qo'shish —
**"Oraliq qo'shish"**.

**Eng oson yo'l:** **"Nechta oraliq"** maydoniga sonni yozib, **"Teng bo'lish"** tugmasini
bosing — tizim 0–100 ni o'zi teng bo'lib beradi (*"3 → 0–33 / 34–66 / 67–100"*).

Xato qilsangiz, aniq xabar chiqadi:

| Xabar | Nima qilish kerak |
|-------|-------------------|
| *"Oraliq chegaralari butun son bo'lishi shart..."* | Kasrli sonni olib tashlang |
| *"Oraliqning boshlanishi tugashidan katta..."* | "Dan" ≤ "Gacha" bo'lsin |
| *"Oraliqlar 0 dan boshlanib 100 da tugashi shart"* | Birinchi qator 0 dan, oxirgisi 100 gacha |
| *"Oraliqlar orasida bo'shliq bor..."* | Keyingi oraliqni oldingisidan +1 dan boshlang |
| *"Oraliqlar ustma-ust tushadi..."* | Keyingisini +1 dan boshlang, bir xil sondan emas |
| *"Har bir oraliqqa daraja nomini kiriting..."* | Har qatorga nom yozing |

Shkalalar ro'yxatida oraliqlar to'ldirilmagan shkala yonida
*"Talqin oraliqlari belgilanmagan — nashrdan oldin qo'shing"* yozuvi turadi.

### 4.5 Anketani nashr qilish

**"Nashr qilish"** tugmasi faqat **"Qoralama"** holatidagi anketada ko'rinadi.
Ochilgan oynada izoh bor:

> *"Nashr etilgan anketa faqat YANGI o'quvchi sessiyalariga qo'shiladi. Nashrdan oldin
> tizim anketani tekshiradi."*

Shart bo'lmasa, **"Nashrga to'sqinlik qilayotgan N ta muammo"** ro'yxati chiqadi. Eng
ko'p uchraydiganlari:

- *"Anketada kamida bitta faol savol bo'lishi kerak."*
- *"Anketada kamida bitta shkala bo'lishi kerak."*
- *"Savolga shkala biriktirilmagan."*
- Shkalada talqin oraliqlari to'liq emas (4.4-bo'lim).

Muammolarni tuzatib **"Qayta urinish"**ni bosing.

**Nashr etilgandan keyin** anketani o'quvchilarga ochish — test sahifasidagi
**"Biriktirish"** bo'limida (3-bo'lim). Biriktirilmagan anketani o'quvchi **hech qachon
ko'rmaydi**.

**Boshqa amallar:** **"Faollashtirish" / "Faollikni to'xtatish"**, **"Arxivlash"**
(*"Arxivlangan anketa yangi sessiyalarga qo'shilmaydi. Eski natijalar saqlanib qoladi"*),
**"O'chirish"** (*"faqat hech qaysi sessiyada ishlatilmagan"* o'z anketalaringizda).

---

## 5. Natijalar

### 5.1 O'quvchilar ro'yxati

Chap menyu → **"O'quvchilar"**.

**Filtrlar:** **"Qidiruv"** (F.I.Sh. yoki telefon bo'yicha), **"Maktab"**, **"Sinf"**,
**"Holat"**, **"Shaxsiyat tipi"**, **"Aktivlik darajasi"**,
**"Faqat e'tibor talab qiladiganlar"**, **"Sanadan"**, **"Sanagacha"**.
Tanlangan filtrlar pastda alohida belgilar ko'rinishida chiqadi; birini olib tashlash yoki
**"Hammasini tozalash"** mumkin.

**Jadval ustunlari:** **"F.I.Sh."**, **"Maktab"**, **"Sinf"**, **"Holat"**, **"Tip"**,
**"Yetuklik"**, **"Aktivlik"**, **"Ishonchlilik"**, **"Sana"**.

**"Holat"** qiymatlari: Qoralama · Davom etmoqda · Yakunlangan · Tahlil qilinmoqda ·
Tahlil qilingan · Tahlil muvaffaqiyatsiz · Tashlab ketilgan.

**"Aktivlik"** darajalari: Passiv · Kam faol · O'rtacha faol · Faol · Juda faol.

Qatorni bosish o'quvchi profilini ochadi.

### 5.2 Ishonchlilik indeksi — nimani anglatadi

**Ishonchlilik** — o'quvchi savollarga qanchalik diqqat bilan javob berganini o'lchaydigan
0–100 ballik ko'rsatkich. Bu **o'quvchining bahosi emas** — bu **natijaning bahosi**.

Ball 100 dan boshlanadi va quyidagi belgilar uchun kamayadi:

| Belgi | Nimani bildiradi |
|-------|------------------|
| Juda tez javoblar (savolga 1 soniyadan kam) | O'quvchi savolni o'qimagan |
| Ketma-ket 12 va undan ortiq bir xil javob | Tugmani ketma-ket bosib chiqqan |
| Ma'noviy qarama-qarshi savollarga bir xil javob | Javoblar izchil emas |
| Barcha savollarga bir xil javob | Test to'ldirilmagan hisoblanadi |
| Butun sessiya 6 daqiqadan qisqa | Jiddiy o'ylab javob berilmagan |

Natijada uch daraja chiqadi:

| Ball | Belgi | Ma'nosi |
|------|-------|---------|
| 70 va yuqori | **"Ishonchli"** | *"Javoblar izchil — natija ishonchli."* |
| 40–69 | **"Shubhali"** | *"Ba'zi javoblarda shubha bor — natijani ehtiyot bilan talqin qiling."* |
| 40 dan past | **"Ishonchsiz"** | *"Javoblar juda tez yoki bir xil berilgan — natija ishonchsiz bo'lishi mumkin."* |

**Past bo'lsa nima qilish kerak:**

1. **Natijani ota-onaga yoki o'quvchiga bermang.** Ayniqsa "Ishonchsiz" bo'lsa — bu
   natija o'quvchi haqida hech narsa aytmaydi.
2. **"Xom javoblar"ni oching** (profil sahifasida, **"Boshqa amallar"** ostida) va
   **"Vaqt"** ustuniga qarang. Agar deyarli hamma javob bir necha yuz millisekundda
   berilgan bo'lsa — o'quvchi shunchaki bosib chiqqan.
3. **Sababini aniqlang.** Odatda uchtadan biri: test dars oxirida shoshib topshirilgan;
   o'quvchi maqsadini tushunmagan ("baho qo'yiladi" deb o'ylagan); test juda uzun
   bo'lgani uchun charchagan.
4. **Qayta topshirtiring.** O'quvchiga bu baho emasligini, to'g'ri-noto'g'ri javob
   yo'qligini va natija o'ziga foyda qilishini tushuntiring. Tinch vaqt va yetarli
   muddat bering.
5. **Bir maktabda ko'p "Ishonchsiz" chiqsa** — muammo o'quvchilarda emas, tashkil
   qilishda. Testni qisqartiring yoki uni boshqa vaqtga ko'chiring.

"Ishonchsiz" o'quvchilar ro'yxatda **"E'tibor talab qiladi"** belgisi bilan ko'rinadi va
ularni **"Faqat e'tibor talab qiladiganlar"** filtri bilan ajratib olish mumkin.
Profil sahifasida AI hisobot tepasida qizil ogohlantirish chiqadi:
*"Javoblar juda tez berilgan, natija ishonchsiz bo'lishi mumkin. Qayta topshirish
tavsiya etiladi."*

### 5.3 O'quvchi profili

Ro'yxatdan istalgan o'quvchini bosing.

**Yuqori qismdagi amallar:**

- **"PDF yuklab olish"** — to'liq hisobot PDF fayl sifatida yuklanadi.
- **"Qayta tahlil"** — ochilgan oynada AI **"Provider"**ini tanlab **"Boshlash"**ni
  bosing. Izohda: *"Tanlangan AI provider bilan yangi tahlil boshlanadi. Joriy hisobot
  tahlil yakunlanguncha ko'rinishda qoladi."* Agar hech qanday provider sozlanmagan
  bo'lsa: *"Faol AI provider topilmadi. Avval AI sozlamalarida provider ulang."*
- **"Boshqa amallar"** ostida:
  - **"Xom javoblar"** — *"Savol, javob va vaqt bo'yicha audit ma'lumoti."*
    Test bloki bo'yicha: Savol · Javob · Vaqt · Tahrirlar.
  - **"O'chirish"** — *"Bu amalni ortga qaytarib bo'lmaydi. O'quvchi va uning barcha
    natijalari o'chiriladi."*

**"Yig'ma ko'rsatkichlar"** — 4 ta kartochka: **"Shaxsiyat tipi"**,
**"Yetuklik indeksi"**, **"Aktivlik indeksi"**, **"Kasb qiziqishlari"**.
Natija hali bo'lmasa — *"Hali natija yo'q"*.

**"Diagrammalar"**: **"16 tip o'qlari"**, **"Shaxsiyatning 5 omili"**,
**"Kasb qiziqishlari"**, **"Aktivlik va motivatsiya"**. Topshirilmagan blok uchun
*"Bu testning natijasi hali mavjud emas"*.

**"AI tahlil"** — 6-bo'limga qarang.

**"Sessiyalar tarixi"** — o'quvchi bir necha marta topshirgan bo'lsa, barcha urinishlar
(Boshlangan · Yakunlangan · Davomiyligi · Ishonchlilik · Holat).

### 5.4 Eksport (Excel va PDF)

**Excel:** **"O'quvchilar"** sahifasidagi **"Excel'ga eksport"** tugmasi. **Joriy
filtrlarga mos** o'quvchilar `.xlsx` fayl sifatida yuklanadi (`oquvchilar-eksport-SANA.xlsx`).
Ya'ni avval filtrni sozlang, keyin eksport qiling — aks holda hamma o'quvchi tushadi.

**PDF:** o'quvchi profilidagi **"PDF yuklab olish"** — bitta o'quvchining to'liq hisoboti
(`hisobot-....pdf`). Maktabga yoki ota-onaga berish uchun mo'ljallangan.

> Eksport qilingan fayllarda **haqiqiy ism va telefon** bo'ladi. Ularni umumiy papkaga
> qo'ymang, guruhga tashlamang — 7-bo'limga qarang. Har bir Excel eksporti
> **"Audit jurnali"**ga *"O'quvchilar ro'yxati eksport qilindi"* deb yoziladi.

---

## 6. AI tahlili

Chap menyu → **"AI sozlamalari"**.

### 6.1 Provayder kalitini kiritish

Sahifada uchta provayder kartasi bor: **"Gemini"**, **"OpenAI"**, **"Anthropic"**.
Ularning hech biri kalitsiz ishlamaydi. Kalit sozlanmagan bo'lsa, sahifa tepasida
qizil ogohlantirish turadi:
*"AI tahlil ishlamaydi — kamida bitta provayder sozlanishi kerak."*

Har bir kartada:

- **"API kaliti"** — kalit kiritilgan bo'lsa yashiringan ko'rinishda turadi.
  Almashtirish uchun **"O'zgartirish"**ni bosing va yangi kalitni
  **"Yangi API kalitini kiriting"** maydoniga yozing. Saqlashdan oldin tasdiqlash
  so'raladi: *"Yangi kalit saqlangach eskisi darhol ishlamay qoladi."*
  - *"Kalit shifrlangan holda saqlanadi va boshqa hech qayerda to'liq ko'rsatilmaydi"* —
    ya'ni kiritganingizdan keyin kalitni tizimdan qayta o'qib bo'lmaydi. Kalitni o'zingiz
    ham xavfsiz joyda saqlang.
  - Maydonni **bo'sh qoldirsangiz mavjud kalit o'zgarmaydi** — bu ataylab shunday
    qilingan, kalitni tasodifan o'chirib qo'ymaslik uchun.
- **"Model nomi"** — tavsiya etilgan model ko'rsatiladi, lekin majburiy emas: istalgan
  model nomini yozishingiz mumkin.
- **"Maksimal token soni"** va **"Temperature"** — javob uzunligi va erkinligi.
  Bilmasangiz tegmang.
- **"Faol (tahlilda ishlatiladi)"** — belgilanmagan provayder umuman ishlatilmaydi.
- **"Default qilish"** — bu provayder birinchi navbatda ishlatiladi. Tasdiqlash oynasida
  ogohlantirish bor: *"Bu ishlab chiqarishda AI xarajati va javob vaqtiga ta'sir qiladi."*
- **"Aloqani tekshirish"** — kalit haqiqatan ishlayotganini tekshiradi. Muvaffaqiyatli
  bo'lsa "Ishlayapti · N ms" deb chiqadi. **Kalit kiritgandan keyin har doim shu tugmani
  bosing** — aks holda xato faqat birinchi o'quvchi test topshirganda ma'lum bo'ladi.

Sahifa pastida yana ikki bo'lim bor: **"Foydalanish statistikasi"** (Chaqiruvlar (joriy oy),
Kirish tokenlari, Chiqish tokenlari, Taxminiy xarajat) va **"Promptlar"** (AI'ga
yuboriladigan ko'rsatma matnlari — **"Ko'rish"** tugmasi bilan o'qish mumkin,
bu yerda tahrirlanmaydi).

### 6.2 Fallback zanjiri nima

**"Fallback tartibi"** bo'limi. Izohi shunday:

> *"Birinchi provayder ishlamasa (kalit xato, limit tugagan, javob bermadi), tizim
> ro'yxatdagi keyingi provayderga o'tadi."*

Oddiy qilib aytganda — bu **zaxira reja**. Tizim avval birinchi provayderga murojaat
qiladi; u javob bermasa yoki xato qaytarsa, o'zi ikkinchisiga, keyin uchinchisiga
o'tadi. Superadmin aralashuvi shart emas.

Tartibni **"Yuqoriga ko'chirish"** / **"Pastga ko'chirish"** tugmalari bilan
o'zgartirasiz.

**Nima uchun kerak:** AI xizmatlari vaqti-vaqti bilan ishlamay qoladi yoki oylik limit
tugaydi. Zanjir bo'lmasa, o'sha kuni topshirgan hamma o'quvchi hisobotsiz qoladi.

**Amaliy maslahat:** kamida **ikkita** provayderga kalit kiriting va ikkalasini ham
**"Faol"** qiling. Bitta provayder — bu bitta nosozlik nuqtasi.

### 6.3 ⚠️ "Avtomatik shablon hisobot" belgisi

Agar **zanjirdagi barcha** provayder ishlamasa, tizim o'quvchini hisobotsiz qoldirmaydi —
u **shablon** asosida matn tayyorlaydi. Bunday hisobot profil sahifasida sariq
**"Avtomatik shablon hisobot"** belgisi bilan chiqadi va yonida izoh turadi:

> *"Bu matnni AI yozmagan: barcha AI provayderlari javob bermagani uchun tizim avtomatik
> shablon hisobot tayyorladi. Uni shaxsiy tahlil sifatida o'qimang — AI sozlamalarini
> tekshirib, tahlilni qayta ishga tushiring."*

**Buni tushunish juda muhim.** Shablon hisobot — bu **oldindan yozilgan matnlarni**
o'quvchining tipi va ball darajalari bo'yicha yig'ib qo'yilgan natija. U:

- **shaxsiy emas** — bir xil tipdagi barcha o'quvchi deyarli bir xil matnni oladi;
- o'quvchining **kombinatsiyasini** hisobga olmaydi — masalan, yuqori qiziqish past
  motivatsiya bilan qanday bog'lanishini aytmaydi;
- ota-onaga yoki o'quvchiga **shaxsiy tahlil sifatida berilmasligi kerak**.

**Ko'rsangiz nima qilish kerak:**

1. **"AI sozlamalari"**ga o'ting va har bir provayderda **"Aloqani tekshirish"**ni bosing.
2. Muammoni toping — odatda kalit muddati tugagan, oylik limit tugagan yoki model nomi
   noto'g'ri yozilgan.
3. Tuzatgandan keyin o'quvchi profilida **"Qayta tahlil"** tugmasini bosing va haqiqiy
   provayderni tanlang.
4. Shu davrda ko'p o'quvchi shablon hisobot olgan bo'lsa, ularning hammasini qayta
   ishga tushiring.

**Boshqa belgi — "Moderatsiya qilingan hisobot"** (to'q sariq):

> *"Bu javobda taqiqlangan atama ikkinchi urinishda ham topilgan, shuning uchun u
> "moderatsiya qilindi" belgisi bilan saqlangan. Matnni o'quvchiga yoki ota-onaga
> ko'rsatishdan oldin albatta ko'zdan kechiring va zarur bo'lsa tahlilni qayta ishga
> tushiring."*

Ya'ni AI matnida tashxisga o'xshash yoki salbiy yorliq bo'lgan so'z topilgan.
Bunday hisobotni **albatta o'zingiz o'qib chiqing** va ko'rsatishdan oldin baholang.

### 6.4 Tahlil qanday kechadi

O'quvchi testni yakunlashi bilan tahlil **navbatga** qo'yiladi va fonda bajariladi.
Profil sahifasida holat ko'rinadi:

- **"Tahlil tayyorlanmoqda"** — *"Odatda 1 daqiqa vaqt ketadi. Sahifa avtomatik
  yangilanadi."*
- **"Tahlil muvaffaqiyatsiz"** — *"AI tahlil yakunlanmadi. Boshqa provider bilan qayta
  urinib ko'ring."* → **"Qayta urinish"** tugmasi.
- *"Bu sessiya uchun AI tahlil hali mavjud emas."*

**"Tarix"** tugmasi — **"AI tahlillar tarixi"**ni ochadi (Navbatda / Ishlamoqda /
Tahlil qilingan / Muvaffaqiyatsiz). Bir necha marta qayta tahlil qilingan bo'lsa,
eskilarini shu yerdan ko'rish mumkin.

Hisobot bo'limlari: Umumiy xulosa · Shaxsiyat portreti · Kuchli tomonlar · O'sish
zonalari · O'quv uslubi · Motivatsiya · Aktivlik · Kasb yo'nalishlari · Tavsiyalar
(O'quvchiga / O'qituvchiga / Ota-onaga).

---

## 7. Maxfiylik va mas'uliyat

### 7.1 AI'ga shaxsiy ma'lumot yuborilmaydi

Tizim AI provayderga **hech qanday shaxsni aniqlaydigan ma'lumot yubormaydi.**

| ❌ Yuborilmaydi | ✅ Yuboriladi |
|----------------|--------------|
| Ism, familiya, otasining ismi | Yoshi (masalan: 16) |
| Telefon raqami (o'zi va ota-onasi) | Sinfi (masalan: 9) |
| Elektron pochta | Jinsi |
| Aniq tug'ilgan sana | Test ballari va daraja nomlari |
| Maktab nomi, viloyat, tuman | Ishonchlilik ko'rsatkichi |

Ya'ni AI uchun bu "16 yoshli, 9-sinf o'quvchisi, quyidagi ballar bilan" — kim ekanini
u bilmaydi. Ism va telefon **faqat sizning boshqaruv panelingizda** saqlanadi.

Bu qoida siz yaratgan anketalarga ham tegishli: ularning ballari yuboriladi, matnli
javoblar va shaxsiy ma'lumot yuborilmaydi. **Shu sababli o'z anketangizga ism, telefon
yoki manzil so'raydigan savol qo'shmang.**

### 7.2 Natija tashxis emas

Bu eng muhim mas'uliyat chegarasi.

- Shaxsiyat testi, 5 omil anketasi, kasb qiziqishlari va aktivlik anketasi —
  **o'zini o'zi baholash** vositalari. Ular o'quvchi o'zi haqida nima deb o'ylashini
  o'lchaydi, kasallikni yoki qobiliyatni emas.
- AI'ga tashxis qo'yish **qat'iy taqiqlangan**: "depressiya", "ADHD", "autizm" kabi
  atamalar promptda taqiqlangan va javob qo'shimcha filtrdan o'tkaziladi. Shunga qaramay
  bironta shubhali atama o'tib ketsa, hisobot **"Moderatsiya qilingan hisobot"** belgisi
  bilan saqlanadi (6.3-bo'lim).
- **Natija taqdirni belgilamaydi.** "Kasb qiziqishlari" — bu qaysi yo'nalish hozir
  qiziqroq tuyulayotganini ko'rsatadi, o'quvchi kim bo'lishi kerakligini emas.
  O'smirda bu ko'rsatkichlar bir yilda sezilarli o'zgaradi.
- **Natijani yorliqqa aylantirmang.** O'quvchini uning tipi bilan atash, sinfda e'lon
  qilish, sinflarga ajratish uchun ishlatish — zarar keltiradi.
- Agar tahlilda tashvishli narsa ko'rsangiz (masalan o'ta past motivatsiya, ijtimoiy
  yakkalanish belgilari) — bu **suhbat boshlash uchun sabab**, xulosa emas. Malakali
  psixolog bilan ishlang.

### 7.3 Ma'lumotni kim ko'radi

| Kim | Nimani ko'radi |
|-----|----------------|
| **Superadmin** | Hammasini: ism, telefon, ballar, AI tahlil, xom javoblar |
| **Maktab mas'uli** | Boshqaruv paneliga **kira olmaydi**. Faqat siz unga bergan narsani ko'radi (masalan PDF hisobot) |
| **O'quvchi** | Testni yakunlagach faqat qisqartirilgan natija: **"Kuchli tomonlaring"** va **"Senga mos yo'nalishlar"**. To'liq hisobot, ballar va boshqalarning natijasi ko'rsatilmaydi |
| **Ota-ona** | Tizimda kirish huquqi yo'q. Siz uzatgan narsanigina biladi |
| **AI provayder** | Faqat 7.1-jadvaldagi anonim ma'lumot |

O'quvchi test oxirida ko'radigan xabar: *"Natijalar maktab psixologiga yuboriladi."*
Ro'yxatdan o'tishda u rozilik belgisini qo'yadi:
*"Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman"*.

**Sizning mas'uliyatingiz:**

- Hisobni **boshqa hech kim bilan bo'lishmang.** Ikkinchi odamga kerak bo'lsa, texnik
  mutaxassisdan unga alohida hisob so'rang.
- **2FA'ni yoqing** (1.3-bo'lim). Sizning parolingiz — butun maktab o'quvchilarining
  shaxsiy ma'lumotiga kalit.
- **Eksport fayllarini nazorat qiling.** `.xlsx` va `.pdf` fayllarda haqiqiy ism va
  telefon bor. Umumiy kompyuterda qoldirmang, kerak bo'lmasa o'chirib tashlang.
- **Barcha muhim amal yozib boriladi.** **"Audit jurnali"**da kim, qachon, nima
  qilgani ko'rinadi: tizimga kirish, parol o'zgarishi, maktab o'chirilishi, havola
  yangilanishi, AI kalitining almashtirilishi, o'quvchining o'chirilishi,
  o'quvchilar ro'yxatining eksport qilinishi va boshqalar. Filtrlar:
  **"Harakat turi"**, **"Obyekt turi"**, **"Sanadan"**, **"Sanagacha"**;
  har bir yozuv uchun **"Batafsil"** tugmasi bilan o'zgarishlarni ko'rish mumkin.

---

## 8. Muammolar va yechimlar

### 8.1 O'quvchi tomonidagi muammolar

| O'quvchi ko'rayotgan xabar | Sabab | Nima qilish kerak |
|---------------------------|-------|-------------------|
| **"Havola ishlamayapti"** — *"Havola ishlamayapti, maktabingizdan yangisini so'rang."* | Havola noto'g'ri ko'chirilgan yoki **"Havolani yangilash"** bilan almashtirilgan | **"Maktablar"**dan **"Havolani nusxalash"** bilan joriy havolani oling va qayta yuboring |
| **"Test vaqtincha yopilgan"** | Maktab **"Nofaol"** holatda | Maktab qatorida **"Faollashtirish"** |
| **"Test hali tayyor emas"** — *"Bu maktab uchun test hali tayyorlanmagan..."* | Maktabga hech qanday ochiq test yo'q | Test sahifasidagi **"Biriktirish"** bo'limida maktabni tanlang yoki **"Barcha maktablarga"** qiling; testning o'zi nashr qilingan va faol bo'lishi kerak (3.1, 3.2) |
| **"Sen allaqachon testni topshirgansan..."** | Bu o'quvchi allaqachon ro'yxatdan o'tgan | To'g'ri xatti-harakat. Qayta topshirish kerak bo'lsa, muammoni tekshiring |
| **"Juda ko'p urinish bo'ldi..."** | Kunlik limit tugagan yoki juda tez-tez urinilgan | Maktab **"Tahrirlash"** → **"Kunlik ro'yxatdan o'tish limiti"**ni oshiring (2.2) |
| **"Sessiya muddati tugagan..."** | O'quvchi juda uzoq tanaffus qilgan | Qaytadan boshlashi kerak |
| Internet uzildi | — | Muammo emas: *"Internet yo'q — javoblaring saqlanmoqda, ulanish tiklanganda yuboriladi"*. Sahifani yopmasin |

### 8.2 Sizning tomondagi muammolar

| Holat | Nima qilish kerak |
|-------|-------------------|
| **Kira olmayapman: "Login yoki parol noto'g'ri"** | Katta/kichik harfga e'tibor bering. 5 marta xato — 15 daqiqa blok |
| **"Hisob 15 daqiqaga bloklandi"** | 15 daqiqa kuting, shoshib qayta urinmang |
| **Telefonim yo'qoldi, 2FA kodi yo'q** | **Zaxira kodlardan** birini "Tasdiqlash kodi (2FA)" maydoniga kiriting. Kodlar ham yo'q bo'lsa — faqat texnik mutaxassis yordam bera oladi (1.3) |
| **Maktabni o'chira olmayapman** | *"Bu maktabda o'quvchilar bor"*. Maktabni **"Nofaol qilish"** yetarli |
| **AI tahlil "tayyorlanmoqda"da qotib qoldi** | Bir necha daqiqa kuting. O'zgarmasa **"Qayta tahlil"**. Undan oldin **"AI sozlamalari"**da **"Aloqani tekshirish"** |
| **"Avtomatik shablon hisobot"** belgisi chiqdi | 6.3-bo'lim — bu AI matni emas |
| **"Faol AI provider topilmadi"** | **"AI sozlamalari"**da kamida bitta provayderga kalit kiriting va **"Faol"** qiling (6.1) |
| **"Biriktirish" saqlanmayapti: "Test arxivlangan"** | Arxivlangan testni biriktirib bo'lmaydi — nusxa oling yoki boshqa test tanlang (3.1) |
| **Anketani nashr qila olmayapman** | **"Nashrga to'sqinlik qilayotgan..."** ro'yxatiga qarang; ko'pincha talqin oraliqlari to'liq emas (4.4) |
| **Tizim testida savol qo'sha olmayapman** | Bu ataylab: 4.1 va 4.2. **"Nusxa olish"** bilan nusxa yarating |
| **"Eksport hali mavjud emas"** | Eksport xizmati javob bermayapti — birozdan keyin qayta urining, davom etsa texnik mutaxassisga murojaat qiling |
| **"Yozuv bir vaqtda boshqa joyda o'zgartirildi"** | Sahifani yangilab, amalni qaytadan bajaring |
| **"Server bilan bog'lanib bo'lmadi"** | Internet aloqasini tekshiring |

### 8.3 Boshqa muammolar

Yuqoridagi ro'yxatda yo'q muammo bo'lsa — texnik mutaxassisga murojaat qiling.
Ular uchun hujjatlar: [`README.md`](../README.md) va
[`13-deploy-va-infratuzilma.md`](13-deploy-va-infratuzilma.md).

Murojaat qilishdan oldin quyidagilarni yozib oling: qaysi sahifada bo'ldi, qanday
xabar chiqdi (aynan matnini), qachon va nima qilayotgan edingiz.

### 8.4 Hali mavjud bo'lmagan imkoniyatlar

Quyidagilar hozircha **yo'q**. Ular haqida so'ralsa, "hali qurilmagan" deng.

| Nima | Holat |
|------|-------|
| **"Sessiyalar"** bo'limi | **Olib tashlangan** (2026-09-23 egasi qarori) — u "O'quvchilar" bilan bir xil narsani ko'rsatardi. Eski havola ochilsa **"O'quvchilar"** ro'yxatiga o'tadi. Boshqaruv panelidagi **"Tahlil navbatida"** kartochkasi ham "O'quvchilar" ro'yxatini *"Tahlil qilinmoqda"* holati bilan ochadi. Bitta sessiyaning tafsiloti o'quvchi profilidagi **"Sessiyalar tarixi"** orqali ochiladi |
| **Maktab mas'uli uchun alohida login** | **Hali mavjud emas** va MVP rejasida ham yo'q. Bitta rol — superadmin |
| **2FA yoqishda QR kod rasmi** | **Hali mavjud emas.** Faqat matn ko'rinishidagi **"Maxfiy kalit"** beriladi, ilovaga qo'lda kiritiladi (1.3) |
| **Zaxira kodlarni qayta ko'rish yoki qayta yaratish** | **Hali mavjud emas.** Kodlar faqat bir marta ko'rsatiladi (1.3) |
| **Promptlarni boshqaruv panelidan tahrirlash** | **Hali mavjud emas.** **"AI sozlamalari" → "Promptlar"** faqat **"Ko'rish"** uchun |
| **AI hisobotini qo'lda tahrirlash** | **Hali mavjud emas.** Faqat **"Qayta tahlil"** bilan qayta yaratish mumkin |
| **Bir necha o'quvchining PDF hisobotini birdaniga yuklash** | **Hali mavjud emas.** PDF har bir o'quvchi profilidan alohida yuklanadi |
| **O'quvchini boshqa maktabga ko'chirish** | **Hali mavjud emas** |
| **Ekran skrinshotlari bu qo'llanmada** | **Hali mavjud emas.** Matnli tavsif ekranga to'liq mos keladi |

---

## 9. Maktab mas'uli uchun qisqa varaqa

> Bu bo'limni maktab mas'uliga alohida yuborish mumkin.

**Sizga nima berildi:** maktabingizga tegishli **shaxsiy havola** (`.../t/...` ko'rinishida)
va, ehtimol, uning **QR kodi**. Balki qo'shimcha **6 xonali kirish kodi** ham berilgan.

**Sizning ishingiz — 5 qadam:**

1. **Havolani o'quvchilarga yetkazing.** QR kodni chop etib sinf doskasiga yoki e'lon
   taxtasiga osing — o'quvchi telefon kamerasi bilan ochadi. Havolani sinf guruhiga
   yuborish ham mumkin.
2. **Kirish kodi berilgan bo'lsa**, uni **og'zaki**, sinfda ayting. Havola bilan bitta
   xabarda yubormang.
3. **O'quvchilarga tushuntiring:**
   - Bu **baho emas**. Tizimda shunday yozilgan: *"Bu test baho emas, shuning uchun
     tashvishlanma"* va *"Bu yerda to'g'ri yoki noto'g'ri javob yo'q — faqat senga xos
     javoblar bor."*
   - Shoshmasin — **diqqat bilan** javob bergan o'quvchining natijasi foydali bo'ladi.
     Tez-tez bosib chiqilgan javoblarni tizim sezadi va bunday natija hisobga olinmaydi.
   - Test bir necha blokdan iborat; **bir o'tirishda tugatish shart emas**, keyinroq
     davom ettirsa bo'ladi (**"Keyinroq davom ettiraman"** tugmasi bor).
   - **Internet uzilsa javoblar yo'qolmaydi** — sahifani yopmasin, ulanish tiklanganda
     o'zi yuboriladi.
4. **Sharoit yarating:** tinch xona, yetarli vaqt (~20–40 daqiqa, testga qarab),
   zaryadlangan telefon yoki kompyuter. Dars oxirining oxirgi 5 daqiqasida
   **topshirtirmang** — natija ishonchsiz chiqadi.
5. **Bir necha test ko'rinsa** — o'quvchiga **qaysi birini** tanlash kerakligini
   oldindan ayting, aks holda har kim boshqasini tanlaydi.

**O'quvchi nima ko'radi:** havolani ochadi → qisqa anketa (F.I.Sh., tug'ilgan sana,
jinsi, sinf, **ota-ona telefoni — majburiy**; o'quvchining shaxsiy raqami ("Shaxsiy
raqamingiz (bo'lsa)") va email ixtiyoriy — *2026-09-23 egasi qarori*) → rozilik belgisi →
**"Testni boshlash"** → savollar bloklari → oxirida qisqacha natija
("Kuchli tomonlaring" va "Senga mos yo'nalishlar").

**Nima sizga ko'rinmaydi:** to'liq natijalar, ballar va tahlil **faqat platforma
mas'uliga (psixologga)** ko'rinadi. Sizda boshqaruv paneliga kirish huquqi yo'q.
Natijalar kerak bo'lsa, platforma mas'uliga murojaat qiling — u har bir o'quvchi uchun
PDF hisobot tayyorlab bera oladi.

**Muammo chiqsa:**

| O'quvchi ko'rgan xabar | Nima qilish |
|------------------------|-------------|
| *"Havola ishlamayapti..."* | Platforma mas'ulidan yangi havola so'rang |
| *"Test vaqtincha yopilgan"* | Platforma mas'uliga xabar bering — maktab nofaol qilingan |
| *"Bu maktab uchun test hali tayyorlanmagan..."* | Platforma mas'uliga xabar bering — maktabga test biriktirilmagan |
| *"Juda ko'p urinish bo'ldi..."* | Kunlik limit tugagan. Ertaga davom ettiring yoki limitni oshirishni so'rang |
| *"Sen allaqachon testni topshirgansan..."* | O'quvchi allaqachon topshirgan. Qayta kerak bo'lsa, platforma mas'uliga murojaat qiling |

---

## Ilova: chap menyudagi bo'limlar

| Menyu bandi | Qaysi bo'limda tavsiflangan |
|-------------|------------------------------|
| **"Boshqaruv paneli"** | 2.7 |
| **"Maktablar"** | 2 |
| **"O'quvchilar"** | 5.1, 5.4 |
| **"Testlar katalogi"** | 3 (biriktirish), 4 |
| **"AI sozlamalari"** | 6 |
| **"Audit jurnali"** | 7.3 |
| **"Sozlamalar"** | 1.2, 1.3 |
