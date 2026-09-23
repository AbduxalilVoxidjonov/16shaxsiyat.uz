import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Textarea } from '@/shared/ui/Textarea';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useSchoolDetailQuery } from '../api/useSchoolDetailQuery';
import { useCreateSchool } from '../api/useCreateSchool';
import { useUpdateSchool } from '../api/useUpdateSchool';
import { UZBEKISTAN_REGIONS } from '../model/regions';
import { SchoolTestsField } from './SchoolTestsField';
import {
  schoolFormSchema,
  SCHOOL_FORM_DEFAULT_VALUES,
  type SchoolFormValues,
} from '../model/schoolFormSchema';
import type { SchoolDetailDto, SchoolUpsertRequest } from '../model/types';

export interface SchoolFormDialogProps {
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

function toFormValues(detail: SchoolDetailDto): SchoolFormValues {
  return {
    name: detail.name,
    region: detail.region,
    district: detail.district,
    schoolNumber: detail.schoolNumber ?? '',
    contactPerson: detail.contactPerson ?? '',
    contactPhone: detail.contactPhone ?? '',
    dailyRegistrationLimit: detail.dailyRegistrationLimit,
    notes: detail.notes ?? '',
    testIds: [...(detail.testIds ?? [])],
  };
}

/**
 * Saqlash xatosi matni. `testIds` bilan bog'liq ikki holat (`docs/07` §3.1, 2026-09-23) aniq
 * o'zbekcha matn bilan; qolganida backend xabari (u ham o'zbekcha) yoki umumiy matn.
 */
function toSchoolSaveErrorMessage(caught: unknown, t: (key: string) => string): string {
  if (!(caught instanceof AppError)) return t('schools.form.genericError');
  if (caught.code === 'TEST_ARCHIVED') return t('schools.form.errors.testArchived');
  if (caught.code === 'NOT_FOUND' && Array.isArray(caught.extensions?.testIds)) {
    return t('schools.form.errors.testsNotFound');
  }
  return caught.message;
}

/**
 * Maydonlarning o'zi — ATAYLAB alohida komponent va faqat boshlang'ich qiymatlar TAYYOR
 * bo'lganda mount qilinadi (`SchoolFormDialog` dagi shartli render + `key`).
 *
 * NEGA (P30-9): oldin bitta `useForm` doim mount holatda turardi va qiymatlar
 * `useEffect(… reset(…) …)` bilan to'ldirilardi. `useEffect` bo'yoqdan (paint) KEYIN
 * ishlaydi — ya'ni oyna allaqachon ekranda, maydonlar bosiladigan holatda edi. Foydalanuvchi
 * oyna ochilishi bilan yozishni boshlasa, `reset` uning yozganini JIMGINA o'chirib
 * tashlardi (goh "Nomi", goh "Viloyat" — qaysi biriga ulgurganiga qarab). Endi `reset`
 * umuman chaqirilmaydi: forma har safar yangidan mount bo'ladi va `defaultValues` ni
 * MOUNT PAYTIDA oladi — hech qanday poyga qolmaydi, xatti-harakat deterministik.
 */
function SchoolFormFields({
  initialValues,
  onValidSubmit,
}: {
  initialValues: SchoolFormValues;
  onValidSubmit: (values: SchoolFormValues) => Promise<void>;
}) {
  const { t } = useTranslation();
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<SchoolFormValues>({
    resolver: zodResolver(schoolFormSchema),
    defaultValues: initialValues,
  });

  const onSubmit = handleSubmit(async (values) => {
    await onValidSubmit(values);
  });

  /*
    Ikki ustunli tartib (egasining talabi: "uzun ustun emas, ikkitalik ustun qil"). `sm:` dan
    yuqorida ikki ustun, mobilda bitta. Mantiqan juft maydonlar yonma-yon: viloyat/tuman,
    raqam/limit, mas'ul/telefon. Nomi va izoh — to'liq kenglik (3 juft + 2 to'liq = grid tekis).
    Eski qo'lda kiritiladigan "Kirish kodi" (`accessCode`) maydoni 2026-09-07 da olib tashlangan —
    o'rnini avtomatik `entryCode` ("Maktab kodi") egalladi; ikkinchi kod adminni chalg'itardi.
    Xato/hint matnlari `Input`/`Select`/`Textarea` ichida, maydon ostida chiziladi.
  */
  return (
    <form
      id={FORM_ID}
      onSubmit={(event) => void onSubmit(event)}
      noValidate
      className="grid grid-cols-1 gap-4 sm:grid-cols-2"
    >
      <div className="sm:col-span-2">
        <Input label={t('schools.form.nameLabel')} error={errors.name?.message} {...register('name')} />
      </div>
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
        type="number"
        label={t('schools.form.dailyLimitLabel')}
        error={errors.dailyRegistrationLimit?.message}
        {...register('dailyRegistrationLimit', { valueAsNumber: true })}
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
      <div className="sm:col-span-2">
        <Textarea
          label={t('schools.form.notesLabel')}
          hint={t('schools.form.notesHint')}
          error={errors.notes?.message}
          {...register('notes')}
        />
      </div>
      {/* 2026-09-23: "Dasturlar" bo'limi olib tashlandi — maktabga TESTLAR tanlanadi (`testIds`). */}
      <div className="sm:col-span-2">
        <Controller
          control={control}
          name="testIds"
          render={({ field }) => (
            <SchoolTestsField
              value={field.value}
              onChange={field.onChange}
              error={errors.testIds?.message}
            />
          )}
        />
      </div>
    </form>
  );
}

/**
 * Yaratish/tahrirlash oynasi — docs/11-ux-va-ekranlar.md A-3; maydonlar `docs/02-biznes-
 * talablar.md` FR-1.1 bo'yicha ("nomi, viloyat, tuman, raqami, mas'ul shaxs, telefon,
 * kunlik limit, izoh"; eski `accessCode` admin UI'dan olib tashlangan, FR-1.5). Submit
 * tugmasi `Dialog`ning `footer`ida (`children`dan tashqarida) — shu sabab HTML `form`/`form` atributi orqali bog'langan (native submit,
 * Enter tugmasi ham ishlaydi).
 */
export function SchoolFormDialog({ open, schoolId, onClose }: SchoolFormDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const isEdit = schoolId !== null;

  const detailQuery = useSchoolDetailQuery(open ? schoolId : null);
  const createSchool = useCreateSchool();
  const updateSchool = useUpdateSchool();

  async function handleValidSubmit(values: SchoolFormValues) {
    const payload: SchoolUpsertRequest = {
      name: values.name,
      region: values.region,
      district: values.district,
      schoolNumber: emptyToUndefined(values.schoolNumber),
      contactPerson: emptyToUndefined(values.contactPerson),
      contactPhone: emptyToUndefined(values.contactPhone),
      dailyRegistrationLimit: values.dailyRegistrationLimit,
      // `accessCode` ATAYLAB yuborilmaydi — backend `null` deb qabul qiladi (eskirgan maydon).
      notes: emptyToUndefined(values.notes),
      // To'plam TO'LIQ almashtiriladi (`docs/07` §3.1): tahrirlashda joriy biriktirmalar
      // formaga yuklangan, shu sabab o'zgartirilmagan bo'lsa ular o'z holicha qaytadi.
      testIds: values.testIds,
    };
    try {
      if (isEdit && schoolId) {
        await updateSchool.mutateAsync({ id: schoolId, payload });
        toast.show({ variant: 'success', title: t('schools.form.editSuccess') });
      } else {
        const created = await createSchool.mutateAsync(payload);
        // Maktab kodi yaratishda avtomatik hosil bo'ladi — admin uni darhol ko'rsin.
        toast.show({
          variant: 'success',
          title: t('schools.form.createSuccess'),
          description: created.entryCode
            ? t('schools.form.createSuccessCode', { code: created.entryCode })
            : undefined,
        });
      }
      onClose();
    } catch (caught) {
      const message = toSchoolSaveErrorMessage(caught, t);
      toast.show({ variant: 'danger', title: message });
    }
  }

  const isMutating = createSchool.isPending || updateSchool.isPending;
  const isLoadingDetail = isEdit && detailQuery.isPending;
  // Tahrirlashda ma'lumot kelmasa forma KO'RSATILMAYDI: aks holda oyna bo'sh (yoki eski)
  // qiymatlar bilan ochilib, saqlash mavjud yozuvni buzishi mumkin edi.
  const isDetailFailed = isEdit && !detailQuery.isPending && !detailQuery.data;
  const initialValues = detailQuery.data ? toFormValues(detailQuery.data) : SCHOOL_FORM_DEFAULT_VALUES;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={isEdit ? t('schools.form.editTitle') : t('schools.form.createTitle')}
      // Ikki ustunli forma uchun standart `max-w-md` tor — `Dialog`ning o'zi o'zgarmaydi, faqat prop.
      className="max-w-2xl"
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isMutating}>
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            form={FORM_ID}
            isLoading={isMutating}
            disabled={isLoadingDetail || isDetailFailed}
          >
            {isEdit ? t('schools.form.submitEditCta') : t('schools.form.submitCreateCta')}
          </Button>
        </>
      }
    >
      {/*
        Oyna yopiq bo'lganda forma UMUMAN render qilinmaydi — shu sabab har ochilishda u
        yangidan mount bo'ladi va boshlang'ich qiymatlarni o'sha zahoti oladi (P30-9).
        `key` — bir oynadan ikkinchi maktabga o'tilganda ham yangi mount kafolati.
      */}
      {!open ? null : isLoadingDetail ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} className="h-11 w-full" />
          ))}
        </div>
      ) : isDetailFailed ? (
        <ErrorState onRetry={() => void detailQuery.refetch()} />
      ) : (
        <SchoolFormFields
          key={schoolId ?? 'new'}
          initialValues={initialValues}
          onValidSubmit={handleValidSubmit}
        />
      )}
    </Dialog>
  );
}
