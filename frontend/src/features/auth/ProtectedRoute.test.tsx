import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ProtectedRoute } from './ProtectedRoute';
import { useAuthStore } from './store/authStore';
import { setAdminAccessToken } from '@/shared/api/adminClient';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';

/** `GET /api/auth/me` javobi — backend `AdminUserDto` (`email`/`role` majburiy). */
const ADMIN_USER = {
  id: 'u1',
  username: 'admin',
  email: 'admin@16shaxsiyat.uz',
  fullName: 'Bosh administrator',
  role: 'SuperAdmin',
  totpEnabled: false,
} satisfies Schemas['AdminUserDto'];

/**
 * `POST /api/auth/refresh` javobi — backend `RefreshResult(AccessToken, ExpiresIn)`.
 * Mock ilgari faqat `{accessToken}` qaytarardi; `expiresIn` majburiy maydoni tushib
 * qolgani tiplanmagan mock tufayli sezilmagan edi.
 */
const REFRESH_RESULT = {
  accessToken: 'restored-token',
  expiresIn: 1800,
} satisfies Schemas['RefreshResult'];

function renderProtected(initialPath = '/admin/students') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/admin/login" element={<div>LOGIN_STUB</div>} />
          <Route
            path="/admin/students"
            element={
              <ProtectedRoute>
                <div>PROTECTED_CONTENT</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    setAdminAccessToken(null);
    useAuthStore.setState({ isRestoring: true, accessToken: null, user: null });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setAdminAccessToken(null);
    useAuthStore.setState({ isRestoring: true, accessToken: null, user: null });
  });

  it("token yo'q va refresh cookie ham yaroqsiz bo'lsa `?returnUrl=` bilan login'ga yo'naltiradi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('UNAUTHORIZED', 401)));

    renderProtected('/admin/students');

    expect(await screen.findByText('LOGIN_STUB')).toBeInTheDocument();
  });

  it("token allaqachon bor bo'lsa (isRestoring: false) hech qanday so'rov yubormasdan darhol himoyalangan mazmunni ko'rsatadi", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    setAdminAccessToken('already-set-token');
    useAuthStore.setState({ isRestoring: false, accessToken: 'already-set-token', user: null });

    renderProtected('/admin/students');

    expect(await screen.findByText('PROTECTED_CONTENT')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("`GET /api/auth/me` (refresh cookie orqali, adminClient 401→refresh mantig'i bilan) muvaffaqiyatli bo'lsa sessiya tiklanadi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const headers = new Headers(init?.headers);

      if (url.endsWith('/api/auth/refresh')) {
        return Promise.resolve(jsonResponse<'RefreshResult'>(REFRESH_RESULT));
      }
      if (url.endsWith('/api/auth/me')) {
        // Birinchi urinishda token yo'q — 401; `adminClient` avtomatik refresh qilib qayta
        // yuboradi, shu safar `Authorization: Bearer restored-token` bilan keladi.
        if (headers.get('Authorization') === 'Bearer restored-token') {
          return Promise.resolve(jsonResponse<'AdminUserDto'>(ADMIN_USER));
        }
        return Promise.resolve(problemResponse('UNAUTHORIZED', 401));
      }
      throw new Error(`Kutilmagan so'rov: ${url}`);
    });
    vi.stubGlobal('fetch', fetchMock);

    renderProtected('/admin/students');

    expect(await screen.findByText('PROTECTED_CONTENT')).toBeInTheDocument();
    await waitFor(() => {
      expect(useAuthStore.getState().accessToken).toBe('restored-token');
    });
    expect(useAuthStore.getState().user).toEqual(ADMIN_USER);
  });

  // REGRESSIYA (2026-09-02, egasining xabari): API bir lahzaga javob bermasa — konteyner
  // qayta ishga tushayotgan bo'lsa `502`, tarmoq uzilsa `status: 0` — ilgari `ProtectedRoute`
  // har qanday xatoda `clear()` chaqirib, HAQIQIY sessiyasi bor superadminni login sahifasiga
  // uloqtirardi. Endi sessiya faqat serverning aniq `401`ida tugatiladi.
  it("`502` kelganda sessiyani tugatmaydi va login'ga yubormaydi — qayta urinish taklif qiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('<html>502</html>', { status: 502 })),
    );

    renderProtected('/admin/students');

    expect(await screen.findByRole('button', { name: /qayta/i })).toBeInTheDocument();
    expect(screen.queryByText('LOGIN_STUB')).not.toBeInTheDocument();
    // Eng muhimi: `isRestoring` o'chirilmagan, ya'ni sessiya "yo'q" deb belgilanmagan.
    expect(useAuthStore.getState().isRestoring).toBe(true);
  });

  it("tarmoq uzilganda ham (fetch reject) sessiyani tugatmaydi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    renderProtected('/admin/students');

    expect(await screen.findByRole('button', { name: /qayta/i })).toBeInTheDocument();
    expect(screen.queryByText('LOGIN_STUB')).not.toBeInTheDocument();
    expect(useAuthStore.getState().isRestoring).toBe(true);
  });

  it("tiklash davomida spinner (yuklanish holati) ko'rsatiladi, darhol login'ga qaytarmaydi", () => {
    // `fetch` hech qachon `resolve` bo'lmaydi — `isRestoring: true` holatida qolamiz.
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation(() => new Promise(() => {})),
    );

    renderProtected('/admin/students');

    expect(screen.queryByText('LOGIN_STUB')).not.toBeInTheDocument();
    expect(screen.queryByText('PROTECTED_CONTENT')).not.toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
