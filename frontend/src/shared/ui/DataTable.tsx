import { type ReactNode, useCallback } from 'react';
import { ArrowDown, ArrowUp, ArrowUpDown, ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import type { DataTableSort, SortDirection } from '@/shared/hooks/useServerTableState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from './Table';
import { Skeleton } from './Skeleton';
import { EmptyState } from './EmptyState';
import { ErrorState } from './ErrorState';
import { Button } from './Button';

export type { DataTableSort, SortDirection };

export interface DataTableColumn<T> {
  /** Backend saralash parametri bilan bir xil bo'lishi shart (`sortable` bo'lsa). */
  id: string;
  header: ReactNode;
  cell: (row: T) => ReactNode;
  sortable?: boolean;
  headerClassName?: string;
  cellClassName?: string;
}

export interface DataTableProps<T> {
  columns: Array<DataTableColumn<T>>;
  rows: T[];
  rowKey: (row: T) => string;
  /** Jami yozuvlar soni (server javobidan) — sahifalash hisobi uchun. */
  total: number;
  page: number;
  pageSize: number;
  sort: DataTableSort | null;
  onSortChange: (sort: DataTableSort) => void;
  onPageChange: (page: number) => void;
  isLoading?: boolean;
  isError?: boolean;
  onRetry?: () => void;
  onRowClick?: (row: T) => void;
  emptyTitle?: string;
  emptyDescription?: string;
  /** Jadval uchun erishimlilik yorlig'i (`aria-label`) — ekran o'quvchisi jadval nomini aytadi. */
  ariaLabel: string;
  skeletonRowCount?: number;
}

const DEFAULT_SKELETON_ROWS = 5;

/**
 * Server-side sahifalash/saralash bilan ishlaydigan umumiy jadval — docs/10, 5.3-bo'lim.
 * Holat (`page`/`sort`) chaqiruvchi tomonda `useServerTableState` (URL query bilan
 * sinxron) orqali boshqariladi; bu komponent o'zi toza taqdimot qatlami — bitta ma'lumot
 * massivi va callback'lar bilan ishlaydi, hech qanday provayder yoki router kontekstiga
 * bog'liq emas (test/Storybook'da oddiy props bilan render qilinadi).
 *
 * Muhandislik qarori (PM tasdiqladi, P22 hisoboti): loyihada o'rnatilgan bo'lgan
 * `@tanstack/react-table` versiyasi (`v9.2.4`) `docs/10` 1-bo'limda ko'rsatilgan `v8`dan
 * butunlay boshqa (kompozitsion, `useTable`+`tableFeatures`) API edi — muvofiqlik xavfi
 * sabab bu komponent mavjud `shared/ui/Table.tsx` primitivlari ustida qo'lda yozilgan;
 * paket keyinchalik ishlatilmaydigan bog'liqlik sifatida `package.json`dan olib tashlandi
 * (`docs/10` PM tomonidan yangilanadi).
 */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  total,
  page,
  pageSize,
  sort,
  onSortChange,
  onPageChange,
  isLoading = false,
  isError = false,
  onRetry,
  onRowClick,
  emptyTitle,
  emptyDescription,
  ariaLabel,
  skeletonRowCount = DEFAULT_SKELETON_ROWS,
}: DataTableProps<T>) {
  const { t } = useTranslation();
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const handleSortClick = useCallback(
    (columnId: string) => {
      const nextDirection: SortDirection =
        sort?.columnId === columnId && sort.direction === 'asc' ? 'desc' : 'asc';
      onSortChange({ columnId, direction: nextDirection });
    },
    [sort, onSortChange],
  );

  if (isError) {
    return <ErrorState onRetry={onRetry} />;
  }

  const isEmpty = !isLoading && rows.length === 0;

  return (
    <div className="flex flex-col gap-3">
      <Table aria-label={ariaLabel}>
        <TableHeader>
          <TableRow>
            {columns.map((column) => (
              <TableHead
                key={column.id}
                className={column.headerClassName}
                aria-sort={
                  column.sortable
                    ? sort?.columnId === column.id
                      ? sort.direction === 'asc'
                        ? 'ascending'
                        : 'descending'
                      : 'none'
                    : undefined
                }
              >
                {column.sortable ? (
                  <button
                    type="button"
                    onClick={() => handleSortClick(column.id)}
                    className="inline-flex items-center gap-1 font-semibold text-ink-soft hover:text-ink"
                  >
                    {column.header}
                    {sort?.columnId === column.id ? (
                      sort.direction === 'asc' ? (
                        <>
                          <ArrowUp size={14} aria-hidden="true" />
                          <span className="sr-only">
                            {t('dataTable.sortedBy', { direction: t('dataTable.ascending') })}
                          </span>
                        </>
                      ) : (
                        <>
                          <ArrowDown size={14} aria-hidden="true" />
                          <span className="sr-only">
                            {t('dataTable.sortedBy', { direction: t('dataTable.descending') })}
                          </span>
                        </>
                      )
                    ) : (
                      <ArrowUpDown size={14} className="opacity-40" aria-hidden="true" />
                    )}
                  </button>
                ) : (
                  column.header
                )}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading &&
            Array.from({ length: skeletonRowCount }, (_, index) => (
              <TableRow key={`skeleton-${String(index)}`}>
                {columns.map((column) => (
                  <TableCell key={column.id}>
                    <Skeleton className="h-4 w-full max-w-40" />
                  </TableCell>
                ))}
              </TableRow>
            ))}
          {!isLoading &&
            rows.map((row) => (
              <TableRow
                key={rowKey(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                onKeyDown={
                  onRowClick
                    ? (event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          onRowClick(row);
                        }
                      }
                    : undefined
                }
                tabIndex={onRowClick ? 0 : undefined}
                role={onRowClick ? 'button' : undefined}
                className={cn(onRowClick && 'cursor-pointer')}
              >
                {columns.map((column) => (
                  <TableCell key={column.id} className={column.cellClassName}>
                    {column.cell(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
        </TableBody>
      </Table>

      {isEmpty && <EmptyState title={emptyTitle} description={emptyDescription} />}

      {!isLoading && !isEmpty && (
        <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-ink-soft">
          <p>{t('dataTable.totalCount', { count: total })}</p>
          <nav aria-label={t('dataTable.paginationLabel')} className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1}
              aria-label={t('dataTable.previousPage')}
            >
              <ChevronLeft size={16} aria-hidden="true" />
            </Button>
            <span aria-live="polite">{t('dataTable.pageIndicator', { page, totalPages })}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages}
              aria-label={t('dataTable.nextPage')}
            >
              <ChevronRight size={16} aria-hidden="true" />
            </Button>
          </nav>
        </div>
      )}
    </div>
  );
}
