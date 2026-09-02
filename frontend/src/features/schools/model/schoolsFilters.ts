export interface SchoolsFilterValues {
  search: string;
  region: string;
  /** `''` — hammasi, `'true'`/`'false'` — faol/nofaol. */
  active: '' | 'true' | 'false';
}

/**
 * URL query'dan joriy filtr qiymatlarini o'qiydi (`SchoolFiltersBar.tsx` va `SchoolsPage.tsx`
 * ikkalasida ham kerak — shu sabab alohida, komponent bo'lmagan faylda, react-refresh
 * "faqat komponent eksport qilinsin" qoidasi buzilmasligi uchun).
 */
export function readSchoolsFilters(searchParams: URLSearchParams): SchoolsFilterValues {
  const active = searchParams.get('active');
  return {
    search: searchParams.get('search') ?? '',
    region: searchParams.get('region') ?? '',
    active: active === 'true' || active === 'false' ? active : '',
  };
}
