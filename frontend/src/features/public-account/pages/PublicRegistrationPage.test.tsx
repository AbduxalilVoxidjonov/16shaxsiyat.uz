import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import { REGISTRATION_FORM_DEFAULT_DEFINITION } from '@/shared/api/registrationFormSettingsTypes';
import PublicRegistrationPage from './PublicRegistrationPage';
import { usePublicUserStore } from '../store/publicUserStore';
import type { MyStudentProfile } from '../model/types';

const START_RESULT = {
  sessionToken: 'session-token-1',
  assessmentId: 'assessment-1',
  status: 'Draft',
  expiresAt: '2026-09-12T10:12:00Z',
  resumed: false,
  tests: [
    {
      code: 'MBTI16',
      name: '16 tipli shaxsiyat modeli',
      status: 'NotStarted',
      answered: 0,
      total: 60,
      order: 1,
      estimatedMinutes: 9,
    },
  ],
} satisfies Schemas['StartSessionResult'];

/** A holati: profil yo'q, Telegram ismi ham yo'q (mavjud testlar F.I.Sh. ni o'zi yozadi). */
const NO_PROFILE = {
  hasProfile: false,
  fullName: null,
  birthDate: null,
  phone: null,
  grade: null,
  email: null,
  consentVersion: null,
  consentCurrent: false,
  parentalConsent: false,
  isMinor: false,
  suggestedFullName: null,
  registrationForm: REGISTRATION_FORM_DEFAULT_DEFINITION,
} satisfies MyStudentProfile;

/** B holati: profil to'liq, rozilik joriy. */
const FULL_PROFILE = {
  hasProfile: true,
  fullName: 'Karimov Sardor Alisherovich',
  birthDate: '1995-04-12',
  gender: 'Male',
  phone: '+998901234567',
  grade: null,
  email: null,
  consentVersion: '1.0',
  consentCurrent: true,
  parentalConsent: false,
  isMinor: false,
  suggestedFullName: 'Valiyev Ali',
  registrationForm: REGISTRATION_FORM_DEFAULT_DEFINITION,
} satisfies MyStudentProfile;

function renderPage(path = '/kabinet/test') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/kabinet" element={<p>KABINET_STUB</p>} />
            <Route path="/kabinet/test" element={<PublicRegistrationPage />} />
            <Route path="/t/:slug/test/:testCode" element={<p>TEST_STUB</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/**
 * `fetch` ni URL/metod bo'yicha yo'naltiradi: `GET /api/me/profile` → profil,
 * `PUT /api/me/profile` → `updateResponse` (berilmasa — yuborilgan tanadan yig'ilgan profil),
 * qolgani (sessiya) → `sessionResponse`. Chaqiruvlar `sessionCall()` / `updateCall()` bilan olinadi.
 */
function mockApi(
  profile: MyStudentProfile,
  sessionResponse: () => Response,
  updateResponse?: () => Response,
) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    if (String(input).includes('/api/me/profile')) {
      if (init?.method === 'PUT') {
        if (updateResponse) return Promise.resolve(updateResponse());
        const sent = JSON.parse(String(init.body)) as Schemas['UpdateStudentProfileRequest'];
        return Promise.resolve(
          jsonResponse<'MyStudentProfileDto'>({
            ...profile,
            hasProfile: true,
            fullName: sent.fullName ?? profile.fullName,
            phone: sent.phone ?? profile.phone,
            consentVersion: '1.0',
            consentCurrent: true,
          }),
        );
      }
      return Promise.resolve(jsonResponse<'MyStudentProfileDto'>(profile));
    }
    return Promise.resolve(sessionResponse());
  });
  vi.stubGlobal('fetch', fetchMock);

  function findCall(path: string, method?: string): [string, RequestInit] | undefined {
    return fetchMock.mock.calls.find(
      ([url, init]) =>
        String(url).includes(path) && (method === undefined || (init as RequestInit | undefined)?.method === method),
    ) as [string, RequestInit] | undefined;
  }

  return {
    fetchMock,
    sessionCall: () => findCall('/api/me/sessions'),
    updateCall: () => findCall('/api/me/profile', 'PUT'),
  };
}

interface FillOptions {
  year?: string;
  grade?: string;
}

/** Anketani to'ldiradi (kattalar uchun standart sana — ota-ona roziligi so'ralmaydi). */
async function fillForm(user: ReturnType<typeof userEvent.setup>, options: FillOptions = {}) {
  await user.type(await screen.findByLabelText('F.I.Sh.'), 'Karimov Sardor Alisherovich');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan kun"), '12');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan oy"), '4');
  await user.selectOptions(screen.getByLabelText("Tug'ilgan yil"), options.year ?? '1995');
  await user.click(screen.getByLabelText('Erkak'));
  if (options.grade) {
    await user.selectOptions(screen.getByLabelText('Sinf'), options.grade);
  }
  await user.type(screen.getByLabelText('Telefon raqami'), '901234567');
  await user.click(screen.getByLabelText(/roziman/));
}

describe('PublicRegistrationPage (maktabsiz anketa)', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken('access-1');
    usePublicUserStore.getState().setSession('access-1', {
      id: 'user-1',
      username: null,
      firstName: 'Ali',
      lastName: null,
      photoUrl: null,
      createdAt: '2026-09-01T10:00:00Z',
      lastLoginAt: '2026-09-05T10:00:00Z',
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  describe("A holati — profil yo'q (birinchi test)", () => {
    it("maktab maydonlarini SO'RAMAYDI (kirish kodi, sinf harfi, ota-ona telefoni)", async () => {
      mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await screen.findByLabelText('F.I.Sh.');

      expect(screen.queryByLabelText('Kirish kodi')).not.toBeInTheDocument();
      expect(screen.queryByLabelText('Sinf harfi')).not.toBeInTheDocument();
      expect(screen.queryByLabelText('Ota-ona telefon raqami')).not.toBeInTheDocument();
    });

    it("F.I.Sh. Telegram ismidan OLDINDAN to'ldiriladi va tahrirlanadi", async () => {
      const user = userEvent.setup();
      mockApi(
        { ...NO_PROFILE, suggestedFullName: 'Valiyev Ali' },
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
      );

      renderPage();

      const fullName = await screen.findByLabelText('F.I.Sh.');
      expect(fullName).toHaveValue('Valiyev Ali');
      expect(screen.getByText(/Telegram ismingizdan olindi/)).toBeInTheDocument();
      // Rozilik bloki bor — yangi profil.
      expect(screen.getByLabelText(/roziman/)).toBeInTheDocument();

      await user.clear(fullName);
      await user.type(fullName, 'Valiyev Ali Akmalovich');
      expect(fullName).toHaveValue('Valiyev Ali Akmalovich');
    });

    it('tugma "Testni boshlash" va ustida "saqlanadi va test boshlanadi" izohi', async () => {
      mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();

      expect(await screen.findByRole('button', { name: 'Testni boshlash' })).toBeInTheDocument();
      expect(screen.getByText("Ma'lumotlar saqlanadi va test boshlanadi.")).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Saqlash' })).not.toBeInTheDocument();
    });

    it("to'ldirilgan anketa `POST /api/me/sessions` ga shartnomadagi tanani yuboradi", async () => {
      const user = userEvent.setup();
      const api = mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      await waitFor(() => {
        expect(api.sessionCall()).toBeDefined();
      });

      const [url, init] = api.sessionCall()!;
      expect(String(url)).toContain('/api/me/sessions');
      expect((init.headers as Record<string, string>).Authorization).toBe('Bearer access-1');
      expect(JSON.parse(String(init.body))).toEqual({
        fullName: 'Karimov Sardor Alisherovich',
        birthDate: '1995-04-12',
        gender: 'Male',
        phone: '+998901234567',
        consentAccepted: true,
        parentalConsent: false,
        // Sinf tanlanmagan — `null` ("maktabda o'qimayman"), `0` EMAS.
        grade: null,
        email: null,
        languageCode: 'uz',
      });
    });

    it("sessiya ochilgach mavjud test oqimiga o'tadi va sessiya tokenini uzatadi", async () => {
      const user = userEvent.setup();
      mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
      // Sessiya `sessionStore` o'qiydigan kalitga yozildi — test oqimi uni `X-Session-Token`
      // sifatida yuboradi (`shared/api/sessionToken.ts`).
      const persisted = JSON.parse(localStorage.getItem(STORAGE_KEYS.session) ?? '{}') as {
        state?: { sessionToken?: string; slug?: string; assessmentId?: string };
      };
      expect(persisted.state?.sessionToken).toBe('session-token-1');
      expect(persisted.state?.slug).toBe('ommaviy');
      expect(persisted.state?.assessmentId).toBe('assessment-1');
    });

    it("18 yoshgacha ota-ona roziligi so'raladi va usiz yuborilmaydi", async () => {
      const user = userEvent.setup();
      const api = mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await fillForm(user, { year: '2012', grade: '9' });

      // Sana kiritilgach maydon paydo bo'ladi.
      const parentalConsent = await screen.findByLabelText(/Ota-onam/);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));
      await screen.findByText(/ota-ona \(qonuniy vakil\) roziligi majburiy/i);
      expect(api.sessionCall()).toBeUndefined();

      await user.click(parentalConsent);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      await waitFor(() => {
        expect(api.sessionCall()).toBeDefined();
      });
      const body = JSON.parse(String(api.sessionCall()![1].body)) as {
        parentalConsent: boolean;
        grade: number | null;
      };
      expect(body.parentalConsent).toBe(true);
      expect(body.grade).toBe(9);
    });

    it("kattalarga ota-ona roziligi maydoni ko'rsatilmaydi", async () => {
      const user = userEvent.setup();
      mockApi(NO_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await fillForm(user);

      expect(screen.queryByLabelText(/Ota-onam/)).not.toBeInTheDocument();
    });

    it("409 DUPLICATE_ASSESSMENT uchun tushunarli xabar ko'rsatiladi", async () => {
      const user = userEvent.setup();
      mockApi(NO_PROFILE, () => problemResponse('DUPLICATE_ASSESSMENT', 409));

      renderPage();
      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText(/yaqinda yakunlagansiz/)).toBeInTheDocument();
    });

    it("409 PUBLIC_SPACE_NOT_CONFIGURED uchun 'sozlanmagan' xabari chiqadi", async () => {
      const user = userEvent.setup();
      mockApi(NO_PROFILE, () => problemResponse('PUBLIC_SPACE_NOT_CONFIGURED', 409));

      renderPage();
      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText(/hali sozlanmagan/)).toBeInTheDocument();
    });

    it("400 VALIDATION_ERROR maydon xatolarini o'z maydoniga bog'laydi", async () => {
      const user = userEvent.setup();
      mockApi(NO_PROFILE, () =>
        problemResponse('VALIDATION_ERROR', 400, 'Xato', {
          errors: { phone: ["Telefon raqami noto'g'ri."] },
        }),
      );

      renderPage();
      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText("Telefon raqami noto'g'ri.")).toBeInTheDocument();
    });
  });

  describe("B holati — profil to'liq, rozilik joriy", () => {
    it("forma KO'RSATILMAYDI: karta + \"Testni boshlash\" faqat `{}` yuboradi", async () => {
      const user = userEvent.setup();
      const api = mockApi(FULL_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();

      expect(
        await screen.findByRole('heading', { name: "Sizning ma'lumotlaringiz" }),
      ).toBeInTheDocument();
      expect(screen.getByText('Karimov Sardor Alisherovich')).toBeInTheDocument();
      expect(screen.getByText('12.04.1995')).toBeInTheDocument();
      expect(screen.getByText('+998 (90) 123-45-67')).toBeInTheDocument();
      expect(screen.getByText("Maktabda o'qimayman")).toBeInTheDocument();
      // Forma yo'q: F.I.Sh. kiritish maydoni ham, rozilik checkbox'i ham.
      expect(screen.queryByLabelText('F.I.Sh.')).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/roziman/)).not.toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
      const [, init] = api.sessionCall()!;
      // Shaxsiy ma'lumot YUBORILMAYDI — profil bazada.
      expect(JSON.parse(String(init.body))).toEqual({});
    });

    it("\"O'zgartirish\" → forma TO'LDIRILGAN holda, rozilik bloki YO'Q, tugma \"Saqlash\", \"Bekor qilish\" bor", async () => {
      const user = userEvent.setup();
      mockApi(FULL_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await user.click(await screen.findByRole('button', { name: "O'zgartirish" }));

      expect(await screen.findByLabelText('F.I.Sh.')).toHaveValue('Karimov Sardor Alisherovich');
      expect(screen.getByLabelText("Tug'ilgan kun")).toHaveValue('12');
      expect(screen.getByLabelText("Tug'ilgan oy")).toHaveValue('4');
      expect(screen.getByLabelText("Tug'ilgan yil")).toHaveValue('1995');
      expect(screen.getByLabelText('Erkak')).toBeChecked();
      expect(screen.getByLabelText('Telefon raqami')).toHaveValue('(90) 123-45-67');
      expect(screen.queryByLabelText(/roziman/)).not.toBeInTheDocument();
      // Tugma "Saqlash" — foydalanuvchi test boshlanmasligini oldindan biladi.
      expect(screen.getByRole('button', { name: 'Saqlash' })).toBeEnabled();
      expect(screen.queryByRole('button', { name: 'Testni boshlash' })).not.toBeInTheDocument();
      expect(screen.queryByText("Ma'lumotlar saqlanadi va test boshlanadi.")).not.toBeInTheDocument();
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent("Ma'lumotlarni o'zgartirish");
      expect(screen.getByText(/test boshlanmaydi/)).toBeInTheDocument();

      // Bekor qilish — kartaga qaytadi.
      await user.click(screen.getByRole('button', { name: 'Bekor qilish' }));
      expect(await screen.findByRole('heading', { name: "Sizning ma'lumotlaringiz" })).toBeInTheDocument();
      expect(screen.queryByLabelText('F.I.Sh.')).not.toBeInTheDocument();
    });

    it("tahrir \"Saqlash\" → `PUT /api/me/profile`, `POST /api/me/sessions` CHAQIRILMAYDI, karta yangilanadi", async () => {
      const user = userEvent.setup();
      const api = mockApi(FULL_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();
      await user.click(await screen.findByRole('button', { name: "O'zgartirish" }));
      const fullName = await screen.findByLabelText('F.I.Sh.');
      await user.clear(fullName);
      await user.type(fullName, 'Karimova Malika Alisherovna');
      await user.click(screen.getByRole('button', { name: 'Saqlash' }));

      // Egasi ko'rgan xatoning ASOSIY tasdig'i: test boshlanmaydi, sessiya so'rovi yo'q.
      expect(await screen.findByText("Ma'lumotlar saqlandi")).toBeInTheDocument();
      expect(api.sessionCall()).toBeUndefined();
      expect(screen.queryByText('TEST_STUB')).not.toBeInTheDocument();

      const [, init] = api.updateCall()!;
      expect(init.method).toBe('PUT');
      // Tana — `POST /api/me/sessions` dagi tahrir semantikasi bilan bir xil, lekin `languageCode` YO'Q.
      expect(JSON.parse(String(init.body))).toEqual({
        fullName: 'Karimova Malika Alisherovna',
        birthDate: '1995-04-12',
        gender: 'Male',
        phone: '+998901234567',
        grade: 0,
        email: '',
      });

      // `ready` kartaga qaytdi — yangilangan F.I.Sh. bilan, forma yo'q.
      expect(await screen.findByRole('heading', { name: "Sizning ma'lumotlaringiz" })).toBeInTheDocument();
      expect(screen.getByText('Karimova Malika Alisherovna')).toBeInTheDocument();
      expect(screen.queryByLabelText('F.I.Sh.')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Testni boshlash' })).toBeInTheDocument();
    });

    it("`?edit=1` (kabinetdan) → \"Saqlash\" `/kabinet` ga qaytaradi, sessiya ochilmaydi", async () => {
      const user = userEvent.setup();
      const api = mockApi(FULL_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage('/kabinet/test?edit=1');
      const phone = await screen.findByLabelText('Telefon raqami');
      await user.clear(phone);
      await user.type(phone, '911112233');
      await user.click(screen.getByRole('button', { name: 'Saqlash' }));

      expect(await screen.findByText('KABINET_STUB')).toBeInTheDocument();
      expect(screen.getByText("Ma'lumotlar saqlandi")).toBeInTheDocument();
      expect(api.sessionCall()).toBeUndefined();
      const sent = JSON.parse(String(api.updateCall()![1].body)) as { phone: string };
      expect(sent.phone).toBe('+998911112233');
    });

    it("saqlashda 400 VALIDATION_ERROR maydon xatosi o'z maydoniga bog'lanadi, karta ochilmaydi", async () => {
      const user = userEvent.setup();
      const api = mockApi(
        FULL_PROFILE,
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
        () =>
          problemResponse('VALIDATION_ERROR', 400, undefined, {
            errors: { phone: ["Telefon raqami noto'g'ri formatda (+998XXXXXXXXX)."] },
          }),
      );

      renderPage();
      await user.click(await screen.findByRole('button', { name: "O'zgartirish" }));
      await user.click(await screen.findByRole('button', { name: 'Saqlash' }));

      expect(await screen.findByText(/noto'g'ri formatda/)).toBeInTheDocument();
      expect(screen.getByLabelText('F.I.Sh.')).toBeInTheDocument();
      expect(api.sessionCall()).toBeUndefined();
    });

    it('`?edit=1` bilan ochilsa darhol forma (kabinetdagi "O\'zgartirish" havolasi)', async () => {
      mockApi(FULL_PROFILE, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage('/kabinet/test?edit=1');

      expect(await screen.findByLabelText('F.I.Sh.')).toHaveValue('Karimov Sardor Alisherovich');
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
        "Ma'lumotlarni o'zgartirish",
      );
      expect(screen.getByRole('button', { name: 'Saqlash' })).toBeInTheDocument();
    });

    it("tez boshlashda 409 DUPLICATE_ASSESSMENT — xabar kartada ko'rsatiladi", async () => {
      const user = userEvent.setup();
      mockApi(FULL_PROFILE, () => problemResponse('DUPLICATE_ASSESSMENT', 409));

      renderPage();
      await user.click(await screen.findByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText(/yaqinda yakunlagansiz/)).toBeInTheDocument();
    });
  });

  describe('C holati — profil bor, lekin rozilik eskirgan', () => {
    const OUTDATED = { ...FULL_PROFILE, consentVersion: '0.9', consentCurrent: false };

    it("forma to'ldirilgan, rozilik bloki BOR, \"Bekor qilish\" YO'Q, roziliksiz yuborilmaydi", async () => {
      const user = userEvent.setup();
      const api = mockApi(OUTDATED, () => jsonResponse<'StartSessionResult'>(START_RESULT));

      renderPage();

      expect(await screen.findByLabelText('F.I.Sh.')).toHaveValue('Karimov Sardor Alisherovich');
      expect(screen.getByText(/Roziliknoma matni yangilangan/)).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Bekor qilish' })).not.toBeInTheDocument();

      const consent = screen.getByLabelText(/roziman/);
      expect(consent).not.toBeChecked();
      // Rozilik belgilanmaguncha tugma o'chiq — hech narsa yuborilmaydi.
      expect(screen.getByRole('button', { name: 'Testni boshlash' })).toBeDisabled();

      await user.click(consent);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      // C rejimi — foydalanuvchi test boshlamoqchi: sessiya ochiladi, `PUT` chaqirilmaydi.
      expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
      const body = JSON.parse(String(api.sessionCall()![1].body)) as { consentAccepted: boolean };
      expect(body.consentAccepted).toBe(true);
      expect(api.updateCall()).toBeUndefined();
    });

    it("voyaga yetmagan, ota-ona roziligi yo'q — forma va ota-ona checkbox'i ko'rsatiladi", async () => {
      const currentYear = new Date().getUTCFullYear();
      mockApi(
        {
          ...FULL_PROFILE,
          birthDate: `${String(currentYear - 15)}-01-01`,
          grade: 9,
          isMinor: true,
          parentalConsent: false,
        },
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
      );

      renderPage();

      expect(await screen.findByLabelText(/Ota-onam/)).not.toBeChecked();
      expect(screen.getByLabelText('Sinf')).toHaveValue('9');
      // Rozilik joriy — u qayta so'ralmaydi.
      expect(screen.queryByLabelText(/roziman/)).not.toBeInTheDocument();
    });
  });

  it("profil so'rovi xato bersa qayta urinish ko'rsatiladi, forma ochilmaydi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SERVER_ERROR', 500)));

    renderPage();

    expect(await screen.findByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
    expect(screen.queryByLabelText('F.I.Sh.')).not.toBeInTheDocument();
  });

  // ---------------------------------------------------------------------------------------
  // P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2) — GLOBAL `registrationForm`: `gender` YAGONA
  // sozlanadigan maydon (`birthDate`/`phone` bu oqimda HAMISHA majburiy), o'z maydonlar FAQAT
  // `new` (birinchi anketa) rejimida so'raladi.
  // ---------------------------------------------------------------------------------------
  describe("registrationForm — GLOBAL sozlama (gender, o'z maydonlar)", () => {
    it("gender 'Hidden' qilinsa forma jins bo'limini ko'rsatmaydi va so'rovga qo'shmaydi", async () => {
      const user = userEvent.setup();
      const api = mockApi(
        {
          ...NO_PROFILE,
          registrationForm: {
            ...REGISTRATION_FORM_DEFAULT_DEFINITION,
            coreFields: {
              ...REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields,
              gender: { requirement: 'Hidden', labelUz: 'Jinsi', placeholderUz: null, order: 3 },
            },
          },
        },
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
      );

      renderPage();
      await screen.findByLabelText('F.I.Sh.');
      expect(screen.queryByText('Jinsi')).not.toBeInTheDocument();
      expect(screen.queryByLabelText('Erkak')).not.toBeInTheDocument();

      await user.type(screen.getByLabelText('F.I.Sh.'), 'Karimov Sardor Alisherovich');
      await user.selectOptions(screen.getByLabelText("Tug'ilgan kun"), '12');
      await user.selectOptions(screen.getByLabelText("Tug'ilgan oy"), '4');
      await user.selectOptions(screen.getByLabelText("Tug'ilgan yil"), '1995');
      await user.type(screen.getByLabelText('Telefon raqami'), '901234567');
      await user.click(screen.getByLabelText(/roziman/));
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      await waitFor(() => {
        expect(api.sessionCall()).toBeDefined();
      });
      const body = JSON.parse(String(api.sessionCall()![1].body)) as Record<string, unknown>;
      expect('gender' in body).toBe(false);
    });

    it("majburiy o'z maydon 'new' rejimida ko'rsatiladi va bo'sh bo'lsa xato beradi", async () => {
      const user = userEvent.setup();
      const api = mockApi(
        {
          ...NO_PROFILE,
          registrationForm: {
            ...REGISTRATION_FORM_DEFAULT_DEFINITION,
            customFields: [
              {
                code: 'PARENT_JOB',
                type: 'ShortText',
                labelUz: 'Ota-onangiz kasbi',
                placeholderUz: null,
                requirement: 'Required',
                maxLength: 200,
                inputPattern: null,
                options: null,
                order: 9,
              },
            ],
          },
        },
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
      );

      renderPage();
      expect(await screen.findByLabelText('Ota-onangiz kasbi')).toBeInTheDocument();

      await fillForm(user);
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      expect(await screen.findByText("'Ota-onangiz kasbi' maydoni kiritilishi shart.")).toBeInTheDocument();
      expect(api.sessionCall()).toBeUndefined();

      await user.type(screen.getByLabelText('Ota-onangiz kasbi'), "O'qituvchi");
      await user.click(screen.getByRole('button', { name: 'Testni boshlash' }));

      await waitFor(() => {
        expect(api.sessionCall()).toBeDefined();
      });
      const body = JSON.parse(String(api.sessionCall()![1].body)) as Record<string, unknown>;
      expect(body.customFields).toEqual({ PARENT_JOB: "O'qituvchi" });
    });

    it("o'z maydonlar 'edit' rejimida (profil bor) ko'rsatilmaydi — faqat 'new'da so'raladi", async () => {
      mockApi(
        {
          ...FULL_PROFILE,
          registrationForm: {
            ...REGISTRATION_FORM_DEFAULT_DEFINITION,
            customFields: [
              {
                code: 'PARENT_JOB',
                type: 'ShortText',
                labelUz: 'Ota-onangiz kasbi',
                placeholderUz: null,
                requirement: 'Required',
                maxLength: 200,
                inputPattern: null,
                options: null,
                order: 9,
              },
            ],
          },
        },
        () => jsonResponse<'StartSessionResult'>(START_RESULT),
      );

      renderPage('/kabinet/test?edit=1');
      await screen.findByLabelText('F.I.Sh.');
      expect(screen.queryByLabelText('Ota-onangiz kasbi')).not.toBeInTheDocument();
    });
  });
});
