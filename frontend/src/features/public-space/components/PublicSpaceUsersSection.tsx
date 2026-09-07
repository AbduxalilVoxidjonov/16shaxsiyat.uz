import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useDebounce } from '@/shared/hooks/useDebounce';
import { useServerTableState, type DataTableSort } from '@/shared/hooks/useServerTableState';
import { DataTable, type DataTableColumn } from '@/shared/ui/DataTable';
import { Badge } from '@/shared/ui/Badge';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { ROUTES } from '@/shared/config/routes';
import { formatDate } from '@/shared/lib/formatDate';
import { usePublicSpaceUsersQuery } from '../api/usePublicSpaceUsersQuery';
import {
  PUBLIC_USER_STATUS_BADGE_VARIANT,
  PUBLIC_USER_STATUS_FILTERS,
  parsePublicUserStatusFilter,
  toPublicUserDisplayStatus,
  type PublicSpaceUserDto,
  type PublicSpaceUserProgressDto,
  type PublicSpaceUsersQuery,
} from '../model/types';

const SEARCH_DEBOUNCE_MS = 400;

function toSortParam(sort: DataTableSort | null): string | undefined {
  if (!sort) return undefined;
  return sort.direction === 'desc' ? `-${sort.columnId}` : sort.columnId;
}

/** Telegram ko'rinadigan ismi: `first last` → `@username` → "Nomsiz" (i18n, chaqiruvchida). */
function telegramDisplayName(row: PublicSpaceUserDto): string | null {
  const parts = [row.telegram.firstName, row.telegram.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(' ');
  return row.telegram.username ? `@${row.telegram.username}` : null;
}

/** Joriy blokdagi javoblar ulushi (0..100), savol yo'q bo'lsa `0`. */
function progressPercent(progress: PublicSpaceUserProgressDto): number {
  if (progress.questionsTotal <= 0) return 0;
  return Math.round((100 * progress.answered) / progress.questionsTotal);
}

function ProgressCell({ progress }: { progress: PublicSpaceUserProgressDto }) {
  const { t } = useTranslation();

  // Hamma blok yakunlangan, lekin sessiya hali yopilmagan — joriy blok yo'q.
  if (progress.currentTestNumber == null) {
    return (
      <span className="whitespace-nowrap text-ink">
        {t('publicSpace.users.progress.allBlocksDone', {
          completed: progress.testsCompleted,
          total: progress.testsTotal,
        })}
      </span>
    );
  }

  const percent = progressPercent(progress);
  const label = t('publicSpace.users.progress.position', {
    block: progress.currentTestNumber,
    answered: progress.answered,
    total: progress.questionsTotal,
  });

  return (
    <div className="flex min-w-40 flex-col gap-1">
      <span className="whitespace-nowrap text-ink">{label}</span>
      {progress.currentTestName && (
        <span className="text-xs text-ink-muted">{progress.currentTestName}</span>
      )}
      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        aria-label={label}
        className="h-1.5 w-full overflow-hidden rounded-full bg-paper-deep"
      >
        {/* Inline `width` — ma'lumotdan kelgan foiz; `dangerouslySetInnerHTML` EMAS. */}
        <div className="h-full rounded-full bg-firuza-500" style={{ width: `${String(percent)}%` }} />
      </div>
    </div>
  );
}

/**
 * Ommaviy makonda ro'yxatdan o'tgan foydalanuvchilar — kim, qachon, nechta test topshirgan
 * va yarim qolgan sessiyada qayerda to'xtagan (egasining talabi, 2026-09-07).
 *
 * Manba — Telegram akkaunti (`public_users`), shu sabab anketa to'ldirmaganlar ham ko'rinadi
 * (`fullName: null`, "Boshlamagan"). Qator bosilsa — `Student` mavjud bo'lsa — mavjud
 * `/admin/students/:id` profiliga o'tiladi; alohida detail sahifa YO'Q.
 *
 * Holat (`page`/`sort`/`search`/`status`) URL query'da (`docs/10` 5.3) — `StudentsPage` +
 * `StudentFiltersBar` bilan bir xil naqsh. Bu bo'limda "maktab" so'zi ishlatilmaydi.
 */
export function PublicSpaceUsersSection() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({
    defaultSort: { columnId: 'registeredAt', direction: 'desc' },
  });

  const urlSearch = searchParams.get('search') ?? '';
  const status = parsePublicUserStatusFilter(searchParams.get('status'));

  const [searchInput, setSearchInput] = useState(urlSearch);
  // URL boshqa joydan o'zgarsa (orqaga tugmasi) matn maydoni ham sinxronlansin — render
  // vaqtida moslashtirish (`StudentFiltersBar` bilan bir xil naqsh).
  const [syncedUrlSearch, setSyncedUrlSearch] = useState(urlSearch);
  if (urlSearch !== syncedUrlSearch) {
    setSyncedUrlSearch(urlSearch);
    setSearchInput(urlSearch);
  }

  const debouncedSearch = useDebounce(searchInput, SEARCH_DEBOUNCE_MS);

  useEffect(() => {
    const current = searchParams.get('search') ?? '';
    if (current === debouncedSearch) return;
    setSearchParams(
      (prev) => {
        const params = new URLSearchParams(prev);
        if (debouncedSearch) {
          params.set('search', debouncedSearch);
        } else {
          params.delete('search');
        }
        params.set('page', '1');
        return params;
      },
      { replace: true },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  function handleStatusChange(value: string) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (value && value !== 'all') {
        params.set('status', value);
      } else {
        params.delete('status');
      }
      params.set('page', '1');
      return params;
    });
  }

  const query: PublicSpaceUsersQuery = {
    search: urlSearch || undefined,
    status,
    page,
    pageSize,
    sort: toSortParam(sort),
  };
  const usersQuery = usePublicSpaceUsersQuery(query);
  const hasActiveFilters = urlSearch !== '' || status !== 'all';

  const columns: Array<DataTableColumn<PublicSpaceUserDto>> = [
    {
      id: 'user',
      header: t('publicSpace.users.table.user'),
      cell: (row) => {
        const name = telegramDisplayName(row) ?? t('publicSpace.users.unnamed');
        const showUsername = row.telegram.username && name !== `@${row.telegram.username}`;
        const details = [
          row.age == null ? null : t('publicSpace.users.age', { count: row.age }),
          row.grade == null ? null : t('publicSpace.users.grade', { grade: row.grade }),
        ].filter(Boolean);
        return (
          <div className="flex flex-col">
            <span className="flex flex-wrap items-baseline gap-x-2">
              <span className="font-medium text-ink">{name}</span>
              {showUsername && (
                <span className="text-xs text-ink-muted">@{row.telegram.username}</span>
              )}
            </span>
            {row.fullName && (
              <span className="text-xs text-ink-soft">
                {row.studentId ? (
                  <Link
                    to={ROUTES.admin.studentProfile(row.studentId)}
                    className="hover:text-firuza-700 hover:underline"
                  >
                    {row.fullName}
                  </Link>
                ) : (
                  row.fullName
                )}
                {details.length > 0 && <span> · {details.join(' · ')}</span>}
              </span>
            )}
          </div>
        );
      },
    },
    {
      id: 'registeredAt',
      header: t('publicSpace.users.table.registeredAt'),
      sortable: true,
      cell: (row) => formatDate(row.registeredAt),
    },
    {
      id: 'lastLoginAt',
      header: t('publicSpace.users.table.lastLoginAt'),
      sortable: true,
      cell: (row) => formatDate(row.lastLoginAt),
    },
    {
      id: 'assessments',
      header: t('publicSpace.users.table.assessments'),
      cell: (row) =>
        row.assessments.total === 0 ? (
          '—'
        ) : (
          <span className="whitespace-nowrap">
            {t('publicSpace.users.assessmentsSummary', {
              completed: row.assessments.completed,
              total: row.assessments.total,
            })}
          </span>
        ),
    },
    {
      id: 'status',
      header: t('publicSpace.users.table.status'),
      cell: (row) => {
        const displayStatus = toPublicUserDisplayStatus(row.lastAssessment);
        return (
          <Badge variant={PUBLIC_USER_STATUS_BADGE_VARIANT[displayStatus]}>
            {t(`publicSpace.users.status.${displayStatus}`)}
          </Badge>
        );
      },
    },
    {
      id: 'progress',
      header: t('publicSpace.users.table.progress'),
      cell: (row) =>
        row.lastAssessment?.progress ? <ProgressCell progress={row.lastAssessment.progress} /> : '—',
    },
  ];

  return (
    <section
      aria-label={t('publicSpace.users.title')}
      data-testid="public-space-users"
      className="flex flex-col gap-3"
    >
      <h2 className="font-display text-base font-bold text-neutral-900">
        {t('publicSpace.users.title')}
      </h2>

      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
        <div className="w-full sm:max-w-xs">
          <Input
            label={t('publicSpace.users.filters.searchLabel')}
            placeholder={t('publicSpace.users.filters.searchPlaceholder')}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>
        <div className="w-full sm:w-48">
          <Select
            label={t('publicSpace.users.filters.statusLabel')}
            value={status}
            onChange={(event) => handleStatusChange(event.target.value)}
            options={PUBLIC_USER_STATUS_FILTERS.map((value) => ({
              value,
              label: t(`publicSpace.users.filters.status.${value}`),
            }))}
          />
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={usersQuery.data?.items ?? []}
        rowKey={(row) => row.publicUserId}
        total={usersQuery.data?.totalCount ?? 0}
        page={page}
        pageSize={pageSize}
        sort={sort}
        onSortChange={setSort}
        onPageChange={setPage}
        isLoading={usersQuery.isPending}
        isError={usersQuery.isError}
        onRetry={() => void usersQuery.refetch()}
        onRowClick={(row) => {
          // Anketa to'ldirilmagan — profil yo'q, qator bosilsa hech narsa bo'lmaydi.
          if (!row.studentId) return;
          navigate(ROUTES.admin.studentProfile(row.studentId));
        }}
        ariaLabel={t('publicSpace.users.table.ariaLabel')}
        emptyTitle={
          hasActiveFilters
            ? t('publicSpace.users.empty.filteredTitle')
            : t('publicSpace.users.empty.title')
        }
        emptyDescription={
          hasActiveFilters
            ? t('publicSpace.users.empty.filteredDescription')
            : t('publicSpace.users.empty.description')
        }
      />
    </section>
  );
}
