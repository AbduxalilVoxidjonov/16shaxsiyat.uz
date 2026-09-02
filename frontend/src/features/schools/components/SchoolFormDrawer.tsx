import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Drawer } from '@/shared/ui/Drawer';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Textarea } from '@/shared/ui/Textarea';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useSchoolDetailQuery } from '../api/useSchoolDetailQuery';
import { useCreateSchool } from '../api/useCreateSchool';
import { useUpdateSchool } from '../api/useUpdateSchool';
import { UZBEKISTAN_REGIONS } from '../model/regions';
import {
  schoolFormSchema,
  SCHOOL_FORM_DEFAULT_VALUES,
  type SchoolFormValues,
} from '../model/schoolFormSchema';
import type { SchoolUpsertRequest } from '../model/types';

export interface SchoolFormDrawerProps {
  open: boolean;
  /** `null` — yaratish rejimi; berilsa — shu maktabni tahrirlash. */
  schoolId: string | null;
  onClose: () => void;
}

const FORM_ID = 'school-form-drawer';
const REGION_OPTIONS = UZBEKISTAN_REGIONS.map((region) => ({ value: region, label: region }));

/** So'rov tanasida keraksiz `""` yubormaslik uchun — `schoolFormSchema.ts`dagi izohga qarang. */
function emptyToUndefined(value: string | undefined): string | undefined {
  return value ? value : undefined;
}

/**
 * Yaratish/tahrirlash drawer'i — docs/11-ux-va-ekranlar.md A-3; maydonlar `docs/02-biznes-
 * talablar.md` FR-1.1 bo'yicha ("nomi, viloyat, tuman, raqami, mas'ul shaxs, telefon,
 * kunlik limit, kirish kodi, izoh"). Submit tugmasi `Drawer`ning `footer`ida (`children`dan
 * tashqarida) — shu sabab HTML `form`/`form` atributi orqali bog'langan (native submit,
 * Enter tugmasi ham ishlaydi).
 */
export function SchoolFormDrawer({ open, schoolId, onClose }: SchoolFormDrawerProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const isEdit = schoolId !== null;

  const detailQuery = useSchoolDetailQuery(open ? schoolId : null);
  const createSchool = useCreateSchool();
  const updateSchool = useUpdateSchool();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SchoolFormValues>({
    resolver: zodResolver(schoolFormSchema),
    defaultValues: SCHOOL_FORM_DEFAULT_VALUES,
  });

  useEffect(() => {
    if (!open) return;
    if (!isEdit) {
      reset(SCHOOL_FORM_DEFAULT_VALUES);
      return;
    }
    if (detailQuery.data) {
      reset({
        name: detailQuery.data.name,
        region: detailQuery.data.region,
        district: detailQuery.data.district,
        schoolNumber: detailQuery.data.schoolNumber ?? '',
        contactPerson: detailQuery.data.contactPerson ?? '',
        contactPhone: detailQuery.data.contactPhone ?? '',
        dailyRegistrationLimit: detailQuery.data.dailyRegistrationLimit,
        accessCode: detailQuery.data.accessCode ?? '',
        notes: detailQuery.data.notes ?? '',
      });
    }
  }, [open, isEdit, detailQuery.data, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const payload: SchoolUpsertRequest = {
      name: values.name,
      region: values.region,
      district: values.district,
      schoolNumber: emptyToUndefined(values.schoolNumber),
      contactPerson: emptyToUndefined(values.contactPerson),
      contactPhone: emptyToUndefined(values.contactPhone),
      dailyRegistrationLimit: values.dailyRegistrationLimit,
      accessCode: emptyToUndefined(values.accessCode),
      notes: emptyToUndefined(values.notes),
    };
    try {
      if (isEdit && schoolId) {
        await updateSchool.mutateAsync({ id: schoolId, payload });
        toast.show({ variant: 'success', title: t('schools.form.editSuccess') });
      } else {
        await createSchool.mutateAsync(payload);
        toast.show({ variant: 'success', title: t('schools.form.createSuccess') });
      }
      onClose();
    } catch (caught) {
      const message = caught instanceof AppError ? caught.message : t('schools.form.genericError');
      toast.show({ variant: 'danger', title: message });
    }
  });

  const isMutating = createSchool.isPending || updateSchool.isPending || isSubmitting;
  const isLoadingDetail = isEdit && detailQuery.isPending;

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? t('schools.form.editTitle') : t('schools.form.createTitle')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form={FORM_ID} isLoading={isMutating} disabled={isLoadingDetail}>
            {isEdit ? t('schools.form.submitEditCta') : t('schools.form.submitCreateCta')}
          </Button>
        </>
      }
    >
      {isLoadingDetail ? (
        <div className="flex flex-col gap-4">
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} className="h-11 w-full" />
          ))}
        </div>
      ) : (
        <form id={FORM_ID} onSubmit={(event) => void onSubmit(event)} noValidate className="flex flex-col gap-4">
          <Input label={t('schools.form.nameLabel')} error={errors.name?.message} {...register('name')} />
          <Select
            label={t('schools.form.regionLabel')}
            placeholder={t('schools.form.regionPlaceholder')}
            options={REGION_OPTIONS}
            error={errors.region?.message}
            {...register('region')}
          />
          <Input
            label={t('schools.form.districtLabel')}
            error={errors.district?.message}
            {...register('district')}
          />
          <Input
            label={t('schools.form.schoolNumberLabel')}
            hint={t('schools.form.schoolNumberHint')}
            error={errors.schoolNumber?.message}
            {...register('schoolNumber')}
          />
          <Input
            label={t('schools.form.contactPersonLabel')}
            hint={t('schools.form.contactPersonHint')}
            error={errors.contactPerson?.message}
            {...register('contactPerson')}
          />
          <Input
            label={t('schools.form.contactPhoneLabel')}
            hint={t('schools.form.contactPhoneHint')}
            error={errors.contactPhone?.message}
            {...register('contactPhone')}
          />
          <Input
            type="number"
            label={t('schools.form.dailyLimitLabel')}
            error={errors.dailyRegistrationLimit?.message}
            {...register('dailyRegistrationLimit', { valueAsNumber: true })}
          />
          <Input
            label={t('schools.form.accessCodeLabel')}
            hint={t('schools.form.accessCodeHint')}
            inputMode="numeric"
            maxLength={6}
            error={errors.accessCode?.message}
            {...register('accessCode')}
          />
          <Textarea
            label={t('schools.form.notesLabel')}
            hint={t('schools.form.notesHint')}
            error={errors.notes?.message}
            {...register('notes')}
          />
        </form>
      )}
    </Drawer>
  );
}
