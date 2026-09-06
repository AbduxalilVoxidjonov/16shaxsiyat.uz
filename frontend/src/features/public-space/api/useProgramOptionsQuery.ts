import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * Dastur tanlash uchun yengil DTO. `features/programs` ning to'liq tipi qayta
 * ISHLATILMAYDI — `docs/10` 2-bo'lim: feature'lar bir-birini import qilmaydi. Shu sabab
 * mavjud `GET /api/admin/programs` endpointiga alohida, minimal shaklda murojaat qilinadi
 * (`features/students/api/useSchoolOptionsQuery.ts` bilan bir xil naqsh).
 */
export interface ProgramOption {
  id: string;
  code: string;
  nameUz: string;
  /** Yagona holat (`ProgramState`) — `Draft` · `Active` · `Paused` · `Archived`. */
  state: string;
}

const PROGRAM_OPTIONS_PAGE_SIZE = 100;

/** Biriktirish uchun tanlanadigan dasturlar — katalog kichik (o'nlab yozuv). */
export function useProgramOptionsQuery() {
  return useQuery({
    queryKey: PUBLIC_SPACE_QUERY_KEYS.programOptions(),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<ProgramOption>>(
        `/api/admin/programs?page=1&pageSize=${String(PROGRAM_OPTIONS_PAGE_SIZE)}&sort=nameUz`,
        { signal },
      ),
    staleTime: 30_000,
  });
}
