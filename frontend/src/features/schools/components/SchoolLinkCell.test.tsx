import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ToastProvider } from '@/shared/ui/Toast';
import { SchoolLinkCell } from './SchoolLinkCell';

const PUBLIC_URL = 'https://salohiyat.uz/t/12-maktab-qokon?k=abc123token';

function renderCell(onShowQr = vi.fn()) {
  return render(
    <ToastProvider>
      <SchoolLinkCell publicUrl={PUBLIC_URL} onShowQr={onShowQr} />
    </ToastProvider>,
  );
}

/**
 * `navigator.clipboard` jsdom'da faqat getter (prototipdagi accessor) — oddiy
 * `Object.assign(navigator, {...})` `TypeError: ... has only a getter` beradi. O'z ustki
 * (own) xususiyat sifatida qayta belgilash kerak; `afterEach`da tozalanadi.
 *
 * **MUHIM tartib:** `@testing-library/user-event`ning `userEvent.setup()`i o'zining
 * `navigator.clipboard` stub'ini o'rnatadi — shu sabab `stubClipboard()` har doim
 * `userEvent.setup()`dan **keyin** chaqirilishi shart, aks holda bizning qiymatimiz
 * user-event stub'i bilan ustidan yozilib ketadi (empirik tekshirilgan).
 */
function stubClipboard(value: Pick<Clipboard, 'writeText'> | undefined) {
  Object.defineProperty(navigator, 'clipboard', {
    configurable: true,
    value,
  });
}

describe('SchoolLinkCell', () => {
  afterEach(() => {
    stubClipboard(undefined);
    delete (navigator as { clipboard?: unknown }).clipboard;
  });

  it("havolani protokolsiz, qisqartirilgan ko'rinishda ko'rsatadi", () => {
    renderCell();
    expect(screen.getByText('salohiyat.uz/t/12-maktab-qokon?k=abc123token')).toBeInTheDocument();
  });

  it("nusxalash tugmasi bosilganda 'clipboard'ga yoziladi va tasdiq toast ko'rsatiladi", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    stubClipboard({ writeText });
    renderCell();

    await user.click(screen.getByRole('button', { name: 'Havolani nusxalash' }));

    expect(writeText).toHaveBeenCalledWith(PUBLIC_URL);
    expect(await screen.findByText('Havola nusxalandi')).toBeInTheDocument();
  });

  it("'clipboard' mavjud bo'lmasa (eski brauzer) jimgina yiqilmaydi — tushunarli xato ko'rsatiladi", async () => {
    const user = userEvent.setup();
    stubClipboard(undefined);
    renderCell();

    await user.click(screen.getByRole('button', { name: 'Havolani nusxalash' }));

    expect(await screen.findByText("Havolani nusxalab bo'lmadi")).toBeInTheDocument();
  });

  it("'clipboard.writeText' rad etsa ham tushunarli xato ko'rsatiladi", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockRejectedValue(new Error('denied'));
    stubClipboard({ writeText });
    renderCell();

    await user.click(screen.getByRole('button', { name: 'Havolani nusxalash' }));

    expect(await screen.findByText("Havolani nusxalab bo'lmadi")).toBeInTheDocument();
  });

  it("QR tugmasi bosilganda 'onShowQr' chaqiriladi", async () => {
    const onShowQr = vi.fn();
    const user = userEvent.setup();
    renderCell(onShowQr);

    await user.click(screen.getByRole('button', { name: "QR kodni ko'rish" }));

    expect(onShowQr).toHaveBeenCalledTimes(1);
  });
});
