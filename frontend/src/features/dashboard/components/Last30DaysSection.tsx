import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import type { DashboardLast30Days } from '../model/types';

export interface Last30DaysSectionProps {
  data: DashboardLast30Days | undefined;
  isLoading: boolean;
}

function StatItem({ label, value, isLoading }: { label: string; value: string; isLoading: boolean }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs font-medium text-neutral-500">{label}</span>
      {isLoading ? (
        <Skeleton className="h-6 w-14" />
      ) : (
        <span className="text-lg font-semibold text-neutral-900">{value}</span>
      )}
    </div>
  );
}

/**
 * `number | null | undefined` uchun "ma'lumot bormi" tekshiruvi — `StudentSummaryCards.tsx`
 * (`features/students`) bilan bir xil naqsh. Backend nullable `double?` maydonlarni
 * Swashbuckle sxemasida optsional+`| null` qilib chiqaradi (`schema.d.ts`,
 * `AdminDashboardLast30DaysDto.avgReliability?: number | null`), garchi amalda maydonni
 * hech qachon tashlab ketmasa ham — shu sabab ikkalasi ham (`null` va `undefined`)
 * "ma'lumot yo'q" deb qaraladi.
 */
function hasValue(value: number | null | undefined): value is number {
  return value !== null && value !== undefined;
}

/**
 * Oxirgi 30 kun bloki — docs/07 3.6-bo'lim `last30Days`. `avgDurationMinutes`/
 * `avgReliability`/`dropOffRate` — uchalasi ham `null` bo'lishi mumkin (ma'lumot yo'q) —
 * CLAUDE.md "MAXSUS DIQQAT": "`null` va `0` farqi: ma'lumot bo'lmasa `0` emas, '—'
 * ko'rsatilsin" (`docs/06` §8, 2026-09-02 qoidasi, PM tasdig'i 2026-09-02: "O'rtacha
 * ishonchlilik 0" adminni ma'lumot sifati falokat deb o'ylashga majbur qilardi, aslida
 * hali sessiya yo'q edi — xuddi shu mantiq `dropOffRate`ga ham tegishli). `0` va `null`
 * shu sabab bu yerda qat'iy ravishda ajratiladi (`hasValue` tekshiruvi, `?? 0`
 * ISHLATILMAYDI).
 */
export function Last30DaysSection({ data, isLoading }: Last30DaysSectionProps) {
  const { t } = useTranslation();

  return (
    <Card title={t('dashboard.last30Days.heading')}>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-5">
        <StatItem
          label={t('dashboard.last30Days.newStudents')}
          value={data ? String(data.newStudents) : '—'}
          isLoading={isLoading}
        />
        <StatItem
          label={t('dashboard.last30Days.completed')}
          value={data ? String(data.completed) : '—'}
          isLoading={isLoading}
        />
        <StatItem
          label={t('dashboard.last30Days.avgDuration')}
          value={
            data && hasValue(data.avgDurationMinutes)
              ? t('dashboard.last30Days.avgDurationValue', {
                  minutes: data.avgDurationMinutes.toFixed(1),
                })
              : '—'
          }
          isLoading={isLoading}
        />
        <StatItem
          label={t('dashboard.last30Days.avgReliability')}
          value={data && hasValue(data.avgReliability) ? data.avgReliability.toFixed(1) : '—'}
          isLoading={isLoading}
        />
        <StatItem
          label={t('dashboard.last30Days.dropOffRate')}
          value={
            data && hasValue(data.dropOffRate) ? `${(data.dropOffRate * 100).toFixed(0)}%` : '—'
          }
          isLoading={isLoading}
        />
      </div>
    </Card>
  );
}
