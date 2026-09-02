import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import CatalogPage from './CatalogPage';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/catalog']}>
          <CatalogPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/**
 * Katalog CRUD backend'da hali yo'q (`features/catalog/model/types.ts` boshidagi izohga
 * qarang) — bu test aynan shu, real, hozircha kutilgan holatni tasdiqlaydi: sahifa soxta
 * muvaffaqiyat ko'rsatmaydi, aniq xato holatini (`ErrorState` + "Qayta urinish") beradi.
 */
describe('CatalogPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('katalog endpointi mavjud bo\'lmasa (404) xato holatini "Qayta urinish" bilan ko\'rsatadi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ code: 'NOT_FOUND', status: 404 }, 404)),
    );
    renderPage();

    expect(await screen.findByText("Katalogni yuklab bo'lmadi")).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("\"Test yuklash\" dialogida noto'g'ri JSON tanlansa yuklash tugmasi o'chiq qoladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ code: 'NOT_FOUND', status: 404 }, 404)),
    );
    const user = userEvent.setup();
    renderPage();

    await screen.findByText("Katalogni yuklab bo'lmadi");
    await user.click(screen.getByRole('button', { name: /Test yuklash/ }));

    const file = new File(['{ not json'], 'test.json', { type: 'application/json' });
    const input = document.getElementById('test-import-file-input') as HTMLInputElement;
    await user.upload(input, file);

    await waitFor(() => {
      expect(screen.getByText('Fayl yaroqli JSON emas.')).toBeInTheDocument();
    });
    expect(screen.getByText('Yuklash').closest('button')).toBeDisabled();
  });

  it("to'g'ri JSON tanlansa preview ko'rsatiladi va yuklash tugmasi yoqiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ code: 'NOT_FOUND', status: 404 }, 404)),
    );
    const user = userEvent.setup();
    renderPage();

    await screen.findByText("Katalogni yuklab bo'lmadi");
    await user.click(screen.getByRole('button', { name: /Test yuklash/ }));

    const validFile = {
      code: 'STRESS',
      nameUz: 'Stress anketasi',
      estimatedMinutes: 5,
      questions: Array.from({ length: 4 }, (_, index) => ({
        code: `Q0${index + 1}`,
        order: index + 1,
        textUz: 'Savol',
        type: 'Likert5',
        scale: 'STRESS',
        direction: 1,
        weight: 1,
        isRequired: true,
      })),
    };
    const file = new File([JSON.stringify(validFile)], 'stress.json', { type: 'application/json' });
    const input = document.getElementById('test-import-file-input') as HTMLInputElement;
    await user.upload(input, file);

    await waitFor(() => {
      expect(screen.getByText("Fayl to'g'ri — yuklashga tayyor")).toBeInTheDocument();
    });
    expect(screen.getByText('Yuklash').closest('button')).toBeEnabled();
  });
});
