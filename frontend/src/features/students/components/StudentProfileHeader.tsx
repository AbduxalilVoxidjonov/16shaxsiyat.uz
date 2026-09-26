import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { ArrowLeft, Download, MoreVertical, RefreshCw } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { ReliabilityBadge, type ReliabilityFlag } from '@/widgets/ReliabilityBadge';
import { ROUTES } from '@/shared/config/routes';
import type { AssessmentStatus } from '../model/enums';
import { ASSESSMENT_STATUS_BADGE_VARIANT } from '../model/enums';
import type { StudentDetailDto } from '../model/profileTypes';

export interface StudentProfileHeaderProps {
  student: StudentDetailDto;
  latestAssessmentStatus: AssessmentStatus | null;
  reliabilityFlag: ReliabilityFlag | null;
  reliabilityScore: number | null;
  onDownloadPdf: () => void;
  isDownloadingPdf: boolean;
  onRerunAnalysis: () => void;
  onDelete: () => void;
  hasLatestAssessment: boolean;
}

const GENDER_SYMBOL: Record<StudentDetailDto['gender'], string> = {
  Male: '♂',
  Female: '♀',
  Unspecified: '—',
};

/**
 * Sarlavha bloki — docs/11 A-5 maket, P25 1-band: "FISH, maktab, sinf, yosh, jins, holat
 * `Badge`, `ReliabilityBadge`, amal tugmalari". "⋯" menyusi native `<details>/<summary>`
 * orqali — kichik ro'yxat uchun to'liq ARIA menu naqshidan ko'ra soddaroq va baribir
 * klaviatura bilan ishlaydi (`Enter`/`Space` bilan ochiladi, `Tab` ichidagi tugmalarga o'tadi).
 */
export function StudentProfileHeader({
  student,
  latestAssessmentStatus,
  reliabilityFlag,
  reliabilityScore,
  onDownloadPdf,
  isDownloadingPdf,
  onRerunAnalysis,
  onDelete,
  hasLatestAssessment,
}: StudentProfileHeaderProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [moreOpen, setMoreOpen] = useState(false);

  const classInfo = student.classLetter
    ? `${String(student.grade)}-${student.classLetter}`
    : String(student.grade);

  function closeMore() {
    setMoreOpen(false);
  }

  return (
    <div className="flex flex-col gap-3 print:gap-1">
      <Button
        variant="ghost"
        size="sm"
        onClick={() => navigate(ROUTES.admin.students)}
        className="w-fit print:hidden"
      >
        <ArrowLeft size={16} aria-hidden="true" />
        {t('studentProfile.backCta')}
      </Button>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-neutral-900">{student.fullName}</h1>
          <p className="text-sm text-neutral-600">
            {student.school.name} · {classInfo}-sinf
            {student.age != null && ` · ${String(student.age)} ${t('studentProfile.yearsShort')}`}
            {' '}· {GENDER_SYMBOL[student.gender]}
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            {latestAssessmentStatus && (
              <Badge variant={ASSESSMENT_STATUS_BADGE_VARIANT[latestAssessmentStatus]}>
                {t(`students.enums.status.${latestAssessmentStatus}`)}
              </Badge>
            )}
            {reliabilityFlag && (
              <ReliabilityBadge flag={reliabilityFlag} score={reliabilityScore} />
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2 print:hidden">
          <Button
            variant="outline"
            size="sm"
            onClick={onDownloadPdf}
            isLoading={isDownloadingPdf}
            disabled={!hasLatestAssessment}
          >
            <Download size={16} aria-hidden="true" />
            {t('studentProfile.header.downloadPdfCta')}
          </Button>
          <Button variant="outline" size="sm" onClick={onRerunAnalysis}>
            <RefreshCw size={16} aria-hidden="true" />
            {t('studentProfile.header.rerunAnalysisCta')}
          </Button>

          <details
            open={moreOpen}
            onToggle={(event) => setMoreOpen(event.currentTarget.open)}
            className="relative"
          >
            <summary
              aria-label={t('studentProfile.header.moreActionsCta')}
              className="flex h-9 w-9 cursor-pointer list-none items-center justify-center rounded-lg border border-neutral-300 text-neutral-600 hover:bg-neutral-50 [&::-webkit-details-marker]:hidden"
            >
              <MoreVertical size={16} aria-hidden="true" />
            </summary>
            <div className="absolute right-0 z-10 mt-2 flex w-48 flex-col gap-1 rounded-lg border border-neutral-200 bg-white p-1 shadow-lg">
              <button
                type="button"
                onClick={() => {
                  closeMore();
                  onDelete();
                }}
                className="rounded-md px-3 py-2 text-left text-sm text-danger-600 hover:bg-danger-50"
              >
                {t('studentProfile.header.deleteCta')}
              </button>
            </div>
          </details>
        </div>
      </div>
    </div>
  );
}
