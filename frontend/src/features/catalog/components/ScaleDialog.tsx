import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Textarea } from '@/shared/ui/Textarea';
import { useToast } from '@/shared/ui/useToast';
import { useCreateCatalogScale, useUpdateCatalogScale } from '../api/useCatalogScales';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { createScaleFormSchema, type ScaleFormValues } from '../model/scaleFormSchema';
import type { CatalogScaleItem } from '../model/types';

export interface ScaleDialogProps {
  open: boolean;
  testId: string;
  /** `null` — yangi shkala; berilsa — shu shkalani tahrirlash. */
  scale: CatalogScaleItem | null;
  onClose: () => void;
}

const FORM_ID = 'catalog-scale-form';

const EMPTY_VALUES: ScaleFormValues = {
  code: '',
  nameUz: '',
  descriptionUz: '',
  displayOrder: 0,
};

/**
 * Shkala yaratish/tahrirlash — faqat `Custom` testlarda ko'rsatiladi.
 * `interpretationBands` UI'da tahrirlanmaydi, lekin tahrirlashda mavjud bandlar
 * o'zgarishsiz qaytariladi (backend ularni `?? []` bilan to'liq almashtiradi —
 * `api/useCatalogScales.ts` izohiga qarang).
 */
export function ScaleDialog({ open, testId, scale, onClose }: ScaleDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const isEdit = scale !== null;

  const createScale = useCreateCatalogScale(testId);
  const updateScale = useUpdateCatalogScale(testId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ScaleFormValues>({
    resolver: zodResolver(createScaleFormSchema(isEdit ? 'edit' : 'create')),
    defaultValues: EMPTY_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    reset(
      scale
        ? {
            code: scale.code,
            nameUz: scale.nameUz,
            descriptionUz: scale.descriptionUz ?? '',
            displayOrder: scale.displayOrder,
          }
        : EMPTY_VALUES,
    );
  }, [open, scale, reset]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (scale) {
        await updateScale.mutateAsync({
          scaleId: scale.id,
          payload: {
            nameUz: values.nameUz,
            descriptionUz: values.descriptionUz ? values.descriptionUz : null,
            displayOrder: values.displayOrder,
            interpretationBands: scale.interpretationBands,
          },
        });
        toast.show({ variant: 'success', title: t('catalog.scaleForm.editSuccess') });
      } else {
        await createScale.mutateAsync({
          code: values.code.toUpperCase(),
          nameUz: values.nameUz,
          descriptionUz: values.descriptionUz ? values.descriptionUz : null,
          displayOrder: values.displayOrder,
          interpretationBands: [],
        });
        toast.show({ variant: 'success', title: t('catalog.scaleForm.createSuccess') });
      }
      onClose();
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = createScale.isPending || updateScale.isPending || isSubmitting;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={isEdit ? t('catalog.scaleForm.editTitle') : t('catalog.scaleForm.createTitle')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form={FORM_ID} isLoading={isMutating}>
            {t('common.save')}
          </Button>
        </>
      }
    >
      <form
        id={FORM_ID}
        onSubmit={(event) => void onSubmit(event)}
        noValidate
        className="flex flex-col gap-4"
      >
        {isEdit ? (
          <Input
            label={t('catalog.scaleForm.codeLabel')}
            hint={t('catalog.scaleForm.codeLockedHint')}
            value={scale.code}
            readOnly
            disabled
          />
        ) : (
          <Input
            label={t('catalog.scaleForm.codeLabel')}
            hint={t('catalog.scaleForm.codeHint')}
            error={errors.code?.message}
            {...register('code')}
          />
        )}
        <Input
          label={t('catalog.scaleForm.nameLabel')}
          error={errors.nameUz?.message}
          {...register('nameUz')}
        />
        <Textarea
          label={t('catalog.scaleForm.descriptionLabel')}
          error={errors.descriptionUz?.message}
          {...register('descriptionUz')}
        />
        <Input
          type="number"
          label={t('catalog.scaleForm.displayOrderLabel')}
          error={errors.displayOrder?.message}
          {...register('displayOrder', { valueAsNumber: true })}
        />
        {isEdit && scale.interpretationBands.length > 0 && (
          <p className="rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
            {t('catalog.scaleForm.bandsPreserved', {
              count: scale.interpretationBands.length,
            })}
          </p>
        )}
      </form>
    </Dialog>
  );
}
