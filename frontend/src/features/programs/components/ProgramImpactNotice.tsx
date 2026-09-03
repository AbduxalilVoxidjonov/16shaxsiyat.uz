import { useTranslation } from 'react-i18next';
import { Spinner } from '@/shared/ui/Spinner';
import type { ProgramImpact } from '../api/useProgramImpactQuery';

export interface ProgramImpactNoticeProps {
  impact: ProgramImpact | undefined;
  isPending: boolean;
  isError: boolean;
}

/**
 * Tasdiq oynasidagi "bu amal nechta maktabni havolasiz qoldiradi" bloki — 2026-09-03 jonli
 * hodisasi: admin yagona dasturni o'chirdi va HECH QANDAY ogohlantirish ko'rmadi.
 *
 * Amal **taqiqlanmaydi** (admin haqli) — faqat oqibat ko'rsatiladi. Ma'lumot kelmasa
 * "hech kim ta'sirlanmaydi" DEYILMAYDI (`docs/06`: "ma'lumot yo'q ≠ nol") — aniq xato matni
 * chiqadi.
 */
export function ProgramImpactNotice({ impact, isPending, isError }: ProgramImpactNoticeProps) {
  const { t } = useTranslation();

  if (isPending) {
    return (
      <p className="flex items-center gap-2 text-sm text-neutral-600">
        <Spinner size={16} />
        {t('programs.impact.checking')}
      </p>
    );
  }

  if (isError || !impact) {
    return <p className="text-sm text-danger-700">{t('programs.impact.loadError')}</p>;
  }

  if (impact.affectedSchoolCount === 0) {
    return <p className="text-sm text-neutral-600">{t('programs.impact.none')}</p>;
  }

  const hiddenCount = impact.affectedSchoolCount - impact.schools.length;

  return (
    <div className="flex flex-col gap-1 rounded-lg bg-danger-50 p-3 text-sm text-danger-800">
      <p className="font-semibold">
        {t('programs.impact.warning', { count: impact.affectedSchoolCount })}
      </p>
      <ul className="ml-4 list-disc">
        {impact.schools.map((school) => (
          <li key={school.id}>{school.name}</li>
        ))}
        {hiddenCount > 0 && <li>{t('programs.impact.more', { count: hiddenCount })}</li>}
      </ul>
    </div>
  );
}
