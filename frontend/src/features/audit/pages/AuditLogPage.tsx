import { useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Eye } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { useServerTableState } from '@/shared/hooks/useServerTableState';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { Button } from '@/shared/ui/Button';
import { useAuditLogsQuery } from '../api/useAuditLogsQuery';
import { AuditFiltersBar } from '../components/AuditFiltersBar';
import { AuditDetailDialog } from '../components/AuditDetailDialog';
import { readAuditFilters } from '../model/auditFilters';
import { findAuditActionLabelKey, findAuditEntityTypeLabelKey } from '../model/auditActions';
import { formatDateTime } from '../model/formatDateTime';
import type { AdminAuditLogItemDto, AuditListQuery } from '../model/types';

/**
 * `/admin/audit` — audit jurnali (`docs/11` A-9, `prompts/29` audit qismi). Faqat o'qish
 * uchun (o'chirish/tahrirlash imkoni yo'q). Backend saralashni QO'LLAMAYDI
 * (`ListAuditLogsQueryHandler` izohi — doim `Id DESC`, "sort" query parametri umuman yo'q),
 * shu sabab jadval ustunlari sortable emas.
 */
export default function AuditLogPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.audit.title'));

  const [searchParams] = useSearchParams();
  const filters = readAuditFilters(searchParams);
  const { page, pageSize, sort, setPage, setSort } = useServerTableState();

  const query: AuditListQuery = {
    action: filters.action || undefined,
    entityType: filters.entityType || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    page,
    pageSize,
  };

  const auditQuery = useAuditLogsQuery(query);
  const [viewing, setViewing] = useState<AdminAuditLogItemDto | null>(null);

  const columns: Array<DataTableColumn<AdminAuditLogItemDto>> = [
    {
      id: 'createdAt',
      header: t('audit.table.createdAt'),
      cell: (row) => formatDateTime(row.createdAt),
    },
    {
      id: 'adminUser',
      header: t('audit.table.adminUser'),
      cell: (row) => (
        <span className="font-mono text-xs">
          {row.adminUserId ? row.adminUserId.slice(0, 8) : t('audit.detail.unknownUser')}
        </span>
      ),
    },
    {
      id: 'action',
      header: t('audit.table.action'),
      cell: (row) => {
        const labelKey = findAuditActionLabelKey(row.action);
        return labelKey ? t(labelKey) : row.action;
      },
    },
    {
      id: 'entityType',
      header: t('audit.table.entityType'),
      cell: (row) => {
        if (!row.entityType) return t('audit.detail.noEntity');
        const labelKey = findAuditEntityTypeLabelKey(row.entityType);
        return labelKey ? t(labelKey) : row.entityType;
      },
    },
    {
      id: 'entityId',
      header: t('audit.table.entityId'),
      cell: (row) => (
        <span className="font-mono text-xs text-neutral-500">
          {row.entityId ? row.entityId.slice(0, 8) : '—'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: t('audit.table.actions'),
      cell: (row) => (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => setViewing(row)}
          aria-label={t('audit.table.viewDetail')}
        >
          <Eye size={14} aria-hidden="true" />
          {t('audit.table.viewDetail')}
        </Button>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold text-neutral-900">{t('pages.audit.title')}</h1>

      <AuditFiltersBar />

      <DataTable
        columns={columns}
        rows={auditQuery.data?.items ?? []}
        rowKey={(row) => String(row.id)}
        total={auditQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={auditQuery.isPending}
        isError={auditQuery.isError}
        onRetry={() => void auditQuery.refetch()}
        ariaLabel={t('audit.table.ariaLabel')}
        emptyTitle={t('audit.table.emptyTitle')}
        emptyDescription={t('audit.table.emptyDescription')}
      />

      <AuditDetailDialog entry={viewing} onClose={() => setViewing(null)} />
    </div>
  );
}
