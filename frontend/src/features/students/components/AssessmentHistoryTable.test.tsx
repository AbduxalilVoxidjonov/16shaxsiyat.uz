import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { AssessmentHistoryTable } from './AssessmentHistoryTable';
import type { AssessmentSummaryDto } from '../model/profileTypes';

/**
 * Egasining talabi (2026-09-12): "testning ichiga kirib qaysi savolga qaysi javob
 * berganini ko'rish" — buning yo'li shu qatorlardan boshlanadi. Ilgari qatorlar oddiy
 * `<TableRow>` edi, na havola, na `onClick` bor edi.
 */
function assessment(overrides: Partial<AssessmentSummaryDto> = {}): AssessmentSummaryDto {
  return {
    id: 'assessment-1',
    status: 'Analyzed',
    startedAt: '2026-08-30T09:00:00Z',
    completedAt: '2026-08-30T10:00:00Z',
    durationMinutes: 60,
    reliabilityScore: 80,
    reliabilityFlag: 'Reliable',
    isLatest: true,
    ...overrides,
  };
}

function renderTable(assessments: AssessmentSummaryDto[]) {
  return render(
    <MemoryRouter initialEntries={['/admin/students/student-1']}>
      <Routes>
        <Route
          path="/admin/students/:id"
          element={<AssessmentHistoryTable assessments={assessments} />}
        />
        <Route path="/admin/assessments/:id" element={<div>DETAIL_STUB</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('AssessmentHistoryTable', () => {
  it('qatorni bosganda sessiya detali sahifasiga o\'tadi', async () => {
    renderTable([assessment({ id: 'assessment-42' })]);
    const user = userEvent.setup();

    const row = screen.getByText('60 daq').closest('tr');
    expect(row).not.toBeNull();
    await user.click(row!);

    expect(await screen.findByText('DETAIL_STUB')).toBeInTheDocument();
  });

  it('klaviatura bilan (Enter) ham ochiladi', async () => {
    renderTable([assessment({ id: 'assessment-42' })]);
    const user = userEvent.setup();

    const row = screen.getByText('60 daq').closest('tr');
    expect(row).not.toBeNull();
    row!.focus();
    await user.keyboard('{Enter}');

    expect(await screen.findByText('DETAIL_STUB')).toBeInTheDocument();
  });

  it('qator klaviatura bilan fokuslanadigan va role="button" bilan belgilangan', () => {
    renderTable([assessment()]);
    const row = screen.getByText('60 daq').closest('tr');
    expect(row).toHaveAttribute('role', 'button');
    expect(row).toHaveAttribute('tabindex', '0');
  });

  it('bo\'sh ro\'yxatda bo\'sh holatni ko\'rsatadi', () => {
    renderTable([]);
    expect(screen.getByText('Hali sessiya yo\'q')).toBeInTheDocument();
  });
});
