import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { components } from '@/shared/api/schema';

/** `GET /api/admin/programs/{id}/impact` javobi — `docs/07` 3.1 (2026-09-03). */
export type ProgramImpact = components['schemas']['AdminProgramImpactDto'];

/**
 * Dastur ustidagi amal qaysi maktablarni havolasiz qoldirishini OLDINDAN so'raydi.
 *
 * `deactivate` — dasturni o'chirish (`toggle-active`), `archive` — arxivlash,
 * `makeAssigned` — ko'rinishni `Public` dan `Assigned` ga o'zgartirish.
 *
 * Hisob BACKENDDA (bitta agregat so'rov, N+1 yo'q) va ommaviy handler mezoni bilan bitta
 * manbadan — tasdiq oynasi va o'quvchi ko'radigan haqiqat ajralib ketmaydi.
 */
export function useProgramImpactQuery(
  programId: string | null,
  action: 'deactivate' | 'archive' | 'makeAssigned',
  enabled: boolean,
) {
  return useQuery({
    queryKey: ['programs', 'impact', programId, action],
    queryFn: ({ signal }) =>
      adminRequest<ProgramImpact>(
        `/api/admin/programs/${String(programId)}/impact?action=${action}`,
        { signal },
      ),
    enabled: enabled && programId !== null,
    // Tasdiq oynasi HAR SAFAR yangi ma'lumot ko'rsatishi kerak — eskirgan son bilan
    // "hech kim ta'sirlanmaydi" deyish xavfli.
    staleTime: 0,
  });
}
