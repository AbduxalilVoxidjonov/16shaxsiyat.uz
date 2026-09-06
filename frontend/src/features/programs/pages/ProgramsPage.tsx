import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Lock, Plus } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useServerTableState } from '@/shared/hooks/useServerTableState';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { ROUTES } from '@/shared/config/routes';
import { useProgramsQuery } from '../api/useProgramsQuery';
import { ProgramFiltersBar } from '../components/ProgramFiltersBar';
import { ProgramFormDialog } from '../components/ProgramFormDialog';
import { readProgramsFilters } from '../model/programsFilters';
import {
  programStateBadgeVariant,
  programStateLabelKey,
  type AdminProgramListItem,
  type ProgramsListQuery,
} from '../model/types';

function toSortParam(
  sort: { columnId: string; direction: 'asc' | 'desc' } | null,
): string | undefined {
  if (!sort) return undefined;
  return sort.direction === 'desc' ? `-${sort.columnId}` : sort.columnId;
}

/**
 * Dasturlar ro'yxati — `prompts/35` C7-band: "nomi, turi (Tizim/Mening), ko'rinishi
 * (Ommaviy/Biriktirilgan), testlar soni, jami savol va vaqt, holat, biriktirilgan
 * maktablar soni". Jami savol/vaqt va biriktirilgan maktablar soni ro'yxat DTO'sida yo'q
 * (`AdminProgramListItemDto` — `docs/07`da hali yozilmagan yangi kontrakt, backend haqiqat
 * manbai `AdminProgramDtos.cs`); N+1 so'rovsiz ro'yxatda ko'rsatib bo'lmaydi (`docs/06` §8
 * "xotirada agregatsiya taqiqlangan" ruhi), shu sabab bu ikkala qiymat **detal sahifasida**
 * (`ProgramDetailPage`) to'liq ko'rsatiladi — hisobotda PM'ga qayd etilgan.
 */
export default function ProgramsPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.programs.title'));
  const navigate = useNavigate();

  const [searchParams] = useSearchParams();
  const filters = readProgramsFilters(searchParams);
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({
    defaultSort: { columnId: 'displayOrder', direction: 'asc' },
  });

  const query: ProgramsListQuery = {
    search: filters.search || undefined,
    state: filters.state || undefined,
    page,
    pageSize,
    sort: toSortParam(sort),
  };

  const programsQuery = useProgramsQuery(query);
  const [formOpen, setFormOpen] = useState(false);

  const columns: Array<DataTableColumn<AdminProgramListItem>> = [
    {
      id: 'name',
      header: t('programs.table.name'),
      sortable: true,
      cell: (row) => (
        <span className="flex items-center gap-1.5 font-medium text-neutral-900">
          {row.isSystem && <Lock size={14} className="text-neutral-400" aria-hidden="true" />}
          {row.nameUz}
        </span>
      ),
    },
    {
      id: 'code',
      header: t('programs.table.code'),
      cell: (row) => <span className="text-neutral-500">{row.code}</span>,
    },
    {
      id: 'kind',
      header: t('programs.table.kind'),
      cell: (row) => (
        <Badge variant={row.kind === 'System' ? 'neutral' : 'primary'}>
          {row.kind === 'System' ? t('programs.kind.system') : t('programs.kind.custom')}
        </Badge>
      ),
    },
    {
      id: 'visibility',
      header: t('programs.table.visibility'),
      cell: (row) =>
        row.visibility === 'Public'
          ? t('programs.visibility.public')
          : t('programs.visibility.assigned'),
    },
    { id: 'testCount', header: t('programs.table.testCount'), cell: (row) => row.testCount },
    // BITTA holat ustuni: ilgari bu yerda ikkita mustaqil belgi bor edi (`status` va
    // `isActive`) va bitta dastur bir vaqtda "Arxiv" ham, "Faol" ham bo'lib ko'rinardi.
    {
      id: 'state',
      header: t('programs.table.state'),
      cell: (row) => (
        <Badge variant={programStateBadgeVariant(row.state)}>
          {t(programStateLabelKey(row.state))}
        </Badge>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.programs.title')}</h1>
        <Button onClick={() => setFormOpen(true)}>
          <Plus size={16} aria-hidden="true" />
          {t('programs.actions.create')}
        </Button>
      </div>

      <ProgramFiltersBar />

      <DataTable
        columns={columns}
        rows={programsQuery.data?.items ?? []}
        rowKey={(row) => row.id}
        total={programsQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={programsQuery.isPending}
        isError={programsQuery.isError}
        onRetry={() => void programsQuery.refetch()}
        onRowClick={(row) => navigate(ROUTES.admin.programDetail(row.id))}
        ariaLabel={t('programs.table.ariaLabel')}
        emptyTitle={t('programs.table.emptyTitle')}
        emptyDescription={t('programs.table.emptyDescription')}
      />

      <ProgramFormDialog
        open={formOpen}
        programId={null}
        onClose={() => setFormOpen(false)}
        onCreated={(id) => navigate(ROUTES.admin.programDetail(id))}
      />
    </div>
  );
}
