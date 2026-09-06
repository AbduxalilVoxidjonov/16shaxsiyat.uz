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

const AUTH_URL = 'https://16shaxsiyat.uz/kirish';

function getWidgetScript(container: HTMLElement): HTMLScriptElement | null {
  return container.querySelector('script');
}

describe('TelegramLoginButton', () => {
  afterEach(() => {
    botName.current = 'shaxsiyat_login_bot';
  });

  it("Telegram skriptini DOM API bilan qo'shadi va bot nomini uzatadi", () => {
    const { container } = render(<TelegramLoginButton authUrl={AUTH_URL} />);

    const script = getWidgetScript(container);
    expect(script).not.toBeNull();
    expect(script?.src).toBe('https://telegram.org/js/telegram-widget.js?22');
    expect(script?.getAttribute('data-telegram-login')).toBe('shaxsiyat_login_bot');
  });

  it('`data-auth-url` (redirect) beradi — `data-onauth` (JS `eval`) UMUMAN yo‘q', () => {
    const { container } = render(<TelegramLoginButton authUrl={AUTH_URL} />);

    const script = getWidgetScript(container);
    expect(script?.getAttribute('data-auth-url')).toBe(AUTH_URL);
    // `data-onauth` qiymati Telegram tomonidan `eval` qilinadi — CSP'da `unsafe-eval`
    // yo'q, shu sabab u qaytib kelmasligi kerak (widget umuman chizilmay qolardi).
    expect(script?.hasAttribute('data-onauth')).toBe(false);
    expect(window).not.toHaveProperty('onTelegramAuth');
  });

  it("`authUrl` o'zgarsa skript yangi manzil bilan qayta qo'yiladi", () => {
    const { container, rerender } = render(<TelegramLoginButton authUrl={AUTH_URL} />);

    rerender(<TelegramLoginButton authUrl={`${AUTH_URL}?returnUrl=%2Fkabinet%2Ftest`} />);

    expect(container.querySelectorAll('script')).toHaveLength(1);
    expect(getWidgetScript(container)?.getAttribute('data-auth-url')).toBe(
      `${AUTH_URL}?returnUrl=%2Fkabinet%2Ftest`,
    );
  });

  it("bot nomi berilmasa skript qo'shilmaydi va tushunarli xabar ko'rsatiladi (oq ekran YO'Q)", () => {
    botName.current = '';

    const { container } = render(<TelegramLoginButton authUrl={AUTH_URL} />);

    expect(getWidgetScript(container)).toBeNull();
    expect(screen.getByText('Telegram kirishi hozircha sozlanmagan')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it("skript yuklanmasa ham xuddi shu tushunarli holat ko'rsatiladi", () => {
    const { container } = render(<TelegramLoginButton authUrl={AUTH_URL} />);

    const script = getWidgetScript(container);
    expect(script).not.toBeNull();
    act(() => {
      script?.dispatchEvent(new Event('error'));
    });

    expect(screen.getByText('Telegram kirishi hozircha sozlanmagan')).toBeInTheDocument();
  });

  it("komponent yo'q qilinganda skript konteyneri tozalanadi", () => {
    const { container, unmount } = render(<TelegramLoginButton authUrl={AUTH_URL} />);
    expect(getWidgetScript(container)).not.toBeNull();

    unmount();

    expect(getWidgetScript(container)).toBeNull();
  });
});
