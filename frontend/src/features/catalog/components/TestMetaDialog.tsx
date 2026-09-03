import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Textarea } from '@/shared/ui/Textarea';
import { Checkbox } from '@/shared/ui/Checkbox';
import { useToast } from '@/shared/ui/useToast';
import { useUpdateCatalogTest } from '../api/useCatalogTestMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { testMetaSchema, type TestMetaFormValues } from '../model/testMetaSchema';
import type { CatalogTestDetail } from '../model/types';
import { PublishedEditWarning } from './PublishedEditWarning';

export interface TestMetaDialogProps {
  open: boolean;
  test: CatalogTestDetail;
  onClose: () => void;
}

const FORM_ID = 'catalog-test-meta-form';

/**
 * Test meta ma'lumotlarini tahrirlash — `PUT /api/admin/catalog/tests/{id}`. Tizim
 * metodikasida ham ochiq: backend bu endpointda `IsSystem` ni tekshirmaydi, qulflangani
 * faqat savol/shkala tarkibi (BR-8).
 */
export function TestMetaDialog({ open, test, onClose }: TestMetaDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const updateTest = useUpdateCatalogTest();
  const toErrorMessage = useCatalogErrorMessage();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TestMetaFormValues>({
    resolver: zodResolver(testMetaSchema),
    defaultValues: {
      nameUz: test.nameUz,
      descriptionUz: test.descriptionUz ?? '',
      displayOrder: test.displayOrder,
      estimatedMinutes: test.estimatedMinutes,
      pageSize: test.pageSize,
      shuffleQuestions: test.shuffleQuestions,
    },
  });

  useEffect(() => {
    if (!open) return;
    reset({
      nameUz: test.nameUz,
      descriptionUz: test.descriptionUz ?? '',
      displayOrder: test.displayOrder,
      estimatedMinutes: test.estimatedMinutes,
      pageSize: test.pageSize,
      shuffleQuestions: test.shuffleQuestions,
    });
  }, [open, reset, test]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await updateTest.mutateAsync({
        id: test.id,
        payload: {
          nameUz: values.nameUz,
          descriptionUz: values.descriptionUz ? values.descriptionUz : null,
          displayOrder: values.displayOrder,
          estimatedMinutes: values.estimatedMinutes,
          shuffleQuestions: values.shuffleQuestions,
          pageSize: values.pageSize,
        },
      });
      toast.show({ variant: 'success', title: t('catalog.meta.successTitle') });
      onClose();
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = updateTest.isPending || isSubmitting;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('catalog.meta.title')}
      description={test.nameUz}
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
        {test.status === 'Published' && <PublishedEditWarning />}

        <Input
          label={t('catalog.meta.nameLabel')}
          error={errors.nameUz?.message}
          {...register('nameUz')}
        />
        <Textarea
          label={t('catalog.meta.descriptionLabel')}
          error={errors.descriptionUz?.message}
          {...register('descriptionUz')}
        />
        <Input
          type="number"
          label={t('catalog.meta.displayOrderLabel')}
          hint={t('catalog.meta.displayOrderHint')}
          error={errors.displayOrder?.message}
          {...register('displayOrder', { valueAsNumber: true })}
        />
        <Input
          type="number"
          label={t('catalog.meta.minutesLabel')}
          hint={t('catalog.meta.minutesHint')}
          error={errors.estimatedMinutes?.message}
          {...register('estimatedMinutes', { valueAsNumber: true })}
        />
        <Input
          type="number"
          label={t('catalog.meta.pageSizeLabel')}
          hint={t('catalog.meta.pageSizeHint')}
          error={errors.pageSize?.message}
          {...register('pageSize', { valueAsNumber: true })}
        />
        <Checkbox
          label={t('catalog.meta.shuffleLabel')}
          error={errors.shuffleQuestions?.message}
          {...register('shuffleQuestions')}
        />
      </form>
    </Dialog>
  );
}
