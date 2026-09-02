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

  it("tanlangan dastur persist (localStorage) orqali sahifa yangilanishidan keyin ham saqlanadi", () => {
    useSessionStore.getState().setSelectedProgram('demo-school', 'CAREER_SURVEY');

    const raw = localStorage.getItem(STORAGE_KEYS.session);
    expect(raw).toBeTruthy();
    const persisted = JSON.parse(raw as string) as { state: { selectedProgramCode: string } };
    expect(persisted.state.selectedProgramCode).toBe('CAREER_SURVEY');
  });

  it("clear() sessiya maydonlarini tozalaydi, lekin dastur tanlovini saqlab qoladi", () => {
    useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
    useSessionStore.getState().setSelectedProgram('demo-school', 'CAREER_SURVEY');

    useSessionStore.getState().clear();

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().slug).toBeNull();
    expect(useSessionStore.getState().assessmentId).toBeNull();
    expect(useSessionStore.getState().selectedProgramCode).toBe('CAREER_SURVEY');
  });
});
