import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import { describe, expect, it } from 'vitest';
import { LegacyAssessmentsRedirect } from './LegacyAssessmentsRedirect';

function LocationProbe() {
  const location = useLocation();
  return <p data-testid="location">{`${location.pathname}${location.search}`}</p>;
}

function renderAt(entry: string) {
  render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route path="/admin/assessments" element={<LegacyAssessmentsRedirect />} />
        <Route path="/admin/students" element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>,
  );
}

/** Egasining qarori (2026-09-23): "Sessiyalar" ro'yxati olib tashlandi, eski URL buzilmaydi. */
describe('LegacyAssessmentsRedirect', () => {
  it("eski sessiyalar URL'ini O'quvchilar ro'yxatiga yo'naltiradi", () => {
    renderAt('/admin/assessments');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin\/students$/);
  });

  it('umumiy filtrlarni saqlaydi, sahifalash/saralashni tashlaydi', () => {
    renderAt(
      '/admin/assessments?status=Analyzing&schoolId=s-1&from=2026-09-01&sort=-startedAt&page=3',
    );
    expect(screen.getByTestId('location')).toHaveTextContent(
      '/admin/students?status=Analyzing&schoolId=s-1&from=2026-09-01',
    );
  });
});
