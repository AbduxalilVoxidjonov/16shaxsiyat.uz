import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { useSessionStore } from './sessionStore';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';

describe('sessionStore', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it("setSelectedProgram dastur kodi va maktab slug'ini birga saqlaydi", () => {
    useSessionStore.getState().setSelectedProgram('demo-school', 'CAREER_SURVEY');

    expect(useSessionStore.getState().selectedProgramCode).toBe('CAREER_SURVEY');
    expect(useSessionStore.getState().selectedProgramSlug).toBe('demo-school');
  });

  it('tanlangan dastur persist (localStorage) orqali sahifa yangilanishidan keyin ham saqlanadi', () => {
    useSessionStore.getState().setSelectedProgram('demo-school', 'CAREER_SURVEY');

    const raw = localStorage.getItem(STORAGE_KEYS.session);
    expect(raw).toBeTruthy();
    const persisted = JSON.parse(raw as string) as { state: { selectedProgramCode: string } };
    expect(persisted.state.selectedProgramCode).toBe('CAREER_SURVEY');
  });

  it('clear() sessiya maydonlarini tozalaydi, lekin dastur tanlovini saqlab qoladi', () => {
    useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
    useSessionStore.getState().setSelectedProgram('demo-school', 'CAREER_SURVEY');

    useSessionStore.getState().clear();

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().slug).toBeNull();
    expect(useSessionStore.getState().assessmentId).toBeNull();
    expect(useSessionStore.getState().selectedProgramCode).toBe('CAREER_SURVEY');
  });
});

/**
 * Havola bilan yangi kirish (`/t/:slug?k=`) — bir qurilma, bir necha o'quvchi. `startFresh`
 * faol sessiyani HAR DOIM tozalaydi; xuddi shu maktab + xuddi shu `k` bo'lsa uni ixtiyoriy
 * "Davom ettirish" taklifiga (`resumable`, persist QILINMAYDI) ko'chiradi.
 */
describe('sessionStore — startFresh / resumable', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it("setSession accessToken'ni ham saqlaydi; berilmasa null (Telegram/kabinet sessiyasi)", () => {
    useSessionStore.getState().setSession('tok-1', 'demo-school', 'a-1', 'k-1');
    expect(useSessionStore.getState().accessToken).toBe('k-1');

    useSessionStore.getState().setSession('tok-2', 'ommaviy', 'a-2');
    expect(useSessionStore.getState().accessToken).toBeNull();
  });

  it('boshqa maktab (slug) sessiyasi turganda startFresh hammasini tozalaydi, taklif bermaydi, yangi k saqlanadi', () => {
    useSessionStore.getState().setSession('tok-old', 'other-school', 'a-old', 'k-old');

    useSessionStore.getState().startFresh('demo-school', 'k-new');

    const state = useSessionStore.getState();
    expect(state.sessionToken).toBeNull();
    expect(state.slug).toBeNull();
    expect(state.assessmentId).toBeNull();
    expect(state.accessToken).toBe('k-new');
    expect(state.resumable).toBeNull();
  });

  it("Telegram ('ommaviy') sessiyasi turganda ham startFresh tozalaydi", () => {
    useSessionStore.getState().setSession('tok-tg', 'ommaviy', 'a-tg');

    useSessionStore.getState().startFresh('demo-school', 'k-new');

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().resumable).toBeNull();
  });

  it("xuddi shu maktab, lekin boshqa k (havola yangilangan) — tozalanadi, taklif yo'q", () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-old');

    useSessionStore.getState().startFresh('demo-school', 'k-new');

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().resumable).toBeNull();
  });

  it("xuddi shu maktab va xuddi shu k — faol sessiya tozalanadi, lekin resumable taklifiga ko'chadi", () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');

    useSessionStore.getState().startFresh('demo-school', 'k-1');

    const state = useSessionStore.getState();
    expect(state.sessionToken).toBeNull();
    expect(state.assessmentId).toBeNull();
    expect(state.resumable).toEqual({
      sessionToken: 'tok-old',
      slug: 'demo-school',
      assessmentId: 'a-old',
      accessToken: 'k-1',
    });
  });

  it('startFresh idempotent: ikkinchi chaqiruv (StrictMode) mos taklifni saqlab qoladi', () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');

    useSessionStore.getState().startFresh('demo-school', 'k-1');
    useSessionStore.getState().startFresh('demo-school', 'k-1');

    expect(useSessionStore.getState().resumable?.sessionToken).toBe('tok-old');
  });

  it("faol sessiya yo'q, lekin boshqa havola taklifi turgan bo'lsa startFresh uni tashlaydi", () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');
    useSessionStore.getState().startFresh('demo-school', 'k-1');
    expect(useSessionStore.getState().resumable).not.toBeNull();

    useSessionStore.getState().startFresh('other-school', 'k-2');

    expect(useSessionStore.getState().resumable).toBeNull();
  });

  it('restoreResumable taklifni yana faol sessiyaga qaytaradi', () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');
    useSessionStore.getState().startFresh('demo-school', 'k-1');

    useSessionStore.getState().restoreResumable();

    const state = useSessionStore.getState();
    expect(state.sessionToken).toBe('tok-old');
    expect(state.slug).toBe('demo-school');
    expect(state.assessmentId).toBe('a-old');
    expect(state.accessToken).toBe('k-1');
    expect(state.resumable).toBeNull();
  });

  it("resumable localStorage'ga YOZILMAYDI (persist partialize) — keyingi tashrifga o'tmaydi", () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');
    useSessionStore.getState().startFresh('demo-school', 'k-1');

    const persisted = JSON.parse(localStorage.getItem(STORAGE_KEYS.session) as string) as {
      state: Record<string, unknown>;
    };
    expect(persisted.state).not.toHaveProperty('resumable');
    expect(persisted.state['sessionToken']).toBeNull();
    expect(persisted.state['accessToken']).toBe('k-1');
  });

  it('clear() taklifni (resumable) ham olib tashlaydi', () => {
    useSessionStore.getState().setSession('tok-old', 'demo-school', 'a-old', 'k-1');
    useSessionStore.getState().startFresh('demo-school', 'k-1');

    useSessionStore.getState().clear();

    expect(useSessionStore.getState().resumable).toBeNull();
    expect(useSessionStore.getState().accessToken).toBeNull();
  });

  it('setSession BOSHQA token bilan javob navbatini tozalaydi, XUDDI SHU token (resumed) bilan saqlaydi', () => {
    useSessionStore.getState().setSession('tok-1', 'demo-school', 'a-1', 'k-1');
    localStorage.setItem(
      STORAGE_KEYS.pendingAnswers,
      JSON.stringify({
        q1: { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 1, pending: true },
      }),
    );

    // Server `resumed: true` — o'sha token, navbat (shu o'quvchining javoblari) qoladi.
    useSessionStore.getState().setSession('tok-1', 'demo-school', 'a-1', 'k-1');
    expect(JSON.parse(localStorage.getItem(STORAGE_KEYS.pendingAnswers) as string)).toHaveProperty(
      'q1',
    );

    // Yangi token — yangi o'quvchi, eski javoblar unga tegishli emas.
    useSessionStore.getState().setSession('tok-2', 'demo-school', 'a-2', 'k-1');
    expect(JSON.parse(localStorage.getItem(STORAGE_KEYS.pendingAnswers) as string)).toEqual({});
  });
});
