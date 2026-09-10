import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, Lock, Pencil, Plus, Trash2 } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import {
  useCatalogSectionsQuery,
  useDeleteCatalogSection,
  useReorderCatalogSections,
} from '../api/useCatalogSections';
import { useCatalogQuestionsQuery } from '../api/useCatalogTestDetailQuery';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import type { CatalogSection } from '../model/types';
import { SectionDialog } from './SectionDialog';

export interface SectionsSectionProps {
  testId: string;
  isSystem: boolean;
  /** `docs/18` B-2: bo'limlar faqat `Survey` anketalarda. `Scored`da bo'lim CRUD yashiriladi. */
  scoringMode: 'Scored' | 'Survey';
}

/**
 * Bo'limlar bo'limi — `docs/18` §6.3, `ScalesSection.tsx` naqshiga ergashadi. Tizim
 * metodikasida (BR-8, B-3) va `Scored` anketalarda (B-2) umuman render qilinmaydi —
 * chaqiruvchi (`CatalogTestDetailPage`) buni hal qiladi, bu yerda faqat ogohlantirish matni.
 */
export function SectionsSection({ testId, isSystem, scoringMode }: SectionsSectionProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();

  const sectionsQuery = useCatalogSectionsQuery(testId);
  // Bir xil so'rov kaliti `QuestionsSection`/`BranchingPreview` bilan — TanStack Query keshi
  // tufayli qo'shimcha tarmoq chaqiruvi bo'lmaydi.
  const questionsQuery = useCatalogQuestionsQuery(testId);
  const reorderSections = useReorderCatalogSections(testId);
  const deleteSection = useDeleteCatalogSection(testId);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<CatalogSection | null>(null);
  const [pendingDelete, setPendingDelete] = useState<CatalogSection | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  if (scoringMode === 'Scored') {
    return (
      <Card title={t('catalog.sections.heading')}>
        <p className="text-sm text-neutral-500">{t('catalog.sections.notAllowedInScored')}</p>
      </Card>
    );
  }

  const sections = [...(sectionsQuery.data ?? [])].sort((a, b) => a.displayOrder - b.displayOrder);
  const questions = questionsQuery.data ?? [];
  const questionCountBySection = new Map<string, number>();
  for (const question of questions) {
    if (!question.sectionId) continue;
    questionCountBySection.set(
      question.sectionId,
      (questionCountBySection.get(question.sectionId) ?? 0) + 1,
    );
  }

  async function handleMove(index: number, offset: -1 | 1) {
    const current = sections[index];
    const neighbour = sections[index + offset];
    if (!current || !neighbour) return;
    try {
      await reorderSections.mutateAsync([
        { id: current.id, displayOrder: neighbour.displayOrder },
        { id: neighbour.id, displayOrder: current.displayOrder },
      ]);
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  }

  async function handleDelete() {
    if (!pendingDelete) return;
    setDeleteError(null);
    try {
      await deleteSection.mutateAsync(pendingDelete.id);
      toast.show({ variant: 'success', title: t('catalog.sections.deleteSuccessTitle') });
      setPendingDelete(null);
    } catch (caught) {
      setDeleteError(toErrorMessage(caught));
    }
  }

  return (
    <Card
      title={t('catalog.sections.heading')}
      actions={
        !isSystem && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => {
              setEditing(null);
              setDialogOpen(true);
            }}
          >
            <Plus size={14} aria-hidden="true" />
            {t('catalog.sections.addSection')}
          </Button>
        )
      }
    >
      {isSystem && (
        <p className="mb-3 flex items-start gap-2 rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
          <Lock size={14} className="mt-0.5 shrink-0 text-neutral-400" aria-hidden="true" />
          {t('catalog.sections.systemNotice')}
        </p>
      )}

      {sectionsQuery.isPending && <Skeleton className="h-24 w-full" />}
      {sectionsQuery.isError && <ErrorState onRetry={() => void sectionsQuery.refetch()} />}

      {!sectionsQuery.isPending && !sectionsQuery.isError && sections.length === 0 && (
        <EmptyState
          title={t('catalog.sections.emptyTitle')}
          description={t('catalog.sections.emptyDescription')}
        />
      )}

      {!sectionsQuery.isPending && !sectionsQuery.isError && sections.length > 0 && (
        <ul className="flex flex-col gap-2">
          {sections.map((section, index) => (
            <li
              key={section.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-neutral-200 p-3"
            >
              <div>
                <p className="text-sm font-medium text-neutral-900">
                  {section.titleUz} <span className="text-neutral-500">({section.code})</span>
                </p>
                {section.descriptionUz && (
                  <p className="text-sm text-neutral-600">{section.descriptionUz}</p>
                )}
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="neutral">
                  {t('catalog.sections.questionCount', {
                    count: questionCountBySection.get(section.id) ?? 0,
                  })}
                </Badge>
                <Badge variant={section.visibility ? 'primary' : 'neutral'}>
                  {section.visibility
                    ? t('catalog.sections.hasVisibility')
                    : t('catalog.sections.noVisibility')}
                </Badge>
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('catalog.sections.moveUpAria', { code: section.code })}
                  disabled={index === 0 || reorderSections.isPending}
                  onClick={() => void handleMove(index, -1)}
                >
                  <ArrowUp size={14} aria-hidden="true" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('catalog.sections.moveDownAria', { code: section.code })}
                  disabled={index === sections.length - 1 || reorderSections.isPending}
                  onClick={() => void handleMove(index, 1)}
                >
                  <ArrowDown size={14} aria-hidden="true" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('catalog.sections.editAria', { code: section.code })}
                  onClick={() => {
                    setEditing(section);
                    setDialogOpen(true);
                  }}
                >
                  <Pencil size={14} aria-hidden="true" />
                </Button>
                {!isSystem && (
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={t('catalog.sections.deleteAria', { code: section.code })}
                    onClick={() => {
                      setDeleteError(null);
                      setPendingDelete(section);
                    }}
                  >
                    <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {dialogOpen && (
        <SectionDialog
          key={editing?.id ?? 'new'}
          open
          testId={testId}
          section={editing}
          sections={sections}
          questions={questions}
          onClose={() => {
            setDialogOpen(false);
          }}
        />
      )}

      {pendingDelete && (
        <ConfirmDialog
          open
          title={t('catalog.sections.deleteTitle')}
          description={`${pendingDelete.titleUz} (${pendingDelete.code})`}
          warning={t('catalog.sections.deleteWarning')}
          confirmLabel={t('catalog.actions.delete')}
          isConfirming={deleteSection.isPending}
          error={deleteError ?? undefined}
          onClose={() => {
            setPendingDelete(null);
            setDeleteError(null);
          }}
          onConfirm={() => void handleDelete()}
        />
      )}
    </Card>
  );
}
