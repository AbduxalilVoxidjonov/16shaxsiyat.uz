import { useEffect, useId } from 'react';
import { useForm, useWatch } from 'react-hook-form';
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
import {
  isChoiceQuestionType,
  isSurveyOnlyQuestionType,
  isTextQuestionType,
  QUESTION_TYPE_VALUES,
  type CatalogQuestionItem,
  type CatalogSection,
  type TestScoringMode,
} from '../model/types';
import { questionsBeforeOrder, toVisibilityEditorQuestion } from '../model/visibilityEditorHelpers';
import { OptionsEditor } from './OptionsEditor';
import { PublishedEditWarning } from './PublishedEditWarning';
import { VisibilityRuleEditor } from './VisibilityRuleEditor';

export interface QuestionEditorDialogProps {
  open: boolean;
  testId: string;
  /** `null` — yangi savol (faqat `Custom`); berilsa — shu savolni tahrirlash. */
  question: CatalogQuestionItem | null;
  isSystem: boolean;
  isPublished: boolean;
  /** Yangi savolning boshlang'ich tartib raqami (oxirgi savoldan keyin). */
  nextOrder: number;
  /** B-1/B-2 (`docs/18` §1): yangi turlar va shart faqat `Survey` anketalarda. */
  scoringMode: TestScoringMode;
  sections: CatalogSection[];
  /** Butun anketaning savollari — ko'rsatish sharti muharriri "oldingi savollar"ni shundan tanlaydi. */
  allQuestions: CatalogQuestionItem[];
  onClose: () => void;
}

const FORM_ID = 'catalog-question-form';
const NO_SECTION_VALUE = '';

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
  sectionCode: '',
  placeholder: '',
  inputPattern: '',
  maxLength: Number.NaN,
  minSelections: Number.NaN,
  maxSelections: Number.NaN,
  options: [],
  visibility: null,
};

/**
 * Savolni yaratish/tahrirlash oynasi.
 *
 * **Tizim metodikasida** (`isSystem`) `scale`/`direction`/`weight` faqat O'QISH uchun
 * ko'rsatiladi (`disabled` + sabab `aria-describedby` orqali) va so'rov tanasiga UMUMAN
 * qo'shilmaydi — `buildQuestionUpdatePayload` ga qarang (aks holda backend
 * `409 SYSTEM_TEST_LOCKED` beradi).
 *
 * **`docs/18` B-1/B-2:** `Scored` anketada (`scoringMode !== 'Survey'`) yangi savol turlari
 * (`ShortText`/`LongText`/`MultiChoice`/`Phone`) tanlov ro'yxatidan chiqarib tashlanadi va
 * bo'lim/ko'rsatish sharti bloklari umuman ko'rsatilmaydi — sababi o'zbekcha izoh bilan
 * (`surveyOnlyNotice`).
 */
export function QuestionEditorDialog({
  open,
  testId,
  question,
  isSystem,
  isPublished,
  nextOrder,
  scoringMode,
  sections,
  allQuestions,
  onClose,
}: QuestionEditorDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const lockedHintId = useId();
  const isEdit = question !== null;
  const allowBranching = scoringMode === 'Survey';

  const createQuestion = useCreateCatalogQuestion(testId);
  const updateQuestion = useUpdateCatalogQuestion(testId);

  const {
    register,
    handleSubmit,
    reset,
    control,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<QuestionDialogFormValues>({
    resolver: zodResolver(createQuestionFormSchema(isEdit ? 'edit' : 'create')),
    defaultValues: EMPTY_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    if (question) {
      const sectionCode = question.sectionId
        ? (sections.find((s) => s.id === question.sectionId)?.code ?? '')
        : '';
      reset({
        ...toQuestionFormValues(question),
        code: question.code,
        type: toQuestionType(question.type),
        sectionCode,
      });
      return;
    }
    reset({ ...EMPTY_VALUES, order: nextOrder });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, question, nextOrder, reset]);

  const type = useWatch({ control, name: 'type' });
  const optionsValue = useWatch({ control, name: 'options' }) ?? [];
  const visibilityValue = useWatch({ control, name: 'visibility' }) ?? null;

  const isSurveyOnlyType = isSurveyOnlyQuestionType(type);
  const isTextType = isTextQuestionType(type);
  const isChoiceType = isChoiceQuestionType(type);

  const typeOptions = QUESTION_TYPE_VALUES.filter(
    (value) => allowBranching || !isSurveyOnlyQuestionType(value),
  ).map((value) => ({ value, label: t(`catalog.questionType.${value}`) }));

  const referenceOrder = isEdit ? (question?.order ?? nextOrder) : nextOrder;
  const visibilityAvailableQuestions = questionsBeforeOrder(
    allQuestions.map(toVisibilityEditorQuestion),
    referenceOrder,
  );

  const sectionOptions = [
    { value: NO_SECTION_VALUE, label: t('catalog.questionForm.sectionNoneOption') },
    ...sections.map((section) => ({
      value: section.code,
      label: `${section.titleUz} (${section.code})`,
    })),
  ];

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (question) {
        await updateQuestion.mutateAsync({
          questionId: question.id,
          payload: buildQuestionUpdatePayload(question, values, { isSystem, allowBranching }),
        });
        toast.show({ variant: 'success', title: t('catalog.questionForm.editSuccess') });
      } else {
        await createQuestion.mutateAsync(
          buildQuestionCreatePayload(
            { ...values, code: values.code, type: values.type },
            { allowBranching },
          ),
        );
        toast.show({ variant: 'success', title: t('catalog.questionForm.createSuccess') });
      }
      onClose();
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = createQuestion.isPending || updateQuestion.isPending || isSubmitting;
  const optionsError =
    typeof errors.options?.message === 'string' ? errors.options.message : undefined;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={isEdit ? t('catalog.questionForm.editTitle') : t('catalog.questionForm.createTitle')}
      description={isEdit ? question.code : t('catalog.questionForm.createDescription')}
      // Ikki ustunli forma uchun standart `max-w-md` tor — `Dialog`ning o'zi o'zgarmaydi,
      // faqat prop (`SchoolFormDialog` naqshi).
      className="max-w-4xl"
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
      {/*
        Ikki ustunli tashqi tartib (egasining talabi: oyna juda uzun bo'lib ketmasin). `sm:`
        dan yuqorida qisqa maydonlar ikkitalik ustunda, mobilda (390px) bitta ustun bo'lib
        qoladi (`SchoolFormDialog` naqshi). Uzun/murakkab bloklar (matni (o'zbekcha), variantlar
        va ko'rsatish sharti muharrirlari, qulflangan tizim izohi) doim to'liq kenglikda —
        `sm:col-span-2`. Uch maydonli mantiqiy guruhlar (kod/tur/tartib, shkala/yo'nalish/
        og'irlik, placeholder/shablon/uzunlik) o'z ichki 3 ustunli mini-grid'iga joylangan —
        oyna `max-w-4xl` bo'lgani uchun uchtasi bir qatorga sig'adi va bo'sh katak qolmaydi.
        Ikki maydonli guruh (min/max tanlov) 2 ustunli qoladi. `sectionCode` hech qanday
        guruhga kirmagani uchun `max-w-xs` bilan tor qilib alohida qatorga chiqarilgan.
        Ruscha/inglizcha matni `<details>` ichida yig'ilgan (mahsulot hozircha faqat
        o'zbekcha) — pastga qarang.
      */}
      <form
        id={FORM_ID}
        onSubmit={(event) => void onSubmit(event)}
        noValidate
        className="grid grid-cols-1 gap-4 sm:grid-cols-2"
      >
        {isPublished && (
          <div className="sm:col-span-2">
            <PublishedEditWarning />
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 sm:col-span-2 sm:grid-cols-3">
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
        </div>

        <div className="sm:col-span-2">
          <Textarea
            label={t('catalog.questionForm.textUzLabel')}
            rows={3}
            error={errors.textUz?.message}
            {...register('textUz')}
          />
        </div>

        {/*
          Mahsulot hozircha faqat o'zbekcha (`ru`/`en` lokal fayllari bo'sh, `PROGRESS.md`),
          shuning uchun bu ikki maydon 99% holatda bo'sh turadi va faqat balandlik yeydi.
          `<details>` ichiga yig'ilgan — klaviatura/skrinrider uchun brauzerning o'zi ishlaydi,
          qo'shimcha `aria-*` shart emas. Maydonlar DOM'da doim mavjud (faqat vizual yig'ilgan),
          shuning uchun `register` va validatsiya buzilmaydi. Tahrirlashda mavjud tarjima
          ko'rinmay qolmasligi uchun `textRu`/`textEn` to'ldirilgan bo'lsa blok ochiq boshlanadi
          (mount vaqtida bir marta hisoblanadi — `QuestionsSection` bu oynani har safar qayta
          o'qishda mount qiladi, shuning uchun foydalanuvchi qo'lda ochgan/yopgan holatiga
          keyingi render aralashmaydi).
        */}
        <details
          className="sm:col-span-2"
          open={Boolean(question?.textRu || question?.textEn)}
        >
          <summary className="cursor-pointer text-sm font-medium text-ink-soft">
            {t('catalog.questionForm.otherLanguagesToggle')}
          </summary>
          <div className="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Textarea
              label={t('catalog.questionForm.textRuLabel')}
              hint={t('catalog.questionForm.optionalHint')}
              rows={3}
              error={errors.textRu?.message}
              {...register('textRu')}
            />
            <Textarea
              label={t('catalog.questionForm.textEnLabel')}
              hint={t('catalog.questionForm.optionalHint')}
              rows={3}
              error={errors.textEn?.message}
              {...register('textEn')}
            />
          </div>
        </details>

        {isSystem ? (
          // Tizim savolida bu uch maydon FAQAT ko'rsatiladi — forma qiymatlaridan emas,
          // to'g'ridan-to'g'ri savol qatoridan o'qiladi va so'rov tanasiga tushmaydi.
          <div className="flex flex-col gap-3 rounded-lg bg-neutral-50 p-3 sm:col-span-2">
            <p id={lockedHintId} className="flex items-start gap-2 text-sm text-neutral-600">
              <Lock size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
              {t('catalog.questionForm.systemLockedHint')}
            </p>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
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
          </div>
        ) : (
          <>
            {!isSurveyOnlyType && (
              <div className="grid grid-cols-1 gap-4 sm:col-span-2 sm:grid-cols-3">
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

            {isTextType && (
              <div className="grid grid-cols-1 gap-4 sm:col-span-2 sm:grid-cols-3">
                <Input
                  label={t('catalog.questionForm.placeholderLabel')}
                  error={errors.placeholder?.message}
                  {...register('placeholder')}
                />
                {(type === 'ShortText' || type === 'Phone') && (
                  <Input
                    label={t('catalog.questionForm.inputPatternLabel')}
                    hint={t('catalog.questionForm.inputPatternHint')}
                    error={errors.inputPattern?.message}
                    {...register('inputPattern')}
                  />
                )}
                <Input
                  type="number"
                  label={t('catalog.questionForm.maxLengthLabel')}
                  error={errors.maxLength?.message}
                  {...register('maxLength', { valueAsNumber: true })}
                />
              </div>
            )}

            {type === 'MultiChoice' && (
              <div className="grid grid-cols-1 gap-4 sm:col-span-2 sm:grid-cols-2">
                <Input
                  type="number"
                  label={t('catalog.questionForm.minSelectionsLabel')}
                  error={errors.minSelections?.message}
                  {...register('minSelections', { valueAsNumber: true })}
                />
                <Input
                  type="number"
                  label={t('catalog.questionForm.maxSelectionsLabel')}
                  error={errors.maxSelections?.message}
                  {...register('maxSelections', { valueAsNumber: true })}
                />
              </div>
            )}

            {isChoiceType && (
              <div className="sm:col-span-2">
                <OptionsEditor
                  options={optionsValue}
                  onChange={(next) => {
                    setValue('options', next, { shouldValidate: true, shouldDirty: true });
                  }}
                  disabled={isMutating}
                  error={optionsError}
                />
              </div>
            )}

            {allowBranching ? (
              <>
                {/*
                  Bu maydon hech qanday qo'shni qisqa maydon bilan bir mantiqiy guruhga
                  kirmaydi (shart va turga bog'liq bo'lmagan yagona tanlov), shuning uchun
                  uni to'liq qatorga chiqarib, `max-w-xs` bilan tor qilamiz — aks holda
                  ikki ustunli tashqi grid'da yonida bo'sh katak qolar edi.
                */}
                <div className="max-w-xs sm:col-span-2">
                  <Select
                    label={t('catalog.questionForm.sectionLabel')}
                    options={sectionOptions}
                    {...register('sectionCode')}
                  />
                </div>
                <div className="sm:col-span-2">
                  <VisibilityRuleEditor
                    value={visibilityValue}
                    onChange={(rule) => {
                      setValue('visibility', rule, { shouldValidate: true, shouldDirty: true });
                    }}
                    availableQuestions={visibilityAvailableQuestions}
                    disabled={isMutating}
                  />
                </div>
              </>
            ) : (
              <p className="text-sm text-neutral-500 sm:col-span-2">
                {t('catalog.questionForm.surveyOnlyNotice')}
              </p>
            )}
          </>
        )}

        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 sm:col-span-2">
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
        </div>
      </form>
    </Dialog>
  );
}
