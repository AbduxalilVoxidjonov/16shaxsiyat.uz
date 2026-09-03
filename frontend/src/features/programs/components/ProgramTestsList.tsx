import { useState } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, Lock, Trash2 } from 'lucide-react';
import { ROUTES } from '@/shared/config/routes';
import { Button } from '@/shared/ui/Button';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useRemoveProgramTest, useReorderProgramTests } from '../api/useProgramTestMutations';
import type { AdminProgramTestItem } from '../model/types';
import type { CatalogTestOption } from '../api/useCatalogTestOptionsQuery';

export interface ProgramTestsListProps {
  programId: string;
  tests: readonly AdminProgramTestItem[];
  isSystem: boolean;
  catalogOptions: readonly CatalogTestOption[] | undefined;
}

/**
 * Dastur tarkibidagi testlar — olib tashlash va tartiblash. `prompts/35` C8-band
 * ("sudrab tartiblash") — loyihada drag-and-drop kutubxonasi (masalan `@dnd-kit`) ULANMAGAN
 * (`package.json`da yo'q, yangi bog'liqlik qo'shish bu promptning qamrovidan tashqarida);
 * shu sabab yuqoriga/pastga tugmalari bilan teng huquqli, klaviatura bilan to'liq
 * ishlaydigan tartiblash qo'llanildi (docs/11 4-bo'lim, a11y talabi bilan ham mosroq).
 *
 * `isSystem` bo'lsa TAHRIRLASH amallari (olib tashlash/tartiblash/qo'shish) o'chirilgan va
 * qulf tushuntirilgan — `prompts/35` 3-band + `CLAUDE.md` 9a-qoida. KO'RISH esa har doim
 * ochiq: test nomi katalogdagi ichki sahifasiga havola (`/admin/catalog/tests/:id`), u yerda
 * savollar ro'yxati va (ruxsat etilgan doirada) tahrirlash bor. Ilgari tizim dasturida
 * hech qanday amal ko'rsatilmagani uchun shaxsiyat testini ro'yxatda ko'rib turib **ochib
 * bo'lmasdi** (egasining 2026-09-03 dagi xabari).
 */
export function ProgramTestsList({
  programId,
  tests,
  isSystem,
  catalogOptions,
}: ProgramTestsListProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const removeTest = useRemoveProgramTest();
  const reorderTests = useReorderProgramTests();
  const [removeTarget, setRemoveTarget] = useState<AdminProgramTestItem | null>(null);

  const byId = new Map((catalogOptions ?? []).map((option) => [option.id, option]));
  const sorted = [...tests].sort((a, b) => a.displayOrder - b.displayOrder);

  async function move(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= sorted.length) return;
    const reordered = [...sorted];
    const [moved] = reordered.splice(index, 1);
    if (!moved) return;
    reordered.splice(target, 0, moved);
    try {
      await reorderTests.mutateAsync({
        programId,
        payload: { testDefinitionIds: reordered.map((test) => test.testDefinitionId) },
      });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('programs.testsList.reorderError'),
      });
    }
  }

  async function handleConfirmRemove() {
    if (!removeTarget) return;
    try {
      await removeTest.mutateAsync({ programId, testDefinitionId: removeTarget.testDefinitionId });
      toast.show({ variant: 'success', title: t('programs.testsList.removeSuccess') });
      setRemoveTarget(null);
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('programs.testsList.removeError'),
      });
    }
  }

  if (sorted.length === 0) {
    return (
      <EmptyState
        title={t('programs.testsList.emptyTitle')}
        description={t('programs.testsList.emptyDescription')}
      />
    );
  }

  return (
    <div className="flex flex-col gap-2">
      {isSystem && (
        <p className="flex items-center gap-2 rounded-lg bg-neutral-50 px-3 py-2 text-sm text-neutral-600">
          <Lock size={14} aria-hidden="true" />
          {t('programs.testsList.systemLockedNotice')}
        </p>
      )}
      <ol className="flex flex-col gap-2">
        {sorted.map((test, index) => {
          const option = byId.get(test.testDefinitionId);
          return (
            <li
              key={test.testDefinitionId}
              className="flex items-center justify-between gap-3 rounded-lg border border-neutral-200 p-3"
            >
              <div className="min-w-0">
                <p className="text-sm font-medium text-neutral-900">
                  {index + 1}.{' '}
                  <Link
                    to={ROUTES.admin.catalogTestDetail(test.testDefinitionId)}
                    className="text-primary-700 hover:underline"
                  >
                    {test.nameUz}
                  </Link>
                </p>
                <p className="text-xs text-neutral-500">
                  {test.code}
                  {option
                    ? ` · ${t('programs.addTestDialog.questionsAndMinutes', {
                        questions: option.questionCount,
                        minutes: option.estimatedMinutes,
                      })}`
                    : ''}
                </p>
              </div>
              {!isSystem && (
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={t('programs.testsList.moveUp')}
                    disabled={index === 0 || reorderTests.isPending}
                    onClick={() => void move(index, -1)}
                  >
                    <ArrowUp size={16} aria-hidden="true" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={t('programs.testsList.moveDown')}
                    disabled={index === sorted.length - 1 || reorderTests.isPending}
                    onClick={() => void move(index, 1)}
                  >
                    <ArrowDown size={16} aria-hidden="true" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={t('programs.testsList.remove')}
                    onClick={() => setRemoveTarget(test)}
                    className="text-danger-600 hover:bg-danger-50"
                  >
                    <Trash2 size={16} aria-hidden="true" />
                  </Button>
                </div>
              )}
            </li>
          );
        })}
      </ol>

      {removeTarget && (
        <ConfirmDialog
          open
          onClose={() => setRemoveTarget(null)}
          onConfirm={() => void handleConfirmRemove()}
          title={t('programs.testsList.removeConfirmTitle')}
          description={removeTarget.nameUz}
          confirmLabel={t('programs.testsList.removeConfirmCta')}
          isConfirming={removeTest.isPending}
        />
      )}
    </div>
  );
}
