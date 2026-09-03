import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ArrowLeft, Lock, Pencil, Plus, Power } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { ROUTES } from '@/shared/config/routes';
import { useProgramQuery } from '../api/useProgramQuery';
import { useToggleProgramActive } from '../api/useProgramLifecycleMutations';
import { useCatalogTestOptionsQuery } from '../api/useCatalogTestOptionsQuery';
import { ProgramFormDialog } from '../components/ProgramFormDialog';
import { PublishProgramDialog } from '../components/PublishProgramDialog';
import { ArchiveProgramDialog } from '../components/ArchiveProgramDialog';
import { DeactivateProgramDialog } from '../components/DeactivateProgramDialog';
import { AddTestDialog } from '../components/AddTestDialog';
import { ProgramTestsList } from '../components/ProgramTestsList';
import { SchoolAssignmentPanel } from '../components/SchoolAssignmentPanel';
import { PROGRAM_STATUS_BADGE_VARIANT } from '../model/types';
import { computeProgramDuration, hasFullMaturityBattery } from '../model/programComputations';

/**
 * Dastur detali — `prompts/35` C8–C11-band. Bitta sahifada: asosiy ma'lumot, holat
 * o'tishlari (nashr/arxiv/faollik), tarkib (testlar) va maktablarga biriktirish.
 */
export default function ProgramDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();

  const programQuery = useProgramQuery(id ?? null);
  const catalogOptionsQuery = useCatalogTestOptionsQuery(true);
  const toggleActive = useToggleProgramActive();

  usePageTitle(programQuery.data?.nameUz ?? t('pages.programDetail.title'));

  const [editOpen, setEditOpen] = useState(false);
  const [publishOpen, setPublishOpen] = useState(false);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [addTestOpen, setAddTestOpen] = useState(false);
  // 2026-09-03: O'CHIRISH endi tasdiq oynasidan o'tadi (nechta maktab havolasiz qolishi
  // ko'rsatiladi). QAYTA YOQISH zararsiz — u avvalgidek bir bosishda bajariladi.
  const [deactivateOpen, setDeactivateOpen] = useState(false);

  async function handleToggleActive() {
    if (!id) return;
    try {
      await toggleActive.mutateAsync(id);
      toast.show({ variant: 'success', title: t('programs.toggleActive.success') });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('programs.toggleActive.error'),
      });
    }
  }

  if (programQuery.isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (programQuery.isError || !programQuery.data) {
    return (
      <ErrorState
        title={t('programs.detail.notFoundTitle')}
        description={t('programs.detail.notFoundDescription')}
        onRetry={() => void programQuery.refetch()}
      />
    );
  }

  const program = programQuery.data;
  const duration = computeProgramDuration(program.tests, catalogOptionsQuery.data);
  const hasBattery = hasFullMaturityBattery(program.tests);

  return (
    <div className="flex flex-col gap-4">
      <button
        type="button"
        onClick={() => navigate(ROUTES.admin.programs)}
        className="flex w-fit items-center gap-1.5 text-sm text-neutral-500 hover:text-neutral-900"
      >
        <ArrowLeft size={16} aria-hidden="true" />
        {t('common.back')}
      </button>

      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="flex items-center gap-2">
              {program.isSystem && (
                <Lock size={16} className="text-neutral-400" aria-hidden="true" />
              )}
              <h1 className="text-xl font-semibold text-neutral-900">{program.nameUz}</h1>
            </div>
            <p className="text-sm text-neutral-500">{program.code}</p>
            {program.descriptionUz && (
              <p className="mt-2 text-sm text-neutral-600">{program.descriptionUz}</p>
            )}
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <Badge
                variant={
                  PROGRAM_STATUS_BADGE_VARIANT[
                    program.status as keyof typeof PROGRAM_STATUS_BADGE_VARIANT
                  ] ?? 'neutral'
                }
              >
                {t(`programs.status.${program.status.toLowerCase()}`)}
              </Badge>
              <Badge variant={program.isActive ? 'success' : 'neutral'}>
                {program.isActive
                  ? t('programs.statusBadge.active')
                  : t('programs.statusBadge.inactive')}
              </Badge>
              <Badge variant="neutral">
                {program.visibility === 'Public'
                  ? t('programs.visibility.public')
                  : t('programs.visibility.assigned')}
              </Badge>
              <span className="text-sm text-neutral-500">
                {duration.isComplete
                  ? t('programs.detail.durationSummary', {
                      questions: duration.totalQuestions,
                      minutes: duration.totalMinutes,
                    })
                  : t('programs.detail.durationUnknown')}
              </span>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
              <Pencil size={14} aria-hidden="true" />
              {t('programs.actions.edit')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              isLoading={toggleActive.isPending}
              onClick={() =>
                program.isActive ? setDeactivateOpen(true) : void handleToggleActive()
              }
            >
              <Power size={14} aria-hidden="true" />
              {program.isActive ? t('programs.actions.deactivate') : t('programs.actions.activate')}
            </Button>
            {program.status === 'Draft' && (
              <Button size="sm" onClick={() => setPublishOpen(true)}>
                {t('programs.actions.publish')}
              </Button>
            )}
            {program.status !== 'Archived' && (
              <Button variant="danger" size="sm" onClick={() => setArchiveOpen(true)}>
                {t('programs.actions.archive')}
              </Button>
            )}
          </div>
        </div>

        {!hasBattery && (
          <p
            className="mt-4 flex items-start gap-2 rounded-lg bg-warning-50 p-3 text-sm text-warning-700"
            role="status"
          >
            <AlertTriangle size={16} className="mt-0.5 shrink-0" aria-hidden="true" />
            {t('programs.detail.noBatteryNotice')}
          </p>
        )}
      </Card>

      <Card
        title={t('programs.detail.testsHeading')}
        actions={
          !program.isSystem && (
            <Button size="sm" variant="outline" onClick={() => setAddTestOpen(true)}>
              <Plus size={14} aria-hidden="true" />
              {t('programs.actions.addTest')}
            </Button>
          )
        }
      >
        <ProgramTestsList
          programId={program.id}
          tests={program.tests}
          isSystem={program.isSystem}
          catalogOptions={catalogOptionsQuery.data}
        />
      </Card>

      {program.visibility === 'Assigned' && (
        <Card title={t('programs.detail.schoolsHeading')}>
          <SchoolAssignmentPanel
            programId={program.id}
            assignedSchoolIds={program.assignedSchoolIds}
          />
        </Card>
      )}

      <ProgramFormDialog
        open={editOpen}
        programId={program.id}
        onClose={() => setEditOpen(false)}
      />

      {publishOpen && (
        <PublishProgramDialog
          open
          programId={program.id}
          programName={program.nameUz}
          tests={program.tests}
          catalogOptions={catalogOptionsQuery.data}
          onClose={() => setPublishOpen(false)}
        />
      )}

      {deactivateOpen && (
        <DeactivateProgramDialog
          open
          programId={program.id}
          programName={program.nameUz}
          onClose={() => setDeactivateOpen(false)}
        />
      )}

      {archiveOpen && (
        <ArchiveProgramDialog
          open
          programId={program.id}
          programName={program.nameUz}
          onClose={() => setArchiveOpen(false)}
        />
      )}

      {addTestOpen && (
        <AddTestDialog
          open
          programId={program.id}
          existingTests={program.tests}
          onClose={() => setAddTestOpen(false)}
        />
      )}
    </div>
  );
}
