import { Navigate } from 'react-router';
import { ROUTES } from '@/shared/config/routes';

/**
 * `/admin/programs` va `/admin/programs/:id` → `/admin/catalog` (egasining qarori,
 * 2026-09-23: "Testlar katalogi" va "Dasturlar" bir xil narsa edi — "Dasturlar" bo'limi
 * olib tashlandi, biriktirish endi test ichidagi "Biriktirish" kartasida).
 *
 * Dastur ID'sini testga aylantirib bo'lmaydi (`/api/admin/programs/*` yo'q), shu sabab
 * detal URL ham katalog ro'yxatiga olib boradi. `replace` — "orqaga" tugmasi foydalanuvchini
 * yana shu yo'naltirishga qaytarmasin (`LegacyAssessmentsRedirect` bilan bir xil naqsh).
 */
export function LegacyProgramsRedirect() {
  return <Navigate to={ROUTES.admin.catalog} replace />;
}
