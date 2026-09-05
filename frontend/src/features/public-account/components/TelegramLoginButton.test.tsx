import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import { TelegramLoginButton } from './TelegramLoginButton';

/**
 * `env` moduli `import.meta.env` ni MODUL YUKLANGANDA o'qiydi, shu sabab `vi.stubEnv`
 * kech qoladi — bot nomi getter orqali beriladigan mock bilan almashtiriladi.
 */
const botName = { current: 'shaxsiyat_login_bot' };

vi.mock('@/shared/config/env', () => ({
  env: {
    apiBaseUrl: '',
    appName: 'Shaxsiyat',
    sentryDsn: '',
    get telegramBot() {
      return botName.current;
    },
  },
}));

function getWidgetScript(container: HTMLElement): HTMLScriptElement | null {
  return container.querySelector('script');
}

describe('TelegramLoginButton', () => {
  afterEach(() => {
    botName.current = 'shaxsiyat_login_bot';
    delete window.onTelegramAuth;
  });

  it("Telegram skriptini DOM API bilan qo'shadi va bot nomini uzatadi", () => {
    const { container } = render(<TelegramLoginButton onAuth={vi.fn()} />);

    const script = getWidgetScript(container);
    expect(script).not.toBeNull();
    expect(script?.src).toBe('https://telegram.org/js/telegram-widget.js?22');
    expect(script?.getAttribute('data-telegram-login')).toBe('shaxsiyat_login_bot');
    expect(script?.getAttribute('data-onauth')).toBe('onTelegramAuth(user)');
  });

  it("`data-onauth` chaqirgan global callback `onAuth` ni AYNAN o'sha obyekt bilan uzatadi", () => {
    const onAuth = vi.fn();
    render(<TelegramLoginButton onAuth={onAuth} />);

    // Telegram bergan obyekt: `username` yo'q (foydalanuvchida yo'q) — u umuman
    // yuborilmasligi kerak, shu sabab obyekt qayta yig'ilmaydi.
    const payload = {
      id: 123456789,
      first_name: 'Ali',
      auth_date: 1767225600,
      hash: 'a'.repeat(64),
    };
    window.onTelegramAuth?.(payload);

    expect(onAuth).toHaveBeenCalledTimes(1);
    // AYNAN o'sha obyekt (nusxa emas) — maydonlar qayta yig'ilmagani shu bilan tekshiriladi.
    expect(onAuth).toHaveBeenCalledWith(payload);
  });

  it("bot nomi berilmasa skript qo'shilmaydi va tushunarli xabar ko'rsatiladi (oq ekran YO'Q)", () => {
    botName.current = '';

    const { container } = render(<TelegramLoginButton onAuth={vi.fn()} />);

    expect(getWidgetScript(container)).toBeNull();
    expect(screen.getByText('Telegram kirishi hozircha sozlanmagan')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it("skript yuklanmasa ham xuddi shu tushunarli holat ko'rsatiladi", () => {
    const { container } = render(<TelegramLoginButton onAuth={vi.fn()} />);

    const script = getWidgetScript(container);
    expect(script).not.toBeNull();
    act(() => {
      script?.dispatchEvent(new Event('error'));
    });

    expect(screen.getByText('Telegram kirishi hozircha sozlanmagan')).toBeInTheDocument();
  });

  it("komponent yo'q qilinganda global callback ham olib tashlanadi", () => {
    const { unmount } = render(<TelegramLoginButton onAuth={vi.fn()} />);
    expect(window.onTelegramAuth).toBeTypeOf('function');

    unmount();

    expect(window.onTelegramAuth).toBeUndefined();
  });
});
