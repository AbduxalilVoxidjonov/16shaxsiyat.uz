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
});
