import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Textarea } from '@/shared/ui/Textarea';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useProgramQuery } from '../api/useProgramQuery';
import { useCreateProgram } from '../api/useCreateProgram';
import { useUpdateProgram } from '../api/useUpdateProgram';
import {
  programFormSchema,
  PROGRAM_FORM_DEFAULT_VALUES,
  type ProgramFormValues,
} from '../model/programFormSchema';

export interface ProgramFormDialogProps {
  open: boolean;
  /** `null` — yaratish rejimi; berilsa — shu dasturni tahrirlash. */
  programId: string | null;
  onClose: () => void;
  /** Yaratilgandan keyin chaqiriladi (masalan detal sahifasiga o'tish uchun). */
  onCreated?: (programId: string) => void;
}

const FORM_ID = 'program-form-dialog';

/**
 * Dastur yaratish/tahrirlash — `prompts/35` C8-band. `Code`/`Kind`/`IsSystem` tahrirlashda
 * o'zgarmaydi (`UpdateProgramCommand` bu maydonlarni umuman qabul qilmaydi — backend haqiqat
 * manbai). Tizim dasturida ham nom/tavsif/tartib/ko'rinish tahrirlanadi (`docs/06` §8
 * 2026-09-02: "ko'rinishi va biriktirishi o'zgartiriladi") — faqat **tarkib** (testlar)
 * qulflangan, u bu dialogda umuman yo'q (alohida `ProgramTestsList`da boshqariladi).
 */
export function ProgramFormDialog({ open, programId, onClose, onCreated }: ProgramFormDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const isEdit = programId !== null;

  const detailQuery = useProgramQuery(open ? programId : null);
  const createProgram = useCreateProgram();
  const updateProgram = useUpdateProgram();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProgramFormValues>({
    resolver: zodResolver(programFormSchema),
    defaultValues: PROGRAM_FORM_DEFAULT_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    if (!isEdit) {
      reset(PROGRAM_FORM_DEFAULT_VALUES);
      return;
    }
    if (detailQuery.data) {
      reset({
        code: detailQuery.data.code,
        nameUz: detailQuery.data.nameUz,
        descriptionUz: detailQuery.data.descriptionUz ?? '',
        displayOrder: detailQuery.data.displayOrder,
        visibility: detailQuery.data.visibility === 'Public' ? 'Public' : 'Assigned',
      });
    }
  }, [open, isEdit, detailQuery.data, reset]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEdit && programId) {
        await updateProgram.mutateAsync({
          id: programId,
          payload: {
            nameUz: values.nameUz,
            descriptionUz: values.descriptionUz ? values.descriptionUz : null,
            displayOrder: values.displayOrder,
            visibility: values.visibility,
          },
        });
        toast.show({ variant: 'success', title: t('programs.form.editSuccess') });
      } else {
        const created = await createProgram.mutateAsync({
          code: values.code,
          nameUz: values.nameUz,
          descriptionUz: values.descriptionUz ? values.descriptionUz : null,
          displayOrder: values.displayOrder,
          visibility: values.visibility,
        });
        toast.show({ variant: 'success', title: t('programs.form.createSuccess') });
        onCreated?.(created.id);
      }
      onClose();
    } catch (caught) {
      const message = caught instanceof AppError ? caught.message : t('programs.form.genericError');
      toast.show({ variant: 'danger', title: message });
    }
  });

  const isMutating = createProgram.isPending || updateProgram.isPending || isSubmitting;
  const isLoadingDetail = isEdit && detailQuery.isPending;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={isEdit ? t('programs.form.editTitle') : t('programs.form.createTitle')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form={FORM_ID} isLoading={isMutating} disabled={isLoadingDetail}>
            {isEdit ? t('programs.form.submitEditCta') : t('programs.form.submitCreateCta')}
          </Button>
        </>
      }
    >
      {isLoadingDetail ? (
        <div className="flex flex-col gap-4">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-11 w-full" />
          ))}
        </div>
      ) : (
        <form
          id={FORM_ID}
          onSubmit={(event) => void onSubmit(event)}
          noValidate
          className="flex flex-col gap-4"
        >
          <Input
            label={t('programs.form.codeLabel')}
            hint={isEdit ? t('programs.form.codeLockedHint') : t('programs.form.codeHint')}
            error={errors.code?.message}
            disabled={isEdit}
            {...register('code')}
          />
          <Input
            label={t('programs.form.nameLabel')}
            error={errors.nameUz?.message}
            {...register('nameUz')}
          />
          <Textarea
            label={t('programs.form.descriptionLabel')}
            error={errors.descriptionUz?.message}
            {...register('descriptionUz')}
          />
          <Input
            type="number"
            label={t('programs.form.displayOrderLabel')}
            error={errors.displayOrder?.message}
            {...register('displayOrder', { valueAsNumber: true })}
          />
          <Select
            label={t('programs.form.visibilityLabel')}
            hint={t('programs.form.visibilityHint')}
            options={[
              { value: 'Public', label: t('programs.visibility.public') },
              { value: 'Assigned', label: t('programs.visibility.assigned') },
            ]}
            error={errors.visibility?.message}
            {...register('visibility')}
          />
        </form>
      )}
    </Dialog>
  );
}
