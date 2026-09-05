import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import PublicLoginPage from './PublicLoginPage';
import { usePublicUserStore } from '../store/publicUserStore';

vi.mock('@/shared/config/env', () => ({
  env: {
    apiBaseUrl: '',
    appName: 'Shaxsiyat',
    sentryDsn: '',
    telegramBot: 'shaxsiyat_login_bot',
  },
}));

const LOGIN_RESULT = {
  accessToken: 'access-1',
  expiresIn: 1800,
  isNewUser: true,
  user: {
    id: 'user-1',
    username: 'alivali',
    firstName: 'Ali',
    lastName: 'Valiyev',
    photoUrl: null,
    createdAt: '2026-09-05T10:12:00Z',
    lastLoginAt: '2026-09-05T10:12:00Z',
  },
} satisfies Schemas['TelegramLoginResult'];

/** Telegram widget bergan obyekt — `username` yo'q (Telegram bermagan). */
const TELEGRAM_PAYLOAD = {
  id: 123456789,
  first_name: 'Ali',
  auth_date: 1767225600,
  hash: 'a'.repeat(64),
};

function renderPage(initialPath = '/kirish') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/kirish" element={<PublicLoginPage />} />
            <Route path="/kabinet" element={<p>KABINET_STUB</p>} />
            <Route path="/kabinet/test" element={<p>ANKETA_STUB</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

async function triggerTelegramAuth() {
  await act(async () => {
    window.onTelegramAuth?.(TELEGRAM_PAYLOAD);
  });
}

describe('PublicLoginPage', () => {
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
    delete window.onTelegramAuth;
  });

  it("Telegram widgetini va kirish taklifini ko'rsatadi", () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Telegram orqali kiring');
    expect(document.querySelector('script[data-telegram-login]')).not.toBeNull();
  });

  it("widget javobini O'ZGARTIRMASDAN yuboradi va muvaffaqiyatda kabinetga o'tadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await triggerTelegramAuth();

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(String(url)).toContain('/api/auth/telegram');
    // Refresh token cookie'da qaytadi — `credentials: 'include'` SHART.
    expect(init.credentials).toBe('include');
    // Tana AYNAN Telegram bergan obyekt: `username`/`photo_url` qo'shilmaydi (bo'sh satr
    // imzoni buzardi), kalitlar `snake_case` qoladi.
    expect(JSON.parse(String(init.body))).toEqual(TELEGRAM_PAYLOAD);

    expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();
    expect(usePublicUserStore.getState().accessToken).toBe('access-1');
  });

  it("`returnUrl` berilgan bo'lsa kirgandan keyin o'sha sahifaga qaytaradi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT)),
    );

    renderPage('/kirish?returnUrl=%2Fkabinet%2Ftest');
    await triggerTelegramAuth();

    expect(await screen.findByText('ANKETA_STUB')).toBeInTheDocument();
  });

  it("tashqi `returnUrl` e'tiborsiz qoldiriladi (ochiq redirect himoyasi)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT)),
    );

    renderPage('/kirish?returnUrl=%2F%2Fevil.example');
    await triggerTelegramAuth();

    expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();
  });

  it("503 TELEGRAM_AUTH_NOT_CONFIGURED uchun tushunarli xabar ko'rsatadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('TELEGRAM_AUTH_NOT_CONFIGURED', 503)),
    );

    renderPage();
    await triggerTelegramAuth();

    expect(
      await screen.findByText(/Telegram kirishi serverda hali sozlanmagan/),
    ).toBeInTheDocument();
  });

  it("401 TELEGRAM_AUTH_EXPIRED uchun 'muddati o'tgan' xabari chiqadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('TELEGRAM_AUTH_EXPIRED', 401)),
    );

    renderPage();
    await triggerTelegramAuth();

    expect(await screen.findByText(/muddati o'tgan/)).toBeInTheDocument();
  });

  it('429 RATE_LIMITED uchun kutish haqidagi xabar chiqadi', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('RATE_LIMITED', 429)));

    renderPage();
    await triggerTelegramAuth();

    expect(await screen.findByText(/Juda ko'p urinish/)).toBeInTheDocument();
  });

  it("allaqachon kirgan foydalanuvchini kabinetga yo'naltiradi", () => {
    usePublicUserStore.getState().setSession('access-1', LOGIN_RESULT.user);

    renderPage();

    expect(screen.getByText('KABINET_STUB')).toBeInTheDocument();
  });
});
