import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, listResponse } from '@/test/apiMock';
import {
  AI_PROVIDER_KINDS,
  type AiProviderConfigDto,
  type AiProviderKind,
  type AiPromptTemplateDto,
  type AiUsageStatsResponse,
  type TestAiConnectionResult,
} from '../model/types';
import AiProvidersPage from './AiProvidersPage';

/**
 * Bu yerdagi barcha javoblar `typedResponse<T>` bilan — AI sozlamalari DTO'lari
 * (`AdminAiProviderDto`, `AdminPromptTemplateDto`, `AdminAiUsageDto`) `schema.d.ts` da hali
 * YO'Q — backend `AiConfigController` (P18) MAVJUD, lekin sxema qayta generatsiya qilinmagan;
 * shu sabab `jsonResponse<'...'>` ishlatib bo'lmaydi. `npm run generate:api` dan keyin ular
 * `jsonResponse<'AdminAiProviderDto'>` va h.k. ga o'tkaziladi. Hozircha mock backend shakliga
 * `features/ai-settings/model/types.ts` orqali bog'langan (u fayl `AdminAiDtos.cs` va jonli
 * `curl` javoblari bilan tasdiqlangan).
 */

/** URL'dan olingan xom provayder kodini `AiProviderKind` ga toraytiradi (`as` YO'Q). */
function toProviderKind(raw: string): AiProviderKind {
  return AI_PROVIDER_KINDS.find((kind) => kind === raw) ?? 'Gemini';
}

/** Haqiqiy kalitning to'liq matni — HECH QACHON ekranga chiqmasligi tekshiriladi. */
const FAKE_FULL_GEMINI_KEY = 'AIzaSyFAKEFULLSECRETVALUE1234567f2b';
const GEMINI_MASKED_KEY = 'AIza••••7f2b';

const GEMINI_CONFIG = {
  provider: 'Gemini',
  displayName: 'Gemini',
  maskedApiKey: GEMINI_MASKED_KEY,
  model: 'gemini-2.0-flash',
  baseUrl: 'https://gemini.proxy.test/v1',
  maxOutputTokens: 4096,
  temperature: 0.4,
  isDefault: true,
  isActive: true,
  fallbackOrder: 1,
  lastCheckedAt: null,
  lastCheckStatus: null,
} satisfies AiProviderConfigDto;

const OPENAI_CONFIG = {
  provider: 'OpenAi',
  displayName: 'OpenAI',
  maskedApiKey: null,
  model: 'gpt-4.1-mini',
  baseUrl: null,
  maxOutputTokens: 4096,
  temperature: 0.4,
  isDefault: false,
  isActive: false,
  fallbackOrder: 2,
  lastCheckedAt: null,
  lastCheckStatus: null,
} satisfies AiProviderConfigDto;

const ANTHROPIC_CONFIG = {
  provider: 'Anthropic',
  displayName: 'Anthropic',
  maskedApiKey: null,
  model: 'claude-sonnet-5',
  baseUrl: null,
  maxOutputTokens: 4096,
  temperature: 0.4,
  isDefault: false,
  isActive: false,
  fallbackOrder: 3,
  lastCheckedAt: null,
  lastCheckStatus: null,
} satisfies AiProviderConfigDto;

/**
 * `AdminAiUsageDto` — jonli `GET /api/admin/ai/usage` javobi bilan AYNAN bir xil shakl
 * (2026-09-02 `curl`): `from`/`to` javobda YO'Q, token maydonlarida `total` prefiksi YO'Q.
 */
const USAGE_STATS = {
  totalCalls: 7,
  inputTokens: 1234,
  outputTokens: 567,
  estimatedCostUsd: null,
  byProvider: [{ provider: 'Gemini', calls: 7, inputTokens: 1234, outputTokens: 567, estimatedCostUsd: null }],
} satisfies AiUsageStatsResponse;

/** `AdminPromptTemplateDto` — `version` MATN ("v1.0"), `id` — Guid. */
const PROMPT_TEMPLATE = {
  id: '5106c0f1-b12e-46eb-8a11-993b2f66666f',
  key: 'full_analysis',
  version: 'v1.0',
  systemText: 'System matni namunasi',
  userText: 'User matni namunasi',
  jsonSchema: '{"type":"object"}',
  isActive: true,
  createdAt: '2026-08-31T10:00:00Z',
} satisfies AiPromptTemplateDto;

interface MockOptions {
  /** provayder kodi bo'yicha `/test` javobi navbati — har chaqiruvda birinchisi ishlatiladi. */
  testResponses?: Record<string, TestAiConnectionResult[]>;
}

function mockFetch(options: MockOptions = {}) {
  const putBodies: Array<{ provider: string; body: unknown }> = [];
  const testQueues = new Map(
    Object.entries(options.testResponses ?? {}).map(([provider, queue]) => [provider, [...queue]]),
  );

  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/api/admin/ai/providers') && url.match(/\/providers\/[A-Za-z]+\/test$/) && method === 'POST') {
      const provider = url.match(/\/providers\/([A-Za-z]+)\/test$/)?.[1] ?? '';
      const queue = testQueues.get(provider) ?? [];
      const next: TestAiConnectionResult = queue.shift() ?? { ok: true, latencyMs: 500, message: 'Ulanish muvaffaqiyatli — provayder javob berdi.' };
      return Promise.resolve(jsonResponse<'AdminAiProviderTestResultDto'>(next));
    }
    if (url.includes('/set-default') && method === 'POST') {
      const provider = url.match(/\/providers\/([A-Za-z]+)\/set-default$/)?.[1] ?? '';
      return Promise.resolve(
        jsonResponse<'AdminAiProviderDto'>({
          ...GEMINI_CONFIG,
          provider: toProviderKind(provider),
          isDefault: true,
        }),
      );
    }
    if (url.match(/\/providers\/([A-Za-z]+)$/) && method === 'PUT') {
      const provider = url.match(/\/providers\/([A-Za-z]+)$/)?.[1] ?? '';
      const body: unknown = init?.body ? JSON.parse(String(init.body)) : {};
      putBodies.push({ provider, body });
      return Promise.resolve(
        jsonResponse<'AdminAiProviderDto'>({ ...GEMINI_CONFIG, provider: toProviderKind(provider) }),
      );
    }
    if (url.includes('/api/admin/ai/providers') && method === 'GET') {
      return Promise.resolve(
        listResponse<'AdminAiProviderDto'>([GEMINI_CONFIG, OPENAI_CONFIG, ANTHROPIC_CONFIG]),
      );
    }
    if (url.includes('/api/admin/ai/prompts') && method === 'GET') {
      return Promise.resolve(listResponse<'AdminPromptTemplateDto'>([PROMPT_TEMPLATE]));
    }
    if (url.includes('/api/admin/ai/usage') && method === 'GET') {
      return Promise.resolve(jsonResponse<'AdminAiUsageDto'>(USAGE_STATS));
    }
    return Promise.reject(new Error(`unexpected fetch: ${method} ${url}`));
  });

  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, putBodies };
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/ai']}>
          <AiProvidersPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('AiProvidersPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('maskalangan kalitni ko\'rsatadi, to\'liq kalit HECH QACHON ekranga chiqmaydi', async () => {
    mockFetch();
    renderPage();

    const maskedFields = await screen.findAllByTestId('masked-api-key');
    // Faqat Gemini'da kalit bor (birinchi karta) — maskalangan holda.
    expect(maskedFields[0]).toHaveTextContent(GEMINI_MASKED_KEY);
    expect(screen.queryByText(FAKE_FULL_GEMINI_KEY)).not.toBeInTheDocument();
    expect(document.body.innerHTML).not.toContain(FAKE_FULL_GEMINI_KEY);
  });

  it("kalit maydoni bo'sh qoldirilsa saqlashda so'rovda `apiKey` yuborilmaydi (mavjud kalit o'zgarmaydi)", async () => {
    const { putBodies } = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');

    // Gemini kartasi — birinchi "Saqlash" tugmasi. Kalit maydoniga tegilmaydi (hali maskalangan).
    const saveButtons = screen.getAllByRole('button', { name: 'Saqlash' });
    await user.click(saveButtons[0]!);

    await waitFor(() => {
      expect(putBodies.some((entry) => entry.provider === 'Gemini')).toBe(true);
    });

    const geminiPut = putBodies.find((entry) => entry.provider === 'Gemini');
    expect(geminiPut?.body).not.toHaveProperty('apiKey');
  });

  it("kalit maydoniga yangi qiymat kiritilsa tasdiqlangach so'rovda yuboriladi", async () => {
    const { putBodies } = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');

    const changeButtons = screen.getAllByRole('button', { name: "O'zgartirish" });
    await user.click(changeButtons[0]!);

    const keyInputs = screen.getAllByPlaceholderText('Yangi API kalitini kiriting');
    await user.type(keyInputs[0]!, 'AIzaNewSecretKeyValue');

    const saveButtons = screen.getAllByRole('button', { name: 'Saqlash' });
    await user.click(saveButtons[0]!);

    // Kalit almashtirish xavfli amal — tasdiq dialogi ochilishi kerak, so'rov hali ketmaydi.
    await screen.findByText('API kalitini almashtirishni tasdiqlang');
    expect(putBodies.some((entry) => entry.provider === 'Gemini')).toBe(false);

    await user.click(screen.getByText('Ha, almashtirish'));

    await waitFor(() => {
      const geminiPut = putBodies.find((entry) => entry.provider === 'Gemini');
      expect(geminiPut?.body).toHaveProperty('apiKey', 'AIzaNewSecretKeyValue');
    });
  });

  it("saqlashda `baseUrl` joriy qiymat bilan qaytariladi (PUT uni tozalab yubormasligi uchun)", async () => {
    const { putBodies } = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');
    await user.click(screen.getAllByRole('button', { name: 'Saqlash' })[0]!);

    await waitFor(() => {
      expect(putBodies.some((entry) => entry.provider === 'Gemini')).toBe(true);
    });

    const geminiPut = putBodies.find((entry) => entry.provider === 'Gemini');
    expect(geminiPut?.body).toHaveProperty('baseUrl', 'https://gemini.proxy.test/v1');
    expect(geminiPut?.body).toHaveProperty('fallbackOrder', 1);
  });

  it('foydalanish statistikasi backend maydon nomlari bilan (`inputTokens`/`outputTokens`) chiqadi', async () => {
    mockFetch();
    renderPage();

    expect(await screen.findByText('1234')).toBeInTheDocument();
    expect(screen.getByText('567')).toBeInTheDocument();
    expect(screen.getByText('7')).toBeInTheDocument();
  });

  it("prompt versiyasi MATN sifatida ko'rsatiladi (`v1.0`, `vv1.0` emas)", async () => {
    mockFetch();
    renderPage();

    expect(await screen.findByText(/full_analysis/)).toHaveTextContent('full_analysis · v1.0');
    expect(screen.queryByText(/vv1\.0/)).not.toBeInTheDocument();
  });

  it("'Aloqani tekshirish' xato TURLARINI ajratib ko'rsatadi (umumiy 'Xatolik' emas)", async () => {
    const { fetchMock } = mockFetch({
      testResponses: {
        Gemini: [{ ok: false, latencyMs: 502, message: "API kaliti noto'g'ri yoki bekor qilingan — kalitni qayta kiriting." }],
        OpenAi: [{ ok: false, latencyMs: 210, message: 'Model topilmadi — model nomini tekshiring.' }],
      },
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');
    const testButtons = screen.getAllByRole('button', { name: 'Aloqani tekshirish' });

    await user.click(testButtons[0]!); // Gemini
    expect(
      await screen.findByText("API kaliti noto'g'ri yoki bekor qilingan — kalitni qayta kiriting."),
    ).toBeInTheDocument();

    await user.click(testButtons[1]!); // OpenAI
    expect(await screen.findByText('Model topilmadi — model nomini tekshiring.')).toBeInTheDocument();

    // Ikkala xato matni bir vaqtda, bir-biridan farqli holda ko'rinadi.
    expect(
      screen.getByText("API kaliti noto'g'ri yoki bekor qilingan — kalitni qayta kiriting."),
    ).toBeInTheDocument();
    expect(screen.getByText('Model topilmadi — model nomini tekshiring.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter(([input]) => String(input).includes('/test'))).toHaveLength(2);
  });

  it("'Aloqani tekshirish' muvaffaqiyatli bo'lsa latency bilan ko'rsatadi", async () => {
    mockFetch({ testResponses: { Gemini: [{ ok: true, latencyMs: 640, message: 'Ulanish muvaffaqiyatli — provayder javob berdi.' }] } });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');
    const testButtons = screen.getAllByRole('button', { name: 'Aloqani tekshirish' });
    await user.click(testButtons[0]!);

    expect(await screen.findByText('Ishlayapti · 640 ms')).toBeInTheDocument();
  });

  it('hech bir provider faol bo\'lmasa ogohlantirish banner ko\'rsatadi', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/api/admin/ai/providers') && !url.includes('/test') && !url.includes('/set-default')) {
        return Promise.resolve(
          listResponse<'AdminAiProviderDto'>([
            { ...GEMINI_CONFIG, isActive: false, isDefault: false },
            OPENAI_CONFIG,
            ANTHROPIC_CONFIG,
          ]),
        );
      }
      if (url.includes('/api/admin/ai/prompts')) return Promise.resolve(listResponse<'AdminPromptTemplateDto'>([PROMPT_TEMPLATE]));
      if (url.includes('/api/admin/ai/usage')) return Promise.resolve(jsonResponse<'AdminAiUsageDto'>(USAGE_STATS));
      return Promise.reject(new Error(`unexpected fetch: ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);
    renderPage();

    expect(
      await screen.findByText('AI tahlil ishlamaydi — kamida bitta provayder sozlanishi kerak.'),
    ).toBeInTheDocument();
  });

  it("'Default qilish' tasdiq dialogisiz bajarilmaydi", async () => {
    mockFetch();
    renderPage();

    await screen.findAllByTestId('masked-api-key');
    // Gemini — allaqachon default, shu sabab tugma o'chirilgan (xavfsiz holat: default
    // provayderni qayta default qilishga urinib bo'lmaydi).
    const defaultButtons = screen.getAllByRole('button', { name: 'Default qilish' });
    expect(defaultButtons[0]).toBeDisabled();
  });

  it("kalitga ega, lekin default bo'lmagan provayderda 'Default qilish' tasdiqlangach ishga tushadi", async () => {
    const setDefaultCalls: string[] = [];
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? 'GET';
      if (url.includes('/set-default') && method === 'POST') {
        const provider = url.match(/\/providers\/([A-Za-z]+)\/set-default$/)?.[1] ?? '';
        setDefaultCalls.push(provider);
        return Promise.resolve(
          jsonResponse<'AdminAiProviderDto'>({
            ...GEMINI_CONFIG,
            provider: toProviderKind(provider),
            isDefault: true,
          }),
        );
      }
      if (url.includes('/api/admin/ai/providers') && method === 'GET') {
        return Promise.resolve(
          listResponse<'AdminAiProviderDto'>([
            GEMINI_CONFIG,
            { ...OPENAI_CONFIG, maskedApiKey: 'sk-••••abcd', isActive: true, isDefault: false },
            ANTHROPIC_CONFIG,
          ]),
        );
      }
      if (url.includes('/api/admin/ai/prompts')) return Promise.resolve(listResponse<'AdminPromptTemplateDto'>([PROMPT_TEMPLATE]));
      if (url.includes('/api/admin/ai/usage')) return Promise.resolve(jsonResponse<'AdminAiUsageDto'>(USAGE_STATS));
      return Promise.reject(new Error(`unexpected fetch: ${method} ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderPage();

    await screen.findAllByTestId('masked-api-key');
    const defaultButtons = screen.getAllByRole('button', { name: 'Default qilish' });
    expect(defaultButtons[1]).toBeEnabled(); // OpenAI — kaliti bor, default emas.

    await user.click(defaultButtons[1]!);
    await screen.findByText('"OpenAI" ni default qilishni tasdiqlang');
    expect(setDefaultCalls).toHaveLength(0);

    await user.click(screen.getByText('Ha, default qilish'));

    await waitFor(() => {
      expect(setDefaultCalls).toEqual(['OpenAi']);
    });
  });
});
