import '@testing-library/jest-dom/vitest';
import '@/shared/lib/i18n';

// jsdom `Element.prototype.scrollIntoView`ni amalga oshirmaydi — E-3 test sahifasi
// (`TestPage.tsx`) javob tanlanganda/`Enter`da shu metodni chaqiradi (docs/10, 4.3-bo'lim).
if (typeof Element !== 'undefined' && !Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = () => {};
}
