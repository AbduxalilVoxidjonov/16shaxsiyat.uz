import { useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Power, RefreshCw, Trash2 } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useServerTableState } from '@/shared/hooks/useServerTableState';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { useToast } from '@/shared/ui/useToast';
import { useSchoolsQuery } from '../api/useSchoolsQuery';
import { useSchoolDetailQuery } from '../api/useSchoolDetailQuery';
import { useToggleSchoolActive } from '../api/useToggleSchoolActive';
import { SchoolFiltersBar } from '../components/SchoolFiltersBar';
import { readSchoolsFilters } from '../model/schoolsFilters';
import { SchoolLinkCell } from '../components/SchoolLinkCell';
import { SchoolFormDrawer } from '../components/SchoolFormDrawer';
import { RegenerateLinkDialog } from '../components/RegenerateLinkDialog';
import { DeleteSchoolDialog } from '../components/DeleteSchoolDialog';
import { SchoolQrModal, type SchoolQrModalData } from '../components/SchoolQrModal';
import type { SchoolListItemDto, SchoolsListQuery } from '../model/types';

interface FormDrawerState {
  open: boolean;
  schoolId: string | null;
}

interface TargetSchool {
  id: string;
  name: string;
}

type QrModalState =
  | { kind: 'closed' }
  | { kind: 'byId'; schoolId: string; schoolName: string; slug: string }
  | { kind: 'manual'; data: SchoolQrModalData };

function toSortParam(sort: { columnId: string; direction: 'asc' | 'desc' } | null): string | undefined {
  if (!sort) return undefined;
  return sort.direction === 'desc' ? `-${sort.columnId}` : sort.columnId;
}

/**
 * `regenerate-link` javobi slug qaytarmaydi (faqat `publicUrl`/`qrCodeBase64`, docs/07 3.1) —
 * QR fayl nomi uchun URL yo'lidan (`/t/{slug}?k=...`) oxirgi segmentni ajratib olamiz.
 */
function slugFromPublicUrl(url: string): string {
  try {
    const segments = new URL(url).pathname.split('/').filter(Boolean);
    return segments[segments.length - 1] ?? 'maktab';
  } catch {
    return 'maktab';
  }
}

/** Jadval qatoridagi ikonka-tugma — amallar ustuni (`⋯`, docs/11 A-3). */
function RowActionButton({
  icon: Icon,
  label,
  onClick,
  danger = false,
}: {
  icon: typeof Pencil;
  label: string;
  onClick: () => void;
  danger?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className={
        danger
          ? 'rounded-md p-1.5 text-danger-500 hover:bg-danger-50 hover:text-danger-700'
          : 'rounded-md p-1.5 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900'
      }
    >
      <Icon size={16} aria-hidden="true" />
    </button>
  );
}

export default function SchoolsPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.schools.title'));
  const toast = useToast();

  const [searchParams] = useSearchParams();
  const filters = readSchoolsFilters(searchParams);
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({
    defaultSort: { columnId: 'name', direction: 'asc' },
  });

  const query: SchoolsListQuery = {
    search: filters.search || undefined,
    region: filters.region || undefined,
    isActive: filters.active === '' ? undefined : filters.active === 'true',
    page,
    pageSize,
    sort: toSortParam(sort),
  };

  const schoolsQuery = useSchoolsQuery(query);
  const toggleActive = useToggleSchoolActive();

  const [formState, setFormState] = useState<FormDrawerState>({ open: false, schoolId: null });
  const [regenerateTarget, setRegenerateTarget] = useState<TargetSchool | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<TargetSchool | null>(null);
  const [qrState, setQrState] = useState<QrModalState>({ kind: 'closed' });

  const qrByIdSchoolId = qrState.kind === 'byId' ? qrState.schoolId : null;
  const qrDetailQuery = useSchoolDetailQuery(qrByIdSchoolId);

  const qrModalData: SchoolQrModalData | null =
    qrState.kind === 'manual'
      ? qrState.data
      : qrState.kind === 'byId' && qrDetailQuery.data
        ? {
            schoolName: qrState.schoolName,
            slug: qrState.slug,
            publicUrl: qrDetailQuery.data.publicUrl,
            qrCodeBase64: qrDetailQuery.data.qrCodeBase64,
          }
        : null;

  async function handleToggleActive(row: SchoolListItemDto) {
    try {
      await toggleActive.mutateAsync(row.id);
      toast.show({
        variant: 'success',
        title: row.isActive
          ? t('schools.toggleActive.deactivateSuccess')
          : t('schools.toggleActive.activateSuccess'),
      });
    } catch {
      toast.show({ variant: 'danger', title: t('schools.toggleActive.error') });
    }
  }

  const columns: Array<DataTableColumn<SchoolListItemDto>> = [
    {
      id: 'name',
      header: t('schools.table.name'),
      sortable: true,
      cell: (row) => <span className="font-medium text-neutral-900">{row.name}</span>,
    },
    {
      id: 'location',
      header: t('schools.table.location'),
      cell: (row) => `${row.region}, ${row.district}`,
    },
    {
      id: 'link',
      header: t('schools.table.link'),
      cell: (row) => (
        <SchoolLinkCell
          publicUrl={row.publicUrl}
          onShowQr={() =>
            setQrState({ kind: 'byId', schoolId: row.id, schoolName: row.name, slug: row.slug })
          }
        />
      ),
    },
    { id: 'studentCount', header: t('schools.table.students'), cell: (row) => row.studentCount },
    { id: 'completedCount', header: t('schools.table.completed'), cell: (row) => row.completedCount },
    {
      id: 'isActive',
      header: t('schools.table.status'),
      cell: (row) => (
        <Badge variant={row.isActive ? 'success' : 'neutral'}>
          {row.isActive ? t('schools.statusBadge.active') : t('schools.statusBadge.inactive')}
        </Badge>
      ),
    },
    {
      id: 'actions',
      header: t('schools.table.actions'),
      cell: (row) => (
        <div className="flex items-center gap-1">
          <RowActionButton
            icon={Pencil}
            label={t('schools.actions.edit')}
            onClick={() => setFormState({ open: true, schoolId: row.id })}
          />
          <RowActionButton
            icon={RefreshCw}
            label={t('schools.actions.regenerateLink')}
            onClick={() => setRegenerateTarget({ id: row.id, name: row.name })}
          />
          <RowActionButton
            icon={Power}
            label={row.isActive ? t('schools.actions.deactivate') : t('schools.actions.activate')}
            onClick={() => void handleToggleActive(row)}
          />
          <RowActionButton
            icon={Trash2}
            label={t('schools.actions.delete')}
            onClick={() => setDeleteTarget({ id: row.id, name: row.name })}
            danger
          />
        </div>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-neutral-900">{t('pages.schools.title')}</h1>
        <Button onClick={() => setFormState({ open: true, schoolId: null })}>
          <Plus size={16} aria-hidden="true" />
          {t('schools.actions.create')}
        </Button>
      </div>

      <SchoolFiltersBar />

      <DataTable
        columns={columns}
        rows={schoolsQuery.data?.items ?? []}
        rowKey={(row) => row.id}
        total={schoolsQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={schoolsQuery.isPending}
        isError={schoolsQuery.isError}
        onRetry={() => void schoolsQuery.refetch()}
        ariaLabel={t('schools.table.ariaLabel')}
        emptyTitle={t('schools.table.emptyTitle')}
        emptyDescription={t('schools.table.emptyDescription')}
      />

      <SchoolFormDrawer
        open={formState.open}
        schoolId={formState.schoolId}
        onClose={() => setFormState({ open: false, schoolId: null })}
      />

      {regenerateTarget && (
        <RegenerateLinkDialog
          open
          schoolId={regenerateTarget.id}
          schoolName={regenerateTarget.name}
          onClose={() => setRegenerateTarget(null)}
          onSuccess={(result) => {
            const target = regenerateTarget;
            if (!target) return;
            setQrState({
              kind: 'manual',
              data: {
                schoolName: target.name,
                slug: slugFromPublicUrl(result.publicUrl),
                publicUrl: result.publicUrl,
                qrCodeBase64: result.qrCodeBase64,
              },
            });
          }}
        />
      )}

      {deleteTarget && (
        <DeleteSchoolDialog
          open
          schoolId={deleteTarget.id}
          schoolName={deleteTarget.name}
          onClose={() => setDeleteTarget(null)}
        />
      )}

      <SchoolQrModal
        open={qrState.kind !== 'closed'}
        onClose={() => setQrState({ kind: 'closed' })}
        data={qrModalData}
        isLoading={qrState.kind === 'byId' && qrDetailQuery.isPending}
        isError={qrState.kind === 'byId' && qrDetailQuery.isError}
        onRetry={() => void qrDetailQuery.refetch()}
      />
    </div>
  );
}
