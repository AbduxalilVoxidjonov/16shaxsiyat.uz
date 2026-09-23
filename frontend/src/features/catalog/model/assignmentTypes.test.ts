import { describe, expect, it } from 'vitest';
import {
  buildAssignmentPayload,
  isAssignmentFormDirty,
  normalizeTestAssignment,
  resolveAssignmentStatus,
  toAssignmentFormState,
  type TestAssignment,
} from './assignmentTypes';

function assignment(overrides: Partial<TestAssignment> = {}): TestAssignment {
  return {
    testDefinitionId: 'test-1',
    testStatus: 'Published',
    testIsActive: true,
    isConfigured: true,
    isPublic: false,
    schoolIds: ['s-1'],
    isInPublicSpace: false,
    registrationMode: 'Full',
    state: 'Active',
    isAvailable: true,
    hasPersonalityBattery: false,
    sessionCount: 0,
    ...overrides,
  };
}

describe('assignmentTypes', () => {
  it("normalize: noma'lum state → null, noma'lum rejim → Full", () => {
    const normalized = normalizeTestAssignment({
      ...assignment(),
      state: 'Weird',
      registrationMode: 'Partial',
    });
    expect(normalized.state).toBeNull();
    expect(normalized.registrationMode).toBe('Full');
  });

  it('payload: "Barcha maktablarga" — schoolIds bo\'sh, isInPublicSpace saqlanadi', () => {
    const form = {
      ...toAssignmentFormState(assignment({ isInPublicSpace: true })),
      audience: 'all' as const,
    };
    expect(buildAssignmentPayload(form)).toEqual({
      isPublic: true,
      schoolIds: [],
      registrationMode: 'Full',
      isInPublicSpace: true,
    });
  });

  it("dirty: tartib o'zgarishi o'zgarish emas, to'plam o'zgarishi — o'zgarish", () => {
    const saved = toAssignmentFormState(assignment({ schoolIds: ['a', 'b'] }));
    expect(isAssignmentFormDirty({ ...saved, schoolIds: ['b', 'a'] }, saved)).toBe(false);
    expect(isAssignmentFormDirty({ ...saved, schoolIds: ['a'] }, saved)).toBe(true);
    expect(isAssignmentFormDirty({ ...saved, audience: 'all' }, saved)).toBe(true);
  });

  it('status — backend maydonlaridan', () => {
    expect(resolveAssignmentStatus(assignment())).toBe('active');
    expect(resolveAssignmentStatus(assignment({ isAvailable: false, state: 'Draft' }))).toBe(
      'draft',
    );
    expect(resolveAssignmentStatus(assignment({ isAvailable: false, state: 'Paused' }))).toBe(
      'paused',
    );
    expect(
      resolveAssignmentStatus(
        assignment({ isConfigured: false, schoolIds: [], state: null, isAvailable: false }),
      ),
    ).toBe('unassigned');
    expect(
      resolveAssignmentStatus(assignment({ testStatus: 'Archived', isAvailable: false })),
    ).toBe('archived');
  });
});
