import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Input } from './Input';

describe('Input', () => {
  it("yorliqni inputga bog'laydi (htmlFor/id) va matn kiritishga ruxsat beradi", () => {
    render(<Input label="Telefon" placeholder="+998" />);

    const input = screen.getByLabelText('Telefon');
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute('placeholder', '+998');
  });

  it("xato berilsa aria-invalid va aria-describedby to'g'ri qo'yiladi", () => {
    render(<Input label="Telefon" error="Telefon raqami noto'g'ri" />);

    const input = screen.getByLabelText('Telefon');
    expect(input).toHaveAttribute('aria-invalid', 'true');

    const describedBy = input.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();

    const message = screen.getByRole('alert');
    expect(message).toHaveTextContent("Telefon raqami noto'g'ri");
    expect(message).toHaveAttribute('id', describedBy);
  });

  it("xato yo'q bo'lsa aria-invalid qo'yilmaydi", () => {
    render(<Input label="Ism" />);
    expect(screen.getByLabelText('Ism')).not.toHaveAttribute('aria-invalid');
  });

  it("isRequired: yorliqda qizil * (aria-hidden) va aria-required; accessible name o'zgarmaydi", () => {
    const { container } = render(<Input label="F.I.Sh." isRequired />);

    // Aniq matnli qidiruv buzilmaydi — belgi CSS `::after`, DOM matni emas.
    const input = screen.getByLabelText('F.I.Sh.');
    expect(input).toHaveAttribute('aria-required', 'true');
    expect(screen.getByRole('textbox', { name: 'F.I.Sh.' })).toBe(input);

    const mark = container.querySelector('[data-required-mark]');
    expect(mark).not.toBeNull();
    expect(mark).toHaveAttribute('aria-hidden', 'true');
    expect(mark).toHaveAttribute('data-mark', '*');
    expect(mark).toHaveClass('text-terakota-600');
  });

  it("ixtiyoriy maydonda (isRequired yo'q) * ham, aria-required ham yo'q", () => {
    const { container } = render(<Input label="Email" />);

    expect(screen.getByLabelText('Email')).not.toHaveAttribute('aria-required');
    expect(container.querySelector('[data-required-mark]')).toBeNull();
  });
});
