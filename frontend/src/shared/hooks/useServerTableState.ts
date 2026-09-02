import { useCallback } from 'react';
import { useSearchParams } from 'react-router';

export type SortDirection = 'asc' | 'desc';

export interface DataTableSort {
  columnId: string;
  direction: SortDirection;
}

export interface ServerTableState {
  page: number;
  pageSize: number;
  sort: DataTableSort | null;
}

export interface UseServerTableStateOptions {
  /** Standart sahifa hajmi — URL da `pageSize` bo'lmasa ishlatiladi. Standart: 20. */
  defaultPageSize?: number;
  /** URL da `sort`/`dir` bo'lmaganda ishlatiladigan boshlang'ich saralash. */
  defaultSort?: DataTableSort | null;
}

export interface UseServerTableStateResult extends ServerTableState {
  setPage: (page: number) => void;
  setSort: (sort: DataTableSort) => void;
}

function parsePositiveInt(value: string | null, fallback: number): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? Math.trunc(parsed) : fallback;
}

/**
 * `DataTable` uchun sahifalash/saralash holatini **URL query** bilan sinxronlaydi
 * (`page`, `pageSize`, `sort`, `dir`) — docs/10-frontend-arxitektura.md, 5.3-bo'lim:
 * "Server-side pagination/sort/filter — barcha holat URL query da". Shu sabab orqaga
 * tugmasi ishlaydi va sahifa ulashilganda holat saqlanadi.
 *
 * Boshqa filtrlar (qidiruv, maktab, sinf va h.k.) sahifaga xos — ular alohida
 * `useSearchParams` chaqiruvi bilan boshqariladi, bu hook faqat jadval uchun umumiy
 * 3 ta parametrni egallaydi.
 */
export function useServerTableState(
  options: UseServerTableStateOptions = {},
): UseServerTableStateResult {
  const { defaultPageSize = 20, defaultSort = null } = options;
  const [searchParams, setSearchParams] = useSearchParams();

  const page = parsePositiveInt(searchParams.get('page'), 1);
  const pageSize = parsePositiveInt(searchParams.get('pageSize'), defaultPageSize);

  const sortColumnId = searchParams.get('sort');
  const sort: DataTableSort | null = sortColumnId
    ? { columnId: sortColumnId, direction: searchParams.get('dir') === 'desc' ? 'desc' : 'asc' }
    : defaultSort;

  const setPage = useCallback(
    (next: number) => {
      setSearchParams((prev) => {
        const params = new URLSearchParams(prev);
        params.set('page', String(next));
        return params;
      });
    },
    [setSearchParams],
  );

  const setSort = useCallback(
    (next: DataTableSort) => {
      setSearchParams((prev) => {
        const params = new URLSearchParams(prev);
        params.set('sort', next.columnId);
        params.set('dir', next.direction);
        params.set('page', '1');
        return params;
      });
    },
    [setSearchParams],
  );

  return { page, pageSize, sort, setPage, setSort };
}
