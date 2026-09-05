import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { Logo } from './Logo';

describe('Logo', () => {
  it('brend nomini `app.name` kalitidan oladi', () => {
    render(<Logo />);

    expect(screen.getByText('Shaxsiyat')).toBeInTheDocument();
  });

  it('standart holatda havola EMAS', () => {
    render(<Logo />);

    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });

  it("asLink bilan bosh sahifaga havola bo'ladi", () => {
    render(
      <MemoryRouter>
        <Logo asLink />
      </MemoryRouter>,
    );

    const link = screen.getByRole('link', { name: 'Shaxsiyat — bosh sahifa' });
    expect(link).toHaveAttribute('href', '/');
  });
});
