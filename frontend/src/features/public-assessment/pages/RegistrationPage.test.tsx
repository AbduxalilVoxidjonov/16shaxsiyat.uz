import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import RegistrationPage from './RegistrationPage';
import { useSessionStore } from '../store/sessionStore';

const CONSENT_TEXT = "Farzandimning testdan o'tishiga roziman.";
const CONSENT_LABEL = "Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman";

function schoolInfoBody(overrides: Record<string, unknown> = {}) {
  return {
    schoolId: 'school-1',
    name: "12-son umumiy o'rta ta'lim maktabi",
    region: "Farg'ona",
    district: "Qo'qon",
    requiresAccessCode: false,
    tests: [
      { code: 'MBTI16', name: '16 tipli shaxsiyat modeli', questionCount: 60, estimatedMinutes: 9, order: 1 },
    ],
    totalEstimatedMinutes: 9,
    consentText: CONSENT_TEXT,
    ...overrides,
  };
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function problemResponse(
  code: string,
  status: number,
  extra: Record<string, unknown> = {},
): Response {
  return jsonResponse(
    { code, title: 'Xato', status, detail: extra.detail, type: `https://studentroadmap/errors/${code}`, ...extra },
    status,
  );
}

interface RouterMockOptions {
  schoolInfo?: Record<string, unknown>;
  postSessionResponse?: () => Response | Promise<Response>;
}

function mockFetch({ schoolInfo, postSessionResponse }: RouterMockOptions) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.includes('/api/public/schools/')) {
      return Promise.resolve(jsonResponse(schoolInfo ?? schoolInfoBody()));
    }
    if (url.includes('/api/public/sessions') && init?.method === 'POST') {
      return Promise.resolve(
        postSessionResponse
          ? postSessionResponse()
          : jsonResponse({
              sessionToken: 'sess-token-1',
              assessmentId: 'assessment-1',
              status: 'Draft',
              expiresAt: '2026-09-10T00:00:00Z',
              resumed: false,
              tests: [{ code: 'MBTI16', status: 'NotStarted', answered: 0, total: 60, order: 1 }],
            }),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderRegistration(initialPath = '/t/demo-school/register?k=tok123') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/t/:slug/register" element={<RegistrationPage />} />
            <Route path="/t/:slug/test/:testCode" element={<div>TEST_STUB</div>} />
            <Route path="/t/:slug/finish" element={<div>FINISH_STUB</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('F.I.Sh.'), 'Aliyev Sardor Bekzodovich');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan kun"), '17');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan oy"), '4');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan yil"), '2015');
  await user.click(screen.getByLabelText("O'g'il bola"));
  await user.selectOptions(screen.getByLabelText('Sinf'), '9');
  await user.type(screen.getByLabelText('Telefon raqami'), '901234567');
}

describe('RegistrationPage', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
    vi.useFakeTimers({ shouldAdvanceTime: true });
    vi.setSystemTime(new Date(Date.UTC(2026, 8, 2)));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it('rozilik matnini API javobidan (consentText) ko\'rsatadi', async () => {
    mockFetch({});
    renderRegistration();

    expect(await screen.findByText(CONSENT_TEXT)).toBeInTheDocument();
  });

  it("rozilik belgilanmaguncha 'Testni boshlash' tugmasi o'chiq turadi", async () => {
    mockFetch({});
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    const submitButton = screen.getByRole('button', { name: 'Testni boshlash' });
    expect(submitButton).toBeDisabled();

    await user.click(screen.getByLabelText(CONSENT_LABEL));
    expect(submitButton).toBeEnabled();

    await user.click(screen.getByLabelText(CONSENT_LABEL));
    expect(submitButton).toBeDisabled();
  });

  it('telefon maydoni +998 (__) ___-__-__ maskasida formatlaydi', async () => {
    mockFetch({});
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    const phoneInput = screen.getByLabelText('Telefon raqami');
    await user.type(phoneInput, '901234567');

    expect(phoneInput).toHaveValue('(90) 123-45-67');
    expect(screen.getAllByText('+998').length).toBeGreaterThan(0);
  });

  it("to'liq to'ldirilgan forma yuborilganda sessionStore yangilanadi va birinchi testga o'tadi", async () => {
    mockFetch({});
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    await waitFor(() => {
      expect(useSessionStore.getState().sessionToken).toBe('sess-token-1');
    });
    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
  });

  it('Enter tugmasi formani yuboradi (klaviatura bilan to\'ldirish)', async () => {
    mockFetch({});
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));

    await user.type(screen.getByLabelText('F.I.Sh.'), '{Enter}');

    await waitFor(() => {
      expect(useSessionStore.getState().sessionToken).toBe('sess-token-1');
    });
  });

  it("409 DUPLICATE_ASSESSMENT bo'lsa tushunarli xabar ko'rsatadi", async () => {
    mockFetch({ postSessionResponse: () => problemResponse('DUPLICATE_ASSESSMENT', 409) });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(
      await screen.findByText(
        "Sen allaqachon testni topshirgansan. Qayta topshirish uchun o'qituvchingga murojaat qil.",
      ),
    ).toBeInTheDocument();
    expect(useSessionStore.getState().sessionToken).toBeNull();
  });

  it("429 RATE_LIMITED bo'lsa limit haqida xabar ko'rsatadi", async () => {
    mockFetch({ postSessionResponse: () => problemResponse('RATE_LIMITED', 429) });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText('Juda ko\'p urinish bo\'ldi. Birozdan keyin qayta urinib ko\'ring.')).toBeInTheDocument();
  });

  it("400 VALIDATION_ERROR maydon xatolarini tegishli maydon ostida ko'rsatadi (backend camelCase kalitlar)", async () => {
    mockFetch({
      postSessionResponse: () =>
        problemResponse('VALIDATION_ERROR', 400, {
          // Backend `ValidationException.ToCamelCasePropertyPath` orqali camelCase qaytaradi —
          // `RegistrationPage` bu kalitlarni endi xaritasiz, to'g'ridan-to'g'ri RHF maydon
          // nomlariga bog'laydi (`phone` ikkalasida ham bir xil nom).
          errors: { phone: ["Telefon raqami noto'g'ri formatda (+998XXXXXXXXX)."] },
        }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(
      await screen.findByText("Telefon raqami noto'g'ri formatda (+998XXXXXXXXX)."),
    ).toBeInTheDocument();
  });

  it("400 VALIDATION_ERROR 'birthDate' kalitini tug'ilgan yil maydoniga bog'laydi", async () => {
    mockFetch({
      postSessionResponse: () =>
        problemResponse('VALIDATION_ERROR', 400, {
          errors: { birthDate: ["Tug'ilgan sana 6-20 yosh oralig'iga to'g'ri kelishi kerak."] },
        }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(
      await screen.findByText("Tug'ilgan sana 6-20 yosh oralig'iga to'g'ri kelishi kerak."),
    ).toBeInTheDocument();
  });

  it("resumed: true bo'lsa bildirishnoma ko'rsatib, davom ettirilayotgan testga o'tadi", async () => {
    mockFetch({
      postSessionResponse: () =>
        jsonResponse({
          sessionToken: 'resumed-token',
          assessmentId: 'assessment-2',
          status: 'InProgress',
          expiresAt: '2026-09-10T00:00:00Z',
          resumed: true,
          tests: [
            { code: 'MBTI16', status: 'Completed', answered: 60, total: 60, order: 1 },
            { code: 'BIG5', status: 'InProgress', answered: 5, total: 50, order: 2 },
          ],
        }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    await fillValidForm(user);
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText('Boshlagan testingni davom ettiramiz.')).toBeInTheDocument();
    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
    expect(useSessionStore.getState().sessionToken).toBe('resumed-token');
  });

  it('requiresAccessCode=true bo\'lsa kirish kodi maydonini ko\'rsatadi va ACCESS_CODE_INVALID xatosini bog\'laydi', async () => {
    mockFetch({
      schoolInfo: schoolInfoBody({ requiresAccessCode: true }),
      postSessionResponse: () => problemResponse('ACCESS_CODE_INVALID', 400, { detail: "Kirish kodi noto'g'ri." }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    expect(screen.getByLabelText('Kirish kodi')).toBeInTheDocument();

    await fillValidForm(user);
    await user.type(screen.getByLabelText('Kirish kodi'), '482913');
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText("Kirish kodi noto'g'ri.")).toBeInTheDocument();
  });

  it("404 (havola noto'g'ri) bo'lsa maktab ma'lumoti o'rniga xato holatini ko'rsatadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404));
    vi.stubGlobal('fetch', fetchMock);

    renderRegistration();

    expect(await screen.findByText('Havola ishlamayapti')).toBeInTheDocument();
  });

  it("410 (maktab nofaol) bo'lsa tegishli xato holatini ko'rsatadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(problemResponse('SCHOOL_INACTIVE', 410));
    vi.stubGlobal('fetch', fetchMock);

    renderRegistration();

    expect(await screen.findByText('Test vaqtincha yopilgan')).toBeInTheDocument();
  });

  it("olti yoshdan kichik tug'ilgan sana kiritilsa yuborishga urinishda maydon ostida xato ko'rsatadi", async () => {
    mockFetch({});
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderRegistration();

    await screen.findByText(CONSENT_TEXT);
    // Tizim sanasi 2026-09-02 (fake timer) — 2020-12-31 tug'ilgan bola hali 6 yoshga
    // to'lmagan (yosh 5), lekin `year` select oralig'ida ("to" = 2026-6 = 2020) tanlanadigan —
    // shu sabab UI select o'zi cheklamaydigan, faqat schema tekshiruvi ushlaydigan holat.
    await user.type(screen.getByLabelText('F.I.Sh.'), 'Aliyev Sardor Bekzodovich');
    await user.selectOptions(screen.getByLabelText("Tug'ilgan kun"), '31');
    await user.selectOptions(screen.getByLabelText("Tug'ilgan oy"), '12');
    await user.selectOptions(screen.getByLabelText("Tug'ilgan yil"), '2020');
    await user.click(screen.getByLabelText("O'g'il bola"));
    await user.selectOptions(screen.getByLabelText('Sinf'), '9');
    await user.type(screen.getByLabelText('Telefon raqami'), '901234567');
    await user.click(screen.getByLabelText(CONSENT_LABEL));
    await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

    expect(await screen.findByText(/6-20 yosh oralig'iga to'g'ri kelishi kerak/)).toBeInTheDocument();
  });
});
