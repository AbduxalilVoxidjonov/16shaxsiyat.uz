import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import CatalogTestDetailPage from './CatalogTestDetailPage';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function testDetail(overrides: Record<string, unknown> = {}) {
  return {
    id: 'test-1',
    code: 'MBTI16',
    nameUz: 'Shaxsiyat testi',
    kind: 'System',
    isSystem: true,
    status: 'Published',
    isActive: true,
    scoringMode: 'Scored',
    questionCount: 2,
    scaleCount: 0,
    estimatedMinutes: 12,
    version: 1,
    usedInProgramCount: 1,
    descriptionUz: 'Shaxsiyat tipini aniqlaydi',
    pageSize: 10,
    shuffleQuestions: false,
    ...overrides,
  };
}

function questionRow(overrides: Record<string, unknown> = {}) {
  return {
    id: 'q-1',
    code: 'MB-Q01',
    order: 1,
    textUz: 'Yangi odamlar bilan tanishish menga oson.',
    textRu: null,
    textEn: null,
    type: 'Likert5',
    scale: 'EI',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: true,
    ...overrides,
  };
}

interface MockOptions {
  detail?: Record<string, unknown>;
  questions?: Record<string, unknown>[];
}

function mockFetch({ detail = testDetail(), questions = [questionRow()] }: MockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/questions')) return Promise.resolve(jsonResponse(questions));
    if (url.endsWith('/scales')) return Promise.resolve(jsonResponse([]));
    if (url.includes('/api/admin/catalog/questions/')) {
      return Promise.resolve(jsonResponse(questions[0] ?? questionRow()));
    }
    if (url.includes('/api/admin/catalog/tests/test-1')) {
      return Promise.resolve(jsonResponse(detail));
    }
    return Promise.resolve(jsonResponse({ code: 'NOT_FOUND', status: 404 }, 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/catalog/tests/test-1']}>
          <Routes>
            <Route path="/admin/catalog/tests/:id" element={<CatalogTestDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('CatalogTestDetailPage — tizim metodikasi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("tahrirlash ochiq, lekin o'chirish/savol qo'shish tugmalari YO'Q", async () => {
    mockFetch();
    renderPage();

    expect(await screen.findByText('Shaxsiyat testi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tahrirlash/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Nusxa olish/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "O'chirish" })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Savol qo'shish/ })).not.toBeInTheDocument();
    // Shkala CRUD bo'limi tizim testida umuman ko'rsatilmaydi.
    expect(screen.queryByRole('button', { name: /Shkala qo'shish/ })).not.toBeInTheDocument();
  });

  it("savol oynasida shkala/yo'nalish/og'irlik disabled va sababi tushuntirilgan", async () => {
    mockFetch();
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    const scaleInput = await screen.findByLabelText('Shkala');
    expect(scaleInput).toBeDisabled();
    expect(screen.getByLabelText("Yo'nalish")).toBeDisabled();
    expect(screen.getByLabelText("Og'irlik")).toBeDisabled();

    const describedBy = scaleInput.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();
    expect(document.getElementById(describedBy ?? '')?.textContent).toContain(
      "shkala, yo'nalish va og'irlik o'zgartirilmaydi",
    );

    // Matn maydonlari esa ochiq.
    expect(screen.getByLabelText("Matni (o'zbekcha)")).toBeEnabled();
  });

  it("nashr etilgan testda ballar o'zgarmasligi haqida ogohlantirish ko'rsatiladi", async () => {
    mockFetch();
    renderPage();

    expect(
      await screen.findByText(/Ballar o'zgarmaydi — hisoblash shkala va og'irlikka tayanadi/),
    ).toBeInTheDocument();
  });

  it('savol matni saqlanganda PUT /questions/{id} ga `scale`/`direction`/`weight`siz tana ketadi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    const textarea = await screen.findByLabelText("Matni (o'zbekcha)");
    await user.clear(textarea);
    await user.type(textarea, 'Yangilangan savol matni');
    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) =>
          String(url).includes('/api/admin/catalog/questions/q-1'),
        ),
      ).toBe(true);
    });

    const call = fetchMock.mock.calls.find(([url]) =>
      String(url).includes('/api/admin/catalog/questions/q-1'),
    );
    const init = call?.[1] as RequestInit | undefined;
    expect(init?.method).toBe('PUT');

    const body: unknown = JSON.parse(String(init?.body));
    expect(body).toEqual({
      textUz: 'Yangilangan savol matni',
      textRu: null,
      textEn: null,
      isActive: true,
      order: 1,
      isRequired: true,
    });
  });
});

describe('CatalogTestDetailPage — `Custom` test', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("savol qo'shish, o'chirish, shkala boshqaruvi va testni o'chirish ochiq", async () => {
    mockFetch({
      detail: testDetail({
        id: 'test-1',
        code: 'STRESS',
        nameUz: 'Stress anketasi',
        kind: 'Custom',
        isSystem: false,
        status: 'Draft',
      }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
    });
    renderPage();

    expect(await screen.findByText('Stress anketasi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Savol qo'shish/ })).toBeInTheDocument();
    expect(
      await screen.findByRole('button', { name: /MB-Q01 savolini o'chirish/ }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Shkala qo'shish/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: "O'chirish" })).toBeInTheDocument();
    // `Draft` bo'lgani uchun "Nashr qilish" ko'rinadi.
    expect(screen.getByRole('button', { name: 'Nashr qilish' })).toBeInTheDocument();
  });

  it('`Custom` savolda shkala maydoni tahrirlanadi', async () => {
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    expect(await screen.findByLabelText('Shkala')).toBeEnabled();
    expect(screen.getByLabelText("Og'irlik")).toBeEnabled();
  });
});
