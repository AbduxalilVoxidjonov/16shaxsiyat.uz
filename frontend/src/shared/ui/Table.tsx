import type { HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

/**
 * Jadval vizual primitivlari — semantik markup + uslub. Ma'lumot va sahifalash/sortlash
 * mantig'i `shared/ui/DataTable.tsx` (server-side, `useServerTableState` bilan) orqali
 * ulanadi — `@tanstack/react-table` ISHLATILMAYDI (docs/10, 5.3-bo'lim; `DataTable.tsx`dagi
 * "Muhandislik qarori" izohiga qarang: paket eskirgan/mos kelmaydigan versiyada edi,
 * PM tasdig'i bilan bog'liqlikdan butunlay olib tashlandi).
 */

/**
 * `relative` ATAYLAB: jadval ichida `sr-only` elementlar bor (saralash yo'nalishini aytuvchi
 * `<span>`, `DataTable.tsx`). Tailwind ning `.sr-only` yordamchisi `position: absolute` beradi,
 * `<th>`/`<td>` esa pozitsiyalangan emas — natijada ularning "containing block"i bosh hujjat
 * (ICB) bo'lib qoladi va ular `overflow-x: auto` konteyneridan QOCHIB chiqadi: 390px da
 * gorizontal siljish 1px lik yashirin `<span>` sababli butun SAHIFAGA tarqaladi (P30-6:
 * `scrollWidth` 676 > 390). `relative` konteynerni "containing block"ka aylantiradi — endi
 * keng mazmun faqat SHU konteyner ichida siljiydi, `body` esa siljimaydi.
 */
export function Table({ className, ...props }: HTMLAttributes<HTMLTableElement>) {
  return (
    <div className="relative w-full overflow-x-auto rounded-2xl border border-line bg-paper-card">
      <table className={cn('w-full border-collapse text-left text-sm', className)} {...props} />
    </div>
  );
}

export function TableHeader({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead className={cn('bg-paper-deep', className)} {...props} />;
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody className={cn('divide-y divide-line', className)} {...props} />;
}

export function TableRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  // Qator ustiga kelganda `firuza-50` (#EAF7F7): `paper` (#FBF8F3) oq fonda deyarli
  // ko'rinmasdi, firuza tint esa brend rangida va aniq seziladi (matn kontrasti 16.54:1).
  return <tr className={cn('hover:bg-firuza-50', className)} {...props} />;
}

export function TableHead({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th
      scope="col"
      // `ink-muted` `paper-deep` fonida 4.15:1 — AA dan past, shu sabab `ink-soft` (8.59:1).
      className={cn('px-4 py-3 font-semibold text-ink-soft', className)}
      {...props}
    />
  );
}

export function TableCell({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn('px-4 py-3 text-ink', className)} {...props} />;
}
