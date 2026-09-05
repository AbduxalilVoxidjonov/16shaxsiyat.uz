import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { jsonResponse, problemResponse } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { AppError } from './AppError';
import {
  getPublicAccessToken,
  hasPublicSessionHint,
  publicUserRequest,
  refreshPublicAccessToken,
  setOnPublicSessionExpired,
  setPublicAccessToken,
  setPublicSessionHint,
} from './publicUserClient';

interface MeBody {
  id: string;
}

/** `fetch` mock'ining N-chaqiruvi — `noUncheckedIndexedAccess` uchun xavfsiz o'qish. */
function fetchCall(mock: Mock, index: number): { url: string; init: RequestInit } {
  const entry = mock.mock.calls[index] as [RequestInfo | URL, RequestInit | undefined] | undefined;
  if (!entry) throw new Error(`fetch ${String(index)}-marta chaqirilmagan`);
  return { url: String(entry[0]), init: entry[1] ?? {} };
}

function meResponse(): Response {
  return new Response(JSON.stringify({ id: 'user-1' } satisfies MeBody), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });
}

describe('publicUserClient', () => {
  beforeEach(() => {
    setPublicAccessToken(null);
    setOnPublicSessionExpired(null);
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setPublicAccessToken(null);
    setOnPublicSessionExpired(null);
    localStorage.clear();
  });

  it("mavjud tokenni `Authorization: Bearer` sifatida qo'shadi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(meResponse());
    vi.stubGlobal('fetch', fetchMock);
    setPublicAccessToken('token-1');

    await publicUserRequest<MeBody>('/api/me');

    const { init } = fetchCall(fetchMock, 0);
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer token-1');
    // Refresh cookie'si `Path=/api/auth/telegram` bilan cheklangan, lekin so'rov baribir
    // `include` bilan ketadi — mijoz bu qarorni bitta joyda qabul qiladi.
    expect(init.credentials).toBe('include');
  });

  it("401 kelganda bir marta refresh qilib so'rovni qayta yuboradi", async () => {
    const fetchMock = vi
      .fn()
      // 1) `/api/me` → 401
      .mockResolvedValueOnce(problemResponse('UNAUTHORIZED', 401))
      // 2) `/api/auth/telegram/refresh` → yangi token
      .mockResolvedValueOnce(
        jsonResponse<'PublicRefreshResult'>({ accessToken: 'token-2', expiresIn: 1800 }),
      )
      // 3) `/api/me` qayta → 200
      .mockResolvedValueOnce(meResponse());
    vi.stubGlobal('fetch', fetchMock);
    setPublicAccessToken('token-1');

    const result = await publicUserRequest<MeBody>('/api/me');

    expect(result.id).toBe('user-1');
    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchCall(fetchMock, 1).url).toContain('/api/auth/telegram/refresh');
    expect(getPublicAccessToken()).toBe('token-2');
    // Qayta yuborilgan so'rov YANGI tokenni olib ketadi.
    const retryInit = fetchCall(fetchMock, 2).init;
    expect((retryInit.headers as Record<string, string>).Authorization).toBe('Bearer token-2');
  });

  it('refresh ham 401 bersa `onSessionExpired` chaqiriladi va token tozalanadi', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(problemResponse('UNAUTHORIZED', 401))
      .mockResolvedValueOnce(problemResponse('UNAUTHORIZED', 401));
    vi.stubGlobal('fetch', fetchMock);
    setPublicAccessToken('token-1');
    const expired = vi.fn();
    setOnPublicSessionExpired(expired);

    await expect(publicUserRequest<MeBody>('/api/me')).rejects.toBeInstanceOf(AppError);

    expect(expired).toHaveBeenCalledTimes(1);
    expect(getPublicAccessToken()).toBeNull();
    // Ikkinchi marta refresh urinilmaydi (cheksiz sikl bo'lmasin).
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("parallel 401'lar BITTA refresh chaqiruvini ulashadi (mutex)", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/refresh')) {
        return Promise.resolve(
          jsonResponse<'PublicRefreshResult'>({ accessToken: 'token-2', expiresIn: 1800 }),
        );
      }
      // Birinchi ikki `/api/me` chaqiruvi 401, keyingilari 200.
      const meCalls = fetchMock.mock.calls.filter(
        (call: unknown[]) => !String(call[0]).includes('/refresh'),
      ).length;
      return Promise.resolve(meCalls <= 2 ? problemResponse('UNAUTHORIZED', 401) : meResponse());
    });
    vi.stubGlobal('fetch', fetchMock);
    setPublicAccessToken('token-1');

    await Promise.all([publicUserRequest<MeBody>('/api/me'), publicUserRequest<MeBody>('/api/me')]);

    const refreshCalls = fetchMock.mock.calls.filter((call: unknown[]) =>
      String(call[0]).includes('/refresh'),
    );
    expect(refreshCalls).toHaveLength(1);
  });

  it("refresh muvaffaqiyatsiz bo'lsa `false` qaytaradi va tokenni tozalaydi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('UNAUTHORIZED', 401)));
    setPublicAccessToken('token-1');

    await expect(refreshPublicAccessToken()).resolves.toBe(false);
    expect(getPublicAccessToken()).toBeNull();
  });

  it('sessiya belgisi `localStorage` da TOKEN emas, faqat bayroq saqlaydi', () => {
    setPublicSessionHint(true);

    expect(hasPublicSessionHint()).toBe(true);
    expect(localStorage.getItem(STORAGE_KEYS.publicSessionHint)).toBe('1');

    setPublicSessionHint(false);
    expect(hasPublicSessionHint()).toBe(false);
    expect(localStorage.getItem(STORAGE_KEYS.publicSessionHint)).toBeNull();
  });
});
