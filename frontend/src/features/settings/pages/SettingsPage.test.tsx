import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { setAdminAccessToken } from '@/shared/api/adminClient';
import { TOTP_BACKUP_CODE_COUNT } from '../model/types';
import SettingsPage from './SettingsPage';
import { emptyResponse, jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';

const BACKUP_CODES = [
  '10000001',
  '10000002',
  '10000003',
  '10000004',
  '10000005',
  '10000006',
  '10000007',
  '10000008',
] as const;

/**
 * `GET /api/auth/me` javobi — backend `AdminUserDto`: sahifa faqat `totpEnabled` ni
 * o'qiydi, lekin javob shakli to'liq DTO (`email`/`role` majburiy). Ilgari mock
 * `{id, username, totpEnabled}` chala shaklda edi.
 */
const ME_TOTP_OFF = {
  id: 'u1',
  username: 'admin',
  email: 'admin@16shaxsiyat.uz',
  fullName: 'Bosh administrator',
  role: 'SuperAdmin',
  totpEnabled: false,
} satisfies Schemas['AdminUserDto'];

const ME_TOTP_ON = { ...ME_TOTP_OFF, totpEnabled: true } satisfies Schemas['AdminUserDto'];

/**
 * `POST /api/auth/totp/enable` javobi — backend `EnableTotpResult(Secret, OtpauthUri, BackupCodes)`
 * bilan **aynan bir xil** shakl (`shared/api/schema.d.ts` → `components['schemas']['EnableTotpResult']`).
 * Ilgari bu mock frontendning o'z taxminini (`otpauthUrl`/`recoveryCodes`) takrorlar edi va shu
 * sababli haqiqiy yiqilishni ushlamagan — mock backend shartnomasidan uzilib qolmasligi shart.
 * Endi shakl `jsonResponse<'EnableTotpResult'>` orqali `tsc` da tekshiriladi.
 */
const TOTP_ENABLE_RESULT = {
  secret: 'JBSWY3DPEHPK3PXP',
  otpauthUri: 'otpauth://totp/Shaxsiyat:admin?secret=JBSWY3DPEHPK3PXP&issuer=Shaxsiyat',
  backupCodes: [...BACKUP_CODES],
} satisfies Schemas['EnableTotpResult'];

/**
 * `POST /api/auth/totp/disable` so'rov tanasi — backend `DisableTotpRequest(CurrentPassword)`.
 * `satisfies` shu maydon nomini sxemaga bog'laydi: agar frontend yana `password` ga
 * qaytsa (2026-09-02 dagi buzilish) `tsc` xato beradi, test jimgina yashil qolmaydi.
 */
const DISABLE_TOTP_BODY = {
  currentPassword: 'Sup3rSecret1',
} satisfies Schemas['DisableTotpRequest'];

function totpEnableFetchMock() {
  return vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/api/auth/totp/enable')) {
      return Promise.resolve(jsonResponse<'EnableTotpResult'>(TOTP_ENABLE_RESULT));
    }
    return Promise.resolve(jsonResponse<'AdminUserDto'>(ME_TOTP_OFF));
  });
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
      vi.fn().mockResolvedValue(jsonResponse<'AdminUserDto'>(ME_TOTP_OFF)),
    );

    renderSettings();

    expect(await screen.findByText('Yoqilmagan')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Yoqish' })).toBeInTheDocument();
  });

  it("parolni muvaffaqiyatli o'zgartirganda muvaffaqiyat bildirishnomasi chiqadi va forma tozalanadi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/auth/change-password')) {
        return Promise.resolve(emptyResponse(204));
      }
      return Promise.resolve(jsonResponse<'AdminUserDto'>(ME_TOTP_OFF));
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
      vi.fn().mockResolvedValue(jsonResponse<'AdminUserDto'>(ME_TOTP_OFF)),
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
    vi.stubGlobal('fetch', totpEnableFetchMock());
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.click(screen.getByRole('button', { name: 'Yoqish' }));

    expect(await screen.findByText('JBSWY3DPEHPK3PXP')).toBeInTheDocument();
    expect(screen.getByText(BACKUP_CODES[0])).toBeInTheDocument();
    expect(screen.getByText(BACKUP_CODES[7])).toBeInTheDocument();
  });

  it("zaxira kodlar backend javobining haqiqiy shakli bilan to'liq (8 ta) render bo'ladi", async () => {
    vi.stubGlobal('fetch', totpEnableFetchMock());
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');

    await user.click(screen.getByRole('button', { name: 'Yoqish' }));

    const list = await screen.findByRole('list', {
      name: /Zaxira kodlar/,
      hidden: true,
    });
    expect(within(list).getAllByRole('listitem', { hidden: true })).toHaveLength(
      TOTP_BACKUP_CODE_COUNT,
    );
    for (const code of BACKUP_CODES) {
      expect(within(list).getByText(code)).toBeInTheDocument();
    }
  });

  it('zaxira kodlar saqlangani tasdiqlanmaguncha dialog yopilmaydi', async () => {
    vi.stubGlobal('fetch', totpEnableFetchMock());
    const user = userEvent.setup();

    renderSettings();
    await screen.findByText('Yoqilmagan');
    await user.click(screen.getByRole('button', { name: 'Yoqish' }));
    await screen.findByText(BACKUP_CODES[0]);

    // Yopish tugmasi (`Dialog`ning X'i) — 2FA server tomonda allaqachon yoqilgan,
    // shuning uchun tasdiqlashsiz yopish kodlarni butunlay yo'qotgan bo'lardi.
    // DOM tartibi barqaror: 0 — zaxira kodlar dialogi, 1 — 2FA'ni o'chirish dialogi.
    const [backupDialogCloseButton] = screen.getAllByLabelText('Yopish');
    await user.click(backupDialogCloseButton as HTMLElement);

    expect(screen.getByText(BACKUP_CODES[0])).toBeInTheDocument();
    expect(
      screen.getByText(
        "Oynani yopishdan oldin zaxira kodlarni saqlaganingizni tasdiqlang — ular boshqa ko'rsatilmaydi.",
      ),
    ).toBeInTheDocument();

    // Tasdiqlangach yopiladi.
    await user.click(screen.getByLabelText('Zaxira kodlarni xavfsiz joyda saqlab oldim'));
    await user.click(screen.getByText('Saqlab oldim'));

    await waitFor(() => {
      expect(screen.queryByText(BACKUP_CODES[0])).not.toBeInTheDocument();
    });
  });

  it("2FA yoqilgan holatda 'O'chirish' bosilganda parol so'raladi va muvaffaqiyatda dialog yopiladi", async () => {
    const disableBodies: Schemas['DisableTotpRequest'][] = [];
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/auth/totp/disable')) {
        disableBodies.push(JSON.parse(String(init?.body)) as Schemas['DisableTotpRequest']);
        return Promise.resolve(emptyResponse(204));
      }
      return Promise.resolve(jsonResponse<'AdminUserDto'>(ME_TOTP_ON));
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
    await user.type(screen.getByLabelText('Parol'), DISABLE_TOTP_BODY.currentPassword);
    await user.click(confirmButton as HTMLElement);

    expect(await screen.findByText("2FA o'chirildi.")).toBeInTheDocument();
    // Backend `DisableTotpRequest(string CurrentPassword)` kutadi (docs/07, 2-bo'lim) —
    // maydon nomi mos kelmasa so'rov 400 bilan qaytardi.
    expect(disableBodies).toEqual([DISABLE_TOTP_BODY]);
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
      return Promise.resolve(jsonResponse<'AdminUserDto'>(ME_TOTP_OFF));
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
