import type { ComponentProps } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { listResponse, problemResponse, typedResponse } from '@/test/apiMock';
import type { CatalogQuestionItem, CatalogSection } from '../model/types';
import { SectionsSection } from './SectionsSection';

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

function question(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-1',
    code: 'Q1',
    order: 1,
    textUz: 'Savol',
    textRu: null,
    textEn: null,
    type: 'ShortText',
    scale: 'SURVEY',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: false,
    sectionId: 's-1',
    visibility: null,
    placeholder: null,
    inputPattern: null,
    maxLength: 200,
    minSelections: null,
    maxSelections: null,
    options: null,
    hasAnswers: false,
    ...overrides,
  };
}

function mockFetch({
  sections = [section()],
  questions = [question()],
}: { sections?: CatalogSection[]; questions?: CatalogQuestionItem[] } = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/sections')) {
      return Promise.resolve(typedResponse<CatalogSection[]>(sections));
    }
    if (url.endsWith('/questions')) {
      return Promise.resolve(listResponse<'CatalogQuestionItemDto'>(questions));
    }
    if (url.includes('/sections/') && !url.endsWith('/reorder')) {
      return Promise.resolve(typedResponse<CatalogSection>(sections[0] ?? section()));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderSections(props: Partial<ComponentProps<typeof SectionsSection>> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <SectionsSection testId="test-1" isSystem={false} scoringMode="Survey" {...props} />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('SectionsSection', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("Scored anketada CRUD o'rniga tushuntirish ko'rsatiladi", () => {
    mockFetch();
    renderSections({ scoringMode: 'Scored' });

    expect(screen.getByText(/faqat so'rovnoma/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Bo'lim qo'shish/ })).not.toBeInTheDocument();
  });

  it("bo'lim yo'q bo'lsa bo'sh holat ko'rsatiladi", async () => {
    mockFetch({ sections: [] });
    renderSections();

    expect(await screen.findByText("Bo'lim yo'q")).toBeInTheDocument();
  });

  it("bo'lim ro'yxati savollar soni va shart holati bilan chiqadi", async () => {
    mockFetch();
    renderSections();

    expect(await screen.findByText("Asosiy ma'lumotlar")).toBeInTheDocument();
    expect(screen.getByText('1 ta savol')).toBeInTheDocument();
    expect(screen.getByText('Har doim ko\'rinadi')).toBeInTheDocument();
  });

  it('tizim metodikasida qulf matni chiqadi va qo\'shish/o\'chirish tugmalari yo\'q', async () => {
    mockFetch();
    renderSections({ isSystem: true });

    await screen.findByText("Asosiy ma'lumotlar");
    expect(screen.getByText(/Tizim metodikasida bo'limlar/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Bo'lim qo'shish/ })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /S1 bo'limini o'chirish/ }),
    ).not.toBeInTheDocument();
  });

  it('yuqoriga ko\'chirish tartiblash so\'rovini yuboradi', async () => {
    const fetchMock = mockFetch({
      sections: [section({ id: 's-1', code: 'S1', displayOrder: 1 }), section({ id: 's-2', code: 'S2', displayOrder: 2 })],
    });
    const user = userEvent.setup();
    renderSections();

    await screen.findByText('S2', { exact: false });
    await user.click(screen.getByRole('button', { name: "S2 bo'limini yuqoriga ko'chirish" }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => String(url).endsWith('/sections/reorder')),
      ).toBe(true);
    });
  });

  it("o'chirish tasdiqlansa DELETE yuboriladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderSections();

    await screen.findByText("Asosiy ma'lumotlar");
    await user.click(screen.getByRole('button', { name: "S1 bo'limini o'chirish" }));
    await user.click(screen.getByText("O'chirish"));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url).includes('/api/admin/catalog/sections/s-1') &&
            (init as RequestInit | undefined)?.method === 'DELETE',
        ),
      ).toBe(true);
    });
  });
});
