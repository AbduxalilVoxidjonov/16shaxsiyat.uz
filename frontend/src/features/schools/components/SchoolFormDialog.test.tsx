import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, type Schemas } from '@/test/apiMock';
import { SchoolFormDialog } from './SchoolFormDialog';

const SCHOOL_DETAIL = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  schoolNumber: '12',
  contactPerson: 'Aliyev Vali',
  contactPhone: '+998901234567',
  dailyRegistrationLimit: 500,
  accessCode: null,
  entryCode: 'ABCD-2345',
  notes: null,
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc',
  qrCodeBase64: 'aGVsbG8=',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  stats: {
    studentCount: 120,
    completedCount: 80,
    inProgressCount: 5,
    completionRate: 0.667,
    lastActivityAt: '2026-08-30T10:00:00Z',
  },
  linkHealth: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
} satisfies Schemas['AdminSchoolDetailDto'];

/**
 * jsdom `showModal()`ni amalga oshirmaydi — oyna `open` atributisiz qoladi va `getByRole`
 * uni topa olmaydi (`SchoolsPage.test.tsx` dagi izoh). Shu sabab maydonlar `getByLabelText`
 * bilan qidiriladi.
 */
function renderDialog(props: { open: boolean; schoolId: string | null }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <SchoolFormDialog open={props.open} schoolId={props.schoolId} onClose={() => undefined} />
      </ToastProvider>
    </QueryClientProvider>,
  );
  return {
    ...view,
    setProps: (next: { open: boolean; schoolId: string | null }) =>
      view.rerender(
        <QueryClientProvider client={queryClient}>
          <ToastProvider>
            <SchoolFormDialog open={next.open} schoolId={next.schoolId} onClose={() => undefined} />
          </ToastProvider>
        </QueryClientProvider>,
      ),
  };
}

describe('SchoolFormDialog', () => {
  /**
   * P30-9 regressiya himoyasi: forma faqat oyna OCHIQ bo'lganda mount qilinadi va
   * boshlang'ich qiymatlarni mount paytida oladi. Agar forma yana doim mount holatda
   * turib, qiymatlar `useEffect(… reset …)` bilan to'ldiriladigan bo'lsa, shu test
   * yiqiladi — o'sha naqsh foydalanuvchi yozganini jimgina o'chirib tashlagan edi.
   */
  it("oyna yopiq bo'lganda forma render qilinmaydi", () => {
    const { setProps } = renderDialog({ open: false, schoolId: null });
    expect(screen.queryByLabelText('Nomi')).toBeNull();

    setProps({ open: true, schoolId: null });
    expect(screen.getByLabelText('Nomi')).toHaveValue('');
  });

  it("ochilgandan keyin yozilgan qiymat jimgina yo'qolmaydi", async () => {
    const user = userEvent.setup();
    renderDialog({ open: true, schoolId: null });

    const nameInput = screen.getByLabelText('Nomi');
    await user.type(nameInput, 'Yangi maktab nomi');

    // Barcha effekt/mikrovazifalar oqib bo'lgach ham qiymat joyida turishi kerak.
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(nameInput).toHaveValue('Yangi maktab nomi');
    expect(screen.getByLabelText('Tuman')).toHaveValue('');
  });

  /**
   * Ikki ustunli tartibga o'tishda regressiya himoyasi: barcha 8 maydon hamon render bo'ladi
   * va to'ldirilgan forma `POST /api/admin/schools` ga to'g'ri tana bilan ketadi. Eski "Kirish
   * kodi" (`accessCode`) maydoni admin UI'dan olib tashlangan (2026-09-07) — u YO'Q va tanada
   * ham yuborilmaydi (backend `null` deb qabul qiladi).
   */
  it("yaratish: barcha maydonlar bor, to'ldirilgach POST ketadi va oyna yopiladi", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(
        jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, id: 'school-2' }, 201),
      );
    vi.stubGlobal('fetch', fetchMock);
    const onClose = vi.fn();
    const user = userEvent.setup();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <SchoolFormDialog open schoolId={null} onClose={onClose} />
        </ToastProvider>
      </QueryClientProvider>,
    );

    for (const label of [
      'Nomi',
      'Viloyat',
      'Tuman',
      'Maktab raqami',
      "Kunlik ro'yxatdan o'tish limiti",
      "Mas'ul shaxs (F.I.Sh.)",
      'Telefon raqami',
      'Izoh',
    ]) {
      expect(screen.getByLabelText(label)).toBeInTheDocument();
    }
    expect(screen.queryByLabelText('Kirish kodi')).toBeNull();

    await user.type(screen.getByLabelText('Nomi'), '12-son maktab');
    await user.selectOptions(screen.getByLabelText('Viloyat'), "Farg'ona");
    await user.type(screen.getByLabelText('Tuman'), "Qo'qon");
    await user.type(screen.getByLabelText('Maktab raqami'), '12');
    await user.click(screen.getByText('Yaratish'));

    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(1));
    const postCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'POST',
    );
    expect(postCall).toBeDefined();
    expect(String(postCall![0])).toContain('/api/admin/schools');
    const body = JSON.parse(String((postCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body).toMatchObject({
      name: '12-son maktab',
      region: "Farg'ona",
      district: "Qo'qon",
      schoolNumber: '12',
      dailyRegistrationLimit: 500,
    });
    // Bo'sh ixtiyoriy maydonlar `""` sifatida YUBORILMAYDI (`emptyToUndefined`).
    expect(body).not.toHaveProperty('contactPerson');
    expect(body).not.toHaveProperty('notes');
    // Eskirgan `accessCode` tanada UMUMAN yo'q — `""` ham, `null` ham emas.
    expect(body).not.toHaveProperty('accessCode');
    expect(await screen.findByText('Maktab yaratildi')).toBeInTheDocument();
  });

  it("tahrirlashda maydonlar ma'lumot kelgach to'ldirilgan holda mount bo'ladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'AdminSchoolDetailDto'>(SCHOOL_DETAIL)),
    );

    renderDialog({ open: true, schoolId: 'school-1' });

    // Yuklanayotganda forma yo'q — skeleton ko'rsatiladi.
    expect(screen.queryByLabelText('Nomi')).toBeNull();

    await waitFor(() => {
      expect(screen.getByLabelText('Nomi')).toHaveValue('12-son maktab');
    });
    expect(screen.getByLabelText('Tuman')).toHaveValue("Qo'qon");
  });

  it("tahrirlashda eski `accessCode` qiymati bo'lsa ham maydon ko'rinmaydi va PUT tanasida yuborilmaydi", async () => {
    const fetchMock = vi.fn().mockImplementation((_input: RequestInfo | URL, init?: RequestInit) =>
      Promise.resolve(
        jsonResponse<'AdminSchoolDetailDto'>({
          ...SCHOOL_DETAIL,
          accessCode: '123456',
          ...(init?.method === 'PUT' ? { updatedAt: '2026-09-07T00:00:00Z' } : {}),
        }),
      ),
    );
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderDialog({ open: true, schoolId: 'school-1' });
    await waitFor(() => {
      expect(screen.getByLabelText('Nomi')).toHaveValue('12-son maktab');
    });
    expect(screen.queryByLabelText('Kirish kodi')).toBeNull();
    expect(screen.queryByDisplayValue('123456')).toBeNull();

    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([, init]) => (init as RequestInit | undefined)?.method === 'PUT'),
      ).toBe(true);
    });
    const putCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'PUT',
    );
    const body = JSON.parse(String((putCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body).toMatchObject({ name: '12-son maktab', district: "Qo'qon" });
    expect(body).not.toHaveProperty('accessCode');
  });
});
