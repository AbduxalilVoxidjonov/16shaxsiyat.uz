import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { setAdminAccessToken } from '@/shared/api/adminClient';
import SettingsPage from './SettingsPage';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: body === undefined ? {} : { 'content-type': 'application/json' },
  });
}

function problemResponse(code: string, status: number, detail?: string): Response {
  return jsonResponse({ code, title: 'Xato', status, detail }, status);
}

function renderSettings() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <SettingsPage />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('SettingsPage', () => {
  beforeEach(() => {
    setAdminAccessToken('token-1');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setAdminAccessToken(null);
  });

  it("2FA holatini yuklab, o'chirilgan holatda 'Yoqish' tugmasini ko'rsatadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: false })),
    );

    renderSettings();

    expect(await screen.findByText('Yoqilmagan')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Yoqish' })).toBeInTheDocument();
  });

  it("parolni muvaffaqiyatli o'zgartirganda muvaffaqiyat bildirishnomasi chiqadi va forma tozalanadi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/auth/change-password')) {
        return Promise.resolve(jsonResponse(undefined, 204));
      }
      return Promise.resolve(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: false }));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.type(screen.getByLabelText('Joriy parol'), 'EskiParol1');
    await user.type(screen.getByLabelText('Yangi parol'), 'YangiParol1');
    await user.type(screen.getByLabelText('Yangi parolni tasdiqlang'), 'YangiParol1');
    await user.click(screen.getByRole('button', { name: 'Saqlash' }));

    expect(await screen.findByText("Parol muvaffaqiyatli o'zgartirildi.")).toBeInTheDocument();
    expect(screen.getByLabelText('Joriy parol')).toHaveValue('');
  });

  it("parol mos kelmasa yuborishdan oldin lokal xato ko'rsatiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: false })),
    );
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.type(screen.getByLabelText('Joriy parol'), 'EskiParol1');
    await user.type(screen.getByLabelText('Yangi parol'), 'YangiParol1');
    await user.type(screen.getByLabelText('Yangi parolni tasdiqlang'), 'Boshqasi1');
    await user.click(screen.getByRole('button', { name: 'Saqlash' }));

    expect(await screen.findByText('Parollar mos emas.')).toBeInTheDocument();
  });

  it("2FA yoqilganda maxfiy kalit va zaxira kodlar dialog oynasida ko'rsatiladi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/auth/totp/enable')) {
        return Promise.resolve(
          jsonResponse({
            secret: 'JBSWY3DPEHPK3PXP',
            otpauthUrl: 'otpauth://totp/Salohiyat:admin?secret=JBSWY3DPEHPK3PXP',
            recoveryCodes: ['CODE-1', 'CODE-2'],
          }),
        );
      }
      return Promise.resolve(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: false }));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.click(screen.getByRole('button', { name: 'Yoqish' }));

    expect(await screen.findByText('JBSWY3DPEHPK3PXP')).toBeInTheDocument();
    expect(screen.getByText('CODE-1')).toBeInTheDocument();
    expect(screen.getByText('CODE-2')).toBeInTheDocument();
  });

  it("2FA yoqilgan holatda 'O'chirish' bosilganda parol so'raladi va muvaffaqiyatda dialog yopiladi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/auth/totp/disable')) {
        return Promise.resolve(jsonResponse(undefined, 204));
      }
      return Promise.resolve(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: true }));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilgan');

    // Eslatma: jsdom `HTMLDialogElement.showModal()`ni amalga oshirmaydi (`Dialog.tsx`dagi
    // himoyalangan chaqiruv izohiga qarang) — dialog CSS orqali doim `display:none` bo'lib
    // qoladi, shu sabab `getByRole` (yashirin elementlarni chiqarib tashlaydi) ishlatib
    // bo'lmaydi; `getAllByText` esa ko'rinishdan qat'i nazar toppadi (DOM tartibi barqaror:
    // 0 — trigger tugma, 1 — dialog ichidagi tasdiqlash tugmasi).
    const [triggerButton, confirmButton] = screen.getAllByText("O'chirish");
    await user.click(triggerButton as HTMLElement);
    await user.type(screen.getByLabelText('Parol'), 'Sup3rSecret1');
    await user.click(confirmButton as HTMLElement);

    expect(await screen.findByText("2FA o'chirildi.")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByLabelText('Parol')).toHaveValue('');
    });
  });

  it("noto'g'ri joriy parolda backend xatosi maydonga bog'lanadi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/auth/change-password')) {
        return Promise.resolve(problemResponse('CURRENT_PASSWORD_INVALID', 400));
      }
      return Promise.resolve(jsonResponse({ id: 'u1', username: 'admin', totpEnabled: false }));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.type(screen.getByLabelText('Joriy parol'), 'NotoGri1');
    await user.type(screen.getByLabelText('Yangi parol'), 'YangiParol1');
    await user.type(screen.getByLabelText('Yangi parolni tasdiqlang'), 'YangiParol1');
    await user.click(screen.getByRole('button', { name: 'Saqlash' }));

    expect(await screen.findByText("Joriy parol noto'g'ri.")).toBeInTheDocument();
  });
});
