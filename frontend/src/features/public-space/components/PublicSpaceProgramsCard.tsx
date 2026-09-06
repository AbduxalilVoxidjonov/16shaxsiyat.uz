import { useState } from 'react';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Select } from '@/shared/ui/Select';
import { programStateBadgeVariant, programStateLabelKey } from '@/shared/lib/programState';
import { useToast } from '@/shared/ui/useToast';
import { useAssignPublicSpaceProgram } from '../api/useAssignPublicSpaceProgram';
import { useProgramOptionsQuery } from '../api/useProgramOptionsQuery';
import { useUnassignPublicSpaceProgram } from '../api/useUnassignPublicSpaceProgram';
import type { PublicSpaceProgramDto } from '../model/types';

export interface PublicSpaceProgramsCardProps {
  programs: readonly PublicSpaceProgramDto[];
}

/**
 * Biriktirilgan dasturlar — backend AYNAN mavjud `school_programs` mexanizmini qayta
 * ishlatadi (yangi jadval/mantiq YO'Q), shu sabab bu yerda ham "biriktirish/olib tashlash"
 * dan boshqa amal yo'q.
 *
 * Har qatorda dasturning HOLATI ham ko'rsatiladi — `Active`dan boshqa har qanday holatdagi
 * dastur test boshlashga yaramaydi (2026-09-03 hodisasi).
 *
 * **2026-09-06:** belgi endi BITTA (`state`), `features/programs` bilan AYNAN bir xil
 * matn va rangda (`@/shared/lib/programState`). Ilgari bu yerda alohida "Faol/O'chirilgan"
 * belgisi bor edi va u dastur holatidan (`Draft`/`Archived`) mustaqil ravishda chizilardi.
 */
export function PublicSpaceProgramsCard({ programs }: PublicSpaceProgramsCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const [selectedProgramId, setSelectedProgramId] = useState('');

  const programOptionsQuery = useProgramOptionsQuery();
  const assignMutation = useAssignPublicSpaceProgram();
  const unassignMutation = useUnassignPublicSpaceProgram();

  const assignedIds = new Set(programs.map((program) => program.id));
  const available = (programOptionsQuery.data?.items ?? []).filter(
    (option) => !assignedIds.has(option.id),
  );

  async function handleAssign() {
    if (!selectedProgramId) return;
    try {
      await assignMutation.mutateAsync(selectedProgramId);
      setSelectedProgramId('');
      toast.show({ variant: 'success', title: t('publicSpace.programs.addSuccess') });
    } catch {
      toast.show({ variant: 'danger', title: t('publicSpace.programs.mutationError') });
    }
  }

  async function handleRemove(programId: string) {
    try {
      await unassignMutation.mutateAsync(programId);
      toast.show({ variant: 'success', title: t('publicSpace.programs.removeSuccess') });
    } catch {
      toast.show({ variant: 'danger', title: t('publicSpace.programs.mutationError') });
    }
  }

  return (
    <Card title={t('publicSpace.programs.title')}>
      {programs.length === 0 ? (
        <div className="rounded-xl border border-dashed border-line px-4 py-6 text-center">
          <p className="font-medium text-neutral-900">{t('publicSpace.programs.emptyTitle')}</p>
          <p className="mt-1 text-sm text-neutral-600">
            {t('publicSpace.programs.emptyDescription')}
          </p>
        </div>
      ) : (
        <ul className="flex flex-col gap-2" data-testid="public-space-programs">
          {programs.map((program) => (
            <li
              key={program.id}
              className="flex flex-wrap items-center gap-3 rounded-xl border border-line px-3 py-2"
            >
              <span className="font-mono text-xs text-neutral-500">{program.code}</span>
              <span className="flex-1 font-medium text-neutral-900">{program.nameUz}</span>

              <Badge variant={programStateBadgeVariant(program.state)}>
                {t(programStateLabelKey(program.state))}
              </Badge>

              <span className="text-xs text-neutral-500">
                {t('publicSpace.programs.testCount', { count: program.testCount })}
              </span>

              {!program.hasUsableTest && (
                <Badge variant="warning">{t('publicSpace.programs.noUsableTest')}</Badge>
              )}

              <Button
                variant="ghost"
                size="sm"
                aria-label={t('publicSpace.programs.removeAria', { name: program.nameUz })}
                onClick={() => void handleRemove(program.id)}
                isLoading={
                  unassignMutation.isPending && unassignMutation.variables === program.id
                }
              >
                <Trash2 size={16} aria-hidden="true" />
                {t('publicSpace.programs.remove')}
              </Button>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="w-full sm:max-w-xs">
          <Select
            label={t('publicSpace.programs.addLabel')}
            placeholder={t('publicSpace.programs.addPlaceholder')}
            value={selectedProgramId}
            onChange={(event) => setSelectedProgramId(event.target.value)}
            options={available.map((option) => ({
              value: option.id,
              label: `${option.code} — ${option.nameUz}`,
            }))}
            disabled={available.length === 0}
            hint={available.length === 0 ? t('publicSpace.programs.allAssigned') : undefined}
          />
        </div>
        <Button
          onClick={() => void handleAssign()}
          disabled={!selectedProgramId}
          isLoading={assignMutation.isPending}
        >
          {t('publicSpace.programs.addCta')}
        </Button>
      </div>
    </Card>
  );
}
