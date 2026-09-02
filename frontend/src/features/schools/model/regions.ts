/**
 * O'zbekiston viloyatlari ro'yxati — "viloyat filtri" (docs/11-ux-va-ekranlar.md, A-3) va
 * yaratish/tahrirlash drawer'idagi "viloyat" maydoni uchun.
 *
 * **Taxmin (hisobotga qarang):** `docs/05-database-schema.md`da `schools.region` erkin
 * `varchar(100)` — standart ro'yxat DB darajasida cheklanmagan. Bu yerda 14 ta rasmiy
 * ma'muriy-hudud nomi qattiq yozilgan (backend qaytaradigan qiymat bilan aynan mos kelishi
 * kerak, masalan docs/07 3.1 misolidagi `"Farg'ona"`). Agar backend boshqacha ro'yxat yoki
 * erkin matn kutsa — PM/backend bilan kelishib shu faylni yangilash kifoya (bitta joy).
 */
export const UZBEKISTAN_REGIONS: readonly string[] = [
  'Andijon',
  'Buxoro',
  "Farg'ona",
  'Jizzax',
  'Namangan',
  'Navoiy',
  'Qashqadaryo',
  "Qoraqalpog'iston Respublikasi",
  'Samarqand',
  'Sirdaryo',
  'Surxondaryo',
  'Toshkent shahri',
  'Toshkent viloyati',
  'Xorazm',
];
