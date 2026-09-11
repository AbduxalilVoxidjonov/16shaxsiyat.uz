import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse } from '@/test/apiMock';
import type { CatalogQuestionItem, CatalogSection } from '../model/types';
import { QuestionEditorDialog, type QuestionEditorDialogProps } from './QuestionEditorDialog';

function section(overrides: Partial<CatalogSection> = {}): CatalogSection {
  return {
    id: 's-1',
    testDefinitionId: 'test-1',
    code: 'S1',
    titleUz: "Asosiy ma'lumotlar",
    descriptionUz: null,
    displayOrder: 1,
    visibility: null,
    ...overrides,
  };
}

function priorQuestion(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-0',
    code: 'Q1_6',
    order: 1,
    textUz: "Hozirda qo'shimcha o'quv kurslariga qatnashasizmi?",
    textRu: null,
    textEn: null,
    type: 'SingleChoice',
    scale: 'SURVEY',
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
    options: [
      { textUz: "Ha, Intellect o'quv markazida o'qiyman", value: 1, displayOrder: 1 },
      { textUz: 'Yoq', value: 2, displayOrder: 2 },
    ],
    ...overrides,
  };
}

function mockFetch() {
  const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(problemResponse('NOT_FOUND', 404)));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderDialog(props: Partial<QuestionEditorDialogProps> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const defaults: QuestionEditorDialogProps = {
    open: true,
    testId: 'test-1',
    question: null,
    isSystem: false,
    isPublished: false,
    nextOrder: 2,
    scoringMode: 'Survey',
    sections: [section()],
    allQuestions: [priorQuestion()],
    onClose: vi.fn(),
  };
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <QuestionEditorDialog {...defaults} {...props} />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('QuestionEditorDialog — turga qarab maydonlar', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("Scored anketada yangi turlar tanlov ro'yxatida yo'q va sababi ko'rsatiladi", () => {
    mockFetch();
    renderDialog({ scoringMode: 'Scored' });

    const typeSelect = screen.getByLabelText('Savol turi') as HTMLSelectElement;
    const values = [...typeSelect.options].map((o) => o.value);
    expect(values).not.toContain('ShortText');
    expect(values).not.toContain('MultiChoice');
    expect(screen.getByText(/faqat so'rovnoma/)).toBeInTheDocument();

    // Bo'lim/shart bloki ham ko'rinmaydi, buning o'rniga tushuntirish matni bor.
    expect(screen.queryByLabelText("Bo'lim")).not.toBeInTheDocument();
  });

  it('Survey anketada ShortText tanlansa placeholder/maxLength ko‘rinadi, shkala/yo‘nalish/og‘irlik yashiriladi', async () => {
    mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'ShortText');

    expect(screen.getByLabelText('Placeholder matni')).toBeInTheDocument();
    expect(screen.getByLabelText('Maksimal uzunlik (belgi)')).toBeInTheDocument();
    expect(screen.getByLabelText('Kiritish shabloni (regex)')).toBeInTheDocument();
    expect(screen.queryByLabelText('Shkala')).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Yo'nalish")).not.toBeInTheDocument();
  });

  it('LongText tanlansa inputPattern maydoni ko‘rinmaydi (faqat ShortText/Phone uchun)', async () => {
    mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'LongText');

    expect(screen.getByLabelText('Placeholder matni')).toBeInTheDocument();
    expect(screen.queryByLabelText('Kiritish shabloni (regex)')).not.toBeInTheDocument();
  });

  it("MultiChoice tanlansa min/max va variantlar muharriri ko'rinadi", async () => {
    mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'MultiChoice');

    expect(screen.getByLabelText('Kamida nechta variant tanlansin')).toBeInTheDocument();
    expect(screen.getByLabelText("Ko'pi bilan nechta variant tanlansin")).toBeInTheDocument();
    expect(screen.getByText('Variantlar')).toBeInTheDocument();
  });

  it("noto'g'ri regex kiritilsa forma darajasida xato ko'rsatiladi (backend so'ralmaydi)", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'ShortText');
    await user.type(screen.getByLabelText('Savol kodi'), 'Q9');
    await user.type(screen.getByLabelText("Matni (o'zbekcha)"), 'Test savoli');
    await user.type(screen.getByLabelText('Kiritish shabloni (regex)'), '(unclosed');
    await user.click(screen.getByText('Saqlash'));

    expect(await screen.findByText(/Regex noto'g'ri/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("bo'lim tanlash faqat Survey rejimida ko'rinadi va tanlanganda sectionCode yuboriladi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/api/admin/catalog/tests/test-1/questions')) {
        return Promise.resolve(
          jsonResponse<'CatalogQuestionItemDto'>({
            id: 'q-new',
            code: 'Q2',
            order: 2,
            textUz: 'Yangi savol',
            textRu: null,
            textEn: null,
            type: 'ShortText',
            scale: 'SURVEY',
            direction: 1,
            weight: 1,
            isRequired: true,
            isActive: true,
            isSystem: false,
          }),
        );
      }
      return Promise.resolve(problemResponse('NOT_FOUND', 404));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'ShortText');
    await user.type(screen.getByLabelText('Savol kodi'), 'Q9');
    await user.type(screen.getByLabelText("Matni (o'zbekcha)"), 'Test savoli');
    await user.selectOptions(screen.getByLabelText("Bo'lim"), 'S1');
    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) =>
          String(url).includes('/api/admin/catalog/tests/test-1/questions'),
        ),
      ).toBe(true);
    });

    const call = fetchMock.mock.calls.find(([url]) =>
      String(url).includes('/api/admin/catalog/tests/test-1/questions'),
    );
    const body: unknown = JSON.parse(String((call?.[1] as RequestInit | undefined)?.body));
    expect(body).toMatchObject({ sectionCode: 'S1', type: 'ShortText' });
  });

  it("kamida 2 variant bo'lmasa saqlash bloklanadi (root xato)", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'SingleChoice');
    await user.type(screen.getByLabelText('Savol kodi'), 'Q9');
    await user.type(screen.getByLabelText("Matni (o'zbekcha)"), 'Test savoli');
    await user.click(screen.getByText('Saqlash'));

    expect(await screen.findByText('Kamida 2 ta variant kerak.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("takroriy variant qiymati bo'lsa saqlash bloklanadi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderDialog();

    await user.selectOptions(screen.getByLabelText('Savol turi'), 'SingleChoice');
    await user.type(screen.getByLabelText('Savol kodi'), 'Q9');
    await user.type(screen.getByLabelText("Matni (o'zbekcha)"), 'Test savoli');

    // `<dialog>` jsdom'da `showModal`siz ochilmaydi — ichidagi elementlar `getByRole` uchun
    // ko'rinmaydi, shu sabab matn bo'yicha olinadi (`CatalogTestDetailPage.test.tsx` naqshi).
    await user.click(screen.getByText("Variant qo'shish"));
    await user.click(screen.getByText("Variant qo'shish"));
    await user.type(screen.getByLabelText('1-variant matni'), 'A');
    await user.type(screen.getByLabelText('2-variant matni'), 'B');

    const secondValue = screen.getByLabelText('2-variant qiymati');
    await user.clear(secondValue);
    await user.type(secondValue, '1');

    await user.click(screen.getByText('Saqlash'));

    expect(await screen.findByText(/takrorlanmasin/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("yangi savolda (ruscha/inglizcha bo'sh) 'Boshqa tillar' bloki yopiq boshlanadi", () => {
    mockFetch();
    const { container } = renderDialog();

    const details = container.querySelector('details');
    expect(details).not.toBeNull();
    expect(details?.open).toBe(false);
    expect(screen.getByText('Boshqa tillar (ixtiyoriy)')).toBeInTheDocument();

    // Maydonlar DOM'da mavjud (register ishlashi uchun) — faqat vizual yig'ilgan.
    expect(screen.getByLabelText('Matni (ruscha)')).toBeInTheDocument();
    expect(screen.getByLabelText('Matni (inglizcha)')).toBeInTheDocument();
  });

  it("tahrirlashda ruscha/inglizcha tarjima mavjud bo'lsa 'Boshqa tillar' bloki ochiq boshlanadi", () => {
    mockFetch();
    const { container } = renderDialog({ question: priorQuestion({ textRu: 'Привет' }) });

    const details = container.querySelector('details');
    expect(details?.open).toBe(true);
  });

  it("forma keng ekranda ikki ustunli grid, 390px'da esa bitta ustun bo'lib qoladi", () => {
    mockFetch();
    const { container } = renderDialog();

    // Forma o'zi 2 ustunli grid (`sm:` dan yuqorida) — mobilda (`sm:` gacha) bitta ustun,
    // chunki `grid-cols-1` ustuvor va `sm:grid-cols-2` faqat kengroq ekranda ishga tushadi.
    const form = container.querySelector(`#${CSS.escape('catalog-question-form')}`);
    expect(form).toHaveClass('grid', 'grid-cols-1', 'sm:grid-cols-2');

    // Uzun maydon (savol matni) — to'liq kenglikda, ikki ustunga siqilmaydi.
    const textUzWrapper = screen.getByLabelText("Matni (o'zbekcha)").closest('div')?.parentElement;
    expect(textUzWrapper).toHaveClass('sm:col-span-2');

    // Kod/tur/tartib qisqa maydonlari — o'z ichki 3 ustunli mini-grid guruhida (uchtasi
    // bir qatorga sig'ishi uchun), forma darajasida esa to'liq kenglikni egallaydi.
    const identityGroup = screen.getByLabelText('Savol kodi').closest('div')?.parentElement;
    expect(identityGroup).toHaveClass('grid', 'sm:grid-cols-3', 'sm:col-span-2');
  });
});
