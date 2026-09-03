import type { ReactNode } from 'react';

/**
 * Faqat ekran o'quvchisi uchun mo'ljallangan mazmun o'rami (`docs/11`, 4-bo'lim —
 * diagrammalarning jadval muqobili).
 *
 * NEGA O'RAM KERAK: Tailwind ning `sr-only` yordamchisini to'g'ridan-to'g'ri `<table>` ga
 * berib bo'lmaydi. U uchta vositaga tayanadi va uchalasi ham jadvalda ishlamaydi:
 *   1. `width: 1px` — CSS jadval algoritmida jadval eni HECH QACHON `min-content` dan
 *      kichik bo'lmaydi, ya'ni jadval o'z mazmuni bo'yicha kengayib ketaveradi;
 *   2. `overflow: hidden` — `display: table` qutisiga qo'llanilmaydi;
 *   3. `clip-path: inset(50%)` — faqat CHIZISHNI kesadi, siljish (scroll) hududini emas.
 * Natijada "yashirin" jadval sahifani yon tomonga siljitadi (P30-5: 390px da
 * `scrollWidth` 429). `display: block` yechim EMAS — u jadvalni a11y daraxtidan yo'qotadi.
 *
 * Oddiy `<div>` esa blok konteyner: `width: 1px` + `overflow: hidden` unda to'liq ishlaydi,
 * jadval shu o'ram ichida qirqiladi. `visibility`/`display` o'zgarmagani uchun mazmun ekran
 * o'quvchisiga ochiq qoladi.
 */
export function VisuallyHidden({ children }: { children: ReactNode }) {
  return <div className="sr-only">{children}</div>;
}
