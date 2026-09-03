import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { AdminLayout } from './AdminLayout';
import { useAuthStore } from '@/features/auth/store/authStore';
import { setAdminAccessToken } from '@/shared/api/adminClient';
import { emptyResponse, type Schemas } from '@/test/apiMock';

function renderAdminLayout() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/admin/login" element={<div>LOGIN_STUB</div>} />
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<div>DASHBOARD_CONTENT</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AdminLayout', () => {
  beforeEach(() => {
    setAdminAccessToken('token-1');
    useAuthStore.setState({
      isRestoring: false,
      accessToken: 'token-1',
      user: {
        id: 'u1',
        username: 'sardor.admin',
        email: 'sardor@16shaxsiyat.uz',
        role: 'SuperAdmin',
        totpEnabled: false,
      } satisfies Schemas['AdminUserDto'],
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setAdminAccessToken(null);
    useAuthStore.setState({ isRestoring: true, accessToken: null, user: null });
  });

  it("navigatsiyada 'Tez orada' belgisi ko'rsatilmaydi", () => {
    renderAdminLayout();

    // Belgi butunlay olib tashlandi: u `ready` bayrog'iga tayanardi, bayroq esa sahifalar
    // qurilgan sari yangilanmay, tayyor bo'limlarda ham ko'rinib qolgan edi.
    expect(screen.queryAllByText('Tez orada')).toHaveLength(0);

    const settingsLinks = screen.getAllByRole('link', { name: /Sozlamalar/ });
    expect(settingsLinks.length).toBeGreaterThan(0);
  });

  it("himoyalangan mazmun (Outlet) ko'rsatiladi", () => {
    renderAdminLayout();
    expect(screen.getByText('DASHBOARD_CONTENT')).toBeInTheDocument();
  });

  it("header foydalanuvchi menyusida joriy login ko'rsatiladi", () => {
    renderAdminLayout();
    expect(screen.getByText('sardor.admin')).toBeInTheDocument();
  });

  it("'Chiqish' bosilganda logout so'rovi yuboriladi, mahalliy sessiya tozalanadi va login sahifasiga o'tadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(emptyResponse(204));
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderAdminLayout();
    await user.click(screen.getByRole('button', { name: 'Chiqish' }));

    expect(await screen.findByText('LOGIN_STUB')).toBeInTheDocument();
    expect(useAuthStore.getState().accessToken).toBeNull();
    expect(useAuthStore.getState().user).toBeNull();
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/auth/logout'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it("server bilan aloqa uzilgan bo'lsa ham (logout so'rovi xato bersa) mahalliy sessiya baribir tozalanadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    const user = userEvent.setup();

    renderAdminLayout();
    await user.click(screen.getByRole('button', { name: 'Chiqish' }));

    expect(await screen.findByText('LOGIN_STUB')).toBeInTheDocument();
    expect(useAuthStore.getState().accessToken).toBeNull();
  });

  it('mobil menyu (hamburger) tugmasi va yopish tugmasi mavjud', () => {
    renderAdminLayout();
    expect(screen.getByRole('button', { name: 'Menyuni ochish' })).toBeInTheDocument();
    // Yopish tugmasi drawer (`<dialog>`) ichida — jsdom `showModal()`ni amalga oshirmagani
    // sabab u doim `display:none` (yopiq holatda), shu sabab `getByRole` uni chiqarib
    // tashlaydi; `getByLabelText` ko'rinishdan qat'i nazar topadi (`Dialog.tsx` izohiga qarang).
    expect(screen.getByLabelText('Menyuni yopish')).toBeInTheDocument();
  });
});
