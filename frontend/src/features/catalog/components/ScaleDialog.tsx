import { useEffect, useState } from 'react';
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
import { areBandsSavable } from '../model/interpretationBands';
import type { CatalogScaleItem, InterpretationBand } from '../model/types';
import { InterpretationBandsEditor } from './InterpretationBandsEditor';

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
 *
 * Talqin oraliqlari (`interpretationBands`) shu yerda TAHRIRLANADI
 * ({@link InterpretationBandsEditor}). Ular react-hook-form emas, oddiy `useState` da
 * saqlanadi: massiv ichidagi son/matn maydonlarini RHF `useFieldArray` bilan boshqarish bu
 * yerda hech qanday foyda bermaydi (zod sxemasi qatorlar orasidagi bog'liqlikni — bo'shliq,
 * ustma-ustlik — baribir alohida tekshiradi), holat esa soddaroq bo'ladi.
 *
 * Oraliqlar buzilgan bo'lsa SAQLASH BLOKLANADI (`areBandsSavable`) — backend `PUT` ni qabul
 * qilardi va xato faqat nashrda chiqardi (`docs/03` §6.3).
 */
export function ScaleDialog({ open, testId, scale, onClose }: ScaleDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const isEdit = scale !== null;

  const createScale = useCreateCatalogScale(testId);
  const updateScale = useUpdateCatalogScale(testId);

  // Oraliqlar boshlang'ich holati MOUNT paytida olinadi (`useEffect` + `setState` emas):
  // oyna har ochilishda qayta mount bo'ladi (`ScalesSection` uni shartli render qiladi va
  // `key` bilan ajratadi), shu sabab effekt orqali sinxronlash kerak emas.
  const [bands, setBands] = useState<InterpretationBand[]>(() =>
    scale ? scale.interpretationBands.map((band) => ({ ...band })) : [],
  );

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

  const bandsSavable = areBandsSavable(bands);

  const onSubmit = handleSubmit(async (values) => {
    if (!bandsSavable) {
      return;
    }
    try {
      if (scale) {
        await updateScale.mutateAsync({
          scaleId: scale.id,
          payload: {
            nameUz: values.nameUz,
            descriptionUz: values.descriptionUz ? values.descriptionUz : null,
            displayOrder: values.displayOrder,
            interpretationBands: bands,
          },
        });
        toast.show({ variant: 'success', title: t('catalog.scaleForm.editSuccess') });
      } else {
        await createScale.mutateAsync({
          code: values.code.toUpperCase(),
          nameUz: values.nameUz,
          descriptionUz: values.descriptionUz ? values.descriptionUz : null,
          displayOrder: values.displayOrder,
          interpretationBands: bands,
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
      className="max-w-2xl"
      title={isEdit ? t('catalog.scaleForm.editTitle') : t('catalog.scaleForm.createTitle')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form={FORM_ID} isLoading={isMutating} disabled={!bandsSavable}>
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
        <InterpretationBandsEditor bands={bands} onChange={setBands} disabled={isMutating} />
      </form>
    </Dialog>
  );
}
