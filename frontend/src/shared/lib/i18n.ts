import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';

import uzCommon from '@/locales/uz/common.json';
import ruCommon from '@/locales/ru/common.json';

/**
 * i18n sozlamalari — CLAUDE.md 1-qoida: foydalanuvchiga ko'rinadigan matn o'zbekcha (lotin).
 * `uz` asosiy va to'liq til; `ru` hozircha bo'sh skelet — kalit topilmasa `uz`ga tushadi.
 */
void i18next.use(initReactI18next).init({
  resources: {
    uz: { common: uzCommon },
    ru: { common: ruCommon },
  },
  lng: 'uz',
  fallbackLng: 'uz',
  defaultNS: 'common',
  ns: ['common'],
  interpolation: {
    escapeValue: false, // React allaqachon XSS'dan himoyalaydi
  },
  returnEmptyString: false,
});

export default i18next;
