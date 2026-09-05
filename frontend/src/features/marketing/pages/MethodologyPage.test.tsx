import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import MethodologyPage from './MethodologyPage';

function renderPage() {
  return render(
    <MemoryRouter>
      <MethodologyPage />
    </MemoryRouter>,
  );
}

describe('MethodologyPage (`/metodika`)', () => {
  it("sarlavha va to'rtta blok ko'rsatiladi", () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      /Platforma nimani va qanday o'lchaydi/,
    );

    for (const title of [
      'Shaxsiyat uslubi',
      'Besh omil',
      'Kasb qiziqishlari',
      "O'quv faolligi va motivatsiya",
    ]) {
      expect(screen.getByRole('heading', { level: 2, name: title })).toBeInTheDocument();
    }
  });

  it('har bir blok uchun "nimani o\'lchaydi" va "qanday ballanadi" bloklari bor', () => {
    renderPage();

    expect(screen.getAllByRole('heading', { name: "Nimani o'lchaydi" })).toHaveLength(4);
    expect(screen.getAllByRole('heading', { name: 'Qanday ballanadi' })).toHaveLength(4);
  });

  it('ishonchlilikning uchta belgisi tavsiflanadi', () => {
    renderPage();

    const section = screen.getByRole('region', { name: /Har bir sessiya tekshiriladi/ });
    expect(within(section).getByText('Ishonchli')).toBeInTheDocument();
    expect(within(section).getByText('Shubhali')).toBeInTheDocument();
    expect(within(section).getByText('Ishonchsiz')).toBeInTheDocument();
  });

  it("AI ball hisoblamasligi va tashxis qo'ymasligi ochiq yozilgan (CLAUDE.md 5 va 6-qoida)", () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Ball hisoblamaydi' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: "Tashxis qo'ymaydi" })).toBeInTheDocument();
    expect(screen.getByText(/tibbiy yoki psixiatrik tashxis o'rnini bosmaydi/)).toBeInTheDocument();
  });

  it('aloqa sahifasiga yakuniy havola beradi', () => {
    renderPage();

    const hrefs = screen.getAllByRole('link').map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/aloqa');
  });
});
