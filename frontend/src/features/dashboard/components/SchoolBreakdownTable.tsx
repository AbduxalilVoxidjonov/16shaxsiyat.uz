import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Card } from '@/shared/ui/Card';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Button } from '@/shared/ui/Button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { ROUTES } from '@/shared/config/routes';
import { formatDate } from '@/shared/lib/formatDate';
import type { SchoolBreakdownItem } from '../model/types';

export interface SchoolBreakdownTableProps {
  /**
   * Backend endi bu maydonni har doim qaytaradi (PM tasdig'i 2026-09-02: "funnel/
   * schoolBreakdown — endi ular doim keladi", `schema.d.ts`: `AdminDashboardStatsDto.
   * schoolBreakdown` majburiy) — shu sabab `| undefined` YO'Q. Ma'lumot yo'qligi
   * haqiqiy holat orqali ifodalanadi: bo'sh massiv (`length === 0`, hali maktab yo'q).
   */
  schoolBreakdown: SchoolBreakdownItem[];
}

/**
 * Maktablar kesimi — vazifa ko'rsatmasi 3-band: "nomi, viloyat, ochilgan, ro'yxatdan
 * o'tgan, yakunlagan, yakunlash foizi, oxirgi faollik. Qatorga bosilsa maktab sahifasiga
 * o'tsin." Alohida maktab profili ekrani hali yo'q (`docs/10`, 3-bo'lim — faqat
 * `/admin/schools` ro'yxati bor) — shu sabab qatorga bosilganda maktablar ro'yxatiga shu
 * maktab nomi bo'yicha filtrlab o'tkaziladi (`SchoolFiltersBar`ning mavjud `search`
 * filtri, P23) — PM tasdig'i 2026-09-02: "alohida maktab sahifasi rejalashtirilmagan;
 * qidiruv filtri orqali ko'rsatish yetarli".
 *
 * `completionRate` — **ulush (0..1)**, foiz emas: ko'rsatishdan oldin `× 100` qilinadi
 * (`dropOffRate` bilan bir xil birlik — `AdminDashboardMath.CompletionRate` = `completed / registered`).
 * Bu bir marta xato qilingan joy: `× 100` siz 50% yakunlagan maktab jadvalda `1%` bo'lib ko'rinardi.
 * `number | null` (`registered === 0` bo'lganda backend `null`
 * qaytaradi, PM tasdig'i 2026-09-02: avval `0` edi, bu "0% yakunladi" va "hali hech kim
 * ro'yxatdan o'tmagan"ni chalkashtirardi) — `null` bo'lsa `'—'`, `0%` EMAS.
 *
 * `DataTable` emas, oddiy `Table` primitivlari ishlatiladi — bu ro'yxat serverda
 * sahifalanmaydi, ko'pi bilan 20 qator (backend shartnomasi) va saralash talab
 * qilinmagan (vazifa ko'rsatmasida yo'q), shu sabab `DataTable`ning
 * sahifalash/saralash infratuzilmasi ortiqcha bo'lardi.
 */
export function SchoolBreakdownTable({ schoolBreakdown }: SchoolBreakdownTableProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  if (schoolBreakdown.length === 0) {
    return (
      <Card title={t('dashboard.schoolBreakdown.heading')}>
        <EmptyState
          title={t('dashboard.schoolBreakdown.emptyTitle')}
          description={t('dashboard.schoolBreakdown.emptyDescription')}
          action={
            <Button size="sm" onClick={() => navigate(ROUTES.admin.schools)}>
              {t('dashboard.schoolBreakdown.addSchoolCta')}
            </Button>
          }
        />
      </Card>
    );
  }

  function goToSchool(name: string) {
    navigate(`${ROUTES.admin.schools}?search=${encodeURIComponent(name)}`);
  }

  return (
    <Card title={t('dashboard.schoolBreakdown.heading')}>
      <Table aria-label={t('dashboard.schoolBreakdown.table.ariaLabel')}>
        <TableHeader>
          <TableRow>
            <TableHead>{t('dashboard.schoolBreakdown.table.name')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.region')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.linkViews')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.registered')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.completed')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.completionRate')}</TableHead>
            <TableHead>{t('dashboard.schoolBreakdown.table.lastActivityAt')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {schoolBreakdown.map((row) => (
            <TableRow
              key={row.schoolId}
              onClick={() => goToSchool(row.name)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  goToSchool(row.name);
                }
              }}
              tabIndex={0}
              role="button"
              className="cursor-pointer"
            >
              <TableCell className="font-medium text-neutral-900">{row.name}</TableCell>
              <TableCell>{row.region}</TableCell>
              <TableCell>{row.linkViews}</TableCell>
              <TableCell>{row.registered}</TableCell>
              <TableCell>{row.completed}</TableCell>
              <TableCell>
                {row.completionRate !== null && row.completionRate !== undefined
                  ? `${(row.completionRate * 100).toFixed(0)}%`
                  : '—'}
              </TableCell>
              <TableCell>{formatDate(row.lastActivityAt)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  );
}
