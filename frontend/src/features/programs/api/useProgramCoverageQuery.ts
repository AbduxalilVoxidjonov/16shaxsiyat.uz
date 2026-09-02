import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { AdminProgramDetail, AdminProgramListItem } from '../model/types';

const MAX_PROGRAMS = 100;

export interface ProgramCoverage {
  /**
   * `true` — nashr etilgan, faol, `Public` ko'rinishli kamida bitta dastur bor: bunday
   * holda **barcha** maktab kirishda test yecha oladi, chunki `Public` dastur har qanday
   * maktabga avtomatik ko'rinadi (`docs/06` §8 2026-09-02 qarori). Shu holatda hech qanday
   * maktab "dastursiz" bo'lolmaydi.
   */
  hasActivePublicProgram: boolean;
  /** `Public` dastur bo'lmasa: `Assigned` dasturlarga biriktirilgan maktab id'lari yig'indisi. */
  schoolIdsWithAssignedProgram: Set<string>;
}

/**
 * Maktab "dastursiz qoldi" ogohlantirishi (`prompts/35` 13-band, "eng qimmatli qism" —
 * "buni admin o'zi payqamaydi") uchun butun tizim bo'yicha qamrovni hisoblaydi.
 *
 * Bounded N+1: `Published && isActive` dasturlar (odatda o'nlab, ko'p emas — admin qo'lda
 * kurira qiladigan ro'yxat) bitta so'rovda olinadi; agar ular orasida `Public` bo'lmasa,
 * har birining biriktirilgan maktablar ro'yxati (`assignedSchoolIds`) alohida so'raladi.
 * `docs/06` §8 "xotirada agregatsiya taqiqlangan" qarori BACKEND SQL so'rovlariga tegishli —
 * bu yerda son cheklangan (`MAX_PROGRAMS`), admin tomonidan boshqariladigan katalog
 * bo'ylab klient-tomon agregatsiya, talab har bir dastur sahifasida ham takrorlanadi.
 */
export function useProgramCoverageQuery() {
  return useQuery({
    queryKey: ['programs', 'coverage'],
    queryFn: async ({ signal }) => {
      const listResult = await adminRequest<PagedResult<AdminProgramListItem>>(
        `/api/admin/programs?status=Published&isActive=true&pageSize=${String(MAX_PROGRAMS)}`,
        { signal },
      );

      const hasActivePublicProgram = listResult.items.some(
        (program) => program.visibility === 'Public',
      );

      if (hasActivePublicProgram) {
        return {
          hasActivePublicProgram,
          schoolIdsWithAssignedProgram: new Set<string>(),
        } satisfies ProgramCoverage;
      }

      const assignedPrograms = listResult.items.filter(
        (program) => program.visibility === 'Assigned',
      );
      const details = await Promise.all(
        assignedPrograms.map((program) =>
          adminRequest<AdminProgramDetail>(`/api/admin/programs/${program.id}`, { signal }),
        ),
      );

      const schoolIdsWithAssignedProgram = new Set<string>();
      for (const detail of details) {
        for (const schoolId of detail.assignedSchoolIds) {
          schoolIdsWithAssignedProgram.add(schoolId);
        }
      }

      return { hasActivePublicProgram, schoolIdsWithAssignedProgram } satisfies ProgramCoverage;
    },
    staleTime: 30_000,
  });
}
