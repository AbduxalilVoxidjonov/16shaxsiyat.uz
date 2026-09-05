import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { CONTACT } from '@/shared/config/contact';
import ContactPage from './ContactPage';

function renderPage() {
  return render(
    <MemoryRouter>
      <ContactPage />
    </MemoryRouter>,
  );
}

describe('ContactPage (`/aloqa`)', () => {
  it('sarlavhani chiqaradi', () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Bizga yozing');
  });

  it('pochta, telefon va Telegram kanallarini havola sifatida beradi', () => {
    renderPage();

    const hrefs = screen.getAllByRole('link').map((link) => link.getAttribute('href'));
    expect(hrefs).toContain(CONTACT.email.href);
    expect(hrefs).toContain(CONTACT.phone.href);
    expect(hrefs).toContain(CONTACT.telegram.href);
  });

  it("aloqa qiymatlari ko'rinadigan matn sifatida chiqadi", () => {
    renderPage();

    expect(screen.getByText(CONTACT.email.display)).toBeInTheDocument();
    expect(screen.getByText(CONTACT.phone.display)).toBeInTheDocument();
    expect(screen.getByText(CONTACT.telegram.display)).toBeInTheDocument();
  });

  it("ko'p yoziladigan mavzular ro'yxati ko'rsatiladi", () => {
    renderPage();

    const section = screen.getByRole('region', { name: "Ko'p yoziladigan mavzular" });
    expect(section).toBeInTheDocument();
    expect(screen.getByText("Maktabimizda sinov o'tkazmoqchimiz")).toBeInTheDocument();
  });

  it("shaxsiy ma'lumot yubormaslik haqida ogohlantiradi", () => {
    renderPage();

    expect(screen.getByText(/shaxsiy ma'lumotlarini .* yubormang/)).toBeInTheDocument();
  });

  it('ishlamaydigan aloqa formasi joylashtirilmagan', () => {
    renderPage();

    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
