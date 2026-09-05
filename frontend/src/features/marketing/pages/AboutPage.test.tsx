import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import AboutPage from './AboutPage';

function renderPage() {
  return render(
    <MemoryRouter>
      <AboutPage />
    </MemoryRouter>,
  );
}

describe('AboutPage (`/biz-haqimizda`)', () => {
  it('sarlavha va kirish matnini chiqaradi', () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      /Maktabdagi o'quvchini yaxshiroq tushunish uchun ko'nikmalar/,
    );
  });

  it("to'rtta tamoyil kartasi ko'rsatiladi", () => {
    renderPage();

    for (const title of ['Ona tilida', 'Qoidalar ochiq', "Kerakli ma'lumotgina", 'Yorliqsiz']) {
      expect(screen.getByRole('heading', { name: title })).toBeInTheDocument();
    }
  });

  it("metodika bo'limida klinik vosita emasligi ochiq aytiladi", () => {
    renderPage();

    expect(screen.getByText(/bu klinik vosita emas va tashxis qo'ymaydi/i)).toBeInTheDocument();
  });

  it('aloqa sahifasiga havola beradi', () => {
    renderPage();

    const hrefs = screen.getAllByRole('link').map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/aloqa');
  });
});
