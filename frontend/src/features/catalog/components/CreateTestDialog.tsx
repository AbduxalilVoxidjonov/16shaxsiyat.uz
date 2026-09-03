import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Textarea } from '@/shared/ui/Textarea';
import { Select } from '@/shared/ui/Select';
import { useToast } from '@/shared/ui/useToast';
import { useCreateCatalogTest } from '../api/useCatalogTestMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { createTestSchema, type CreateTestFormValues } from '../model/createTestSchema';

export interface CreateTestDialogProps {
  open: boolean;
  onClose: () => void;
  /** Yaratilgach tahrirlash sahifasiga o'tish — savol va shkala qo'shish o'sha yerda (P38). */
  onCreated: (testId: string) => void;
}

const FORM_ID = 'catalog-create-test-form';

const DEFAULT_VALUES: CreateTestFormValues = {
  code: '',
  nameUz: '',
  descriptionUz: '',
  estimatedMinutes: 6,
  pageSize: 10,
  scoringMode: 'Scored',
};

/**
 * "Yangi anketa" — fayl YUKLAMASDAN, katalog sahifasining o'zida `Draft` anketa yaratadi
 * (`POST /api/admin/catalog/tests`), keyin tahrirlash sahifasiga o'tadi: savol va shkala
 * qo'shish u yerda allaqachon ishlaydi (P38).
 *
 * Ilgari katalogda YAGONA tugma import dialogini ochardi — backendda to'liq CRUD bo'lsa ham
 * anketani "noldan" yaratishning UI yo'li umuman yo'q edi.
 */
export function CreateTestDialog({ open, onClose, onCreated }: CreateTestDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const createTest = useCreateCatalogTest();
  const toErrorMessage = useCatalogErrorMessage();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateTestFormValues>({
    resolver: zodResolver(createTestSchema),
    defaultValues: DEFAULT_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    reset(DEFAULT_VALUES);
  }, [open, reset]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const created = await createTest.mutateAsync({
        code: values.code,
        nameUz: values.nameUz,
        descriptionUz: values.descriptionUz ? values.descriptionUz : null,
        estimatedMinutes: values.estimatedMinutes,
        pageSize: values.pageSize,
        scoringMode: values.scoringMode,
      });
      toast.show({ variant: 'success', title: t('catalog.createDialog.successTitle') });
      onCreated(created.id);
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = createTest.isPending || isSubmitting;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('catalog.createDialog.title')}
      description={t('catalog.createDialog.description')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form={FORM_ID} isLoading={isMutating}>
            {t('catalog.createDialog.submitCta')}
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
        <Input
          label={t('catalog.createDialog.codeLabel')}
          hint={t('catalog.createDialog.codeHint')}
          error={errors.code?.message}
          {...register('code')}
        />
        <Input
          label={t('catalog.createDialog.nameLabel')}
          error={errors.nameUz?.message}
          {...register('nameUz')}
        />
        <Textarea
          label={t('catalog.createDialog.descriptionLabel')}
          error={errors.descriptionUz?.message}
          {...register('descriptionUz')}
        />
        <Select
          label={t('catalog.createDialog.scoringModeLabel')}
          hint={t('catalog.createDialog.scoringModeHint')}
          error={errors.scoringMode?.message}
          options={[
            { value: 'Scored', label: t('catalog.scoringMode.scored') },
            { value: 'Survey', label: t('catalog.scoringMode.survey') },
          ]}
          {...register('scoringMode')}
        />
        <Input
          type="number"
          label={t('catalog.createDialog.minutesLabel')}
          error={errors.estimatedMinutes?.message}
          {...register('estimatedMinutes', { valueAsNumber: true })}
        />
        <Input
          type="number"
          label={t('catalog.createDialog.pageSizeLabel')}
          hint={t('catalog.createDialog.pageSizeHint')}
          error={errors.pageSize?.message}
          {...register('pageSize', { valueAsNumber: true })}
        />
      </form>
    </Dialog>
  );
}
