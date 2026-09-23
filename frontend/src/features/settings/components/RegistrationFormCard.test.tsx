import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { setAdminAccessToken } from '@/shared/api/adminClient';
import { problemResponse, typedResponse } from '@/test/apiMock';
import {
  REGISTRATION_FORM_DEFAULT_DEFINITION,
  type RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';
import { RegistrationFormCard } from './RegistrationFormCard';

function renderCard() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <RegistrationFormCard />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/**
 * `RegistrationCustomFieldDialog`/`ConfirmDialog` — native `<dialog>` asosida (`shared/ui/
 * Dialog.tsx`). jsdom `showModal()`ni amalga oshirmaydi, shu sabab `open` atributi hech qachon
 * qo'yilmaydi va tarkib CSS orqali doim `display:none` bo'lib qoladi (`SettingsPage.test.tsx`
 * dagi izohga qarang) — `getByRole` bunday elementlarni chiqarib tashlaydi, shu sabab bu yerda
 * sarlavha matnidan `<dialog>` konteynerini topib, `within()` bilan doiralanadi.
 */
function withinDialog(titleText: string) {
  const heading = screen.getByText(titleText);
  const dialog = heading.closest('dialog');
  if (!dialog) throw new Error(`Dialog topilmadi: ${titleText}`);
  return within(dialog);
}

function mockGetOnly(definition: RegistrationFormDefinition = REGISTRATION_FORM_DEFAULT_DEFINITION) {
  return vi.fn().mockResolvedValue(typedResponse<RegistrationFormDefinition>(definition));
}

describe('RegistrationFormCard', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    setAdminAccessToken(null);
  });

  it("standart sozlama yuklanadi va sakkizta asosiy maydon ko'rsatiladi", async () => {
    vi.stubGlobal('fetch', mockGetOnly());

    renderCard();

    expect(await screen.findByDisplayValue('F.I.Sh.')).toBeInTheDocument();
    expect(screen.getByDisplayValue("Tug'ilgan sana")).toBeInTheDocument();
    expect(screen.getByDisplayValue('Jins')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Sinf')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Sinf harfi')).toBeInTheDocument();
    // 2026-09-23 egasi qarori: ota-ona telefoni birinchi (majburiy), keyin shaxsiy raqam (ixtiyoriy).
    expect(screen.getByDisplayValue('Ota-ona telefoni')).toBeInTheDocument();
    expect(screen.getByDisplayValue("Shaxsiy raqamingiz (bo'lsa)")).toBeInTheDocument();
    const labelValues = screen.getAllByLabelText("Yorlig'i").map((input) => (input as HTMLInputElement).value);
    expect(labelValues.indexOf('Ota-ona telefoni')).toBeLessThan(labelValues.indexOf("Shaxsiy raqamingiz (bo'lsa)"));
    expect(screen.getByDisplayValue('Email')).toBeInTheDocument();
    // Hali hech narsa o'zgarmagan — saqlash tugmasi o'chiq.
    expect(screen.getByText('Formani saqlash')).toBeDisabled();
  });

  it("'F.I.Sh.' holatini o'zgartirib bo'lmaydi, lekin yorlig'ini tahrirlash mumkin", async () => {
    vi.stubGlobal('fetch', mockGetOnly());
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    // `coreFieldEntries` `order` bo'yicha tartiblaydi — `fullName` order=1, shu sabab BIRINCHI.
    const requirementSelects = screen.getAllByLabelText('Holat');
    expect(requirementSelects[0]).toBeDisabled();

    const labelInputs = screen.getAllByLabelText("Yorlig'i");
    await user.clear(labelInputs[0]!);
    await user.type(labelInputs[0]!, 'Familiya Ism Sharif');

    expect(labelInputs[0]).toHaveValue('Familiya Ism Sharif');
    expect(screen.getByText('Formani saqlash')).not.toBeDisabled();
  });

  it("o'z maydon qo'shiladi, ro'yxatda ko'rinadi va PUT tanasiga to'g'ri tushadi", async () => {
    // `let putBody: ... | null = null` ATAYLAB ishlatilmaydi: TypeScript callback ichidagi
    // tayinlashni kuzata olmaydi va o'zgaruvchini `null`ga toraytirib qo'yadi — keyingi
    // o'qishda tur `never` bo'lib, `tsc -b` (Docker build ham shuni yuritadi) yiqiladi.
    // Massivga `push` bunday toraytirishga tushmaydi.
    const putBodies: RegistrationFormDefinition[] = [];
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/admin/settings/registration-form') && init?.method === 'PUT') {
        const body = JSON.parse(String(init.body)) as RegistrationFormDefinition;
        putBodies.push(body);
        return Promise.resolve(typedResponse<RegistrationFormDefinition>(body));
      }
      return Promise.resolve(typedResponse<RegistrationFormDefinition>(REGISTRATION_FORM_DEFAULT_DEFINITION));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    await user.click(screen.getByText("Maydon qo'shish"));
    const dialog = withinDialog("O'z maydon qo'shish");
    await user.type(dialog.getByLabelText('Kod'), 'PARENT_JOB');
    await user.type(dialog.getByLabelText("Yorlig'i"), 'Ota-onangiz kasbi');
    await user.click(dialog.getByText('Saqlash'));

    expect(await screen.findByText('PARENT_JOB')).toBeInTheDocument();
    // "Ota-onangiz kasbi" ikki joyda ko'rinadi: ro'yxat elementi VA jonli oldindan ko'rish.
    expect(screen.getAllByText('Ota-onangiz kasbi').length).toBeGreaterThanOrEqual(2);

    await user.click(screen.getByText('Formani saqlash'));

    await waitFor(() => {
      expect(putBodies).toHaveLength(1);
    });
    expect(putBodies[0]?.customFields).toHaveLength(1);
    expect(putBodies[0]?.customFields[0]).toMatchObject({
      code: 'PARENT_JOB',
      labelUz: 'Ota-onangiz kasbi',
      type: 'ShortText',
      requirement: 'Optional',
      order: 9,
    });
    expect(await screen.findByText('Ro\'yxatdan o\'tish formasi saqlandi.')).toBeInTheDocument();
  });

  it("mavjud o'z maydon o'chiriladi va PUT tanasida qolmaydi", async () => {
    const seeded: RegistrationFormDefinition = {
      ...REGISTRATION_FORM_DEFAULT_DEFINITION,
      customFields: [
        {
          code: 'PARENT_JOB',
          type: 'ShortText',
          labelUz: 'Ota-onangiz kasbi',
          placeholderUz: null,
          requirement: 'Optional',
          maxLength: null,
          inputPattern: null,
          options: null,
          order: 9,
        },
      ],
    };
    // `let putBody: ... | null = null` ATAYLAB ishlatilmaydi: TypeScript callback ichidagi
    // tayinlashni kuzata olmaydi va o'zgaruvchini `null`ga toraytirib qo'yadi — keyingi
    // o'qishda tur `never` bo'lib, `tsc -b` (Docker build ham shuni yuritadi) yiqiladi.
    // Massivga `push` bunday toraytirishga tushmaydi.
    const putBodies: RegistrationFormDefinition[] = [];
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/admin/settings/registration-form') && init?.method === 'PUT') {
        const body = JSON.parse(String(init.body)) as RegistrationFormDefinition;
        putBodies.push(body);
        return Promise.resolve(typedResponse<RegistrationFormDefinition>(body));
      }
      return Promise.resolve(typedResponse<RegistrationFormDefinition>(seeded));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderCard();
    expect(await screen.findByText('PARENT_JOB')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Ota-onangiz kasbi maydonini o\'chirish'));
    const confirmDialog = withinDialog('Maydonni o\'chirish');
    await user.click(confirmDialog.getByText('Tasdiqlash'));

    expect(screen.queryByText('PARENT_JOB')).not.toBeInTheDocument();

    await user.click(screen.getByText('Formani saqlash'));

    await waitFor(() => {
      expect(putBodies).toHaveLength(1);
    });
    expect(putBodies[0]?.customFields).toHaveLength(0);
  });

  it("band (asosiy) kod dialog darajasida rad etiladi, notoʻgʻri format ham", async () => {
    vi.stubGlobal('fetch', mockGetOnly());
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    await user.click(screen.getByText("Maydon qo'shish"));
    const dialog = withinDialog("O'z maydon qo'shish");
    await user.type(dialog.getByLabelText('Kod'), 'phone');
    await user.type(dialog.getByLabelText("Yorlig'i"), 'Ikkinchi telefon');
    await user.click(dialog.getByText('Saqlash'));

    expect(await screen.findByText('Bu kod band — boshqa nom tanlang.')).toBeInTheDocument();

    await user.clear(dialog.getByLabelText('Kod'));
    await user.type(dialog.getByLabelText('Kod'), 'bad code!');
    await user.click(dialog.getByText('Saqlash'));

    expect(
      await screen.findByText(
        "Kod faqat lotin harf, raqam, '-' va '_' belgilaridan (1-20 ta) iborat bo'lishi mumkin.",
      ),
    ).toBeInTheDocument();
  });

  it("SingleChoice turida kamida 2 ta variant talab qilinadi va muharrir ishlaydi", async () => {
    vi.stubGlobal('fetch', mockGetOnly());
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    await user.click(screen.getByText("Maydon qo'shish"));
    const dialog = withinDialog("O'z maydon qo'shish");
    await user.type(dialog.getByLabelText('Kod'), 'TRANSPORT');
    await user.type(dialog.getByLabelText("Yorlig'i"), 'Transport turi');
    await user.selectOptions(dialog.getByLabelText('Turi'), 'SingleChoice');
    await user.click(dialog.getByText('Saqlash'));

    expect(await screen.findByText('Kamida 2 ta tanlov varianti kerak.')).toBeInTheDocument();

    await user.click(dialog.getByText("Variant qo'shish"));
    await user.click(dialog.getByText("Variant qo'shish"));

    const textInputs = dialog.getAllByLabelText(/Variant \d+ matni/);
    const valueInputs = dialog.getAllByLabelText(/Variant \d+ qiymati/);
    await user.type(textInputs[0]!, 'Piyoda');
    await user.type(valueInputs[0]!, 'foot');
    await user.type(textInputs[1]!, 'Avtobus');
    await user.type(valueInputs[1]!, 'bus');
    await user.click(dialog.getByText('Saqlash'));

    expect(await screen.findByText('TRANSPORT')).toBeInTheDocument();
  });

  it("jonli oldindan ko'rish yorliq o'zgarishiga darhol mos keladi", async () => {
    vi.stubGlobal('fetch', mockGetOnly());
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    // `coreFieldEntries` tartibi: fullName(0), birthDate(1), gender(2), ...
    const labelInputs = screen.getAllByLabelText("Yorlig'i");
    await user.clear(labelInputs[2]!);
    await user.type(labelInputs[2]!, 'Jinsi');

    const previewHeading = screen.getByText("Jonli oldindan ko'rish");
    const previewRegion = previewHeading.closest('section');
    expect(previewRegion).not.toBeNull();
    expect(within(previewRegion!).getByText('Jinsi')).toBeInTheDocument();
  });

  it('serverdan qaytgan REGISTRATION_FORM_* xatosi tushunarli matn bilan ko\'rsatiladi', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/admin/settings/registration-form') && init?.method === 'PUT') {
        return Promise.resolve(problemResponse('REGISTRATION_FORM_OPTION_VALUE_DUPLICATE', 409));
      }
      return Promise.resolve(typedResponse<RegistrationFormDefinition>(REGISTRATION_FORM_DEFAULT_DEFINITION));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderCard();
    await screen.findByDisplayValue('F.I.Sh.');

    const labelInputs = screen.getAllByLabelText("Yorlig'i");
    await user.type(labelInputs[0]!, ' (yangi)');
    await user.click(screen.getByText('Formani saqlash'));

    expect(
      await screen.findByText('Bitta maydon ichida tanlov qiymati takrorlangan.'),
    ).toBeInTheDocument();
  });
});
