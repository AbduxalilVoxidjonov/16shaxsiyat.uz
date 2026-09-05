import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import { PublicUserRoute } from './PublicUserRoute';
import { usePublicUserStore } from './store/publicUserStore';

const USER = {
  id: 'user-1',
  username: 'alivali',
  firstName: 'Ali',
  lastName: 'Valiyev',
  photoUrl: null,
  createdAt: '2026-09-01T10:00:00Z',
  lastLoginAt: '2026-09-05T10:00:00Z',
} satisfies Schemas['PublicUserDto'];

function renderGuard() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/kabinet?x=1']}>
        <Routes>
          <Route
            path="/kabinet"
            element={
              <PublicUserRoute>
                <p>KABINET_MAZMUNI</p>
              </PublicUserRoute>
            }
          />
          <Route path="/kirish" element={<LoginStub />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** Kirish sahifasi o'rniga — `returnUrl` ni ham ko'rsatadi (qaytish yo'li tekshiruvi uchun). */
function LoginStub() {
  const location = useLocation();
  return <p>KIRISH_STUB {location.search}</p>;
}

/**
 * Sessiyani tiklash faqat `localStorage` dagi BELGI bo'lganda urinib ko'riladi — belgi
 * token EMAS, shunchaki "bu brauzerda kirilgan edi" bayrog'i.
 */
function setSessionHint() {
  localStorage.setItem(STORAGE_KEYS.publicSessionHint, '1');
}

describe('PublicUserRoute', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.setState({ status: 'anonymous', accessToken: null, user: null });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.setState({ status: 'anonymous', accessToken: null, user: null });
  });

  it("anonim foydalanuvchini kirish sahifasiga yuboradi va so'rov YUBORMAYDI", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    renderGuard();

    // Qaytish yo'li saqlanadi — kirgandan keyin foydalanuvchi o'sha sahifaga qaytadi.
    expect(screen.getByText(/KIRISH_STUB/)).toHaveTextContent('returnUrl=%2Fkabinet%3Fx%3D1');
    expect(screen.queryByText('KABINET_MAZMUNI')).not.toBeInTheDocument();
    // Belgisi yo'q brauzerda refresh chaqirilmaydi (ortiqcha 401 so'rov bo'lmasin).
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("kirgan foydalanuvchiga mazmunni ko'rsatadi", () => {
    usePublicUserStore.getState().setSession('access-1', USER);
    vi.stubGlobal('fetch', vi.fn());

    renderGuard();

    expect(screen.getByText('KABINET_MAZMUNI')).toBeInTheDocument();
  });

  it("belgi bo'lsa sessiyani refresh + `GET /api/me` bilan tiklaydi", async () => {
    setSessionHint();
    usePublicUserStore.setState({ status: 'restoring', accessToken: null, user: null });
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      if (String(input).includes('/api/auth/telegram/refresh')) {
        return Promise.resolve(
          jsonResponse<'PublicRefreshResult'>({ accessToken: 'access-2', expiresIn: 1800 }),
        );
      }
      return Promise.resolve(jsonResponse<'PublicUserDto'>(USER));
    });
    vi.stubGlobal('fetch', fetchMock);

    renderGuard();

    expect(await screen.findByText('KABINET_MAZMUNI')).toBeInTheDocument();
    expect(usePublicUserStore.getState().accessToken).toBe('access-2');
    expect(usePublicUserStore.getState().user?.firstName).toBe('Ali');
  });

  it('refresh 401 bersa kirish sahifasiga qaytaradi va belgini tozalaydi', async () => {
    setSessionHint();
    usePublicUserStore.setState({ status: 'restoring', accessToken: null, user: null });
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('UNAUTHORIZED', 401)));

    renderGuard();

    expect(await screen.findByText(/KIRISH_STUB/)).toBeInTheDocument();
    expect(usePublicUserStore.getState().status).toBe('anonymous');
    expect(localStorage.getItem(STORAGE_KEYS.publicSessionHint)).toBeNull();
  });
});
