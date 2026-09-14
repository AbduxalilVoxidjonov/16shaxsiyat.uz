import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import StudentProfilePage from './StudentProfilePage';

/**
 * `GET /api/admin/students/{id}` javobining HARFMA-HARF shakli — docs/07, 3.2-bo'lim.
 *
 * Bu fayl ataylab XOM JSON MATNI bilan ishlaydi (TS obyekt literali emas): kalit nomidagi
 * farq — `results.mbti16` vs `results.MBTI16`, `types.ART` vs `types.A` — TS tipida
 * ko'rinmaydi, chunki javob `adminRequest` ichida `unknown`dan cast qilinadi. Shu sabab
 * shaxsiyat va kasb qiziqishlari bo'limlari JIMGINA bo'sh (yoki buzuq) chiqardi va hech
 * qanday xato ko'rsatilmasdi (2026-09-03 tekshiruvi). Bunday xatoni faqat haqiqiy javob
 * shakli bilan render qiladigan test ushlaydi.
 */
const BACKEND_RESPONSE_JSON = `{
  "student": {
    "id": "student-1",
    "fullName": "Aliyev Sardor Bekzodovich",
    "birthDate": "2010-04-17",
    "age": 16,
    "gender": "Male",
    "grade": 9,
    "classLetter": "B",
    "phone": "+998901234567",
    "parentPhone": null,
    "email": null,
    "school": { "id": "school-1", "name": "12-son maktab, Qo'qon" },
    "consentGivenAt": "2026-08-01T10:00:00Z",
    "createdAt": "2026-08-01T10:00:00Z"
  },
  "assessments": [
    {
      "id": "assessment-1",
      "status": "Analyzed",
      "startedAt": "2026-08-30T09:00:00Z",
      "completedAt": "2026-08-30T09:29:00Z",
      "durationMinutes": 29,
      "reliabilityScore": 82.5,
      "reliabilityFlag": "Reliable",
      "isLatest": true
    }
  ],
  "latestAssessment": {
    "id": "assessment-1",
    "results": {
      "MBTI16": {
        "resultCode": "INTJ",
        "typeName": "Loyihachi",
        "axes": {
          "EI": { "pct": 28.3, "letter": "I", "borderline": false },
          "SN": { "pct": 71.6, "letter": "N", "borderline": false },
          "TF": { "pct": 33.3, "letter": "T", "borderline": false },
          "JP": { "pct": 64.1, "letter": "J", "borderline": false }
        },
        "borderlineAxes": []
      },
      "BIG5": {
        "factors": {
          "O": { "raw": 38, "pct": 70, "level": "Yuqori" },
          "C": { "raw": 41, "pct": 77.5, "level": "Yuqori" },
          "E": { "raw": 24, "pct": 35, "level": "Past" },
          "A": { "raw": 35, "pct": 62.5, "level": "Yuqori" },
          "N": { "raw": 22, "pct": 30, "level": "Past" }
        },
        "stabilityPct": 70,
        "maturityIndex": 68.4,
        "maturityLevel": "Yaxshi"
      },
      "RIASEC": {
        "resultCode": "IRA",
        "types": { "R": 62, "I": 88, "A": 71, "S": 40, "E": 35, "C": 48 },
        "differentiation": 53,
        "consistency": "High",
        "careerFields": [{ "name": "Muhandislik", "professions": ["Dasturchi"] }]
      },
      "ACTIVITY": {
        "scales": { "MOT": 74, "SELF": 68, "SOCA": 52, "ENG": 60 },
        "activityIndex": 65.2,
        "activityLevel": "Moderate",
        "needsAttention": false
      }
    },
    "aiAnalysis": null,
    "aiHistory": [],
    "tests": [
      { "code": "MBTI16", "nameUz": "Shaxsiyat tipi", "status": "Completed", "scoringMode": "Scored", "batteryRole": "PersonalityType" },
      { "code": "BIG5", "nameUz": "Katta beshlik", "status": "Completed", "scoringMode": "Scored", "batteryRole": "Traits" },
      { "code": "RIASEC", "nameUz": "Kasb qiziqishlari", "status": "Completed", "scoringMode": "Scored", "batteryRole": "CareerInterest" },
      { "code": "ACTIVITY", "nameUz": "Aktivlik", "status": "Completed", "scoringMode": "Scored", "batteryRole": "Activity" }
    ],
    "hasPersonalityBattery": true
  }
}`;

/** Eski (nomuvofiq) backend shakli — sukut camelCase siyosati bergan kalitlar. */
const CAMEL_CASE_RESPONSE_JSON = BACKEND_RESPONSE_JSON
  .replace('"MBTI16"', '"mbti16"')
  .replace('"BIG5"', '"big5"')
  .replace('"RIASEC"', '"riasec"')
  .replace('"ACTIVITY"', '"activity"');

function renderPage(rawJson: string) {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/api/admin/students/student-1')) {
        return Promise.resolve(
          new Response(rawJson, { status: 200, headers: { 'content-type': 'application/json' } }),
        );
      }
      return Promise.reject(new Error(`unexpected fetch: ${url}`));
    }),
  );

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/students/student-1']}>
          <Routes>
            <Route path="/admin/students/:id" element={<StudentProfilePage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe("StudentProfilePage — javob kalitlari shartnomasi (docs/07 3.2)", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("`results` kalitlari `TestDefinition.Code` bilan bir xil: MBTI16 / BIG5 / RIASEC / ACTIVITY", () => {
    const parsed = JSON.parse(BACKEND_RESPONSE_JSON) as {
      latestAssessment: { results: Record<string, unknown> };
    };

    expect(Object.keys(parsed.latestAssessment.results)).toEqual([
      'MBTI16',
      'BIG5',
      'RIASEC',
      'ACTIVITY',
    ]);
  });

  it('`RIASEC.types` kalitlari — Holland harflari R I A S E C (scale kodlari EMAS)', () => {
    const parsed = JSON.parse(BACKEND_RESPONSE_JSON) as {
      latestAssessment: { results: { RIASEC: { types: Record<string, number> } } };
    };

    expect(Object.keys(parsed.latestAssessment.results.RIASEC.types)).toEqual([
      'R',
      'I',
      'A',
      'S',
      'E',
      'C',
    ]);
  });

  it("haqiqiy backend javob shakli bilan barcha bo'limlar qiymat bilan render bo'ladi", async () => {
    renderPage(BACKEND_RESPONSE_JSON);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();

    // Shaxsiyat (MBTI16) va yetuklik (BIG5) — kartalarda haqiqiy qiymat, "—" emas.
    expect(screen.getByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('Loyihachi')).toBeInTheDocument();
    expect(screen.getByText('68.4')).toBeInTheDocument();

    // Kasb qiziqishlari (RIASEC) — Holland kodi va HAR OLTI tipning foizi.
    expect(screen.getByText('IRA')).toBeInTheDocument();
    const riasecSummary = within(screen.getByTestId('riasec-chart-summary'));
    expect(riasecSummary.getByText('62.0%')).toBeInTheDocument(); // R
    expect(riasecSummary.getByText('88.0%')).toBeInTheDocument(); // I
    expect(riasecSummary.getByText('71.0%')).toBeInTheDocument(); // A — ilgari `ART` kutilardi
    expect(riasecSummary.getByText('40.0%')).toBeInTheDocument(); // S — ilgari `SOC`
    expect(riasecSummary.getByText('35.0%')).toBeInTheDocument(); // E — ilgari `ENT`
    expect(riasecSummary.getByText('48.0%')).toBeInTheDocument(); // C — ilgari `CONV`

    // Aktivlik.
    expect(screen.getAllByText('65.2').length).toBeGreaterThan(0);
  });

  it("camelCase kalitli javob JIMGINA to'g'ri ko'rinmaydi — bo'sh holat chiqadi", async () => {
    renderPage(CAMEL_CASE_RESPONSE_JSON);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.queryByText('INTJ')).not.toBeInTheDocument();
    expect(screen.queryByTestId('riasec-chart-summary')).not.toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
  });
});
