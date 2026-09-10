import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import type { VisibilityRule } from '@/shared/lib/visibility';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Textarea } from '@/shared/ui/Textarea';
import { useToast } from '@/shared/ui/useToast';
import { useCreateCatalogSection, useUpdateCatalogSection } from '../api/useCatalogSections';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import { createSectionFormSchema, type SectionFormValues } from '../model/sectionFormSchema';
import type { CatalogQuestionItem, CatalogSection } from '../model/types';
import {
  questionsBeforeSection,
  toVisibilityEditorQuestion,
} from '../model/visibilityEditorHelpers';
import { VisibilityRuleEditor } from './VisibilityRuleEditor';

export interface SectionDialogProps {
  open: boolean;
  testId: string;
  /** `null` — yangi bo'lim; berilsa — shu bo'limni tahrirlash. */
  section: CatalogSection | null;
  sections: CatalogSection[];
  questions: CatalogQuestionItem[];
  onClose: () => void;
}

const FORM_ID = 'catalog-section-form';

function buildQuestionSectionCodeMap(
  sections: readonly CatalogSection[],
  questions: readonly CatalogQuestionItem[],
): Map<string, string | null> {
  const sectionCodeById = new Map(sections.map((s) => [s.id, s.code]));
  return new Map(
    questions.map((q) => [q.code, q.sectionId ? (sectionCodeById.get(q.sectionId) ?? null) : null]),
  );
}

/**
 * Bo'lim yaratish/tahrirlash — faqat `Custom` testlarda (`docs/18` §6.3). `ScaleDialog.tsx`
 * naqshiga ergashadi: ko'rsatish sharti (`VisibilityRule`) react-hook-form'dan TASHQARIDA,
 * `useState`da — lekin bu yerda "oldingi savollar" ro'yxati forma qiymatiga (`displayOrder`)
 * bog'liq bo'lgani uchun `useWatch` orqali kuzatiladi.
 */
export function SectionDialog({
  open,
  testId,
  section,
  sections,
  questions,
  onClose,
}: SectionDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toErrorMessage = useCatalogErrorMessage();
  const isEdit = section !== null;

  const createSection = useCreateCatalogSection(testId);
  const updateSection = useUpdateCatalogSection(testId);

  const emptyValues: SectionFormValues = {
    code: '',
    titleUz: '',
    descriptionUz: '',
    displayOrder: sections.length,
  };

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<SectionFormValues>({
    resolver: zodResolver(createSectionFormSchema(isEdit ? 'edit' : 'create')),
    defaultValues: emptyValues,
  });

  // Boshlang'ich qiymat MOUNT paytida olinadi (`useEffect` + `setState` emas): oyna har
  // ochilishda qayta mount bo'ladi (`SectionsSection` uni shartli render qiladi va `key`
  // bilan ajratadi, xuddi `ScaleDialog.tsx`dagi `bands` kabi), shu sabab effekt orqali
  // sinxronlash shart emas — buni RHF `reset()` bilan effektga aralashtirish "avval render,
  // keyin effekt setState" kaskadini keltirib chiqarardi (`react-hooks/set-state-in-effect`).
  const [visibility, setVisibility] = useState<VisibilityRule | null>(section?.visibility ?? null);

  useEffect(() => {
    if (!open) return;
    reset(
      section
        ? {
            code: section.code,
            titleUz: section.titleUz,
            descriptionUz: section.descriptionUz ?? '',
            displayOrder: section.displayOrder,
          }
        : emptyValues,
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, section, reset]);

  const displayOrderValue = useWatch({ control, name: 'displayOrder' });

  const questionSectionCode = buildQuestionSectionCodeMap(sections, questions);
  const availableQuestions = questionsBeforeSection(
    sections,
    questions.map(toVisibilityEditorQuestion),
    questionSectionCode,
    section?.code ?? null,
    Number.isFinite(displayOrderValue) ? displayOrderValue : (section?.displayOrder ?? sections.length),
  );

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (section) {
        await updateSection.mutateAsync({
          sectionId: section.id,
          payload: {
            titleUz: values.titleUz,
            descriptionUz: values.descriptionUz ? values.descriptionUz : null,
            displayOrder: values.displayOrder,
            visibility,
          },
        });
        toast.show({ variant: 'success', title: t('catalog.sectionForm.editSuccess') });
      } else {
        await createSection.mutateAsync({
          code: values.code.trim(),
          titleUz: values.titleUz,
          descriptionUz: values.descriptionUz ? values.descriptionUz : null,
          displayOrder: values.displayOrder,
          visibility,
        });
        toast.show({ variant: 'success', title: t('catalog.sectionForm.createSuccess') });
      }
      onClose();
    } catch (caught) {
      toast.show({ variant: 'danger', title: toErrorMessage(caught) });
    }
  });

  const isMutating = createSection.isPending || updateSection.isPending || isSubmitting;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      className="max-w-2xl"
      title={isEdit ? t('catalog.sectionForm.editTitle') : t('catalog.sectionForm.createTitle')}
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
            label={t('catalog.sectionForm.codeLabel')}
            hint={t('catalog.sectionForm.codeLockedHint')}
            value={section.code}
            readOnly
            disabled
          />
        ) : (
          <Input
            label={t('catalog.sectionForm.codeLabel')}
            hint={t('catalog.sectionForm.codeHint')}
            error={errors.code?.message}
            {...register('code')}
          />
        )}
        <Input
          label={t('catalog.sectionForm.titleLabel')}
          error={errors.titleUz?.message}
          {...register('titleUz')}
        />
        <Textarea
          label={t('catalog.sectionForm.descriptionLabel')}
          error={errors.descriptionUz?.message}
          {...register('descriptionUz')}
        />
        <Input
          type="number"
          label={t('catalog.sectionForm.displayOrderLabel')}
          error={errors.displayOrder?.message}
          {...register('displayOrder', { valueAsNumber: true })}
        />

        <VisibilityRuleEditor
          value={visibility}
          onChange={setVisibility}
          availableQuestions={availableQuestions}
          disabled={isMutating}
        />
      </form>
    </Dialog>
  );
}
