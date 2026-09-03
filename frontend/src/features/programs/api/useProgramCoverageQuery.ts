/**
 * **BEKOR QILINDI (2026-09-03).**
 *
 * Bu hook maktab "dastursiz qoldi" ni KLIENT tomonda taxmin qilardi — bu IKKINCHI mezon edi
 * va ommaviy handler mezonidan (`ProgramAvailability`) farq qilardi: dasturda test bor-yo'qligini
 * ham, `Status`/`IsActive` ni ham to'liq hisobga olmasdi. Jonli hodisa (yagona dastur o'chirilgan,
 * panel esa "hammasi joyida" degan) aynan shu shakldagi xato edi.
 *
 * O'rniga backend hisoblaydi va yagona manba beradi:
 * - `SchoolListItemDto.linkHealth` / `SchoolDetailDto.linkHealth` (`docs/07` 3.1);
 * - `GET /api/admin/schools/link-health` (dashboard banneri);
 * - `GET /api/admin/programs/{id}/impact` (amaldan oldingi oqibat).
 *
 * Fayl ATAYIN saqlanib qoldi (o'chirilmadi) — kelajakda kimdir bu naqshni qayta ixtiro
 * qilmasligi uchun sabab shu yerda yozilgan.
 */
export {};
