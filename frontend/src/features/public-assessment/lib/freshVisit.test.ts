import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { QueryClient } from '@tanstack/react-query';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import { useSessionStore } from '../store/sessionStore';
import { readAnswerStore, upsertAnswer, writeAnswerStore } from './answerQueue';
import { beginFreshVisit, removeSessionQueries } from './freshVisit';

describe('freshVisit', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it("beginFreshVisit oldingi o'quvchining sessiyasini, javob navbatini va sessiya keshini birga tozalaydi", () => {
    useSessionStore.getState().setSession('tok-old', 'other-school', 'a-old', 'k-old');
    writeAnswerStore(
      upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 100 }),
    );
    const queryClient = new QueryClient();
    queryClient.setQueryData(QUERY_KEYS.publicSessionMe(), { assessmentId: 'a-old' });
    queryClient.setQueryData(QUERY_KEYS.publicTestQuestions('BIG5', 1), { questions: [] });
    queryClient.setQueryData(QUERY_KEYS.publicTestQuestions('MBTI16', 2), { questions: [] });
    queryClient.setQueryData(QUERY_KEYS.publicStudentResult(), { ok: true });
    queryClient.setQueryData(QUERY_KEYS.publicSchoolInfo('demo-school', 'k-new'), { name: 'S' });

    beginFreshVisit(queryClient, 'demo-school', 'k-new');

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().accessToken).toBe('k-new');
    expect(readAnswerStore()).toEqual({});
    expect(localStorage.getItem(STORAGE_KEYS.pendingAnswers)).toBe('{}');
    expect(queryClient.getQueryData(QUERY_KEYS.publicSessionMe())).toBeUndefined();
    expect(queryClient.getQueryData(QUERY_KEYS.publicTestQuestions('BIG5', 1))).toBeUndefined();
    expect(queryClient.getQueryData(QUERY_KEYS.publicTestQuestions('MBTI16', 2))).toBeUndefined();
    expect(queryClient.getQueryData(QUERY_KEYS.publicStudentResult())).toBeUndefined();
    // Maktab ma'lumoti sessiyaga bog'liq emas — qoladi.
    expect(queryClient.getQueryData(QUERY_KEYS.publicSchoolInfo('demo-school', 'k-new'))).toEqual({
      name: 'S',
    });
  });

  it('removeSessionQueries queryClient.removeQueries ni sessiya kalitlari bilan chaqiradi', () => {
    const queryClient = new QueryClient();
    const removeSpy = vi.spyOn(queryClient, 'removeQueries');

    removeSessionQueries(queryClient);

    expect(removeSpy).toHaveBeenCalledWith({ queryKey: QUERY_KEYS.publicSessionMe() });
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['public', 'test-questions'] });
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: QUERY_KEYS.publicStudentResult() });
  });
});
