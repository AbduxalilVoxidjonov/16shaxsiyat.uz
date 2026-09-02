import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useSearchParams } from 'react-router';
import AuditLogPage from './AuditLogPage';

const ENTRY_1 = {
  id: 42,
  adminUserId: '11111111-2222-3333-4444-555555555555',
  action: 'School.Created',
  entityType: 'School',
  entityId: '99999999-8888-7777-6666-555555555555',
  beforeJson: null,
  afterJson: JSON.stringify({ name: '12-son maktab', isActive: true }),
  ipHash: 'abc123hash',
  userAgent: 'Mozilla/5.0',
  createdAt: '2026-08-30T10:15:00Z',
};

const ENTRY_NO_DIFF = {
  id: 40,
  adminUserId: null,
  action: 'Auth.LoginFailed',
  entityType: null,
  entityId: null,
  beforeJson: null,
  afterJson: null,
  ipHash: 'abc123hash',
  userAgent: 'Mozilla/5.0',
  createdAt: '2026-08-28T08:00:00Z',
};

const ENTRY_2 = {
  id: 41,
  adminUserId: '11111111-2222-3333-4444-555555555555',
  action: 'School.Updated',
  entityType: 'School',
  entityId: '99999999-8888-7777-6666-555555555555',
  beforeJson: JSON.stringify({ name: 'Eski nom' }),
  afterJson: JSON.stringify({ name: 'Yangi nom' }),
  ipHash: 'abc123hash',
  userAgent: 'Mozilla/5.0',
  createdAt: '2026-08-29T09:00:00Z',
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function pagedResponse(items: unknown[]) {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
    hasNext: false,
    hasPrevious: false,
  };
}

function mockFetch(items: unknown[] = [ENTRY_1, ENTRY_2]) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/audit-logs')) {
      return Promise.resolve(jsonResponse(pagedResponse(items)));
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function UrlProbe() {
  const [searchParams] = useSearchParams();
  return <div data-testid="url-probe">{searchParams.toString()}</div>;
}

function renderAuditPage(initialEntry = '/admin/audit') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <UrlProbe />
        <AuditLogPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AuditLogPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("audit yozuvlarini jadval sifatida ko'rsatadi", async () => {
    mockFetch();
    renderAuditPage();

    await screen.findByRole('table');
    const table = within(screen.getByRole('table'));

    expect(await table.findByText('Maktab yaratildi')).toBeInTheDocument();
    expect(table.getByText('Maktab yangilandi')).toBeInTheDocument();
    // sirlar (IP xeshi/user-agent) jadval qatorida XOM holda ko'rsatilmaydi
    expect(screen.queryByText('abc123hash')).not.toBeInTheDocument();
  });

  it("harakat turi filtri URL query'ga yoziladi va deep-link'dan to'g'ri o'qiladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderAuditPage();

    await screen.findAllByRole('button', { name: 'Batafsil' });
    fetchMock.mockClear();

    const filtersBar = within(screen.getByTestId('audit-filters'));
    await user.selectOptions(filtersBar.getByLabelText('Harakat turi'), 'School.Created');

    await waitFor(() => {
      expect(screen.getByTestId('url-probe').textContent).toContain('action=School.Created');
    });
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => String(input).includes('action=School.Created')),
      ).toBe(true);
    });
  });

  it("chuqur havoladan (deep-link) filtrlar to'g'ri o'qiladi", async () => {
    const fetchMock = mockFetch();
    renderAuditPage('/admin/audit?action=School.Created&entityType=School&from=2026-08-01&to=2026-08-31');

    await screen.findAllByRole('button', { name: 'Batafsil' });

    const filtersBar = within(screen.getByTestId('audit-filters'));
    expect(filtersBar.getByLabelText('Harakat turi')).toHaveValue('School.Created');
    expect(filtersBar.getByLabelText('Obyekt turi')).toHaveValue('School');
    expect(filtersBar.getByLabelText('Sanadan')).toHaveValue('2026-08-01');
    expect(filtersBar.getByLabelText('Sanagacha')).toHaveValue('2026-08-31');

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => {
          const url = String(input);
          return (
            url.includes('action=School.Created') &&
            url.includes('entityType=School') &&
            url.includes('from=2026-08-01') &&
            url.includes('to=2026-08-31')
          );
        }),
      ).toBe(true);
    });
  });

  it("'Batafsil' bosilganda before/after farqi tushunarli shaklda ko'rsatiladi (xom JSON emas)", async () => {
    mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderAuditPage();

    const rows = await screen.findAllByRole('button', { name: 'Batafsil' });
    // ENTRY_2 ("Maktab yangilandi") ikkinchi qatorda — bittasini bosamiz, ikkalasi ham diff'ga ega.
    await user.click(rows[1] ?? rows[0]!);

    expect(await screen.findByText('name')).toBeInTheDocument();
    expect(screen.getByText('Eski nom')).toBeInTheDocument();
    expect(screen.getByText('Yangi nom')).toBeInTheDocument();
    // Xom JSON qatori (masalan `{"name":"Eski nom"}`) hech qayerda ko'rsatilmaydi.
    expect(screen.queryByText(/{"name"/)).not.toBeInTheDocument();
  });

  it("diff bo'lmagan yozuvda tushunarli bo'sh xabar ko'rsatiladi", async () => {
    mockFetch([ENTRY_NO_DIFF]);
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderAuditPage();

    await user.click(await screen.findByRole('button', { name: 'Batafsil' }));

    expect(await screen.findByText("Bu yozuvda qo'shimcha ma'lumot yo'q.")).toBeInTheDocument();
  });
});
