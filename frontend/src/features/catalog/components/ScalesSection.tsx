import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { EmptyState } from '@/shared/ui/EmptyState';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import { useCatalogScalesQuery, useDeleteCatalogScale } from '../api/useCatalogScales';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import type { CatalogScaleItem } from '../model/types';
import { ScaleDialog } from './ScaleDialog';

export interface ScalesSectionProps {
  testId: string;
}

/**
 * Shkalalar bo'limi — `docs/07` 3.4-bo'lim: "Shkalalar (faqat `Custom`)". Tizim
 * metodikasida bu bo'lim umuman render qilinmaydi (chaqiruvchi sahifa hal qiladi).
 */
export function ScalesSection({ testId }: ScalesSectionProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();

  const scalesQuery = useCatalogScalesQuery(testId);
  const deleteScale = useDeleteCatalogScale(testId);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<CatalogScaleItem | null>(null);
  const [pendingDelete, setPendingDelete] = useState<CatalogScaleItem | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const scales = [...(scalesQuery.data ?? [])].sort((a, b) => a.displayOrder - b.displayOrder);

  async function handleDelete() {
    if (!pendingDelete) return;
    setDeleteError(null);
    try {
      await deleteScale.mutateAsync(pendingDelete.id);
      toast.show({ variant: 'success', title: t('catalog.scaleDelete.successTitle') });
      setPendingDelete(null);
    } catch (caught) {
      setDeleteError(toErrorMessage(caught));
    }
  }

  return (
    <Card
      title={t('catalog.detail.scalesHeading')}
      actions={
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            setEditing(null);
            setDialogOpen(true);
          }}
        >
          <Plus size={14} aria-hidden="true" />
          {t('catalog.actions.addScale')}
        </Button>
      }
    >
      {scalesQuery.isPending && <Skeleton className="h-24 w-full" />}
      {scalesQuery.isError && <ErrorState onRetry={() => void scalesQuery.refetch()} />}

      {!scalesQuery.isPending && !scalesQuery.isError && scales.length === 0 && (
        <EmptyState
          title={t('catalog.detail.scalesEmptyTitle')}
          description={t('catalog.detail.scalesEmptyDescription')}
        />
      )}

      {!scalesQuery.isPending && !scalesQuery.isError && scales.length > 0 && (
        <ul className="flex flex-col gap-2">
          {scales.map((scale) => (
            <li
              key={scale.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-neutral-200 p-3"
            >
              <div>
                <p className="text-sm font-medium text-neutral-900">
                  {scale.nameUz} <span className="text-neutral-500">({scale.code})</span>
                </p>
                {scale.descriptionUz && (
                  <p className="text-sm text-neutral-600">{scale.descriptionUz}</p>
                )}
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="neutral">
                  {t('catalog.scalesList.questionCount', { count: scale.questionCount })}
                </Badge>
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('catalog.actions.editScaleAria', { code: scale.code })}
                  onClick={() => {
                    setEditing(scale);
                    setDialogOpen(true);
                  }}
                >
                  <Pencil size={14} aria-hidden="true" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('catalog.actions.deleteScaleAria', { code: scale.code })}
                  onClick={() => {
                    setDeleteError(null);
                    setPendingDelete(scale);
                  }}
                >
                  <Trash2 size={14} className="text-danger-600" aria-hidden="true" />
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {dialogOpen && (
        <ScaleDialog
          open
          testId={testId}
          scale={editing}
          onClose={() => {
            setDialogOpen(false);
          }}
        />
      )}

      {pendingDelete && (
        <ConfirmDialog
          open
          title={t('catalog.scaleDelete.title')}
          description={`${pendingDelete.nameUz} (${pendingDelete.code})`}
          warning={t('catalog.scaleDelete.warning')}
          confirmLabel={t('catalog.actions.deleteScale')}
          isConfirming={deleteScale.isPending}
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
