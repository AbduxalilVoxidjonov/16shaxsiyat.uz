import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { PublicSpaceAssignmentBlock, type PublicSpaceAssignmentBlockProps } from './PublicSpaceAssignmentBlock';

/**
 * Dastur detalidagi "Ommaviy makon" bloki (2026-09-07). Qulflanadigan shartlar:
 * - biriktirish/olib tashlash MAVJUD ommaviy endpointga boradi
 *   (`POST | DELETE /api/admin/public-space/programs/{id}`), dastur tomonida yangi yo'l yo'q;
 * - `Active` bo'lmagan dasturda biriktirish tugmasi o'chirilgan va sababi yozilgan;
 * - blokda "maktab" so'zi yo'q — ommaviy makon maktab emas.
 */
const PUBLIC_SPACE = {
  id: 'public-space-1',
  name: 'Ommaviy makon',
  slug: 'ommaviy',
  isActive: true,
  showResultToStudent: true,
  dailyRegistrationLimit: 500,
  publicUrl: 'https://16shaxsiyat.uz/kirish',
  availability: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
  programs: [],
  stats: {
    userCount: 0,
    deletedUserCount: 0,
    totalAssessments: 0,
    inProgressCount: 0,
    completedCount: 0,
    analyzedCount: 0,
    lastActivityAt: null,
  },
} satisfies Schemas['AdminPublicSpaceDto'];

const PUBLIC_SPACE_PROGRAM_URL = '/api/admin/public-space/programs/program-1';

function mockFetch() {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.endsWith(PUBLIC_SPACE_PROGRAM_URL) && (init?.method === 'POST' || init?.method === 'DELETE')) {
      return Promise.resolve(jsonResponse<'AdminPublicSpaceDto'>(PUBLIC_SPACE));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderBlock(overrides: Partial<PublicSpaceAssignmentBlockProps> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <PublicSpaceAssignmentBlock
          programId="program-1"
          programName="Shaxsiyat profili"
          state="Active"
          isAssignedToPublicSpace={false}
          {...overrides}
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function calledWith(fetchMock: ReturnType<typeof vi.fn>, method: string): boolean {
  return fetchMock.mock.calls.some(
    (call) =>
      String(call[0]).endsWith(PUBLIC_SPACE_PROGRAM_URL) &&
      (call[1] as RequestInit | undefined)?.method === method,
  );
}

describe('PublicSpaceAssignmentBlock', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("biriktirilmagan holat: 'Biriktirilmagan' belgisi va biriktirish tugmasi, blokda \"maktab\" so'zi yo'q", () => {
    mockFetch();
    renderBlock();

    const block = screen.getByTestId('public-space-assignment');
    expect(within(block).getByRole('heading', { name: 'Ommaviy makon' })).toBeInTheDocument();
    expect(within(block).getByText('Biriktirilmagan')).toBeInTheDocument();
    expect(
      within(block).getByText(/Telegram orqali kirgan har kim topshira oladi/),
    ).toBeInTheDocument();
    expect(within(block).getByRole('button', { name: 'Ommaviyga biriktirish' })).toBeEnabled();
    expect(
      within(block).queryByRole('button', { name: 'Ommaviydan olib tashlash' }),
    ).not.toBeInTheDocument();

    // Ommaviy makon "maktab" EMAS — blok matnida bu so'z uchramasligi kerak.
    expect(block.textContent?.toLowerCase()).not.toContain('maktab');
  });

  it("biriktirilgan holat: 'Biriktirilgan' belgisi va olib tashlash tugmasi", () => {
    mockFetch();
    renderBlock({ isAssignedToPublicSpace: true });

    const block = screen.getByTestId('public-space-assignment');
    expect(within(block).getByText('Biriktirilgan')).toBeInTheDocument();
    expect(within(block).getByRole('button', { name: 'Ommaviydan olib tashlash' })).toBeEnabled();
    expect(
      within(block).queryByRole('button', { name: 'Ommaviyga biriktirish' }),
    ).not.toBeInTheDocument();
  });

  it("biriktirish POST /api/admin/public-space/programs/{id} ga boradi (dastur tomonida yangi endpoint yo'q)", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderBlock();

    await user.click(screen.getByRole('button', { name: 'Ommaviyga biriktirish' }));

    await waitFor(() => {
      expect(calledWith(fetchMock, 'POST')).toBe(true);
    });
    expect(await screen.findByText('Dastur ommaviy makonga biriktirildi')).toBeInTheDocument();
    // Dastur yo'liga hech qanday so'rov ketmagan.
    expect(
      fetchMock.mock.calls.some((call) => String(call[0]).includes('/api/admin/programs/')),
    ).toBe(false);
  });

  it('olib tashlash tasdiq oynasidan o‘tadi va DELETE /api/admin/public-space/programs/{id} ga boradi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderBlock({ isAssignedToPublicSpace: true });

    await user.click(screen.getByRole('button', { name: 'Ommaviydan olib tashlash' }));

    // Tasdiqlanmaguncha so'rov YO'Q.
    expect(await screen.findByText('Shaxsiyat profili')).toBeInTheDocument();
    expect(calledWith(fetchMock, 'DELETE')).toBe(false);

    // `DeactivateProgramDialog.test` naqshi: jsdom `<dialog>` ichidagi tugma rol bo'yicha
    // topilmaydi, matn bo'yicha topiladi (aniq moslik — "Ommaviydan olib tashlash" emas).
    await user.click(screen.getByText('Olib tashlash'));

    await waitFor(() => {
      expect(calledWith(fetchMock, 'DELETE')).toBe(true);
    });
    expect(await screen.findByText('Dastur ommaviy makondan olib tashlandi')).toBeInTheDocument();
  });

  it.each(['Draft', 'Paused', 'Archived'])(
    "%s holatida biriktirish tugmasi o'chirilgan va sabab yozilgan",
    (state) => {
      mockFetch();
      renderBlock({ state });

      const button = screen.getByRole('button', { name: 'Ommaviyga biriktirish' });
      expect(button).toBeDisabled();
      const reason = screen.getByText('Faqat faol dastur biriktiriladi');
      expect(reason).toBeInTheDocument();
      expect(button).toHaveAttribute('aria-describedby', reason.id);
    },
  );

  it("faol bo'lmagan, lekin allaqachon biriktirilgan dasturni OLIB TASHLASH mumkin", () => {
    mockFetch();
    renderBlock({ state: 'Paused', isAssignedToPublicSpace: true });

    expect(screen.getByRole('button', { name: 'Ommaviydan olib tashlash' })).toBeEnabled();
    expect(screen.queryByText('Faqat faol dastur biriktiriladi')).not.toBeInTheDocument();
  });

  it('biriktirish xatosi toast bilan ko‘rsatiladi', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      problemResponse('PUBLIC_SPACE_NOT_CONFIGURED', 409, 'Ommaviy makon sozlanmagan.'),
    );
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderBlock();

    await user.click(screen.getByRole('button', { name: 'Ommaviyga biriktirish' }));

    expect(await screen.findByText('Ommaviy makon sozlanmagan.')).toBeInTheDocument();
  });
});
