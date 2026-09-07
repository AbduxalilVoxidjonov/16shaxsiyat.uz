import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, X } from 'lucide-react';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';
import { Badge } from '@/shared/ui/Badge';
import { Skeleton } from '@/shared/ui/Skeleton';
import { EmptyState } from '@/shared/ui/EmptyState';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useDebounce } from '@/shared/hooks/useDebounce';
import {
  SCHOOL_SEARCH_DEBOUNCE_MS,
  useSchoolOptionsQuery,
  type SchoolOption,
  useSchoolsByIdsQuery,
} from '../api/useSchoolOptionsQuery';
import { useAssignProgramSchool, useUnassignProgramSchool } from '../api/useProgramSchoolMutations';
import { PublicSpaceAssignmentBlock } from './PublicSpaceAssignmentBlock';

export interface SchoolAssignmentPanelProps {
  programId: string;
  programName: string;
  /** `AdminProgramDetailDto.state` — ommaviy blokdagi "faqat faol dastur" cheklovi uchun. */
  state: string;
  /** FAQAT `Kind = School` makonlar — ommaviy makon bu ro'yxatga KIRMAYDI (backend ajratadi). */
  assignedSchoolIds: readonly string[];
  isAssignedToPublicSpace: boolean;
}

/**
 * `Visibility = Assigned` dasturlar uchun maktablarga biriktirish paneli — `prompts/35`
 * C9-band: "qidiruvli ko'p tanlovli ro'yxat, biriktirilgan maktablar chipi bilan".
 *
 * Har bir qidiruv natijasidagi maktab yonida "Dastursiz" belgisi ko'rsatiladi agar shu
 * maktabda **hech qanday** mavjud dastur bo'lmasa — admin qaysi maktabga ustuvorlik berish
 * kerakligini shu yerda ko'radi (`prompts/35` 13-band, "eng qimmatli qism").
 *
 * **2026-09-03:** belgi endi BACKENDDAN keladi (`SchoolOption.linkHealth`, `docs/07` 3.1) —
 * ilgari u klient tomonda alohida hisoblanardi va ommaviy handler mezonidan farq qilardi
 * (o'chirilgan dasturni ham, testsiz dasturni ham "joyida" deb ko'rsatardi). Ikkinchi mezon
 * OLIB TASHLANDI: panel yolg'on aytmasligi uchun mezon bitta bo'lishi shart.
 *
 * **2026-09-07:** panel tepasida maktablardan ALOHIDA "Ommaviy makon" bloki
 * (`PublicSpaceAssignmentBlock`) — ommaviy makon `AdminSchoolScope.SchoolsOnly` sababli
 * maktab qidiruvida chiqmaydi va `assignedSchoolIds` da ham yo'q (backend uni
 * `isAssignedToPublicSpace` bayrog'iga ajratadi), shu sabab u shu yerda o'z bloki bilan
 * boshqariladi.
 */
export function SchoolAssignmentPanel({
  programId,
  programName,
  state,
  assignedSchoolIds,
  isAssignedToPublicSpace,
}: SchoolAssignmentPanelProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, SCHOOL_SEARCH_DEBOUNCE_MS);

  const optionsQuery = useSchoolOptionsQuery(debouncedSearch, true);
  const assignedQuery = useSchoolsByIdsQuery([...assignedSchoolIds]);
  const assignSchool = useAssignProgramSchool();
  const unassignSchool = useUnassignProgramSchool();

  const assignedIdSet = new Set(assignedSchoolIds);

  /**
   * Maktabda bironta MAVJUD dastur bormi — ommaviy `GET /api/public/schools/{slug}` mezoni
   * bilan bitta manbadan (`linkHealth.availableProgramCount`). Shu dasturga biriktirilgan
   * maktablarda bu son allaqachon biriktirmani hisobga oladi — qo'shimcha shart kerak emas.
   */
  function isWithoutAnyProgram(school: SchoolOption): boolean {
    return school.linkHealth.availableProgramCount === 0;
  }

  async function handleAssign(schoolId: string) {
    try {
      await assignSchool.mutateAsync({ programId, schoolId });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('programs.schoolPanel.assignError'),
      });
    }
  }

  async function handleUnassign(schoolId: string) {
    try {
      await unassignSchool.mutateAsync({ programId, schoolId });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title:
          caught instanceof AppError ? caught.message : t('programs.schoolPanel.unassignError'),
      });
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <PublicSpaceAssignmentBlock
        programId={programId}
        programName={programName}
        state={state}
        isAssignedToPublicSpace={isAssignedToPublicSpace}
      />

      <div className="border-t border-line pt-4">
        <h3 className="mb-2 text-sm font-medium text-neutral-700">
          {t('programs.schoolPanel.assignedHeading', { count: assignedSchoolIds.length })}
        </h3>
        {assignedSchoolIds.length === 0 && (
          <EmptyState
            title={t('programs.schoolPanel.noneAssignedTitle')}
            description={t('programs.schoolPanel.noneAssignedDescription')}
          />
        )}
        {assignedQuery.isPending && assignedSchoolIds.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {assignedSchoolIds.map((id) => (
              <Skeleton key={id} className="h-8 w-32" />
            ))}
          </div>
        )}
        {!assignedQuery.isPending && (
          <ul className="flex flex-wrap gap-2">
            {(assignedQuery.data ?? []).map((school) => (
              <li
                key={school.id}
                className="flex items-center gap-2 rounded-full bg-primary-50 py-1 pr-1 pl-3 text-sm text-primary-700"
              >
                {school.name}
                <button
                  type="button"
                  onClick={() => void handleUnassign(school.id)}
                  aria-label={t('programs.schoolPanel.unassignAria', { name: school.name })}
                  className="rounded-full p-1 hover:bg-primary-100"
                  disabled={unassignSchool.isPending}
                >
                  <X size={14} aria-hidden="true" />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div>
        <Input
          label={t('programs.schoolPanel.searchLabel')}
          placeholder={t('programs.schoolPanel.searchPlaceholder')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />

        {optionsQuery.isPending && (
          <div className="mt-2 flex flex-col gap-2">
            {Array.from({ length: 3 }, (_, index) => (
              <Skeleton key={index} className="h-11 w-full" />
            ))}
          </div>
        )}

        {!optionsQuery.isPending && (
          <ul className="mt-2 flex flex-col gap-1">
            {(optionsQuery.data?.items ?? [])
              .filter((school) => !assignedIdSet.has(school.id))
              .map((school) => (
                <li
                  key={school.id}
                  className="flex items-center justify-between gap-3 rounded-lg border border-neutral-200 px-3 py-2"
                >
                  <div>
                    <p className="text-sm text-neutral-900">{school.name}</p>
                    <p className="text-xs text-neutral-500">
                      {school.region}, {school.district}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    {isWithoutAnyProgram(school) && (
                      <Badge variant="warning">
                        <AlertTriangle size={12} aria-hidden="true" className="mr-1 inline" />
                        {t('programs.schoolPanel.noProgramBadge')}
                      </Badge>
                    )}
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => void handleAssign(school.id)}
                    >
                      {t('programs.schoolPanel.assignCta')}
                    </Button>
                  </div>
                </li>
              ))}
          </ul>
        )}
      </div>
    </div>
  );
}
