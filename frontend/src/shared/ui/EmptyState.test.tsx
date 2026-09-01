import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EmptyState } from './EmptyState';

describe('EmptyState', () => {
  it("standart (i18n) sarlavha va tavsifni ko'rsatadi", () => {
    render(<EmptyState />);

    expect(screen.getByText("Bu yerda hali hech narsa yo'q")).toBeInTheDocument();
    expect(screen.getByText("Ma'lumot paydo bo'lganda shu yerda ko'rinadi.")).toBeInTheDocument();
  });

  it('berilgan title/description bilan ustidan yozadi', () => {
    render(<EmptyState title="Hali o'quvchi yo'q" description="Maktab havolasini ulashing" />);

    expect(screen.getByText("Hali o'quvchi yo'q")).toBeInTheDocument();
    expect(screen.getByText('Maktab havolasini ulashing')).toBeInTheDocument();
  });

  it("dekorativ ikonka ekran o'quvchisidan yashiriladi", () => {
    const { container } = render(<EmptyState />);
    const icon = container.querySelector('[aria-hidden="true"]');
    expect(icon).not.toBeNull();
  });
});
