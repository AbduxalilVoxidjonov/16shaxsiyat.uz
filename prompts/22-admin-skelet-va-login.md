# P22 — Admin panel skeleti va kirish

## O'qish shart
- `docs/11-ux-va-ekranlar.md` (A-1)
- `docs/10-frontend-arxitektura.md` (5.1)

## Vazifa
1. `AdminLayout` — chap sidebar (Dashboard, Maktablar, O'quvchilar, Sessiyalar, Katalog,
   AI, Audit, Sozlamalar), yuqori header (foydalanuvchi menyusi, chiqish), breadcrumb.
   Mobil: sidebar drawer sifatida.
2. **A-1 Login** (`/admin/login`) — markazda karta, username/parol, TOTP maydoni
   (faqat backend so'rasa), xato xabarlari, blokirovka holati.
3. `authStore` (Zustand): access token **faqat xotirada** (persist yo'q), foydalanuvchi ma'lumoti.
4. `adminClient` 401 ishlashi: bitta refresh chaqiruvi (mutex), muvaffaqiyatda so'rov qaytariladi,
   aks holda logout + `/admin/login?returnUrl=`.
5. `ProtectedRoute` — token yo'q bo'lsa login'ga; ilova ochilganda `GET /auth/me` bilan
   sessiyani tiklash urinishi.
6. Sozlamalar sahifasi skeleti: parol o'zgartirish, 2FA yoqish/o'chirish.
7. Umumiy `DataTable` komponenti: server-side pagination/sort, URL query bilan sinxron,
   skeleton loading, bo'sh holat.

## Cheklovlar
- Access token `localStorage` ga yozilmasin.
- Barcha admin sahifalari `ProtectedRoute` ostida.
- Sidebar'da hali tayyor bo'lmagan bo'limlar "tez orada" belgisi bilan.

## DoD
- [ ] Login → dashboard placeholder; noto'g'ri parol → xato; blokirovka xabari
- [ ] Token eskirganda avtomatik refresh (DevTools bilan tekshirilgan)
- [ ] Sahifa yangilanganda sessiya saqlanadi (refresh cookie orqali)
- [ ] `DataTable` demo ma'lumot bilan ishlaydi
- [ ] Mobil sidebar ochiladi/yopiladi

## Tekshiruv
```bash
cd frontend && npm run build
# qo'lda: login → refresh → chiqish → himoyalangan sahifaga kirishga urinish
```
