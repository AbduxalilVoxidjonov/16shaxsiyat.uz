export interface SchoolNameBadgeProps {
  name: string;
}

/**
 * Maktab nomi — o'quvchi o'z maktabini darhol tanishi uchun yirik, ko'zga tashlanadigan
 * nishon (2026-09-23 egasi qarori: avval 11px `eyebrow` edi, maktab raqami ko'rinmasdi).
 */
export function SchoolNameBadge({ name }: SchoolNameBadgeProps) {
  return (
    <p className="inline-flex max-w-full items-center rounded-full border border-firuza-200 bg-firuza-50 px-5 py-2 font-display text-xl font-extrabold tracking-tight text-balance text-firuza-800 sm:text-2xl">
      {name}
    </p>
  );
}
