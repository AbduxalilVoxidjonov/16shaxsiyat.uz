import { useEffect, useId } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Lock } from 'lucide-react';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Textarea } from '@/shared/ui/Textarea';
import { Checkbox } from '@/shared/ui/Checkbox';
import { useToast } from '@/shared/ui/useToast';
import {
  useCreateCatalogQuestion,
  useUpdateCatalogQuestion,
} from '../api/useCatalogQuestionMutations';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import {
  createQuestionFormSchema,
  type QuestionDialogFormValues,
} from '../model/questionFormSchema';
import {
  buildQuestionCreatePayload,
  buildQuestionUpdatePayload,
  toQuestionFormValues,
  toQuestionType,
} from '../model/questionPayload';
import { QUESTION_TYPE_VALUES, type CatalogQuestionItem } from '../model/types';
import { PublishedEditWarning } from './PublishedEditWarning';

export interface QuestionEditorDialogProps {
  open: boolean;
  testId: string;
  /** `null` — yangi savol (faqat `Custom`); berilsa — shu savolni tahrirlash. */
  question: CatalogQuestionItem | null;
  isSystem: boolean;
  isPublished: boolean;
  /** Yangi savolning boshlang'ich tartib raqami (oxirgi savoldan keyin). */
  nextOrder: number;
  onClose: () => void;
}

const FORM_ID = 'catalog-question-form';

const EMPTY_VALUES: QuestionDialogFormValues = {
  code: '',
  type: 'Likert5',
  textUz: '',
  textRu: '',
  textEn: '',
  order: 1,
  isActive: true,
  isRequired: true,
  scale: '',
  direction: 1,
  weight: 1,
};

/**
 * Savolni yaratish/tahrirlash oynasi.
 *
 * **Tizim metodikasida** (`isSystem`) `scale`/`direction`/`weight` faqat O'QISH uchun
 * ko'rsatiladi (`disabled` + sabab `aria-describedby` orqali) va so'rov tanasiga UMUMAN
 * qo'shilmaydi — `buildQuestionUpdatePayload` ga qarang (aks holda backend
 * `409 SYSTEM_TEST_LOCKED` beradi).
 */
export function QuestionEditorDialog({
  open,
  testId,
  question,
  isSystem,
  isPublished,
  nextOrder,
  onClose,
}: QuestionEditorDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const lockedHintId = useId();
  const isEdit = question !== null;

  const createQuestion = useCreateCatalogQuestion(testId);
  const updateQuestion = useUpdateCatalogQuestion(testId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<QuestionDialogFormValues>({
    resolver: zodResolver(createQuestionFormSchema(isEdit ? 'edit' : 'create')),
    defaultValues: EMPTY_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    if (question) {
      reset({
        ...toQuestionFormValues(question),
        code: question.code,
        type: toQuestionType(question.type),
      });
      return;
    }
    reset({ ...EMPTY_VALUES, order: nextOrder });
  }, [open, question, nextOrder, reset]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (question) {
        await updateQuestion.mutateAsync({
          questionId: question.id,
          payload: buildQuestionUpdatePayload(question, values, { isSystem }),
        });
        toast.show({ variant: 'success', title: t('catalog.questionForm.editSuccess') });
      } else {
        await createQuestion.mutateAsync(
          buildQuestionCreatePayload({
            ...values,
            code: values.code,
            type: values.type,
          }),
        );
        toast.show({ variant: 'success', title: t('catalog.questionForm.createSuccess') });
      }
      onClose();
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = createQuestion.isPending || updateQuestion.isPending || isSubmitting;
  const typeOptions = QUESTION_TYPE_VALUES.map((value) => ({
    value,
    label: t(`catalog.questionType.${value}`),
  }));

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={isEdit ? t('catalog.questionForm.editTitle') : t('catalog.questionForm.createTitle')}
      description={isEdit ? question.code : t('catalog.questionForm.createDescription')}
      className="max-w-lg"
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
        {isPublished && <PublishedEditWarning />}

        {isEdit ? (
          <Input
            label={t('catalog.questionForm.codeLabel')}
            hint={t('catalog.questionForm.codeLockedHint')}
            value={question.code}
            readOnly
            disabled
          />
        ) : (
          <Input
            label={t('catalog.questionForm.codeLabel')}
            hint={t('catalog.questionForm.codeHint')}
            error={errors.code?.message}
            {...register('code')}
          />
        )}

        <Textarea
          label={t('catalog.questionForm.textUzLabel')}
          error={errors.textUz?.message}
          {...register('textUz')}
        />
        <Textarea
          label={t('catalog.questionForm.textRuLabel')}
          hint={t('catalog.questionForm.optionalHint')}
          error={errors.textRu?.message}
          {...register('textRu')}
        />
        <Textarea
          label={t('catalog.questionForm.textEnLabel')}
          hint={t('catalog.questionForm.optionalHint')}
          error={errors.textEn?.message}
          {...register('textEn')}
        />

        {isEdit ? (
          <Input
            label={t('catalog.questionForm.typeLabel')}
            hint={t('catalog.questionForm.typeLockedHint')}
            value={t(`catalog.questionType.${question.type}`, { defaultValue: question.type })}
            readOnly
            disabled
          />
        ) : (
          <Select
            label={t('catalog.questionForm.typeLabel')}
            options={typeOptions}
            error={errors.type?.message}
            {...register('type')}
          />
        )}

        <Input
          type="number"
          label={t('catalog.questionForm.orderLabel')}
          error={errors.order?.message}
          {...register('order', { valueAsNumber: true })}
        />

        {isSystem ? (
          // Tizim savolida bu uch maydon FAQAT ko'rsatiladi — forma qiymatlaridan emas,
          // to'g'ridan-to'g'ri savol qatoridan o'qiladi va so'rov tanasiga tushmaydi.
          <div className="flex flex-col gap-3 rounded-lg bg-neutral-50 p-3">
            <p id={lockedHintId} className="flex items-start gap-2 text-sm text-neutral-600">
              <Lock size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
              {t('catalog.questionForm.systemLockedHint')}
            </p>
            <Input
              label={t('catalog.questionsTable.scale')}
              value={question?.scale ?? ''}
              aria-describedby={lockedHintId}
              readOnly
              disabled
            />
            <Input
              label={t('catalog.questionsTable.direction')}
              value={
                question?.direction === -1
                  ? t('catalog.questionsTable.directionReverse')
                  : t('catalog.questionsTable.directionForward')
              }
              aria-describedby={lockedHintId}
              readOnly
              disabled
            />
            <Input
              label={t('catalog.questionsTable.weight')}
              value={String(question?.weight ?? '')}
              aria-describedby={lockedHintId}
              readOnly
              disabled
            />
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            <Input
              label={t('catalog.questionsTable.scale')}
              hint={t('catalog.questionForm.scaleHint')}
              error={errors.scale?.message}
              {...register('scale')}
            />
            <Select
              label={t('catalog.questionsTable.direction')}
              hint={t('catalog.questionForm.directionHint')}
              options={[
                { value: '1', label: t('catalog.questionsTable.directionForward') },
                { value: '-1', label: t('catalog.questionsTable.directionReverse') },
              ]}
              error={errors.direction?.message}
              {...register('direction', { valueAsNumber: true })}
            />
            <Input
              type="number"
              step="0.1"
              label={t('catalog.questionsTable.weight')}
              hint={t('catalog.questionForm.weightHint')}
              error={errors.weight?.message}
              {...register('weight', { valueAsNumber: true })}
            />
          </div>
        )}

        <Checkbox
          label={t('catalog.questionForm.isActiveLabel')}
          error={errors.isActive?.message}
          {...register('isActive')}
        />
        <Checkbox
          label={t('catalog.questionForm.isRequiredLabel')}
          error={errors.isRequired?.message}
          {...register('isRequired')}
        />
      </form>
    </Dialog>
  );
}
