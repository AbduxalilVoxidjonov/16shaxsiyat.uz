import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation, useNavigationType } from 'react-router';
import { describe, expect, it } from 'vitest';
import { ROUTE_PATTERNS } from '@/shared/config/routes';
import { LegacyProgramsRedirect } from './LegacyProgramsRedirect';

function LocationProbe() {
  const location = useLocation();
  const navigationType = useNavigationType();
  return (
    <>
      <p data-testid="location">{`${location.pathname}${location.search}`}</p>
      <p data-testid="navigation-type">{navigationType}</p>
    </>
  );
}

function renderAt(entry: string) {
  render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        {/* `app/router.tsx` dagi AYNAN shu ikki naqsh. */}
        <Route path={ROUTE_PATTERNS.admin.legacyPrograms} element={<LegacyProgramsRedirect />} />
        <Route
          path={ROUTE_PATTERNS.admin.legacyProgramDetail}
          element={<LegacyProgramsRedirect />}
        />
        <Route path={ROUTE_PATTERNS.admin.catalog} element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>,
  );
}

/** Egasining qarori (2026-09-23): "Dasturlar" bo'limi olib tashlandi, eski URL buzilmaydi. */
describe('LegacyProgramsRedirect', () => {
  it("eski dasturlar ro'yxati URL'ini Testlar katalogiga yo'naltiradi (replace)", () => {
    renderAt('/admin/programs?state=Active');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin\/catalog$/);
    expect(screen.getByTestId('navigation-type')).toHaveTextContent('REPLACE');
  });

  it("eski dastur detali URL'i ham Testlar katalogiga yo'naltiriladi", () => {
    renderAt('/admin/programs/3f0c1a2b-0000-0000-0000-000000000001');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/admin\/catalog$/);
    expect(screen.getByTestId('navigation-type')).toHaveTextContent('REPLACE');
  });
});
