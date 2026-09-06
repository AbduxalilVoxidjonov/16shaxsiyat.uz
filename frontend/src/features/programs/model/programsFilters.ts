import { isProgramState, type ProgramState } from './types';

export interface ProgramsFilterValues {
  search: string;
  /** `''` — hammasi. */
  state: ProgramState | '';
}

/**
 * URL query'dan joriy filtr qiymatlarini o'qiydi — `SchoolsFilterValues`/`readSchoolsFilters`
 * naqshiga o'xshash.
 *
 * **2026-09-06:** ilgari ikkita mustaqil filtr bor edi (`status` va `active`) va ular
 * `status=Archived&active=true` kabi hech qachon natija bermaydigan juftlikni tanlash
 * imkonini berardi. Endi bitta `state` filtri — eski parametrlar O'QILMAYDI (bu ichki admin
 * bo'lim, saqlangan havolalar uchun orqaga moslik talab qilinmaydi; noma'lum parametr
 * shunchaki e'tiborsiz qoladi va ro'yxat filtrsiz ochiladi).
 */
export function readProgramsFilters(searchParams: URLSearchParams): ProgramsFilterValues {
  const state = searchParams.get('state');
  return {
    search: searchParams.get('search') ?? '',
    state: isProgramState(state) ? state : '',
  };
}
