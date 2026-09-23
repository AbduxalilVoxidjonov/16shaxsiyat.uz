import { useState } from 'react';
import { Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Select } from '@/shared/ui/Select';
import { programStateBadgeVariant, programStateLabelKey } from '@/shared/lib/programState';
import { useToast } from '@/shared/ui/useToast';
import { useAssignPublicSpaceTest, useUnassignPublicSpaceTest } from '../api/useSetPublicSpaceTest';
import { useTestOptionsQuery, type TestOption } from '../api/useTestOptionsQuery';
import { useUnassignPublicSpaceProgram } from '../api/useUnassignPublicSpaceProgram';
import type { PublicSpaceProgramDto } from '../model/types';

export interface PublicSpaceProgramsCardProps {
  programs: readonly PublicSpaceProgramDto[];
}

/**
 * Biriktirilgan testlar (2026-09-23 egasi qarori: "Dasturlar" bo'limi olib tashlandi —
 * ommaviy makonga endi TEST biriktiriladi, `POST`/`DELETE /api/admin/public-space/tests/{testId}`).
 *
 * `programs[]` javobida har element — test dasturi (`testDefinitionId` bor, nomi = test nomi)
 * yoki ESKI dastur (`testDefinitionId == null`, masalan ko'p testli tizim dasturi). Eskisi
 * faqat "Eski dastur" belgisi va "Olib tashlash" tugmasi bilan ko'rsatiladi (eski
 * `DELETE programs/{programId}`); yangisini qo'shib bo'lmaydi.
 *
 * Har qatorda HOLAT ham ko'rsatiladi — `Active`dan boshqa holatdagi test boshlashga yaramaydi
 * (2026-09-03 hodisasi). Belgi `@/shared/lib/programState` dan — test ichidagi "Biriktirish"
 * kartasi bilan bir xil manba.
 */
export function PublicSpaceProgramsCard({ programs }: PublicSpaceProgramsCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const [selectedTestId, setSelectedTestId] = useState('');

  const testOptionsQuery = useTestOptionsQuery();
  const assignTest = useAssignPublicSpaceTest();
  const unassignTest = useUnassignPublicSpaceTest();
  const unassignLegacy = useUnassignPublicSpaceProgram();

  const assignedTestIds = new Set(
    programs.flatMap((program) => (program.testDefinitionId ? [program.testDefinitionId] : [])),
  );
  // Arxivlangan test biriktirilmaydi (`409 TEST_ARCHIVED`) — ro'yxatda umuman ko'rsatilmaydi.
  const available = (testOptionsQuery.data ?? []).filter(
    (option) => option.status !== 'Archived' && !assignedTestIds.has(option.id),
  );

  function optionLabel(option: TestOption): string {
    const base = `${option.code} — ${option.nameUz}`;
    if (option.status === 'Draft') return `${base} (${t('publicSpace.programs.draftSuffix')})`;
    if (!option.isActive) return `${base} (${t('publicSpace.programs.inactiveSuffix')})`;
    return base;
  }

  async function handleAssign() {
    if (!selectedTestId) return;
    try {
      await assignTest.mutateAsync(selectedTestId);
      setSelectedTestId('');
      toast.show({ variant: 'success', title: t('publicSpace.programs.addSuccess') });
    } catch {
      toast.show({ variant: 'danger', title: t('publicSpace.programs.mutationError') });
    }
  }

  async function handleRemove(program: PublicSpaceProgramDto) {
    try {
      if (program.testDefinitionId) {
        await unassignTest.mutateAsync(program.testDefinitionId);
      } else {
        await unassignLegacy.mutateAsync(program.id);
      }
      toast.show({ variant: 'success', title: t('publicSpace.programs.removeSuccess') });
    } catch {
      toast.show({ variant: 'danger', title: t('publicSpace.programs.mutationError') });
    }
  }

  function isRemoving(program: PublicSpaceProgramDto): boolean {
    return program.testDefinitionId
      ? unassignTest.isPending && unassignTest.variables === program.testDefinitionId
      : unassignLegacy.isPending && unassignLegacy.variables === program.id;
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
          {programs.map((program) => {
            const isLegacy = !program.testDefinitionId;
            return (
              <li
                key={program.id}
                className="flex flex-wrap items-center gap-3 rounded-xl border border-line px-3 py-2"
              >
                <span className="font-mono text-xs text-neutral-500">{program.code}</span>
                <span className="flex-1 font-medium text-neutral-900">{program.nameUz}</span>

                <Badge variant={programStateBadgeVariant(program.state)}>
                  {t(programStateLabelKey(program.state))}
                </Badge>

                {isLegacy && (
                  <Badge variant="neutral" title={t('publicSpace.programs.legacyHint')}>
                    {t('publicSpace.programs.legacyBadge')}
                  </Badge>
                )}

                {/* Eski (ko'p testli) dasturda anketalar soni mazmunli; test dasturida u doim 1. */}
                {isLegacy && (
                  <span className="text-xs text-neutral-500">
                    {t('publicSpace.programs.testCount', { count: program.testCount })}
                  </span>
                )}

                {!program.hasUsableTest && (
                  <Badge variant="warning">{t('publicSpace.programs.noUsableTest')}</Badge>
                )}

                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('publicSpace.programs.removeAria', { name: program.nameUz })}
                  onClick={() => void handleRemove(program)}
                  isLoading={isRemoving(program)}
                >
                  <Trash2 size={16} aria-hidden="true" />
                  {t('publicSpace.programs.remove')}
                </Button>
              </li>
            );
          })}
        </ul>
      )}

      <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="w-full sm:max-w-xs">
          <Select
            label={t('publicSpace.programs.addLabel')}
            placeholder={t('publicSpace.programs.addPlaceholder')}
            value={selectedTestId}
            onChange={(event) => setSelectedTestId(event.target.value)}
            options={available.map((option) => ({ value: option.id, label: optionLabel(option) }))}
            disabled={available.length === 0}
            hint={available.length === 0 ? t('publicSpace.programs.allAssigned') : undefined}
          />
        </div>
        <Button
          onClick={() => void handleAssign()}
          disabled={!selectedTestId}
          isLoading={assignTest.isPending}
        >
          {t('publicSpace.programs.addCta')}
        </Button>
      </div>
    </Card>
  );
}
