import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, listResponse, problemResponse, typedResponse } from '@/test/apiMock';
import type { CatalogQuestionItem, CatalogSection, CatalogTestDetail } from '../model/types';
import { QuestionsSection } from './QuestionsSection';

/**
 * Egasi topgan jonli xato (2026-09-11): javob berilgan savolni o'chirishga urinilganda avval
 * tushunarsiz `500` ko'rinardi. Bu fayl ikki narsani tekshiradi: (1) `hasAnswers: true`
 * bo'lgan savolda o'chirish tugmasi OLDINDAN bloklanadi (server bilan gaplashmasdan), (2)
 * `hasAnswers: false` bo'lgan eski xatti-harakat buzilmagan (regressiya).
 */

function testDetail(overrides: Partial<CatalogTestDetail> = {}): CatalogTestDetail {
  return {
    id: 'test-1',
    code: 'STRESS',
    nameUz: 'Stress anketasi',
    kind: 'Custom',
    isSystem: false,
    status: 'Draft',
    isActive: true,
    scoringMode: 'Scored',
    questionCount: 1,
    scaleCount: 1,
    estimatedMinutes: 5,
    version: 1,
    usedInProgramCount: 0,
    descriptionUz: null,
    pageSize: 10,
    shuffleQuestions: false,
    displayOrder: 1,
    ...overrides,
  };
}

function question(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-1',
    code: 'ST-Q01',
    order: 1,
    textUz: "Ko'p ishlar bilan band bo'lganimda o'zimni bosib turaman.",
    textRu: null,
    textEn: null,
    type: 'Likert5',
    scale: 'STRESS',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: false,
    sectionId: null,
    visibility: null,
    placeholder: null,
    inputPattern: null,
    maxLength: null,
    minSelections: null,
    maxSelections: null,
    options: null,
    hasAnswers: false,
    ...overrides,
  };
}

function mockFetch({
  questions = [question()],
  sections = [],
  deleteResponse,
  putResponse,
}: {
  questions?: CatalogQuestionItem[];
  sections?: CatalogSection[];
  deleteResponse?: () => Response;
  putResponse?: () => Response;
} = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.endsWith('/questions')) {
      return Promise.resolve(listResponse<'CatalogQuestionItemDto'>(questions));
    }
    if (url.endsWith('/sections')) {
      return Promise.resolve(typedResponse<CatalogSection[]>(sections));
    }
    if (url.includes('/api/admin/catalog/questions/') && init?.method === 'DELETE') {
      return Promise.resolve(deleteResponse ? deleteResponse() : new Response(null, { status: 204 }));
    }
    if (url.includes('/api/admin/catalog/questions/') && init?.method === 'PUT') {
      return Promise.resolve(
        putResponse ? putResponse() : jsonResponse<'CatalogQuestionItemDto'>(questions[0] ?? question()),
      );
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderSection(test: CatalogTestDetail = testDetail()) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <QuestionsSection test={test} />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('QuestionsSection', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("hasAnswers: true bo'lgan savolda o'chirish tugmasi bloklangan va sababi ko'rinadi", async () => {
    mockFetch({ questions: [question({ hasAnswers: true })] });
    renderSection();

    const deleteButton = await screen.findByRole('button', {
      name: /ST-Q01 savolini o'chirib bo'lmaydi/,
    });

    expect(deleteButton).toBeDisabled();
    expect(deleteButton).toHaveAttribute('title', expect.stringContaining('Faol emas'));
    expect(deleteButton.getAttribute('aria-label')).toContain('javob berilgan');
  });

  it("hasAnswers: true bo'lgan savolda bosish hech qanday so'rov yubormaydi (server bilan aniqlashdan OLDIN bloklanadi)", async () => {
    const fetchMock = mockFetch({ questions: [question({ hasAnswers: true })] });
    const user = userEvent.setup();
    renderSection();

    const deleteButton = await screen.findByRole('button', {
      name: /ST-Q01 savolini o'chirib bo'lmaydi/,
    });
    await user.click(deleteButton);

    // Tasdiqlash oynasi umuman ochilmaydi — chunki tugma `disabled`.
    expect(screen.queryByText('Savolni o\'chirish')).not.toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some(([, init]) => (init as RequestInit | undefined)?.method === 'DELETE'),
    ).toBe(false);
  });

  it("hasAnswers: false bo'lgan savolda o'chirish avvalgidek ishlaydi (regressiya)", async () => {
    const fetchMock = mockFetch({ questions: [question({ hasAnswers: false })] });
    const user = userEvent.setup();
    renderSection();

    const deleteButton = await screen.findByRole('button', { name: "ST-Q01 savolini o'chirish" });
    expect(deleteButton).toBeEnabled();
    expect(deleteButton).not.toHaveAttribute('title');

    await user.click(deleteButton);
    // `<dialog>` jsdom'da `showModal` siz ochilmaydi (display:none) — shu sabab oyna ichidagi
    // elementlar `getByRole` uchun ko'rinmaydi va matn bo'yicha olinadi (mavjud testlar
    // naqshi, `CatalogTestDetailPage.test.tsx`ga qarang). Sarlavha ("Savolni o'chirish") va
    // tasdiqlash tugmasi AYNAN bir xil matnga ega — ikkinchisi (tugma) DOM tartibida keyin
    // keladi.
    const confirmButtons = await screen.findAllByText("Savolni o'chirish");
    await user.click(confirmButtons[confirmButtons.length - 1]!);

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url).includes('/api/admin/catalog/questions/q-1') &&
            (init as RequestInit | undefined)?.method === 'DELETE',
        ),
      ).toBe(true);
    });
  });

  it('qatordagi tezkor almashtirgich savolni "Faol emas" qiladi (PUT so\'rovi isActive: false bilan)', async () => {
    const fetchMock = mockFetch({ questions: [question({ hasAnswers: true, isActive: true })] });
    const user = userEvent.setup();
    renderSection();

    await screen.findByText(/Ko'p ishlar bilan band/);
    await user.click(screen.getByRole('button', { name: 'ST-Q01 savolini faollikni almashtirish' }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url).includes('/api/admin/catalog/questions/q-1') &&
            (init as RequestInit | undefined)?.method === 'PUT',
        ),
      ).toBe(true);
    });

    const call = fetchMock.mock.calls.find(
      ([url, init]) =>
        String(url).includes('/api/admin/catalog/questions/q-1') &&
        (init as RequestInit | undefined)?.method === 'PUT',
    );
    const body: unknown = JSON.parse(String((call?.[1] as RequestInit | undefined)?.body));
    expect(body).toMatchObject({ isActive: false });

    expect(await screen.findByText('Savol "Faol emas" qilindi')).toBeInTheDocument();
  });

  it("server 409 QUESTION_IN_USE qaytarsa ham (hasAnswers noto'g'ri kelgan taqdirda) tushunarli xabar ko'rsatiladi", async () => {
    mockFetch({
      questions: [question({ hasAnswers: false })],
      deleteResponse: () => problemResponse('QUESTION_IN_USE', 409),
    });
    const user = userEvent.setup();
    renderSection();

    const deleteButton = await screen.findByRole('button', { name: "ST-Q01 savolini o'chirish" });
    await user.click(deleteButton);
    const confirmButtons = await screen.findAllByText("Savolni o'chirish");
    await user.click(confirmButtons[confirmButtons.length - 1]!);

    expect(await screen.findByText(/"Faol emas" holatiga o'tkazing/)).toBeInTheDocument();
  });
});
