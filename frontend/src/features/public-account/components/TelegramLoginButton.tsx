import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { env } from '@/shared/config/env';

/**
 * Telegram Login Widget (`docs/07` §2a.1, `docs/08` §2a).
 *
 * ## Qanday ulanadi
 *
 * Telegram tayyor React komponenti bermaydi — u RASMIY skriptni sahifaga qo'shishni talab
 * qiladi, skript esa o'z o'rniga `<iframe>` joylashtiradi. Shu sabab bu yerda `<script>`
 * elementi **DOM API bilan** yaratiladi (`document.createElement` + `setAttribute` +
 * `appendChild`) — `dangerouslySetInnerHTML` CLAUDE.md 12-qoidasi bo'yicha taqiqlangan va
 * bu yerda kerak ham emas.
 *
 * ## Nega `data-auth-url` (redirect), `data-onauth` (callback) EMAS
 *
 * `data-onauth` atributining qiymati Telegram tomonidan **matn sifatida `eval` qilinadi**
 * (`eval("onTelegramAuth(user)")`). Bizning CSP'da `unsafe-eval` ATAYLAB yo'q
 * (`docs/08` 7-bo'lim), shu sabab jonli saytda widget skripti `200` bilan yuklansa ham
 * iframe'ni umuman chiza olmasdi:
 *
 *     Evaluating a string as JavaScript violates the following Content Security Policy
 *     directive because 'unsafe-eval' is not an allowed source of script
 *
 * Foydalanuvchi `/kirish` sahifasida hech qanday tugma ko'rmasdi. `data-auth-url` esa
 * `eval` talab qilmaydi: Telegram brauzerni berilgan manzilga query parametrlar bilan
 * QAYTARADI, sahifa esa ularni o'qib API'ga yuboradi (`lib/telegramCallback.ts`).
 * Yechim CSP'ni bo'shatmaydi.
 *
 * ## Bot sozlanmagan holat
 *
 * `VITE_TELEGRAM_BOT` bo'sh bo'lsa skript umuman qo'shilmaydi (widget bot nomisiz
 * ishlamaydi) — foydalanuvchi oq ekran emas, "Telegram kirishi hozircha sozlanmagan"
 * degan tushunarli xabarni ko'radi. Skript yuklanmasa (tarmoq/bloklash) ham xuddi shunday
 * xabar chiqadi.
 */

const TELEGRAM_WIDGET_SRC = 'https://telegram.org/js/telegram-widget.js?22';

export interface TelegramLoginButtonProps {
  /**
   * Foydalanuvchi tasdiqlagach Telegram brauzerni QAYTARADIGAN manzil. **Mutlaq bo'lishi
   * shart** (`https://...`) — Telegram nisbiy yo'lni qabul qilmaydi.
   */
  authUrl: string;
  /** Kirish so'rovi ketayotganda widget o'chiriladi (ikki marta bosishdan himoya). */
  disabled?: boolean;
}

export function TelegramLoginButton({ authUrl, disabled = false }: TelegramLoginButtonProps) {
  const { t } = useTranslation();
  const containerRef = useRef<HTMLDivElement>(null);
  const [scriptFailed, setScriptFailed] = useState(false);

  const botName = env.telegramBot;

  useEffect(() => {
    const container = containerRef.current;
    if (!container || !botName) {
      return;
    }

    const script = document.createElement('script');
    script.src = TELEGRAM_WIDGET_SRC;
    script.async = true;
    script.setAttribute('data-telegram-login', botName);
    script.setAttribute('data-size', 'large');
    script.setAttribute('data-radius', '20');
    // Foydalanuvchi rasmi widget tugmasida ko'rsatilmaydi — u bizga faqat javobda kerak.
    script.setAttribute('data-userpic', 'false');
    script.setAttribute('data-auth-url', authUrl);
    script.onerror = () => {
      setScriptFailed(true);
    };
    container.appendChild(script);

    return () => {
      container.replaceChildren();
    };
  }, [botName, authUrl]);

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
