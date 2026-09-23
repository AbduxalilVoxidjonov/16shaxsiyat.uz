import { Navigate, useLocation } from 'react-router';
import { ROUTES } from '@/shared/config/routes';

/**
 * Eski sessiyalar ro'yxati filtrlari — `/admin/students` da AYNAN shu nom va ma'noda bor
 * (`docs/07` 3.2: `status` — "shu holatdagi sessiyasi BOR o'quvchilar", `schoolId`,
 * `from`/`to`). Sahifalash/saralash (`page`, `sort`) ataylab ko'chirilmaydi: saralash
 * ustunlari ikki jadvalda har xil.
 */
const CARRIED_OVER_PARAMS = ['status', 'schoolId', 'from', 'to'] as const;

/**
 * `/admin/assessments` → `/admin/students` (egasining qarori, 2026-09-23: "Sessiyalar"
 * bo'limi "O'quvchilar" bilan bir xil narsani ko'rsatardi va olib tashlandi). Eski bookmark
 * va havolalar buzilmasin deb marshrut qoladi; tushunarli filtrlar saqlanadi, `replace` —
 * "orqaga" tugmasi foydalanuvchini yana shu yo'naltirishga qaytarmasin.
 */
export function LegacyAssessmentsRedirect() {
  const { search } = useLocation();
  const source = new URLSearchParams(search);
  const target = new URLSearchParams();
  for (const key of CARRIED_OVER_PARAMS) {
    const value = source.get(key);
    if (value) target.set(key, value);
  }
  const query = target.toString();
  return (
    <Navigate to={query ? `${ROUTES.admin.students}?${query}` : ROUTES.admin.students} replace />
  );
}
