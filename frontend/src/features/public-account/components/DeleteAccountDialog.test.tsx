import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DeleteAccountDialog } from './DeleteAccountDialog';

/**
 * Ikki qadamli o'chirish oqimi (egasining talabi, 2026-09-08) — `docs/10` E-1..E-6 kabinet
 * oqimidan MUSTAQIL, faqat komponent darajasida sinaladi.
 *
 * `getByRole('button', …)` ISHLATILMAYDI: `Dialog.tsx` native `<dialog>`ni `showModal()`
 * bilan ochadi, jsdom esa uni amalga oshirmagani uchun (`Dialog.tsx` izohi) element hech
 * qachon `open` bo'lmaydi va aria-rol daraxtidan tushib qoladi — `AccountPage.test.tsx`dagi
 * `dialogButton` yordamchisi bilan bir xil sabab, shu naqsh shu yerda ham qaytariladi.
 */
function dialogButton(label: string): HTMLElement {
  const node = screen.getByText(label).closest('button');
  if (!node) throw new Error(`Dialogda "${label}" tugmasi topilmadi`);
  return node;
}

describe('DeleteAccountDialog', () => {
  it("1-qadamdan 2-qadamga o'tadi va hali O'CHIRMAYDI", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<DeleteAccountDialog open onClose={vi.fn()} onConfirm={onConfirm} />);

    expect(screen.getByText("Akkauntni o'chirasizmi?")).toBeInTheDocument();
    await user.click(dialogButton('Davom etish'));

    expect(screen.getByText("O'chirish sababi")).toBeInTheDocument();
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("sabab tanlanmaguncha tasdiq tugmasi o'chiq", async () => {
    const user = userEvent.setup();
    render(<DeleteAccountDialog open onClose={vi.fn()} onConfirm={vi.fn()} />);

    await user.click(dialogButton('Davom etish'));

    const confirmButton = dialogButton("Butunlay o'chirish");
    expect(confirmButton).toBeDisabled();

    await user.click(screen.getByLabelText('Endi kerak emas'));
    expect(confirmButton).toBeEnabled();
  });

  it("`Boshqa sabab` tanlanib izoh bo'sh qoldirilsa xato ko'rsatiladi va o'chirilmaydi", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<DeleteAccountDialog open onClose={vi.fn()} onConfirm={onConfirm} />);

    await user.click(dialogButton('Davom etish'));
    await user.click(screen.getByLabelText('Boshqa sabab'));
    await user.click(dialogButton("Butunlay o'chirish"));

    expect(screen.getByText('Sababni qisqacha yozing')).toBeInTheDocument();
    expect(onConfirm).not.toHaveBeenCalled();

    // `Boshqa sabab` tanlanganda izoh yorlig'i "majburiy"ga o'zgaradi (ixtiyoriy EMAS).
    await user.type(screen.getByLabelText('Izoh (majburiy)'), 'Boshqa test topshiraman');
    await user.click(dialogButton("Butunlay o'chirish"));

    expect(onConfirm).toHaveBeenCalledWith({
      reason: 'Other',
      comment: 'Boshqa test topshiraman',
    });
  });

  it("muvaffaqiyatli holatda `onConfirm` AYNAN `{reason, comment}` bilan chaqiriladi", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<DeleteAccountDialog open onClose={vi.fn()} onConfirm={onConfirm} />);

    await user.click(dialogButton('Davom etish'));
    await user.click(screen.getByLabelText("Natijalar foydali bo'lmadi"));
    await user.click(dialogButton("Butunlay o'chirish"));

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onConfirm).toHaveBeenCalledWith({ reason: 'NotUseful', comment: undefined });
  });

  it('dialog yopilib qayta ochilganda 1-qadamdan boshlanadi', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { rerender } = render(
      <DeleteAccountDialog open onClose={onClose} onConfirm={vi.fn()} />,
    );

    await user.click(dialogButton('Davom etish'));
    expect(screen.getByText("O'chirish sababi")).toBeInTheDocument();

    await user.click(dialogButton('Bekor qilish'));
    expect(onClose).toHaveBeenCalled();

    rerender(<DeleteAccountDialog open={false} onClose={onClose} onConfirm={vi.fn()} />);
    rerender(<DeleteAccountDialog open onClose={onClose} onConfirm={vi.fn()} />);

    expect(screen.getByText("Akkauntni o'chirasizmi?")).toBeInTheDocument();
  });
});
