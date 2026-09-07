import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { Globe } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
// Ommaviy makon mutatsiyalari `features/public-space` dan QAYTA ISHLATILADI (nusxa yo'q):
// backendda ham bitta endpoint (`POST | DELETE /api/admin/public-space/programs/{programId}`),
// frontendda ham bitta hook. `docs/10` §2 ("feature'lar bir-birini import qilmaydi") ga
// ongli istisno — `features/dashboard → features/schools` bilan bir xil sabab: ikkinchi nusxa
// muqarrar ravishda birinchisidan ajralib ketardi (`schoolPanel` 2026-09-03 saboqlari).
import { useAssignPublicSpaceProgram } from '@/features/public-space/api/useAssignPublicSpaceProgram';
import { useUnassignPublicSpaceProgram } from '@/features/public-space/api/useUnassignPublicSpaceProgram';
import { PUBLIC_SPACE_QUERY_KEYS } from '@/features/public-space/api/publicSpaceKeys';
import { PROGRAMS_QUERY_KEYS } from '../api/programsKeys';

export interface PublicSpaceAssignmentBlockProps {
  programId: string;
  programName: string;
  /** `AdminProgramDetailDto.state` — `Draft` · `Active` · `Paused` · `Archived`. */
  state: string;
  isAssignedToPublicSpace: boolean;
}

/** `['public-space', …]` — bo'limning barcha so'rovlari (detal, dastur variantlari). */
const PUBLIC_SPACE_ROOT_KEY = PUBLIC_SPACE_QUERY_KEYS.detail()[0];

/**
 * Dastur detalidagi "Ommaviy makon" bloki (2026-09-07, egasining talabi) — maktablar
 * ro'yxatidan ALOHIDA: ommaviy makon "maktab" emas (`AdminSchoolScope.SchoolsOnly` uni
 * maktablar ro'yxatidan chiqarib tashlaydi), shu sabab bu blokda "maktab" so'zi ishlatilmaydi.
 *
 * Holat `AdminProgramDetailDto.isAssignedToPublicSpace` dan keladi (backend `school_programs`
 * dagi ommaviy qatorni `assignedSchoolIds` dan ajratib beradi).
 *
 * **"Faqat faol dastur biriktiriladi"** — MIJOZ tomonidagi cheklov: backend endpointi dastur
 * holatini tekshirmaydi (`Draft` ham `200` bilan biriktiriladi — integratsiya testida
 * qulflangan), lekin `Active` bo'lmagan dastur ommaviy foydalanuvchiga ko'rinmaydi
 * (`ProgramAvailability`: `Published && IsActive`). Foydasiz biriktirmani taklif qilmaslik
 * uchun tugma o'chiriladi va sabab yoziladi. OLIB TASHLASH holatdan qat'i nazar mumkin —
 * aks holda to'xtatilgan dastur ommaviy makonda "qulflanib" qolardi.
 *
 * Olib tashlash — tasdiq oynasi orqali (`DeactivateProgramDialog` naqshi): ommaviy oqimda
 * bu oxirgi dastur bo'lishi mumkin va u yo'qolsa hech kim test boshlay olmaydi.
 */
export function PublicSpaceAssignmentBlock({
  programId,
  programName,
  state,
  isAssignedToPublicSpace,
}: PublicSpaceAssignmentBlockProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const assign = useAssignPublicSpaceProgram();
  const unassign = useUnassignPublicSpaceProgram();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const canAssign = state === 'Active';
  const reasonId = `public-space-assign-reason-${programId}`;

  async function invalidateAfterChange() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: PROGRAMS_QUERY_KEYS.detail(programId) }),
      queryClient.invalidateQueries({ queryKey: ['programs', 'list'] }),
      queryClient.invalidateQueries({ queryKey: [PUBLIC_SPACE_ROOT_KEY] }),
    ]);
  }

  async function handleAssign() {
    try {
      await assign.mutateAsync(programId);
      await invalidateAfterChange();
      toast.show({ variant: 'success', title: t('programs.publicSpace.assignSuccess') });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('programs.publicSpace.assignError'),
      });
    }
  }

  function closeDialog() {
    setDialogError(null);
    unassign.reset();
    setConfirmOpen(false);
  }

  async function handleUnassignConfirm() {
    setDialogError(null);
    try {
      await unassign.mutateAsync(programId);
      await invalidateAfterChange();
      toast.show({ variant: 'success', title: t('programs.publicSpace.unassignSuccess') });
      setConfirmOpen(false);
    } catch (caught) {
      setDialogError(
        caught instanceof AppError ? caught.message : t('programs.publicSpace.unassignError'),
      );
    }
  }

  return (
    <section
      aria-labelledby={`public-space-heading-${programId}`}
      data-testid="public-space-assignment"
      className="flex flex-wrap items-start gap-3 rounded-2xl border border-firuza-200 bg-firuza-50 px-4 py-3"
    >
      <span
        className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-xl bg-white text-firuza-700"
        aria-hidden="true"
      >
        <Globe size={18} />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3
            id={`public-space-heading-${programId}`}
            className="text-sm font-semibold text-ink"
          >
            {t('programs.publicSpace.heading')}
          </h3>
          <Badge variant={isAssignedToPublicSpace ? 'success' : 'neutral'}>
            {isAssignedToPublicSpace
              ? t('programs.publicSpace.assignedBadge')
              : t('programs.publicSpace.unassignedBadge')}
          </Badge>
        </div>
        <p className="mt-1 text-xs text-neutral-600">{t('programs.publicSpace.note')}</p>
        {!isAssignedToPublicSpace && !canAssign && (
          <p id={reasonId} className="mt-1 text-xs font-medium text-zarhal-800" role="note">
            {t('programs.publicSpace.inactiveReason')}
          </p>
        )}
      </div>

      {isAssignedToPublicSpace ? (
        <Button size="sm" variant="outline" onClick={() => setConfirmOpen(true)}>
          {t('programs.publicSpace.unassignCta')}
        </Button>
      ) : (
        <Button
          size="sm"
          onClick={() => void handleAssign()}
          disabled={!canAssign}
          aria-describedby={canAssign ? undefined : reasonId}
          isLoading={assign.isPending}
        >
          {t('programs.publicSpace.assignCta')}
        </Button>
      )}

      {confirmOpen && (
        <ConfirmDialog
          open
          onClose={closeDialog}
          onConfirm={() => void handleUnassignConfirm()}
          title={t('programs.publicSpace.unassignDialog.title')}
          description={programName}
          warning={t('programs.publicSpace.unassignDialog.description')}
          confirmLabel={t('programs.publicSpace.unassignDialog.confirmCta')}
          isConfirming={unassign.isPending}
          error={dialogError ?? undefined}
        />
      )}
    </section>
  );
}
