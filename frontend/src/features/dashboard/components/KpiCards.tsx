import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ROUTES } from '@/shared/config/routes';
import type { DashboardTotals } from '../model/types';

export interface KpiCardsProps {
  totals: DashboardTotals | undefined;
  isLoading: boolean;
}

function KpiCard({
  label,
  value,
  hint,
  isLoading,
  onClick,
}: {
  label: string;
  value: string;
  hint?: string;
  isLoading: boolean;
  onClick?: () => void;
}) {
  const content = (
    <>
      <span className="text-xs font-medium text-neutral-500">{label}</span>
      {isLoading ? (
        <Skeleton className="h-8 w-16" />
      ) : (
        <span className="text-2xl font-bold text-neutral-900">{value}</span>
      )}
      {hint && !isLoading && <span className="text-xs text-neutral-500">{hint}</span>}
    </>
  );

  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        className="flex flex-col gap-1 rounded-xl border border-neutral-200 bg-white p-4 text-left shadow-sm hover:bg-neutral-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600"
      >
        {content}
      </button>
    );
  }

  return <Card className="flex flex-col gap-1">{content}</Card>;
}

/**
 * KPI kartalar — docs/11 A-2 wireframe (4 karta) + vazifa ko'rsatmasi 1-band: "maktablar
 * (faol/jami), o'quvchilar, yakunlangan sessiyalar, tahlil navbatida, e'tibor talab
 * qiladiganlar" (5 karta — wireframe eskirgan, ko'rsatma to'liqroq). "Tahlil navbatida"
 * va "e'tibor talab qiladi" kartalari bosilganda O'quvchilar ro'yxatiga tegishli filtr bilan
 * o'tadi: `?status=Analyzing` ("shu holatdagi sessiyasi BOR o'quvchilar", `docs/07` 3.2) va
 * `?needsAttention=true`. Sessiyalar ro'yxati sahifasi 2026-09-23 da olib tashlangan
 * (egasining qarori — O'quvchilar bilan bir xil edi), shu sabab ikkala karta ham shu yerga.
 */
export function KpiCards({ totals, isLoading }: KpiCardsProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div
      data-testid="dashboard-kpi-cards"
      className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5"
    >
      {/*
        Maktablar kartasi FAQAT maktab kesimida (P48). Ommaviy kesimda backend `schools`/
        `activeSchools` ni `null` qilib qaytaradi ("bu kesimda bunday ko'rsatkich YO'Q") —
        `—` ko'rsatish emas, kartani BUTUNLAY yashirish to'g'ri: `—` "ma'lumot hali yo'q"
        degan boshqa ma'noni beradi (`docs/06` §8 qoidasi).
      */}
      {totals?.schools != null && (
        <KpiCard
          label={t('dashboard.kpi.schools')}
          value={String(totals.schools)}
          hint={t('dashboard.kpi.schoolsHint', {
            active: totals.activeSchools ?? 0,
            total: totals.schools,
          })}
          isLoading={isLoading}
        />
      )}
      <KpiCard
        label={t('dashboard.kpi.students')}
        value={totals ? String(totals.students) : '—'}
        isLoading={isLoading}
      />
      <KpiCard
        label={t('dashboard.kpi.completedAssessments')}
        value={totals ? String(totals.completedAssessments) : '—'}
        isLoading={isLoading}
      />
      <KpiCard
        label={t('dashboard.kpi.pendingAnalysis')}
        value={totals ? String(totals.pendingAnalysis) : '—'}
        hint={t('dashboard.kpi.pendingAnalysisHint')}
        isLoading={isLoading}
        onClick={() => navigate(`${ROUTES.admin.students}?status=Analyzing`)}
      />
      <KpiCard
        label={t('dashboard.kpi.needsAttention')}
        value={totals ? String(totals.needsAttention) : '—'}
        hint={t('dashboard.kpi.needsAttentionHint')}
        isLoading={isLoading}
        onClick={() => navigate(`${ROUTES.admin.students}?needsAttention=true`)}
      />
    </div>
  );
}
