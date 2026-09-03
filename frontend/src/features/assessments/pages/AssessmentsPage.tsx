import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useServerTableState, type DataTableSort } from '@/shared/hooks/useServerTableState';
import { Badge } from '@/shared/ui/Badge';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { ROUTES } from '@/shared/config/routes';
import { ReliabilityBadge } from '@/widgets/ReliabilityBadge';
import { useAssessmentsQuery } from '../api/useAssessmentsQuery';
import { AssessmentFiltersBar } from '../components/AssessmentFiltersBar';
import { readAssessmentsFilters } from '../model/assessmentsFilters';
import { ASSESSMENT_STATUS_BADGE_VARIANT, formatDateTime } from '../model/sessionMeta';
import {
  isAssessmentStatus,
  isReliabilityFlag,
  type AssessmentListItemDto,
  type AssessmentsListQuery,
} from '../model/types';

/** Ma'lumot yo'qligining ko'rinishi — jadval katagida qisqa belgi (`0` yoki bo'sh satr EMAS). */
const UNKNOWN = '—';

function toSortParam(sort: DataTableSort | null): string | undefined {
  if (!sort) return undefined;
  return sort.direction === 'desc' ? `-${sort.columnId}` : sort.columnId;
}

/**
 * `/admin/assessments` — sessiyalar ro'yxati (`docs/11` A-6, `docs/07` 3.3-bo'lim).
 *
 * Boshqaruv panelidagi "Tahlil navbatida" kartasi shu sahifaga `?status=Analyzing` bilan
 * o'tadi — filtr URL'dan o'qilgani uchun (`readAssessmentsFilters`) darhol qo'llanadi.
 * Qatorga bosilganda sessiya detali ochiladi va qator navigatsiya holati sifatida
 * uzatiladi: detal endpointi sarlavha maydonlarini (holat, vaqt, maktab, o'quvchi)
 * qaytarmaydi, shu sabab ular aynan shu yo'l bilan yetkaziladi.
 */
export default function AssessmentsPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.assessments.title'));
  const navigate = useNavigate();

  const [searchParams] = useSearchParams();
  const filters = readAssessmentsFilters(searchParams);
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({
    defaultSort: { columnId: 'startedAt', direction: 'desc' },
  });

  const query: AssessmentsListQuery = {
    schoolId: filters.schoolId || undefined,
    status: filters.status || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    page,
    pageSize,
    sort: toSortParam(sort),
  };

  const assessmentsQuery = useAssessmentsQuery(query);

  const columns: Array<DataTableColumn<AssessmentListItemDto>> = [
    {
      id: 'studentName',
      header: t('assessments.table.student'),
      cell: (row) => <span className="font-medium text-neutral-900">{row.studentName}</span>,
    },
    { id: 'schoolName', header: t('assessments.table.school'), cell: (row) => row.schoolName },
    {
      id: 'programName',
      header: t('assessments.table.program'),
      cell: (row) => row.programName ?? UNKNOWN,
    },
    {
      id: 'status',
      header: t('assessments.table.status'),
      cell: (row) =>
        isAssessmentStatus(row.status) ? (
          <Badge variant={ASSESSMENT_STATUS_BADGE_VARIANT[row.status]}>
            {t(`assessmentDetail.enums.status.${row.status}`)}
          </Badge>
        ) : (
          UNKNOWN
        ),
    },
    {
      id: 'startedAt',
      header: t('assessments.table.startedAt'),
      sortable: true,
      cell: (row) => formatDateTime(row.startedAt) ?? UNKNOWN,
    },
    {
      id: 'completedAt',
      header: t('assessments.table.completedAt'),
      sortable: true,
      cell: (row) => formatDateTime(row.completedAt) ?? UNKNOWN,
    },
    {
      id: 'reliabilityScore',
      header: t('assessments.table.reliability'),
      sortable: true,
      // Ball hisoblanmagan bo'lsa `0` EMAS, belgi ko'rsatiladi (`docs/06`, 2026-09-02).
      cell: (row) =>
        isReliabilityFlag(row.reliabilityFlag) ? (
          <ReliabilityBadge flag={row.reliabilityFlag} score={row.reliabilityScore} />
        ) : row.reliabilityScore == null ? (
          UNKNOWN
        ) : (
          row.reliabilityScore.toFixed(1)
        ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold text-neutral-900">{t('pages.assessments.title')}</h1>

      <AssessmentFiltersBar />

      <DataTable
        columns={columns}
        rows={assessmentsQuery.data?.items ?? []}
        rowKey={(row) => row.id}
        total={assessmentsQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={assessmentsQuery.isPending}
        isError={assessmentsQuery.isError}
        onRetry={() => void assessmentsQuery.refetch()}
        onRowClick={(row) =>
          void navigate(ROUTES.admin.assessmentDetail(row.id), { state: { assessment: row } })
        }
        ariaLabel={t('assessments.table.ariaLabel')}
        emptyTitle={t('assessments.table.emptyTitle')}
        emptyDescription={t('assessments.table.emptyDescription')}
      />
    </div>
  );
}
