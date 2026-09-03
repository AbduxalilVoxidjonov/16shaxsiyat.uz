import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { emptyResponse, typedResponse } from '@/test/apiMock';
import { apiRequest } from './client';
import { AppError } from './AppError';
import type { ProblemDetails } from './types';

/**
 * Bu fayl `ProblemDetails` PARSERINI sinaydi, shu sabab sim ustidagi tananing AYNAN o'zi
 * fikstura — `problemResponse()` boilerplate'i sinaladigan tanani almashtirib yuborardi.
 *
 * Tip sxemadan EMAS, `types.ts` dagi qo'lda yozilgan `ProblemDetails` dan olinadi:
 * generatsiya qilingan `components['schemas']['ProblemDetails']` — Swashbuckle ning o'z
 * vakili, unda `code`/`traceId`/`errors` indeks signature (`[key: string]: unknown`) ortida
 * yashiringan, ya'ni hech narsani tekshirmaydi (`types.ts` boshidagi izoh).
 */
function problemBody(body: ProblemDetails, status: number): Response {
  return typedResponse<ProblemDetails>(body, status);
}

describe('apiRequest', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("ProblemDetails javobini AppError'ga o'giradi (code, message, errors)", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      problemBody(
        {
          type: 'https://studentroadmap/errors/validation',
          title: "Noto'g'ri so'rov",
          status: 400,
          detail: "Telefon raqami noto'g'ri",
          code: 'VALIDATION_ERROR',
          traceId: 'trace-1',
          errors: { phone: ["Telefon raqami noto'g'ri formatda"] },
        },
        400,
      ),
    );

    await expect(apiRequest('/api/public/sessions')).rejects.toMatchObject({
      code: 'VALIDATION_ERROR',
      message: "Telefon raqami noto'g'ri",
      status: 400,
      errors: { phone: ["Telefon raqami noto'g'ri formatda"] },
      traceId: 'trace-1',
    });
  });

  it("ProblemDetails kengaytmalarini (`issues[]`) saqlaydi — docs/07 3.4-bo'lim", async () => {
    // `POST /api/admin/catalog/tests/{id}/publish` ning haqiqiy 400 javobi: backend
    // `CatalogPublishValidator` barcha muammoni `issues[]` kengaytmasida bir yo'la beradi.
    vi.mocked(fetch).mockResolvedValueOnce(
      problemBody(
        {
          title: 'Anketa nashr qilishga tayyor emas.',
          status: 400,
          code: 'TEST_NOT_PUBLISHABLE',
          traceId: 'trace-2',
          issues: [
            {
              code: 'SCALE_TOO_FEW_QUESTIONS',
              scale: 'SUPPORT',
              questionCode: null,
              message: 'Kamida 4 savol kerak, hozir 2',
            },
            {
              code: 'SCALE_BAND_GAP',
              scale: 'STRESS',
              questionCode: null,
              message: "Talqin oraliqlarida bo'shliq bor.",
            },
          ],
        },
        400,
      ),
    );

    const error = await apiRequest('/api/admin/catalog/tests/1/publish', {
      method: 'POST',
    }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(AppError);
    const appError = error as AppError;
    expect(appError.code).toBe('TEST_NOT_PUBLISHABLE');
    expect(appError.extension('issues')).toHaveLength(2);
    expect(appError.extensions?.issues).toMatchObject([
      { code: 'SCALE_TOO_FEW_QUESTIONS', scale: 'SUPPORT' },
      { code: 'SCALE_BAND_GAP', scale: 'STRESS' },
    ]);
    // Standart maydonlar kengaytmalar ichida TAKRORLANMAYDI.
    expect(appError.extensions).not.toHaveProperty('code');
    expect(appError.extensions).not.toHaveProperty('traceId');
    expect(appError.traceId).toBe('trace-2');
  });

  it("kengaytmasiz ProblemDetails'da `extensions` undefined qoladi", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      problemBody({ code: 'NOT_FOUND', title: 'Topilmadi', status: 404 }, 404),
    );

    const error = await apiRequest('/api/admin/catalog/tests/1').catch((e: unknown) => e);
    expect((error as AppError).extensions).toBeUndefined();
    expect((error as AppError).extension('issues')).toBeUndefined();
  });

  it('rad etilgan javobni AppError instansiyasi sifatida uloqtiradi', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      problemBody({ code: 'NOT_FOUND', title: 'Topilmadi', status: 404 }, 404),
    );

    await expect(apiRequest('/api/public/schools/xxx')).rejects.toBeInstanceOf(AppError);
  });

  it('204 No Content javobini xatosiz undefined sifatida qaytaradi', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(emptyResponse(204));

    await expect(apiRequest('/api/public/sessions/tests/MBTI16/answers')).resolves.toBeUndefined();
  });

  it("muvaffaqiyatli JSON javobni to'g'ri parslaydi", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(typedResponse<{ ok: boolean }>({ ok: true }, 200));

    await expect(apiRequest('/api/public/schools/demo')).resolves.toEqual({ ok: true });
  });

  it("202 (hali tayyor emas) ni ham AppError sifatida uloqtiradi — docs/07 1.9-bo'lim", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      problemBody({ title: 'Tahlil tayyor emas', status: 202 }, 202),
    );

    await expect(apiRequest('/api/public/sessions/result')).rejects.toMatchObject({
      code: 'NOT_READY',
      status: 202,
    });
  });

  it('tarmoq xatosini (fetch reject) AppError sifatida ushlaydi', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new TypeError('Failed to fetch'));

    const error = await apiRequest('/api/public/schools/demo').catch((e: unknown) => e);
    expect(error).toBeInstanceOf(AppError);
    expect((error as AppError).code).toBe('NETWORK_ERROR');
    expect((error as AppError).status).toBe(0);
  });
});
