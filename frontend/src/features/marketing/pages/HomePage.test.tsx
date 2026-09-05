import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import HomePage from './HomePage';

function renderPage() {
  return render(
    <MemoryRouter>
      <HomePage />
    </MemoryRouter>,
  );
}

describe('HomePage (ommaviy bosh sahifa)', () => {
  it("bitta `h1` chiqaradi va unda asosiy va'da yozilgan", () => {
    renderPage();

    const headings = screen.getAllByRole('heading', { level: 1 });
    expect(headings).toHaveLength(1);
    expect(headings[0]).toHaveTextContent(/asosli profil/);
  });

  it("CTA'lar aloqa va metodika sahifalariga olib boradi (test havolasi YO'Q)", () => {
    renderPage();

    const links = screen.getAllByRole('link');
    const hrefs = links.map((link) => link.getAttribute('href'));

    expect(hrefs).toContain('/aloqa');
    expect(hrefs).toContain('/metodika');
    // O'quvchi testga faqat maktab havolasi (`/t/:slug`) orqali kiradi — bosh sahifada
    // testni boshlash havolasi bo'lmasligi kerak.
    expect(hrefs.some((href) => href?.startsWith('/t/'))).toBe(false);
  });

  it("namunaviy karta 'haqiqiy ma'lumot emas' deb belgilanadi", () => {
    renderPage();

    expect(screen.getByText(/haqiqiy o'quvchi ma'lumoti emas/)).toBeInTheDocument();
  });

  it("qadamlar tartiblangan ro'yxatda va to'rttadan iborat", () => {
    renderPage();

    const stepsSection = screen.getByRole('region', { name: /Havoladan hisobotgacha/ });
    const steps = within(stepsSection).getAllByRole('listitem');
    expect(steps).toHaveLength(4);
  });

  it('FAQ savollari akkordeon sifatida ochiladi', () => {
    renderPage();

    const question = screen.getByText("Hisobot tashxis o'rnini bosadimi?");
    const details = question.closest('details');
    expect(details).not.toBeNull();
    expect(details).not.toHaveAttribute('open');
  });

  it("raqobatchi metodikasining atamalari sahifada YO'Q (CLAUDE.md 6a-qoida)", () => {
    const { container } = renderPage();
    const text = container.textContent ?? '';

    expect(text).not.toMatch(/MBTI/i);
    expect(text).not.toMatch(/\b(INTJ|INFP|ESTJ|ENFP)\b/);
    expect(text).not.toMatch(/Tahlilchilar|Diplomatlar|Posbonlar|Izlovchilar/);
  });
});
