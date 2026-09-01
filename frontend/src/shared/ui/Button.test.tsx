import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from './Button';

describe('Button', () => {
  it('matnini render qiladi va bosilganda onClick chaqiriladi', async () => {
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Saqlash</Button>);

    const button = screen.getByRole('button', { name: 'Saqlash' });
    await userEvent.click(button);

    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it("isLoading bo'lsa disabled va aria-busy bo'ladi", () => {
    render(<Button isLoading>Yuborish</Button>);

    const button = screen.getByRole('button', { name: 'Yuborish' });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('aria-busy', 'true');
  });

  it("disabled bo'lsa bosilganda onClick chaqirilmaydi", async () => {
    const onClick = vi.fn();
    render(
      <Button onClick={onClick} disabled>
        Bekor qilish
      </Button>,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Bekor qilish' }));
    expect(onClick).not.toHaveBeenCalled();
  });
});
