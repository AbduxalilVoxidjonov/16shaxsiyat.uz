/**
 * Ommaviy makon (`SchoolKind.PublicSpace`) uchun umumiy konstantalar — P47.
 *
 * Maktabsiz sessiya (`POST /api/me/sessions`, `docs/07` §5.4) ochilgach foydalanuvchi
 * **maktab oqimidagi AYNAN o'sha** test sahifalarida davom etadi (`/t/:slug/test/...`,
 * `X-Session-Token` bilan) — parallel oqim YO'Q. Route'da esa `:slug` bo'lishi shart va u
 * sessiya store'idagi qiymat bilan mos kelishi kerak (`storedSlug === slug` tekshiruvi),
 * shu sabab ommaviy sessiyaga ham slug kerak bo'ladi.
 *
 * Qiymat backenddagi seed konstantasining nusxasi:
 * `StudentRoadMap.Infrastructure/Persistence/Seeding/DbSeeder.PublicSpaceSlug = "ommaviy"`.
 * U yerda slug unikal va ommaviy makon bitta bo'lgani uchun hech qanday MAKTAB bu slugni
 * ololmaydi — ya'ni bu qiymat maktab oqimi bilan hech qachon to'qnashmaydi.
 *
 * Backend uni javobda qaytarmaydi (`StartSessionResult` da `slug` maydoni yo'q), shu sabab
 * bu yerda nusxa saqlanadi. Seed konstantasi o'zgarsa, shu fayl ham yangilanishi kerak.
 */
export const PUBLIC_SPACE_SLUG = 'ommaviy';
