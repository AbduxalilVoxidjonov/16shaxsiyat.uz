import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { pagedResponse, problemResponse } from '@/test/apiMock';
import { SchoolAssignmentPanel } from './SchoolAssignmentPanel';

/**
 * Panel tepasida ommaviy makon bloki maktablar ro'yxatidan ALOHIDA turadi (2026-09-07).
 * Ommaviy makon `assignedSchoolIds` da yo'q — u uchun `GET /api/admin/schools/{id}` so'rovi
 * ketmaydi (aks holda `AdminSchoolScope.SchoolsOnly` `404` qaytarardi).
 */
function mockFetch() {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/schools?')) {
      return Promise.resolve(pagedResponse<'AdminSchoolListItemDto'>([]));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPanel(isAssignedToPublicSpace: boolean) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <SchoolAssignmentPanel
          programId="program-1"
          programName="Shaxsiyat profili"
          state="Active"
          assignedSchoolIds={[]}
          isAssignedToPublicSpace={isAssignedToPublicSpace}
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('SchoolAssignmentPanel', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("ommaviy makon bloki maktablar ro'yxatidan alohida ko'rinadi", async () => {
    mockFetch();
    renderPanel(true);

    const block = screen.getByTestId('public-space-assignment');
    expect(within(block).getByRole('heading', { name: 'Ommaviy makon' })).toBeInTheDocument();
    expect(within(block).getByText('Biriktirilgan')).toBeInTheDocument();

    // Maktablar bo'limi o'z sarlavhasi bilan ALOHIDA — blok ichida "maktab" yo'q.
    expect(await screen.findByText('Biriktirilgan maktablar (0)')).toBeInTheDocument();
    expect(block.textContent?.toLowerCase()).not.toContain('maktab');
  });

  it("ommaviy makon uchun GET /api/admin/schools/{id} so'rovi ketmaydi", async () => {
    const fetchMock = mockFetch();
    renderPanel(true);

    expect(await screen.findByText('Hali maktab biriktirilmagan')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some((call) => /\/api\/admin\/schools\/[^?]/.test(String(call[0]))),
    ).toBe(false);
  });
});
