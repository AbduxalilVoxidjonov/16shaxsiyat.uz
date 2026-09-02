import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import LoginPage from './LoginPage';
import { useAuthStore } from '../store/authStore';
import { getAdminAccessToken } from '@/shared/api/adminClient';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function problemResponse(code: string, status: number, detail?: string): Response {
  return jsonResponse(
    { code, title: 'Xato', status, detail, type: `https://studentroadmap/errors/${code}` },
    status,
  );
}

function renderLoginPage(initialPath = '/admin/login') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/admin/login" element={<LoginPage />} />
          <Route path="/admin" element={<div>DASHBOARD_STUB</div>} />
          <Route path="/admin/students" element={<div>STUDENTS_STUB</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function submitLogin(username = 'admin', password = 'Sup3rSecret1') {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Login'), username);
  await user.type(screen.getByLabelText('Parol'), password);
  await user.click(screen.getByRole('button', { name: 'Kirish' }));
  return user;
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ isRestoring: false, accessToken: null, user: null });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    useAuthStore.setState({ isRestoring: false, accessToken: null, user: null });
  });

  it("muvaffaqiyatli login qilinganda boshqaruv paneliga o'tadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          accessToken: 'access-123',
          expiresIn: 1800,
          user: { id: 'u1', username: 'admin' },
        }),
      ),
    );

    renderLoginPage();
    await submitLogin();

    expect(await screen.findByText('DASHBOARD_STUB')).toBeInTheDocument();
    expect(useAuthStore.getState().accessToken).toBe('access-123');
    expect(useAuthStore.getState().user).toEqual({ id: 'u1', username: 'admin' });
  });

  it("returnUrl bo'lsa muvaffaqiyatli logindan keyin o'sha sahifaga o'tadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          accessToken: 'access-123',
          expiresIn: 1800,
          user: { id: 'u1', username: 'admin' },
        }),
      ),
    );

    renderLoginPage('/admin/login?returnUrl=%2Fadmin%2Fstudents');
    await submitLogin();

    expect(await screen.findByText('STUDENTS_STUB')).toBeInTheDocument();
  });

  it("access token hech qachon localStorage'ga yozilmaydi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          accessToken: 'access-123',
          expiresIn: 1800,
          user: { id: 'u1', username: 'admin' },
        }),
      ),
    );

    renderLoginPage();
    await submitLogin();

    await screen.findByText('DASHBOARD_STUB');

    expect(getAdminAccessToken()).toBe('access-123');
    expect(localStorage.length).toBe(0);
    expect(JSON.stringify(localStorage)).not.toContain('access-123');
  });

  it("noto'g'ri login/parolda qaysi maydon xato ekanini aytmaydigan umumiy xabar chiqadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('INVALID_CREDENTIALS', 401)));

    renderLoginPage();
    await submitLogin();

    expect(await screen.findByText("Login yoki parol noto'g'ri.")).toBeInTheDocument();
    expect(useAuthStore.getState().accessToken).toBeNull();
  });

  it("blokirovka holatida tushunarli xabar ko'rsatiladi va forma o'chiriladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          problemResponse(
            'ACCOUNT_LOCKED',
            403,
            "Hisob 15 daqiqaga bloklandi. Birozdan so'ng qayta urinib ko'ring.",
          ),
        ),
    );

    renderLoginPage();
    await submitLogin();

    expect(
      await screen.findByText("Hisob 15 daqiqaga bloklandi. Birozdan so'ng qayta urinib ko'ring."),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Login')).toBeDisabled();
    expect(screen.getByLabelText('Parol')).toBeDisabled();
  });

  it("backend TOTP_REQUIRED qaytarsa 2FA maydoni faqat shundan keyin ko'rinadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('TOTP_REQUIRED', 401)));

    renderLoginPage();

    expect(screen.queryByLabelText('Tasdiqlash kodi (2FA)')).not.toBeInTheDocument();

    await submitLogin();

    expect(await screen.findByLabelText('Tasdiqlash kodi (2FA)')).toBeInTheDocument();
  });

  it("TOTP kodi kiritilib qayta yuborilganda so'rov tanasida totpCode bo'ladi", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation((_input: RequestInfo | URL, init?: RequestInit) => {
        const body = init?.body ? (JSON.parse(String(init.body)) as { totpCode?: string }) : {};
        if (body.totpCode === '123456') {
          return Promise.resolve(
            jsonResponse({
              accessToken: 'access-456',
              expiresIn: 1800,
              user: { id: 'u1', username: 'admin' },
            }),
          );
        }
        return Promise.resolve(problemResponse('TOTP_REQUIRED', 401));
      });
    vi.stubGlobal('fetch', fetchMock);

    renderLoginPage();
    const user = await submitLogin();

    const totpInput = await screen.findByLabelText('Tasdiqlash kodi (2FA)');
    await user.type(totpInput, '123456');
    await user.click(screen.getByRole('button', { name: 'Kirish' }));

    expect(await screen.findByText('DASHBOARD_STUB')).toBeInTheDocument();
  });

  it('tarmoq xatosida umumiy xato xabari chiqadi', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    renderLoginPage();
    await submitLogin();

    await waitFor(() => {
      expect(
        screen.getByText("Kirishda xatolik yuz berdi. Birozdan so'ng qayta urinib ko'ring."),
      ).toBeInTheDocument();
    });
  });
});
