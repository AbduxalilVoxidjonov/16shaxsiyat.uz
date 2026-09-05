import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { env } from '@/shared/config/env';
import type { TelegramLoginRequestBody } from '@/shared/api/types';

/**
 * Telegram Login Widget (`docs/07` §2a.1).
 *
 * ## Qanday ulanadi
 *
 * Telegram tayyor React komponenti bermaydi — u RASMIY skriptni sahifaga qo'shishni talab
 * qiladi, skript esa o'z o'rniga `<iframe>` joylashtiradi. Shu sabab bu yerda `<script>`
 * elementi **DOM API bilan** yaratiladi (`document.createElement` + `setAttribute` +
 * `appendChild`) — `dangerouslySetInnerHTML` CLAUDE.md 12-qoidasi bo'yicha taqiqlangan va
 * bu yerda kerak ham emas.
 *
 * `data-onauth` atributi Telegram tomonidan GLOBAL doirada bajariladi, shu sabab callback
 * `window` ga vaqtincha o'rnatiladi va komponent yo'q qilinganda olib tashlanadi. Callback
 * ichida `onAuthRef` ishlatiladi — skript bir marta yuklanadi, `onAuth` esa har renderda
 * yangi funksiya bo'lishi mumkin.
 *
 * ## Bot sozlanmagan holat
 *
 * `VITE_TELEGRAM_BOT` bo'sh bo'lsa skript umuman qo'shilmaydi (widget bot nomisiz
 * ishlamaydi) — foydalanuvchi oq ekran emas, "Telegram kirishi hozircha sozlanmagan"
 * degan tushunarli xabarni ko'radi. Skript yuklanmasa (tarmoq/bloklash) ham xuddi shunday
 * xabar chiqadi.
 */

declare global {
  interface Window {
    /** Telegram widget `data-onauth` orqali chaqiradigan global callback. */
    onTelegramAuth?: (user: TelegramLoginRequestBody) => void;
  }
}

const TELEGRAM_WIDGET_SRC = 'https://telegram.org/js/telegram-widget.js?22';
const AUTH_CALLBACK_NAME = 'onTelegramAuth';

export interface TelegramLoginButtonProps {
  /**
   * Telegram bergan obyekt — **o'zgartirilmagan holda**. Chaqiruvchi uni to'g'ridan-to'g'ri
   * `POST /api/auth/telegram` tanasiga yuboradi.
   */
  onAuth: (user: TelegramLoginRequestBody) => void;
  /** Kirish so'rovi ketayotganda widget o'chiriladi (ikki marta bosishdan himoya). */
  disabled?: boolean;
}

export function TelegramLoginButton({ onAuth, disabled = false }: TelegramLoginButtonProps) {
  const { t } = useTranslation();
  const containerRef = useRef<HTMLDivElement>(null);
  const onAuthRef = useRef(onAuth);
  const [scriptFailed, setScriptFailed] = useState(false);

  useEffect(() => {
    onAuthRef.current = onAuth;
  }, [onAuth]);

  const botName = env.telegramBot;

  useEffect(() => {
    const container = containerRef.current;
    if (!container || !botName) {
      return;
    }

    window[AUTH_CALLBACK_NAME] = (user: TelegramLoginRequestBody) => {
      onAuthRef.current(user);
    };

    const script = document.createElement('script');
    script.src = TELEGRAM_WIDGET_SRC;
    script.async = true;
    script.setAttribute('data-telegram-login', botName);
    script.setAttribute('data-size', 'large');
    script.setAttribute('data-radius', '20');
    // Foydalanuvchi rasmi widget tugmasida ko'rsatilmaydi — u bizga faqat javobda kerak.
    script.setAttribute('data-userpic', 'false');
    script.setAttribute('data-onauth', `${AUTH_CALLBACK_NAME}(user)`);
    script.onerror = () => {
      setScriptFailed(true);
    };
    container.appendChild(script);

    return () => {
      container.replaceChildren();
      delete window[AUTH_CALLBACK_NAME];
    };
  }, [botName]);

  if (!botName || scriptFailed) {
    return (
      <div
        role="status"
        className="rounded-4xl border border-zarhal-200 bg-zarhal-50 px-5 py-4 text-center"
      >
        <p className="font-display text-sm font-bold text-ink">
          {t('account.login.notConfiguredTitle')}
        </p>
        <p className="mt-1.5 text-sm leading-relaxed text-ink-soft">
          {t('account.login.notConfiguredDescription')}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-3">
      {/*
        Widget iframe'i shu konteynerga tushadi. `aria-busy` — so'rov ketayotganini
        skrinriderga bildiradi; `pointer-events-none` esa shu paytda ikkinchi bosishni
        to'sadi (iframe ichidagi tugmani `disabled` qilib bo'lmaydi).
      */}
      <div
        ref={containerRef}
        aria-busy={disabled || undefined}
        className={disabled ? 'pointer-events-none opacity-60' : undefined}
      />
      <p className="max-w-xs text-center text-[13px] leading-relaxed text-ink-soft">
        {t('account.login.widgetHint')}
      </p>
    </div>
  );
}
