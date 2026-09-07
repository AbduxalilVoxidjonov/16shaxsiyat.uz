import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation, useParams } from 'react-router';
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

const HASH = 'a'.repeat(64);

/**
 * Telegram redirect'i: `data-auth-url` bilan chizilgan widget foydalanuvchini shu
 * parametrlar bilan qaytaradi. `username`/`photo_url` YO'Q — Telegram ularni bermagan,
 * ya'ni so'rov tanasiga ham tushmasligi kerak (bo'sh satr imzoni buzardi).
 */
const CALLBACK_QUERY = `id=123456789&first_name=Ali&last_name=Valiyev&auth_date=1767225600&hash=${HASH}`;

const EXPECTED_BODY = {
  id: 123456789,
  first_name: 'Ali',
  last_name: 'Valiyev',
  auth_date: 1767225600,
  hash: HASH,
};

/** `/t/:slug` stub — maktab oqimiga o'tilganini va `?k=` saqlanganini ko'rsatadi. */
function LandingStub() {
  const { slug } = useParams<{ slug: string }>();
  const { search } = useLocation();
  return <p>LANDING_STUB {slug} {search}</p>;
}

function renderPage(initialPath = '/kirish') {
  // Manzil satri MemoryRouter'dan mustaqil — `history.replaceState` aynan brauzer
  // manzilini tozalaydi, shu sabab jsdom URL'i ham mos qo'yiladi.
  window.history.replaceState(null, '', initialPath);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/kirish" element={<PublicLoginPage />} />
            <Route path="/kabinet" element={<p>KABINET_STUB</p>} />
            <Route path="/kabinet/test" element={<p>ANKETA_STUB</p>} />
            <Route path="/t/:slug" element={<LandingStub />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function widgetScript(): HTMLScriptElement | null {
  return document.querySelector('script[data-telegram-login]');
}

describe('PublicLoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
    window.history.replaceState(null, '', '/');
  });

  it("Telegram widgetini MUTLAQ `data-auth-url` bilan ko'rsatadi", () => {
    renderPage();

    // Sahifa endi ikki kartali: Telegram sarlavhasi h2 (h1 — umumiy "Shaxsiyat testiga kirish").
    expect(screen.getByRole('heading', { name: 'Telegram orqali kiring' })).toBeInTheDocument();
    const authUrl = widgetScript()?.getAttribute('data-auth-url');
    // Telegram nisbiy yo'lni qabul qilmaydi — manzil origin bilan birga bo'lishi shart.
    expect(authUrl).toBe(`${window.location.origin}/kirish`);
    expect(widgetScript()?.hasAttribute('data-onauth')).toBe(false);
  });

  it('`returnUrl` callback manziliga ham qo‘shiladi (Telegram uni qaytaradi)', () => {
    renderPage('/kirish?returnUrl=%2Fkabinet%2Ftest');

    expect(widgetScript()?.getAttribute('data-auth-url')).toBe(
      `${window.location.origin}/kirish?returnUrl=%2Fkabinet%2Ftest`,
    );
  });

  it("oddiy tashrifda hech qanday so'rov yubormaydi", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    renderPage();

    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("callback parametrlarini O'ZGARTIRMASDAN yuboradi va kabinetga o'tadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage(`/kirish?${CALLBACK_QUERY}`);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(String(url)).toContain('/api/auth/telegram');
    // Refresh token cookie'da qaytadi — `credentials: 'include'` SHART.
    expect(init.credentials).toBe('include');
    // Tana AYNAN Telegram bergan qiymatlar: kalitlar `snake_case`, `username`/`photo_url`
    // qo'shilmaydi (bo'sh satr imzoni buzardi).
    expect(JSON.parse(String(init.body))).toEqual(EXPECTED_BODY);

    expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();
    expect(usePublicUserStore.getState().accessToken).toBe('access-1');
  });

  it("so'rovdan keyin DARHOL query parametrlarni manzildan olib tashlaydi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT));
    vi.stubGlobal('fetch', fetchMock);
    const replaceSpy = vi.spyOn(window.history, 'replaceState');

    renderPage(`/kirish?returnUrl=%2Fkabinet%2Ftest&${CALLBACK_QUERY}`);

    // Tozalash javobni KUTMAYDI — so'rov yuborilishi bilan bajariladi.
    expect(replaceSpy).toHaveBeenCalled();
    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    // Telegram maydonlari yo'q, `returnUrl` esa saqlangan.
    expect(window.location.search).toBe('?returnUrl=%2Fkabinet%2Ftest');
    expect(window.location.href).not.toContain('Ali');
    expect(window.location.href).not.toContain(HASH);
  });

  it('bitta callback ikki marta YUBORILMAYDI (qayta render)', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage(`/kirish?${CALLBACK_QUERY}`);
    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    // Muvaffaqiyatdan keyin sahifa qayta renderlanadi (`setSession` + `navigate`) —
    // effekt yana ishga tushsa ham `hash` ikkinchi marta yuborilmaydi.
    expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("`returnUrl` berilgan bo'lsa kirgandan keyin o'sha sahifaga qaytaradi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT)),
    );

    renderPage(`/kirish?returnUrl=%2Fkabinet%2Ftest&${CALLBACK_QUERY}`);

    expect(await screen.findByText('ANKETA_STUB')).toBeInTheDocument();
  });

  it("tashqi `returnUrl` e'tiborsiz qoldiriladi (ochiq redirect himoyasi)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'TelegramLoginResult'>(LOGIN_RESULT)),
    );

    renderPage(`/kirish?returnUrl=%2F%2Fevil.example&${CALLBACK_QUERY}`);

    expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();
  });

  it("503 TELEGRAM_AUTH_NOT_CONFIGURED uchun tushunarli xabar ko'rsatadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('TELEGRAM_AUTH_NOT_CONFIGURED', 503)),
    );

    renderPage(`/kirish?${CALLBACK_QUERY}`);

    expect(
      await screen.findByText(/Telegram kirishi serverda hali sozlanmagan/),
    ).toBeInTheDocument();
  });

  it("401 TELEGRAM_AUTH_EXPIRED uchun 'muddati o'tgan' xabari chiqadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('TELEGRAM_AUTH_EXPIRED', 401)),
    );

    renderPage(`/kirish?${CALLBACK_QUERY}`);

    expect(await screen.findByText(/muddati o'tgan/)).toBeInTheDocument();
  });

  it('401 TELEGRAM_AUTH_INVALID uchun tasdiqlanmadi xabari chiqadi va tugma qaytadi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('TELEGRAM_AUTH_INVALID', 401)),
    );

    renderPage(`/kirish?${CALLBACK_QUERY}`);

    expect(await screen.findByText(/tasdiqlanmadi/)).toBeInTheDocument();
    // Xatodan keyin foydalanuvchi qayta urina olishi kerak — widget yana ko'rinadi.
    expect(widgetScript()).not.toBeNull();
  });

  it('429 RATE_LIMITED uchun kutish haqidagi xabar chiqadi', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('RATE_LIMITED', 429)));

    renderPage(`/kirish?${CALLBACK_QUERY}`);

    expect(await screen.findByText(/Juda ko'p urinish/)).toBeInTheDocument();
  });

  it("allaqachon kirgan foydalanuvchini kabinetga yo'naltiradi", () => {
    usePublicUserStore.getState().setSession('access-1', LOGIN_RESULT.user);

    renderPage();

    expect(screen.getByText('KABINET_STUB')).toBeInTheDocument();
  });

  describe('Maktab uchun (kod bilan kirish)', () => {
    async function openCodeForm() {
      const user = userEvent.setup();
      renderPage();
      await user.click(screen.getByRole('button', { name: 'Maktab uchun' }));
      return user;
    }

    it("ikki teng karta ko'rsatadi: Telegram va Maktab uchun", () => {
      renderPage();

      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Shaxsiyat testiga kirish');
      expect(screen.getByRole('heading', { level: 2, name: 'Telegram orqali kiring' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { level: 2, name: 'Maktab uchun' })).toBeInTheDocument();
      expect(screen.getByText('Kodni maktabingiz beradi. Akkaunt ochish shart emas.')).toBeInTheDocument();
      // Telegram widget ham joyida — ikki yo'l bir-biriga xalaqit bermaydi.
      expect(widgetScript()).not.toBeNull();
    });

    it("tugma bosilgach kod maydoni ochiladi (autoComplete o'chiq)", async () => {
      await openCodeForm();

      const input = screen.getByLabelText('Maktab kodi');
      expect(input).toHaveAttribute('autocomplete', 'off');
      expect(input).toHaveAttribute('placeholder', 'XXXX-XXXX');
      expect(screen.getByRole('button', { name: 'Davom etish' })).toBeInTheDocument();
    });

    it("kodni normalize qilib (katta harf, defissiz) TANADA yuboradi va maktab oqimiga o'tadi", async () => {
      const fetchMock = vi
        .fn()
        .mockResolvedValue(
          jsonResponse<'ResolveSchoolCodeResult'>({ slug: '12-maktab', accessToken: 'tok' }),
        );
      vi.stubGlobal('fetch', fetchMock);
      const user = await openCodeForm();

      const input = screen.getByLabelText('Maktab kodi');
      await user.type(input, '7k3m-9xq2');
      // Ko'rsatish shakli — `XXXX-XXXX`.
      expect(input).toHaveValue('7K3M-9XQ2');
      await user.click(screen.getByRole('button', { name: 'Davom etish' }));

      await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
      const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
      expect(String(url)).toContain('/api/public/schools/resolve-code');
      expect(init.method).toBe('POST');
      // Kod URL'da EMAS, tanada; defis/kichik harf yechilgan.
      expect(String(url)).not.toContain('7K3M');
      expect(JSON.parse(String(init.body))).toEqual({ code: '7K3M9XQ2' });

      // Mavjud maktab oqimi: `/t/{slug}?k={accessToken}`.
      expect(await screen.findByText('LANDING_STUB 12-maktab ?k=tok')).toBeInTheDocument();
    });

    it("to'liq bo'lmagan kod bilan so'rov yuborilmaydi, xato ko'rsatiladi", async () => {
      const fetchMock = vi.fn();
      vi.stubGlobal('fetch', fetchMock);
      const user = await openCodeForm();

      await user.type(screen.getByLabelText('Maktab kodi'), '7k3m');
      await user.click(screen.getByRole('button', { name: 'Davom etish' }));

      expect(fetchMock).not.toHaveBeenCalled();
      expect(screen.getByRole('alert')).toHaveTextContent("8 belgili kodni to'liq kiriting.");
    });

    it("404 SCHOOL_CODE_INVALID uchun generic 'Kod topilmadi' xabari", async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SCHOOL_CODE_INVALID', 404)));
      const user = await openCodeForm();

      await user.type(screen.getByLabelText('Maktab kodi'), 'ABCD2345');
      await user.click(screen.getByRole('button', { name: 'Davom etish' }));

      expect(await screen.findByText('Kod topilmadi. Maktabingizdan tekshiring.')).toBeInTheDocument();
      // Foydalanuvchi shu yerda qoladi — hech qayerga yo'naltirilmaydi.
      expect(screen.queryByText(/LANDING_STUB/)).not.toBeInTheDocument();
    });

    it('429 RATE_LIMITED uchun kutish xabari', async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('RATE_LIMITED', 429)));
      const user = await openCodeForm();

      await user.type(screen.getByLabelText('Maktab kodi'), 'ABCD2345');
      await user.click(screen.getByRole('button', { name: 'Davom etish' }));

      expect(await screen.findByText(/Juda ko'p urinish/)).toBeInTheDocument();
    });
  });
});
