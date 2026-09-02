import { Download } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useServerTableState, type DataTableSort } from '@/shared/hooks/useServerTableState';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { ROUTES } from '@/shared/config/routes';
import { formatDate } from '@/shared/lib/formatDate';
import { useStudentsQuery } from '../api/useStudentsQuery';
import {
  EXPORT_NOT_AVAILABLE_CODE,
  useExportStudentsMutation,
} from '../api/useExportStudentsMutation';
import { StudentFiltersBar } from '../components/StudentFiltersBar';
import { readStudentsFilters } from '../model/studentsFilters';
import {
  ASSESSMENT_STATUS_BADGE_VARIANT,
  RELIABILITY_FLAG_BADGE_VARIANT,
} from '../model/enums';
import type { StudentListItemDto, StudentsListQuery } from '../model/types';

function toSortParam(sort: DataTableSort | null): string | undefined {
  if (!sort) return undefined;
  return sort.direction === 'desc' ? `-${sort.columnId}` : sort.columnId;
}

export default function StudentsPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.students.title'));
  const toast = useToast();
  const navigate = useNavigate();

  const [searchParams] = useSearchParams();
  const filters = readStudentsFilters(searchParams);
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({
    defaultSort: { columnId: 'lastAssessmentAt', direction: 'desc' },
  });

  const query: StudentsListQuery = {
    schoolId: filters.schoolId || undefined,
    grade: filters.grade ? Number(filters.grade) : undefined,
    status: filters.status || undefined,
    needsAttention: filters.needsAttention || undefined,
    personalityType: filters.personalityType || undefined,
    activityLevel: filters.activityLevel || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    search: filters.search || undefined,
    page,
    pageSize,
    sort: toSortParam(sort),
  };

  const studentsQuery = useStudentsQuery(query);
  const exportMutation = useExportStudentsMutation();

  async function handleExport() {
    try {
      await exportMutation.mutateAsync(query);
      toast.show({ variant: 'success', title: t('students.export.successTitle') });
    } catch (caught) {
      if (caught instanceof AppError && caught.code === EXPORT_NOT_AVAILABLE_CODE) {
        toast.show({
          variant: 'warning',
          title: t('students.export.notAvailableTitle'),
          description: t('students.export.notAvailableDescription'),
        });
        return;
      }
      toast.show({ variant: 'danger', title: t('students.export.errorTitle') });
    }
  }

  const columns: Array<DataTableColumn<StudentListItemDto>> = [
    {
      id: 'fullName',
      header: t('students.table.fullName'),
      sortable: true,
      cellClassName: 'relative p-0',
      cell: (row) => (
        <div className="relative flex items-center gap-2 px-4 py-3">
          {row.needsAttention && (
            <span
              aria-hidden="true"
              title={t('students.table.needsAttentionLabel')}
              data-testid="needs-attention-marker"
              className="absolute inset-y-0 left-0 w-1 rounded-r bg-warning-500"
            />
          )}
          <span className="font-medium text-neutral-900">{row.fullName}</span>
          {row.needsAttention && (
            <span className="sr-only">{t('students.table.needsAttentionLabel')}</span>
          )}
        </div>
      ),
    },
    { id: 'schoolName', header: t('students.table.school'), cell: (row) => row.schoolName },
    {
      id: 'grade',
      header: t('students.table.grade'),
      sortable: true,
      cell: (row) => (row.classLetter ? `${String(row.grade)}-${row.classLetter}` : String(row.grade)),
    },
    {
      id: 'status',
      header: t('students.table.status'),
      cell: (row) =>
        row.lastAssessmentStatus ? (
          <Badge variant={ASSESSMENT_STATUS_BADGE_VARIANT[row.lastAssessmentStatus]}>
            {t(`students.enums.status.${row.lastAssessmentStatus}`)}
          </Badge>
        ) : (
          '—'
        ),
    },
    {
      id: 'personalityType',
      header: t('students.table.personalityType'),
      cell: (row) => row.personalityType ?? '—',
    },
    {
      id: 'maturityIndex',
      header: t('students.table.maturityIndex'),
      cell: (row) => (row.maturityIndex === null ? '—' : row.maturityIndex.toFixed(1)),
    },
    {
      id: 'activityLevel',
      header: t('students.table.activityLevel'),
      cell: (row) =>
        row.activityLevel ? t(`students.enums.activityLevel.${row.activityLevel}`) : '—',
    },
    {
      id: 'reliabilityFlag',
      header: t('students.table.reliabilityFlag'),
      cell: (row) =>
        row.reliabilityFlag ? (
          <Badge variant={RELIABILITY_FLAG_BADGE_VARIANT[row.reliabilityFlag]}>
            {t(`students.enums.reliabilityFlag.${row.reliabilityFlag}`)}
          </Badge>
        ) : (
          '—'
        ),
    },
    {
      id: 'lastAssessmentAt',
      header: t('students.table.lastAssessmentAt'),
      sortable: true,
      cell: (row) => formatDate(row.lastAssessmentAt),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.students.title')}</h1>
        <Button
          variant="outline"
          onClick={() => void handleExport()}
          isLoading={exportMutation.isPending}
        >
          <Download size={16} aria-hidden="true" />
          {t('students.export.cta')}
        </Button>
      </div>

      <StudentFiltersBar />

      <DataTable
        columns={columns}
        rows={studentsQuery.data?.items ?? []}
        rowKey={(row) => row.id}
        total={studentsQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={studentsQuery.isPending}
        isError={studentsQuery.isError}
        onRetry={() => void studentsQuery.refetch()}
        onRowClick={(row) => navigate(ROUTES.admin.studentProfile(row.id))}
        ariaLabel={t('students.table.ariaLabel')}
        emptyTitle={t('students.table.emptyTitle')}
        emptyDescription={t('students.table.emptyDescription')}
      />
    </div>
  );
}
