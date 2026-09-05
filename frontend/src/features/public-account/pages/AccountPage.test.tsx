import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { emptyResponse, jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import AccountPage from './AccountPage';
import { usePublicUserStore } from '../store/publicUserStore';

const USER = {
  id: 'user-1',
  username: 'alivali',
  firstName: 'Ali',
  lastName: 'Valiyev',
  photoUrl: null,
  createdAt: '2026-09-01T10:12:00Z',
  lastLoginAt: '2026-09-05T10:12:00Z',
} satisfies Schemas['PublicUserDto'];

const ASSESSMENTS = {
  items: [
    {
      id: 'assessment-1',
      status: 'Analyzed',
      startedAt: '2026-09-01T09:00:00Z',
      completedAt: '2026-09-01T09:48:00Z',
      programCode: 'PERSONALITY_PROFILE',
      programName: 'Shaxsiyat profili',
      resultAvailable: true,
    },
    {
      id: 'assessment-2',
      status: 'Analyzing',
      startedAt: '2026-09-04T09:00:00Z',
      completedAt: null,
      programCode: 'PERSONALITY_PROFILE',
      programName: 'Shaxsiyat profili',
      resultAvailable: false,
    },
  ],
} satisfies Schemas['ListMyAssessmentsResult'];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/kabinet']}>
          <Routes>
            <Route path="/kabinet" element={<AccountPage />} />
            <Route path="/" element={<p>BOSH_SAHIFA_STUB</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function signIn() {
  usePublicUserStore.getState().setSession('access-1', USER);
}

/**
 * jsdom `HTMLDialogElement.showModal()` ni amalga oshirmaydi (`Dialog.tsx` dagi himoyalangan
 * chaqiruvga qarang), shu sabab dialog `open` atributisiz qoladi va `getByRole` (yashirin
 * elementlarni chiqarib tashlaydi) uning ichidagini topa olmaydi — `SchoolsPage.test.tsx`
 * dagi bilan bir xil holat. Dialog ichidagi tugma shu sabab matn bo'yicha olinadi.
 */
function dialogButton(label: string): HTMLElement {
  const node = screen.getByText(label).closest('button');
  if (!node) throw new Error(`Dialogda "${label}" tugmasi topilmadi`);
  return node;
}

describe('AccountPage', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  it("profilni va test tarixini ko'rsatadi, natijaga havola beradi", async () => {
    signIn();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>(ASSESSMENTS)),
    );

    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Ali Valiyev');
    expect(screen.getByText(/01\.09\.2026/)).toBeInTheDocument();

    const [ready, pending] = await screen.findAllByRole('listitem');
    expect(pending).toBeDefined();

    // Natijasi tayyor sessiyada havola bor, tayyor bo'lmaganida — yo'q (bayroq backenddan).
    expect(
      within(ready as HTMLElement).getByRole('link', { name: /Natijani ko'rish/ }),
    ).toHaveAttribute('href', '/kabinet/natijalar/assessment-1');
    expect(within(pending as HTMLElement).queryByRole('link')).not.toBeInTheDocument();
    expect(within(pending as HTMLElement).getByText('Natija hali ochilmagan')).toBeInTheDocument();
  });

  it("bo'sh tarixda tushunarli holat va test boshlash taklifi chiqadi", async () => {
    signIn();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>({ items: [] })),
    );

    renderPage();

    expect(await screen.findByText('Hali test topshirmagansiz')).toBeInTheDocument();
    const startLinks = screen.getAllByRole('link', { name: /Yangi test boshlash/ });
    expect(startLinks[0]).toHaveAttribute('href', '/kabinet/test');
  });

  it("tarix so'rovi xato bersa qayta urinish tugmasi ko'rsatiladi", async () => {
    signIn();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SERVER_ERROR', 500)));

    renderPage();

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("akkauntni o'chirish tasdiqsiz bajarilmaydi va oqibatlari yozilgan", async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: "Akkauntni o'chirish" }));

    expect(screen.getByText("Akkauntni o'chirasizmi?")).toBeInTheDocument();
    expect(screen.getByText(/YANGI akkaunt ochiladi/)).toBeInTheDocument();
    // Dialog ochilishining O'ZI hech narsani o'chirmaydi.
    expect(
      fetchMock.mock.calls.some((call: unknown[]) => (call[1] as RequestInit).method === 'DELETE'),
    ).toBe(false);
  });

  it('tasdiqlangach `DELETE /api/me` yuboriladi va sessiya tozalanadi', async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi
      .fn()
      .mockImplementation((_input: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === 'DELETE') {
          return Promise.resolve(emptyResponse(204));
        }
        return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
      });
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: "Akkauntni o'chirish" }));
    await user.click(dialogButton("Ha, o'chirilsin"));

    expect(await screen.findByText('BOSH_SAHIFA_STUB')).toBeInTheDocument();
    expect(usePublicUserStore.getState().accessToken).toBeNull();
    expect(usePublicUserStore.getState().status).toBe('anonymous');
  });

  it("chiqish `logout` so'rovini yuboradi va bosh sahifaga qaytaradi", async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      if (String(input).includes('/logout')) {
        return Promise.resolve(emptyResponse(204));
      }
      return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
    });
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: 'Chiqish' }));

    expect(await screen.findByText('BOSH_SAHIFA_STUB')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some((call: unknown[]) =>
        String(call[0]).includes('/api/auth/telegram/logout'),
      ),
    ).toBe(true);
    expect(usePublicUserStore.getState().status).toBe('anonymous');
  });
});
