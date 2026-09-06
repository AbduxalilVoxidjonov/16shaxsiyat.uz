import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, type Schemas } from '@/test/apiMock';
import { DeactivateProgramDialog } from './DeactivateProgramDialog';

/**
 * 2026-09-03 jonli hodisasi: admin yagona dasturni to'xtatdi, HECH QANDAY ogohlantirish
 * ko'rmadi, va barcha maktab havolasi jimgina o'lik bo'lib qoldi. Endi tasdiq oynasi
 * amaldan OLDIN nechta maktab dastursiz qolishini aytadi.
 *
 * Amal TAQIQLANMAYDI — admin haqli; faqat oqibat ko'rsatiladi.
 */
function mockFetch(impact: Schemas['AdminProgramImpactDto']) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/impact')) {
      return Promise.resolve(jsonResponse<'AdminProgramImpactDto'>(impact));
    }
    if (url.includes('/toggle-active')) {
      return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(PROGRAM_DETAIL));
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const PROGRAM_DETAIL = {
  id: 'program-1',
  code: 'PERSONALITY_PROFILE',
  nameUz: 'Shaxsiyat profili',
  descriptionUz: null,
  kind: 'System',
  visibility: 'Public',
  state: 'Paused',
  isSystem: true,
  displayOrder: 1,
  tests: [],
  assignedSchoolIds: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
} satisfies Schemas['AdminProgramDetailDto'];

function renderDialog() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter>
          <DeactivateProgramDialog
            open
            programId="program-1"
            programName="Shaxsiyat profili"
            onClose={() => {}}
          />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('DeactivateProgramDialog', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("ta'sir qiladigan maktablar sonini va nomlarini tasdiqdan OLDIN ko'rsatadi", async () => {
    mockFetch({
      action: 'deactivate',
      affectedSchoolCount: 12,
      schools: [
        { id: 'school-1', name: '12-son maktab' },
        { id: 'school-2', name: '7-son maktab' },
      ],
    });

    renderDialog();

    expect(
      await screen.findByText(
        /Diqqat: bu amaldan keyin 12 ta maktab umuman dastursiz qoladi/,
      ),
    ).toBeInTheDocument();
    expect(screen.getByText('12-son maktab')).toBeInTheDocument();
    expect(screen.getByText('7-son maktab')).toBeInTheDocument();
    expect(screen.getByText('va yana 10 ta maktab')).toBeInTheDocument();

    // Amal TAQIQLANMAYDI — tasdiq tugmasi bosiladigan holatda qoladi.
    expect(screen.getByText("To'xtatish")).toBeEnabled();
  });

  it("hech kim ta'sirlanmasa aniq \"hech kim\" deyiladi (bo'sh joy emas)", async () => {
    mockFetch({ action: 'deactivate', affectedSchoolCount: 0, schools: [] });

    renderDialog();

    expect(
      await screen.findByText('Bu amal hech qaysi maktabni dastursiz qoldirmaydi.'),
    ).toBeInTheDocument();
  });

  it("ta'sirni hisoblab bo'lmasa \"hech kim\" DEYILMAYDI — xato matni chiqadi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/impact')) {
        return Promise.resolve(new Response('{}', { status: 500 }));
      }
      return Promise.reject(new Error(`unexpected fetch: ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);

    renderDialog();

    expect(await screen.findByText(/Ta'sirni hisoblab bo'lmadi/)).toBeInTheDocument();
    expect(
      screen.queryByText('Bu amal hech qaysi maktabni dastursiz qoldirmaydi.'),
    ).not.toBeInTheDocument();
  });

  it("tasdiqlansa dastur to'xtatiladi (amal taqiqlanmaydi)", async () => {
    const fetchMock = mockFetch({ action: 'deactivate', affectedSchoolCount: 3, schools: [] });
    const user = userEvent.setup();

    renderDialog();

    await screen.findByText(/Diqqat: bu amaldan keyin 3 ta maktab/);
    await user.click(screen.getByText("To'xtatish"));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some((call) => String(call[0]).includes('/toggle-active')),
      ).toBe(true);
    });
  });
});
