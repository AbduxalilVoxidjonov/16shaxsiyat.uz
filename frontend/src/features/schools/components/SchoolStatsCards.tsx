import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { formatDate } from '@/shared/lib/formatDate';
import type { SchoolStatsDto } from '../model/types';

export interface SchoolStatsCardsProps {
  stats: SchoolStatsDto;
}

function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card className="flex flex-col gap-1">
      <span className="text-xs font-medium text-neutral-500">{label}</span>
      <span className="text-2xl font-bold text-neutral-900">{value}</span>
      {hint && <span className="text-xs text-neutral-500">{hint}</span>}
    </Card>
  );
}

/**
 * Maktab ichki sahifasidagi ISHTIROK statistikasi — egasining talabi: "maktabning ichiga
 * kirilganda, o'sha maktabda nechta odam test topshirgani ko'rinishi kerak".
 *
 * **Nol va "ma'lumot yo'q" ajratiladi:**
 * - hisoblagichlar (ro'yxatdan o'tgan / yakunlagan / jarayonda) — hech kim topshirmagan bo'lsa
 *   ham `0` ko'rsatiladi, `—` EMAS;
 * - `completionRate` — backend **ulush (0..1)** qaytaradi (`docs/07` 3.1), shu sabab bu yerda
 *   `× 100` qilinadi; `null`/yo'q (hali hech kim ro'yxatdan o'tmagan → nisbat aniqlanmagan)
 *   bo'lsa `—` ko'rsatiladi, `0%` emas — `dashboard`dagi `SchoolBreakdownTable` bilan bir xil qoida;
 * - `lastActivityAt` — `null` bo'lsa `formatDate` `—` qaytaradi.
 */
export function SchoolStatsCards({ stats }: SchoolStatsCardsProps) {
  const { t } = useTranslation();

  // `== null` — `null` VA `undefined` ikkalasini ham qamraydi: sxemada maydon ixtiyoriy
  // (`completionRate?: number | null`), ya'ni javobda umuman bo'lmasligi mumkin. Ilgari bu
  // yerda `=== null` turardi va maydon tushib qolganda `undefined * 100` → `NaN%` chiqardi.
  const completionRate =
    stats.completionRate == null ? '—' : `${(stats.completionRate * 100).toFixed(0)}%`;

  return (
    <section aria-label={t('schools.detail.stats.ariaLabel')}>
      <h2 className="mb-2 text-base font-semibold text-neutral-900">
        {t('schools.detail.stats.heading')}
      </h2>
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
        <StatCard
          label={t('schools.detail.stats.registered')}
          value={String(stats.studentCount)}
          hint={t('schools.detail.stats.registeredHint')}
        />
        <StatCard
          label={t('schools.detail.stats.completed')}
          value={String(stats.completedCount)}
          hint={t('schools.detail.stats.completedHint')}
        />
        <StatCard label={t('schools.detail.stats.completionRate')} value={completionRate} />
        <StatCard
          label={t('schools.detail.stats.inProgress')}
          value={String(stats.inProgressCount)}
          hint={t('schools.detail.stats.inProgressHint')}
        />
        <StatCard
          label={t('schools.detail.stats.lastActivity')}
          value={formatDate(stats.lastActivityAt)}
        />
      </div>
    </section>
  );
}
