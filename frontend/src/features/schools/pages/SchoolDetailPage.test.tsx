import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { ROUTE_PATTERNS } from '@/shared/config/routes';
import SchoolDetailPage from './SchoolDetailPage';
import type { SchoolDetailDto, SchoolStatsDto } from '../model/types';

const BASE_DETAIL: SchoolDetailDto = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  schoolNumber: '12',
  contactPerson: 'Aliyev Vali',
  contactPhone: '+998901234567',
  dailyRegistrationLimit: 500,
  accessCode: null,
  notes: null,
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc123token',
  qrCodeBase64: 'aGVsbG8=',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  stats: {
    studentCount: 120,
    completedCount: 80,
    inProgressCount: 7,
    completionRate: 0.667,
    lastActivityAt: '2026-08-30T10:00:00Z',
  },
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function mockFetch(stats?: Partial<SchoolStatsDto>, status = 200) {
  const detail: SchoolDetailDto = { ...BASE_DETAIL, stats: { ...BASE_DETAIL.stats, ...stats } };
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/schools/school-1')) {
      return Promise.resolve(
        status === 200
          ? jsonResponse(detail)
          : jsonResponse({ code: 'NOT_FOUND', title: 'Topilmadi', status }, status),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderDetailPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/schools/school-1']}>
          <Routes>
            <Route path={ROUTE_PATTERNS.admin.schoolDetail} element={<SchoolDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/** Statistika bo'limidagi bitta karta qiymatini o'qiydi (label bo'yicha). */
function statValue(label: string): string {
  const section = screen.getByRole('region', { name: 'Maktab ishtirok statistikasi' });
  const card = within(section).getByText(label).parentElement;
  expect(card).not.toBeNull();
  return card!.querySelectorAll('span')[1]?.textContent ?? '';
}

describe('SchoolDetailPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("maktab ma'lumotlari va ishtirok statistikasini ko'rsatadi", async () => {
    mockFetch();
    renderDetailPage();

    expect(await screen.findByRole('heading', { name: '12-son maktab' })).toBeInTheDocument();
    expect(screen.getByText("Farg'ona, Qo'qon")).toBeInTheDocument();
    expect(screen.getByText('Aliyev Vali')).toBeInTheDocument();
    expect(screen.getByText('+998901234567')).toBeInTheDocument();

    expect(statValue("Ro'yxatdan o'tgan")).toBe('120');
    expect(statValue('Testni yakunlagan')).toBe('80');
    expect(statValue('Jarayonda')).toBe('7');
    expect(statValue('Oxirgi topshirilgan')).toBe('30.08.2026');
  });

  it("yakunlash ulushi (0..1) foizga o'giriladi — 0.667 → 67%", async () => {
    mockFetch();
    renderDetailPage();

    await screen.findByRole('heading', { name: '12-son maktab' });

    expect(statValue('Yakunlash ulushi')).toBe('67%');
  });

  it("hech kim topshirmagan bo'lsa 0 ko'rsatiladi, ulush esa '—'", async () => {
    mockFetch({
      studentCount: 0,
      completedCount: 0,
      inProgressCount: 0,
      completionRate: null,
      lastActivityAt: null,
    });
    renderDetailPage();

    await screen.findByRole('heading', { name: '12-son maktab' });

    expect(statValue("Ro'yxatdan o'tgan")).toBe('0');
    expect(statValue('Testni yakunlagan')).toBe('0');
    expect(statValue('Jarayonda')).toBe('0');
    // Nol va "ma'lumot yo'q" ajratiladi: nisbat aniqlanmagan → `—`, `0%` EMAS.
    expect(statValue('Yakunlash ulushi')).toBe('—');
    expect(statValue('Oxirgi topshirilgan')).toBe('—');
  });

  it("shu maktab o'quvchilariga `schoolId` filtri bilan havola beradi", async () => {
    mockFetch();
    renderDetailPage();

    const link = await screen.findByRole('link', { name: "Shu maktab o'quvchilarini ko'rish" });
    expect(link).toHaveAttribute('href', '/admin/students?schoolId=school-1');
  });

  it('maktab topilmasa xato holati va qayta urinish tugmasi ko\'rsatiladi', async () => {
    mockFetch(undefined, 404);
    renderDetailPage();

    expect(await screen.findByText('Maktab topilmadi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });
});
