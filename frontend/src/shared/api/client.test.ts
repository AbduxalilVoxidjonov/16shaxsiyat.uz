import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { apiRequest } from './client';
import { AppError } from './AppError';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
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
      jsonResponse(
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

  it('rad etilgan javobni AppError instansiyasi sifatida uloqtiradi', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({ code: 'NOT_FOUND', title: 'Topilmadi', status: 404 }, 404),
    );

    await expect(apiRequest('/api/public/schools/xxx')).rejects.toBeInstanceOf(AppError);
  });

  it('204 No Content javobini xatosiz undefined sifatida qaytaradi', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(apiRequest('/api/public/sessions/tests/MBTI16/answers')).resolves.toBeUndefined();
  });

  it("muvaffaqiyatli JSON javobni to'g'ri parslaydi", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(jsonResponse({ ok: true }, 200));

    await expect(apiRequest('/api/public/schools/demo')).resolves.toEqual({ ok: true });
  });

  it('tarmoq xatosini (fetch reject) AppError sifatida ushlaydi', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new TypeError('Failed to fetch'));

    const error = await apiRequest('/api/public/schools/demo').catch((e: unknown) => e);
    expect(error).toBeInstanceOf(AppError);
    expect((error as AppError).code).toBe('NETWORK_ERROR');
    expect((error as AppError).status).toBe(0);
  });
});
