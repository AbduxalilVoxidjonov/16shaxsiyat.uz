import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { MarketingLayout } from './MarketingLayout';

function renderLayout(initialPath = '/') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<MarketingLayout />}>
          <Route path="/" element={<p>Bosh sahifa mazmuni</p>} />
          <Route path="/metodika" element={<p>Metodika mazmuni</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe('MarketingLayout', () => {
  it("mazmunga o'tish havolasi va `main` landmark beradi", () => {
    renderLayout();

    const skipLink = screen.getByRole('link', { name: "Asosiy mazmunga o'tish" });
    expect(skipLink).toHaveAttribute('href', '#main-content');
    expect(screen.getByRole('main')).toHaveAttribute('id', 'main-content');
    expect(screen.getByText('Bosh sahifa mazmuni')).toBeInTheDocument();
  });

  it("asosiy navigatsiyada uchta bo'lim havolasi bor", () => {
    renderLayout();

    const nav = screen.getByRole('navigation', { name: 'Asosiy menyu' });
    const hrefs = within(nav)
      .getAllByRole('link')
      .map((link) => link.getAttribute('href'));

    expect(hrefs).toEqual(['/metodika', '/biz-haqimizda', '/aloqa']);
  });

  it('joriy sahifa havolasi `aria-current="page"` bilan belgilanadi', () => {
    renderLayout('/metodika');

    const nav = screen.getByRole('navigation', { name: 'Asosiy menyu' });
    const active = within(nav).getByRole('link', { name: 'Metodika' });
    expect(active).toHaveAttribute('aria-current', 'page');
  });

  it('mobil menyu tugmasi `aria-expanded` holatini almashtiradi', async () => {
    const user = userEvent.setup();
    renderLayout();

    const toggle = screen.getByRole('button', { name: 'Menyuni ochish' });
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    expect(
      screen.getByRole('navigation', { name: 'Mobil menyu', hidden: true }),
    ).toBeInTheDocument();

    await user.click(toggle);

    const opened = screen.getByRole('button', { name: 'Menyuni yopish' });
    expect(opened).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('navigation', { name: 'Mobil menyu' })).toBeInTheDocument();

    await user.click(opened);
    expect(screen.getByRole('button', { name: 'Menyuni ochish' })).toHaveAttribute(
      'aria-expanded',
      'false',
    );
  });

  it("footer'da eslatma va joriy yil ko'rsatiladi", () => {
    renderLayout();

    const footer = screen.getByRole('contentinfo');
    expect(
      within(footer).getByText(/tibbiy yoki psixologik tashxis qo'ymaydi/),
    ).toBeInTheDocument();
    expect(
      within(footer).getByText(new RegExp(String(new Date().getFullYear()))),
    ).toBeInTheDocument();
  });
});
