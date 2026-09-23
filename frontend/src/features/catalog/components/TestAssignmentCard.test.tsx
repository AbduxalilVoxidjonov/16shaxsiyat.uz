import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, typedResponse, type Schemas } from '@/test/apiMock';
import type { SchoolOption } from '../api/useSchoolOptionsQuery';
import { TestAssignmentCard } from './TestAssignmentCard';

/**
 * "Biriktirish" kartasi — `docs/07` §3.4.1 (2026-09-23 egasi qarori: "Dasturlar" bo'limi olib
 * tashlandi, test kimga ochiqligi test ichida boshqariladi).
 */
function assignment(
  overrides: Partial<Schemas['AdminTestAssignmentDto']> = {},
): Schemas['AdminTestAssignmentDto'] {
  return {
    testDefinitionId: 'test-1',
    testStatus: 'Published',
    testIsActive: true,
    isConfigured: true,
    isPublic: false,
    schoolIds: ['school-1'],
    isInPublicSpace: false,
    registrationMode: 'Full',
    state: 'Active',
    isAvailable: true,
    hasPersonalityBattery: false,
    sessionCount: 4,
    ...overrides,
  };
}

const SCHOOLS: SchoolOption[] = [
  { id: 'school-1', name: '12-son maktab', region: "Farg'ona", district: "Qo'qon" },
  { id: 'school-2', name: '5-son maktab', region: 'Toshkent', district: 'Chilonzor' },
];

interface MockOptions {
  initial?: Schemas['AdminTestAssignmentDto'];
  /** `PUT` javobi — berilmasa so'rov tanasi asosida muvaffaqiyatli javob. */
  putResponse?: () => Response;
}

function mockFetch({ initial = assignment(), putResponse }: MockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';
    if (url.endsWith('/api/admin/catalog/tests/test-1/assignment')) {
      if (method === 'PUT') {
        if (putResponse) return Promise.resolve(putResponse());
        const body = JSON.parse(String(init?.body)) as Schemas['UpdateTestAssignmentRequest'];
        return Promise.resolve(
          jsonResponse<'AdminTestAssignmentDto'>({
            ...initial,
            isConfigured: true,
            isPublic: body.isPublic,
            schoolIds: body.schoolIds ?? [],
            isInPublicSpace: body.isInPublicSpace ?? initial.isInPublicSpace,
            registrationMode: body.registrationMode ?? 'Full',
          }),
        );
      }
      return Promise.resolve(jsonResponse<'AdminTestAssignmentDto'>(initial));
    }
    const detailMatch = /\/api\/admin\/schools\/([^/?]+)$/.exec(url);
    if (detailMatch) {
      const school = SCHOOLS.find((item) => item.id === detailMatch[1]);
      return Promise.resolve(
        school ? typedResponse<SchoolOption>(school) : problemResponse('NOT_FOUND', 404),
      );
    }
    if (url.includes('/api/admin/schools?')) {
      return Promise.resolve(schoolsPage());
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

/** `GET /api/admin/schools?…` — kartaga faqat `id/name/region/district` kerak. */
function schoolsPage(): Response {
  return typedResponse({
    items: SCHOOLS,
    page: 1,
    pageSize: 20,
    totalCount: SCHOOLS.length,
    totalPages: 1,
    hasNext: false,
    hasPrevious: false,
  });
}

function renderCard() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <TestAssignmentCard testId="test-1" />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function findPutBody(fetchMock: ReturnType<typeof vi.fn>) {
  const call = fetchMock.mock.calls.find(
    ([input, init]) =>
      String(input).endsWith('/assignment') && (init as RequestInit | undefined)?.method === 'PUT',
  );
  expect(call).toBeDefined();
  return JSON.parse(String((call![1] as RequestInit).body)) as Record<string, unknown>;
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('TestAssignmentCard', () => {
  it("GET natijasini ko'rsatadi: holat, sessiyalar soni, tanlangan maktablar", async () => {
    mockFetch();
    renderCard();

    const status = await screen.findByTestId('assignment-status');
    expect(within(status).getByText("Faol — o'quvchilarga ko'rinadi.")).toBeInTheDocument();
    expect(
      within(status).getByText('Shu biriktirish orqali 4 ta sessiya ochilgan'),
    ).toBeInTheDocument();

    expect(screen.getByRole('radio', { name: /Tanlangan maktablarga/ })).toBeChecked();
    expect(screen.getByRole('radio', { name: /Barcha maktablarga/ })).not.toBeChecked();
    expect(screen.getByRole('radio', { name: /To'liq ro'yxatdan o'tish/ })).toBeChecked();
    expect(
      screen.getByRole('checkbox', { name: 'Ommaviy (kabinet orqali hamma uchun)' }),
    ).not.toBeChecked();

    // Chip nomi `GET /api/admin/schools/{id}` dan.
    expect(
      await screen.findByRole('button', { name: '12-son maktab maktabini olib tashlash' }),
    ).toBeInTheDocument();
    // O'zgarish yo'q — saqlash tugmasi o'chiq.
    expect(screen.getByRole('button', { name: 'Biriktirishni saqlash' })).toBeDisabled();
  });

  it("maktab qidiruvdan tanlanib saqlanganda PUT tanasi to'g'ri ketadi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderCard();

    const picker = within(await screen.findByTestId('assignment-school-picker'));
    await user.click(await picker.findByRole('checkbox', { name: /5-son maktab/ }));
    expect(
      await screen.findByRole('button', { name: '5-son maktab maktabini olib tashlash' }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    expect(await screen.findByText('Biriktirish saqlandi')).toBeInTheDocument();
    expect(findPutBody(fetchMock)).toEqual({
      isPublic: false,
      schoolIds: ['school-1', 'school-2'],
      registrationMode: 'Full',
      isInPublicSpace: false,
    });
  });

  it("chip × bilan maktab olib tashlanadi va to'plam to'liq almashtiriladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderCard();

    await user.click(
      await screen.findByRole('button', { name: '12-son maktab maktabini olib tashlash' }),
    );
    expect(screen.getByText('Hali maktab tanlanmagan.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    await waitFor(() => {
      expect(findPutBody(fetchMock)).toMatchObject({ isPublic: false, schoolIds: [] });
    });
  });

  it('"Barcha maktablarga" — isPublic: true, schoolIds: [], ommaviy kabinet belgisi avtomatik', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderCard();

    await user.click(await screen.findByRole('radio', { name: /Barcha maktablarga/ }));
    // Maktab tanlovi yashiriladi, ommaviy kabinet — isPublic ichida (belgilangan, o'chiq).
    expect(screen.queryByTestId('assignment-school-picker')).not.toBeInTheDocument();
    const publicSpace = screen.getByRole('checkbox', {
      name: 'Ommaviy (kabinet orqali hamma uchun)',
    });
    expect(publicSpace).toBeChecked();
    expect(publicSpace).toBeDisabled();

    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));
    await screen.findByText('Biriktirish saqlandi');
    expect(findPutBody(fetchMock)).toEqual({
      isPublic: true,
      schoolIds: [],
      registrationMode: 'Full',
      isInPublicSpace: false,
    });
    expect(screen.getByRole('radio', { name: /Barcha maktablarga/ })).toBeChecked();
  });

  it('ommaviy kabinet belgisi isInPublicSpace: true bilan yuboriladi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderCard();

    await user.click(
      await screen.findByRole('checkbox', { name: 'Ommaviy (kabinet orqali hamma uchun)' }),
    );
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    await screen.findByText('Biriktirish saqlandi');
    expect(findPutBody(fetchMock)).toMatchObject({ isPublic: false, isInPublicSpace: true });
  });

  it('anonim rejim tanlanganda registrationMode: None ketadi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderCard();

    await user.click(await screen.findByRole('radio', { name: /Ro'yxatdan o'tmasdan \(anonim\)/ }));
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    await screen.findByText('Biriktirish saqlandi');
    expect(findPutBody(fetchMock)).toMatchObject({ registrationMode: 'None' });
  });

  it("shaxsiyat batareyasi bo'lsa anonim rejim o'chiq va sababi yoziladi", async () => {
    mockFetch({ initial: assignment({ hasPersonalityBattery: true }) });
    renderCard();

    const none = await screen.findByRole('radio', { name: /Ro'yxatdan o'tmasdan \(anonim\)/ });
    expect(none).toBeDisabled();
    expect(screen.getByText(/shaxsiyat batareyasi bor/)).toBeInTheDocument();
    expect(none).toHaveAccessibleDescription(/shaxsiyat batareyasi bor/);
  });

  it("PUBLIC_SPACE_NOT_CONFIGURED bo'lsa tushunarli xabar chiqadi", async () => {
    mockFetch({ putResponse: () => problemResponse('PUBLIC_SPACE_NOT_CONFIGURED', 409) });
    const user = userEvent.setup();
    renderCard();

    await user.click(
      await screen.findByRole('checkbox', { name: 'Ommaviy (kabinet orqali hamma uchun)' }),
    );
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    const error = await screen.findByTestId('assignment-error');
    expect(error).toHaveTextContent(/Ommaviy makon hali sozlanmagan/);
    expect(error).not.toHaveTextContent('PUBLIC_SPACE_NOT_CONFIGURED');
  });

  it("REGISTRATION_REQUIRED_FOR_BATTERY va noma'lum maktab (404) xabarlari", async () => {
    let attempt = 0;
    mockFetch({
      putResponse: () => {
        attempt += 1;
        return attempt === 1
          ? problemResponse('REGISTRATION_REQUIRED_FOR_BATTERY', 400)
          : problemResponse('NOT_FOUND', 404, 'Topilmadi', { schoolIds: ['school-9'] });
      },
    });
    const user = userEvent.setup();
    renderCard();

    await user.click(await screen.findByRole('radio', { name: /Ro'yxatdan o'tmasdan \(anonim\)/ }));
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));
    expect(await screen.findByTestId('assignment-error')).toHaveTextContent(
      /anonim\) rejimni tanlab bo'lmaydi/,
    );

    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));
    await waitFor(() => {
      expect(screen.getByTestId('assignment-error')).toHaveTextContent(/1 tasi topilmadi/);
    });
  });

  it("409 TEST_ARCHIVED — forma faqat o'qish uchun bo'ladi", async () => {
    mockFetch({ putResponse: () => problemResponse('TEST_ARCHIVED', 409) });
    const user = userEvent.setup();
    renderCard();

    await user.click(await screen.findByRole('radio', { name: /Barcha maktablarga/ }));
    await user.click(screen.getByRole('button', { name: 'Biriktirishni saqlash' }));

    expect(await screen.findByTestId('assignment-readonly-notice')).toBeInTheDocument();
    expect(screen.getByTestId('assignment-error')).toHaveTextContent(/Test arxivlangan/);
    expect(screen.queryByRole('button', { name: 'Biriktirishni saqlash' })).not.toBeInTheDocument();
    expect(screen.getByRole('radio', { name: /Tanlangan maktablarga/ })).toBeDisabled();
    expect(within(screen.getByTestId('assignment-status')).getByText('Arxiv')).toBeInTheDocument();
  });

  it("arxivlangan test GET'da ham faqat o'qish uchun ko'rsatiladi", async () => {
    mockFetch({
      initial: assignment({ testStatus: 'Archived', state: 'Archived', isAvailable: false }),
    });
    renderCard();

    expect(await screen.findByTestId('assignment-readonly-notice')).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: /Barcha maktablarga/ })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Biriktirishni saqlash' })).not.toBeInTheDocument();
  });

  it('biriktirilmagan test — "Hech kimga biriktirilmagan"', async () => {
    mockFetch({
      initial: assignment({
        isConfigured: false,
        schoolIds: [],
        state: null,
        isAvailable: false,
        sessionCount: 0,
      }),
    });
    renderCard();

    expect(
      await screen.findByText("Hech kimga biriktirilmagan — o'quvchilar bu testni ko'rmaydi."),
    ).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: /Tanlangan maktablarga/ })).toBeChecked();
  });

  it('qoralama test — "test nashr qilinganda ochiladi"', async () => {
    mockFetch({
      initial: assignment({ testStatus: 'Draft', state: 'Draft', isAvailable: false }),
    });
    renderCard();

    expect(
      await screen.findByText("Qoralama — test nashr qilinganda o'quvchilarga ochiladi."),
    ).toBeInTheDocument();
    // Qoralama testni oldindan biriktirish RUXSAT etiladi — forma ochiq.
    expect(screen.getByRole('radio', { name: /Barcha maktablarga/ })).toBeEnabled();
  });
});
