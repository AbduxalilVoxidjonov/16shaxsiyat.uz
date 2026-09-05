/**
 * Loyihaning ommaviy aloqa ma'lumotlari — YAGONA manba.
 *
 * Ilgari bu qiymatlar uch joyda takrorlangan edi (`ContactPage`, `MarketingLayout` footer'i va
 * locale fayli), shu sabab biri yangilanganda qolganlari eskirib qolar edi. Endi ko'rinadigan
 * matn ham, `href` ham shu yerdan olinadi: locale faylida faqat YORLIQLAR (masalan "Telegram")
 * qoladi, qiymatning o'zi emas.
 *
 * `display` — ekranda ko'rinadigan shakl, `href` — bosilganda ochiladigan manzil.
 */
export const CONTACT = {
  email: {
    display: 'abduhalilvohidjonov@gmail.com',
    href: 'mailto:abduhalilvohidjonov@gmail.com',
  },
  /** Raqam `href` da E.164 formatida (probelsiz), ekranda esa o'qishga qulay bo'lib ajratiladi. */
  phone: {
    display: '+998 90 632 39 00',
    href: 'tel:+998906323900',
  },
  telegram: {
    display: '@abduxalilvoxidjonov',
    href: 'https://t.me/abduxalilvoxidjonov',
  },
} as const;

export type ContactChannelKey = keyof typeof CONTACT;
