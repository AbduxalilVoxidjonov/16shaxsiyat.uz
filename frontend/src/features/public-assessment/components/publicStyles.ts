import { cn } from '@/shared/lib/cn';

/**
 * Ommaviy oqim (E-1…E-4) uchun tugma ko'rinishini "16shaxsiyat" uslubiga yaqinlashtiruvchi
 * QO'SHIMCHA klasslar.
 *
 * MUHIM — bu yerda RANG BERILMAYDI: `shared/ui/Button` allaqachon P45 palitrasida
 * (`firuza-600` primary, `outline`/`ghost` variantlari). Ranglarni takrorlash ikki manba
 * muammosini tug'dirardi — dizayn tizimi o'zgarsa ommaviy tugmalar orqada qolardi. Shu
 * sabab faqat `.btn-lg`/`.btn-primary` ga xos O'LCHAM, SOYA va "ko'tarilish" harakati
 * qo'shiladi (`cn` oxirgi argument bo'lgani uchun tailwind-merge o'lchamni to'g'ri
 * almashtiradi).
 *
 * `shared/ui/Button` ning XATTI-HARAKATI (yuklanish, `disabled`, `aria-busy`, ikki marta
 * bosishdan himoya) hech qanday o'zgarishsiz qoladi — faqat `className` beriladi.
 */

export type PublicButtonTone = 'primary' | 'ghost';
export type PublicButtonSize = 'sm' | 'md' | 'lg';

const TONE_CLASSES: Record<PublicButtonTone, string> = {
  /** Asosiy harakat — firuza "porlashi" (dizayn tizimidagi `.btn-primary` kabi). */
  primary: 'shadow-glow',
  /** Ikkilamchi harakat — qo'shimcha soya yo'q, faqat o'lcham va harakat. */
  ghost: '',
};

const SIZE_CLASSES: Record<PublicButtonSize, string> = {
  sm: 'h-9 gap-2 px-4 text-[13px]',
  md: 'h-11 gap-2 px-6 text-sm',
  lg: 'h-14 gap-2 px-8 text-[15px]',
};

/**
 * `shared/ui/Button` uchun ommaviy ko'rinish qo'shimchasi.
 * `disabled:hover:translate-y-0` — o'chiq tugma sichqoncha ostida "sakramasligi" uchun
 * (`:disabled:hover` selektori `:hover` dan kuchliroq, shu sabab tartib muhim emas).
 */
export function publicButtonClass(
  tone: PublicButtonTone = 'primary',
  size: PublicButtonSize = 'md',
  className?: string,
): string {
  return cn(
    'transition-all duration-200 hover:-translate-y-0.5 active:translate-y-0',
    'disabled:hover:translate-y-0 motion-reduce:transition-none motion-reduce:hover:translate-y-0',
    TONE_CLASSES[tone],
    SIZE_CLASSES[size],
    className,
  );
}
