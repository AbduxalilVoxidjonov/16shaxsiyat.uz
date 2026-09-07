import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ToastProvider } from '@/shared/ui/Toast';
import { SchoolEntryCodeCell } from './SchoolEntryCodeCell';

/** `SchoolLinkCell.test.tsx` dagi bilan bir xil sabab/tartib: `userEvent.setup()` dan KEYIN chaqiriladi. */
function stubClipboard(value: Pick<Clipboard, 'writeText'> | undefined) {
  Object.defineProperty(navigator, 'clipboard', { configurable: true, value });
}

function renderCell(entryCode: string | null = 'ABCD-2345', onRegenerate = vi.fn()) {
  return render(
    <ToastProvider>
      <SchoolEntryCodeCell entryCode={entryCode} onRegenerate={onRegenerate} />
    </ToastProvider>,
  );
}

describe('SchoolEntryCodeCell', () => {
  afterEach(() => {
    stubClipboard(undefined);
    delete (navigator as { clipboard?: unknown }).clipboard;
  });

  it("kodni ko'rsatadi va qayta yaratish tugmasi chaqiruvchiga xabar beradi", async () => {
    const user = userEvent.setup();
    const onRegenerate = vi.fn();
    renderCell('ABCD-2345', onRegenerate);

    expect(screen.getByText('ABCD-2345')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Kodni qayta yaratish' }));
    expect(onRegenerate).toHaveBeenCalledTimes(1);
  });

  it("nusxalash tugmasi kodni 'clipboard'ga yozadi va toast ko'rsatadi", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    stubClipboard({ writeText });
    renderCell();

    await user.click(screen.getByRole('button', { name: 'Maktab kodini nusxalash' }));

    expect(writeText).toHaveBeenCalledWith('ABCD-2345');
    expect(await screen.findByText('Maktab kodi nusxalandi')).toBeInTheDocument();
  });

  it("'clipboard' bo'lmasa jimgina yiqilmaydi — xato toast", async () => {
    const user = userEvent.setup();
    stubClipboard(undefined);
    renderCell();

    await user.click(screen.getByRole('button', { name: 'Maktab kodini nusxalash' }));

    expect(await screen.findByText("Havolani nusxalab bo'lmadi")).toBeInTheDocument();
  });

  it("kod yo'q bo'lsa (ommaviy makon) `—` ko'rsatadi, tugmalar yo'q", () => {
    renderCell(null);

    expect(screen.getByText('—')).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it("`onRegenerate` berilmasa faqat kod + nusxalash — 'qayta yaratish' tugmasi CHIQMAYDI", () => {
    render(
      <ToastProvider>
        <SchoolEntryCodeCell entryCode="ABCD-2345" />
      </ToastProvider>,
    );

    expect(screen.getByText('ABCD-2345')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Maktab kodini nusxalash' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Kodni qayta yaratish' })).not.toBeInTheDocument();
  });

  it('`size="lg"` — katta variantda ham kod, nusxalash va qayta yaratish joyida', async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    stubClipboard({ writeText });
    const onRegenerate = vi.fn();
    render(
      <ToastProvider>
        <SchoolEntryCodeCell entryCode="ABCD-2345" size="lg" onRegenerate={onRegenerate} />
      </ToastProvider>,
    );

    expect(screen.getByText('ABCD-2345')).toHaveClass('text-2xl');
    await user.click(screen.getByRole('button', { name: 'Maktab kodini nusxalash' }));
    expect(writeText).toHaveBeenCalledWith('ABCD-2345');
    await user.click(screen.getByRole('button', { name: 'Kodni qayta yaratish' }));
    expect(onRegenerate).toHaveBeenCalledTimes(1);
  });
});
