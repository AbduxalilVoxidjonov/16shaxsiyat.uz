import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import PublicRegistrationPage from './PublicRegistrationPage';
import { usePublicUserStore } from '../store/publicUserStore';

const START_RESULT = {
  sessionToken: 'session-token-1',
  assessmentId: 'assessment-1',
  status: 'Draft',
  expiresAt: '2026-09-12T10:12:00Z',
  resumed: false,
  tests: [
    {
      code: 'MBTI16',
      name: '16 tipli shaxsiyat modeli',
      status: 'NotStarted',
      answered: 0,
      total: 60,
      order: 1,
      estimatedMinutes: 9,
    },
  ],
} satisfies Schemas['StartSessionResult'];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/kabinet/test']}>
          <Routes>
            <Route path="/kabinet/test" element={<PublicRegistrationPage />} />
            <Route path="/t/:slug/test/:testCode" element={<p>TEST_STUB</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

interface FillOptions {
  year?: string;
  grade?: string;
}

/** Anketani to'ldiradi (kattalar uchun standart sana — ota-ona roziligi so'ralmaydi). */
async function fillForm(user: ReturnType<typeof userEvent.setup>, options: FillOptions = {}) {
  await user.type(screen.getByLabelText('F.I.Sh.'), 'Karimov Sardor Alisherovich');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan kun"), '12');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan oy"), '4');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan yil"), options.year ?? '1995');
  await user.click(screen.getByLabelText('Erkak'));
  if (options.grade) {
    await user.selectOptions(screen.getByLabelText('Sinf'), options.grade);
  }
  await user.type(screen.getByLabelText('Telefon raqami'), '901234567');
  await user.click(screen.getByLabelText(/roziman/));
}

describe('PublicRegistrationPage (maktabsiz anketa)', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken('access-1');
    usePublicUserStore.getState().setSession('access-1', {
      id: 'user-1',
      username: null,
      firstName: 'Ali',
      lastName: null,
      photoUrl: null,
      createdAt: '2026-09-01T10:00:00Z',
      lastLoginAt: '2026-09-05T10:00:00Z',
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  it("maktab maydonlarini SO'RAMAYDI (kirish kodi, sinf harfi, ota-ona telefoni)", () => {
    renderPage();

    expect(screen.queryByLabelText('Kirish kodi')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Sinf harfi')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Ota-ona telefon raqami')).not.toBeInTheDocument();
  });

  it("to'ldirilgan anketa `POST /api/me/sessions` ga shartnomadagi tanani yuboradi", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'StartSessionResult'>(START_RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await fillForm(user);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled();
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(String(url)).toContain('/api/me/sessions');
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer access-1');
    expect(JSON.parse(String(init.body))).toEqual({
      fullName: 'Karimov Sardor Alisherovich',
      birthDate: '1995-04-12',
      gender: 'Male',
      phone: '+998901234567',
      consentAccepted: true,
      parentalConsent: false,
      // Sinf tanlanmagan — `null` ("maktabda o'qimayman"), `0` EMAS.
      grade: null,
      email: null,
      languageCode: 'uz',
    });
  });

  it("sessiya ochilgach mavjud test oqimiga o'tadi va sessiya tokenini uzatadi", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'StartSessionResult'>(START_RESULT)),
    );

    renderPage();
    await fillForm(user);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
    // Sessiya `sessionStore` o'qiydigan kalitga yozildi — test oqimi uni `X-Session-Token`
    // sifatida yuboradi (`shared/api/sessionToken.ts`).
    const persisted = JSON.parse(localStorage.getItem(STORAGE_KEYS.session) ?? '{}') as {
      state?: { sessionToken?: string; slug?: string; assessmentId?: string };
    };
    expect(persisted.state?.sessionToken).toBe('session-token-1');
    expect(persisted.state?.slug).toBe('ommaviy');
    expect(persisted.state?.assessmentId).toBe('assessment-1');
  });

  it("18 yoshgacha ota-ona roziligi so'raladi va usiz yuborilmaydi", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'StartSessionResult'>(START_RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await fillForm(user, { year: '2012', grade: '9' });

    // Sana kiritilgach maydon paydo bo'ladi.
    const parentalConsent = await screen.findByLabelText(/Ota-onam/);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));
    await screen.findByText(/ota-ona \(qonuniy vakil\) roziligi majburiy/i);
    expect(fetchMock).not.toHaveBeenCalled();

    await user.click(parentalConsent);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled();
    });
    const body = JSON.parse(String((fetchMock.mock.calls[0] as [string, RequestInit])[1].body)) as {
      parentalConsent: boolean;
      grade: number | null;
    };
    expect(body.parentalConsent).toBe(true);
    expect(body.grade).toBe(9);
  });

  it("kattalarga ota-ona roziligi maydoni ko'rsatilmaydi", async () => {
    const user = userEvent.setup();
    vi.stubGlobal('fetch', vi.fn());

    renderPage();
    await fillForm(user);

    expect(screen.queryByLabelText(/Ota-onam/)).not.toBeInTheDocument();
  });

  it("409 DUPLICATE_ASSESSMENT uchun tushunarli xabar ko'rsatiladi", async () => {
    const user = userEvent.setup();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('DUPLICATE_ASSESSMENT', 409)));

    renderPage();
    await fillForm(user);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText(/yaqinda yakunlagansiz/)).toBeInTheDocument();
  });

  it("409 PUBLIC_SPACE_NOT_CONFIGURED uchun 'sozlanmagan' xabari chiqadi", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('PUBLIC_SPACE_NOT_CONFIGURED', 409)),
    );

    renderPage();
    await fillForm(user);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText(/hali sozlanmagan/)).toBeInTheDocument();
  });

  it("400 VALIDATION_ERROR maydon xatolarini o'z maydoniga bog'laydi", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        problemResponse('VALIDATION_ERROR', 400, 'Xato', {
          errors: { phone: ["Telefon raqami noto'g'ri."] },
        }),
      ),
    );

    renderPage();
    await fillForm(user);
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText("Telefon raqami noto'g'ri.")).toBeInTheDocument();
  });
});
