import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import ProgramsPage from './ProgramsPage';
import { jsonResponse, pagedResponse, problemResponse, type Schemas } from '@/test/apiMock';

/** `GET /api/admin/programs` qatori — backend `AdminProgramListItemDto`. */
const PROGRAM_1 = {
  id: 'program-1',
  code: 'PERSONALITY_PROFILE',
  nameUz: 'Shaxsiyat profili',
  kind: 'System',
  visibility: 'Public',
  status: 'Published',
  isActive: true,
  isSystem: true,
  displayOrder: 1,
  testCount: 4,
} satisfies Schemas['AdminProgramListItemDto'];

/**
 * `POST /api/admin/programs` javobi — backend `AdminProgramDetailDto` (ro'yxat qatori EMAS:
 * `testCount` yo'q, o'rniga `tests`/`assignedSchoolIds`/`descriptionUz` bor). Ilgari mock
 * ro'yxat qatorini yoyib yuborardi va `testCount` ortiqcha maydoni sezilmay qolardi.
 */
const CREATED_PROGRAM = {
  id: 'program-2',
  code: 'NEW',
  nameUz: 'Yangi dastur',
  descriptionUz: null,
  kind: 'Custom',
  visibility: 'Public',
  status: 'Draft',
  isActive: true,
  isSystem: false,
  displayOrder: 1,
  tests: [],
  assignedSchoolIds: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
} satisfies Schemas['AdminProgramDetailDto'];

function mockFetch() {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/api/admin/programs') && method === 'GET') {
      return Promise.resolve(pagedResponse<'AdminProgramListItemDto'>([PROGRAM_1]));
    }
    if (url.includes('/api/admin/programs') && method === 'POST') {
      return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(CREATED_PROGRAM, 201));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/programs']}>
          <ProgramsPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('ProgramsPage', () => {
  beforeEach(() => {
    mockFetch();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("dasturlar ro'yxatini ko'rsatadi", async () => {
    renderPage();
    expect(await screen.findByText('Shaxsiyat profili')).toBeInTheDocument();
    expect(screen.getByText('Tizim')).toBeInTheDocument();
  });

  it('"Yangi dastur" tugmasi forma ochadi va yaratish so\'rovini yuboradi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat profili');
    await user.click(screen.getByRole('button', { name: /Yangi dastur/ }));

    await user.type(screen.getByLabelText('Dastur kodi'), 'NEW');
    await user.type(screen.getByLabelText('Nomi'), 'Yangi dastur');

    await user.click(screen.getByText('Yaratish'));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('/api/admin/programs'),
        expect.objectContaining({ method: 'POST' }),
      );
    });
  });
});
