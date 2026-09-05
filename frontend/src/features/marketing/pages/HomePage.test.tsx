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

  /*
    P47 gacha bu test bosh sahifada testni boshlash havolasi BO'LMASLIGINI qo'riqlardi:
    o'sha paytda testga faqat maktab havolasi (`/t/:slug`) orqali kirilardi. Platforma
    hamma uchun ochilgach shart o'zgardi — endi asosiy CTA `/kirish` (Telegram) ga boradi.
    Test o'chirilmadi, chunki uning IKKINCHI vazifasi hamon kuchda: bosh sahifa hech qachon
    MAKTAB havolasini (`/t/...`) chiqarmaydi — u har maktabga xos va shaxsiy.
  */
  it("asosiy CTA kirish sahifasiga boradi, maktab havolasi (`/t/...`) chiqmaydi", () => {
    renderPage();

    const links = screen.getAllByRole('link');
    const hrefs = links.map((link) => link.getAttribute('href'));

    expect(hrefs).toContain('/kirish');
    expect(hrefs).toContain('/metodika');
    // Maktablar uchun so'rov yo'li ham saqlanib qoldi (endi ikkinchi darajali harakat).
    expect(hrefs).toContain('/aloqa');
    // Maktab havolasi shaxsiy — ommaviy sahifada hech qachon ko'rinmaydi.
    expect(hrefs.some((href) => href?.startsWith('/t/'))).toBe(false);
  });

  it("hero'dagi asosiy tugma 'Testni boshlash' deb nomlanadi (P47)", () => {
    renderPage();

    const [startCta] = screen.getAllByRole('link', { name: 'Testni boshlash' });
    expect(startCta).toHaveAttribute('href', '/kirish');
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

  it("hero'dagi uchta ishonch nuqtasi ochiladigan karta sifatida beriladi", () => {
    renderPage();

    const list = screen.getByRole('list', { name: 'Platforma qoidalari' });
    const items = within(list).getAllByRole('listitem');
    expect(items).toHaveLength(3);

    // Har biri yopiq holatda ochiladi — sarlavha ko'rinadi, tushuntirish esa bosilgach.
    for (const item of items) {
      const details = item.querySelector('details');
      expect(details).not.toBeNull();
      expect(details).not.toHaveAttribute('open');
    }

    expect(
      within(list).getByText('Ball berish qoidalari oldindan belgilangan'),
    ).toBeInTheDocument();
    expect(within(list).getByText('Hisobot tashxis emas')).toBeInTheDocument();
  });

  it('ishonch nuqtasi ochilganda tushuntirish matni bilan birga keladi', () => {
    renderPage();

    const list = screen.getByRole('list', { name: 'Platforma qoidalari' });
    const details = within(list).getByText('Hisobot tashxis emas').closest('details');

    expect(details).not.toBeNull();
    // Matn markapda doim bor (`details` uni faqat vizual yashiradi) — mazmun
    // skrinriderlar va qidiruv tizimlari uchun ham yo'qolmasligi kerak.
    expect(details).toHaveTextContent(/suhbat boshlash nuqtasi/);
  });

  it('ommaviy matnda "ballash" ot shakli ishlatilmaydi (egasining atama qarori)', () => {
    const { container } = renderPage();

    expect(container.textContent ?? '').not.toMatch(/ballash/i);
  });

  it("raqobatchi metodikasining atamalari sahifada YO'Q (CLAUDE.md 6a-qoida)", () => {
    const { container } = renderPage();
    const text = container.textContent ?? '';

    expect(text).not.toMatch(/MBTI/i);
    expect(text).not.toMatch(/\b(INTJ|INFP|ESTJ|ENFP)\b/);
    expect(text).not.toMatch(/Tahlilchilar|Diplomatlar|Posbonlar|Izlovchilar/);
  });
});
