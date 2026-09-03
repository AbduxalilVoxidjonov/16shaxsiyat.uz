import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { typedResponse } from '@/test/apiMock';
import { publicRequest } from './publicClient';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';

/**
 * Bu testda sinaladigan narsa — `X-Session-Token` header'i, javob DTO'si emas; shu sabab
 * tana ataylab minimal joy egallovchi (sxemadagi biror DTO emas).
 */
interface OkStub {
  ok: boolean;
}

describe('publicRequest', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(typedResponse<OkStub>({ ok: true })));
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
  });

  it("sessiya tokeni bo'lsa X-Session-Token header'ini qo'shadi", async () => {
    localStorage.setItem(
      STORAGE_KEYS.session,
      JSON.stringify({
        state: { sessionToken: 'sess-123', slug: 'demo', assessmentId: null },
        version: 0,
      }),
    );

    await publicRequest('/api/public/sessions/me');

    const [, init] = vi.mocked(fetch).mock.calls[0] ?? [];
    const headers = new Headers(init?.headers);
    expect(headers.get('X-Session-Token')).toBe('sess-123');
  });

  it("sessiya tokeni yo'q bo'lsa header qo'shilmaydi", async () => {
    await publicRequest('/api/public/schools/demo');

    const [, init] = vi.mocked(fetch).mock.calls[0] ?? [];
    const headers = new Headers(init?.headers);
    expect(headers.has('X-Session-Token')).toBe(false);
  });
});
