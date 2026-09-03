import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { jsonResponse, problemResponse, typedResponse } from '@/test/apiMock';
import { adminRequest, setAdminAccessToken, setOnAdminSessionExpired } from './adminClient';

/**
 * `GET /api/admin/students` ning HAQIQIY javobi — `AdminStudentListItemDtoPagedResult`
 * (`pagedResponse<'AdminStudentListItemDto'>`). Bu yerda ataylab MINIMAL joy egallovchi tana
 * ishlatiladi: sinaladigan narsa — `401` → bitta refresh (mutex) → qayta yuborish zanjiri,
 * javob DTO'si emas; to'liq sahifalangan tana testni faqat shovqinliroq qilardi.
 */
interface StudentsListStub {
  items: never[];
}

describe('adminRequest', () => {
  beforeEach(() => {
    setAdminAccessToken('old-token');
    setOnAdminSessionExpired(null);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setAdminAccessToken(null);
    setOnAdminSessionExpired(null);
  });

  it("401 kelganda bitta refresh qiladi (mutex) — parallel 3 so'rovda ham refresh 1 marta chaqiriladi", async () => {
    let refreshCalls = 0;

    const mockFetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const headers = new Headers(init?.headers);

      if (url.endsWith('/api/auth/refresh')) {
        refreshCalls += 1;
        return jsonResponse<'RefreshResult'>({ accessToken: 'new-token', expiresIn: 900 });
      }

      if (url.endsWith('/api/admin/students')) {
        if (headers.get('Authorization') === 'Bearer old-token') {
          return problemResponse('UNAUTHORIZED', 401);
        }
        expect(headers.get('Authorization')).toBe('Bearer new-token');
        return typedResponse<StudentsListStub>({ items: [] });
      }

      throw new Error(`Kutilmagan so'rov: ${url}`);
    });
    vi.stubGlobal('fetch', mockFetch);

    const results = await Promise.all([
      adminRequest('/api/admin/students'),
      adminRequest('/api/admin/students'),
      adminRequest('/api/admin/students'),
    ]);

    expect(refreshCalls).toBe(1);
    results.forEach((result) => {
      expect(result).toEqual({ items: [] });
    });
  });

  it('refresh ham 401 bersa onSessionExpired chaqiriladi va xato uloqtiriladi', async () => {
    const onExpired = vi.fn();
    setOnAdminSessionExpired(onExpired);

    const mockFetch = vi.fn(async () => problemResponse('UNAUTHORIZED', 401));
    vi.stubGlobal('fetch', mockFetch);

    await expect(adminRequest('/api/admin/students')).rejects.toMatchObject({ status: 401 });
    expect(onExpired).toHaveBeenCalledTimes(1);
  });
});
