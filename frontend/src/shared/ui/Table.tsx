import type { HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

/**
 * Jadval vizual primitivlari — semantik markup + uslub. Ma'lumot va sahifalash/sortlash
 * mantig'i `shared/ui/DataTable.tsx` (server-side, `useServerTableState` bilan) orqali
 * ulanadi — `@tanstack/react-table` ISHLATILMAYDI (docs/10, 5.3-bo'lim; `DataTable.tsx`dagi
 * "Muhandislik qarori" izohiga qarang: paket eskirgan/mos kelmaydigan versiyada edi,
 * PM tasdig'i bilan bog'liqlikdan butunlay olib tashlandi).
 */

export function Table({ className, ...props }: HTMLAttributes<HTMLTableElement>) {
  return (
    <div className="w-full overflow-x-auto rounded-xl border border-neutral-200">
      <table className={cn('w-full border-collapse text-left text-sm', className)} {...props} />
    </div>
  );
}

export function TableHeader({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead className={cn('bg-neutral-50', className)} {...props} />;
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody className={cn('divide-y divide-neutral-100', className)} {...props} />;
}

export function TableRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  return <tr className={cn('hover:bg-neutral-50', className)} {...props} />;
}

export function TableHead({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th
      scope="col"
      className={cn('px-4 py-3 font-medium text-neutral-500', className)}
      {...props}
    />
  );
}

export function TableCell({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn('px-4 py-3 text-neutral-900', className)} {...props} />;
}
