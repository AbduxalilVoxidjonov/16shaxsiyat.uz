import { PROGRAM_STATUS_VALUES, type ProgramStatus } from './types';

export interface ProgramsFilterValues {
  search: string;
  /** `''` — hammasi. */
  status: ProgramStatus | '';
  /** `''` — hammasi, `'true'`/`'false'` — faol/nofaol. */
  active: '' | 'true' | 'false';
}

function isProgramStatus(value: string | null): value is ProgramStatus {
  return value !== null && (PROGRAM_STATUS_VALUES as readonly string[]).includes(value);
}

/** URL query'dan joriy filtr qiymatlarini o'qiydi — `SchoolsFilterValues`/`readSchoolsFilters` naqshiga o'xshash. */
export function readProgramsFilters(searchParams: URLSearchParams): ProgramsFilterValues {
  const status = searchParams.get('status');
  const active = searchParams.get('active');
  return {
    search: searchParams.get('search') ?? '',
    status: isProgramStatus(status) ? status : '',
    active: active === 'true' || active === 'false' ? active : '',
  };
}
