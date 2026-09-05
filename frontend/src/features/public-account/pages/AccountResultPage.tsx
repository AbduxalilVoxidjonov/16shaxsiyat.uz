import { Link, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ErrorState } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import {
  StudentResultNotice,
  StudentResultSkeleton,
  StudentResultView,
} from '@/widgets/StudentResultView';
import { useMyAssessmentResult } from '../api/useMyAssessmentResult';

/**
 * `/kabinet/natijalar/:assessmentId` — kabinetdagi ARXIV natija (`docs/07` §5.3).
 *
 * Ko'rinish maktab oqimidagi natija ekrani bilan UMUMIY (`widgets/StudentResultView`) —
 * javob shakli §1.9 bilan aynan bir xil. Farqi faqat holatlarda: bu yerda `410
 * SESSION_EXPIRED` YO'Q (arxiv muddatsiz), lekin `404` bor — sessiya mavjud emas yoki
 * boshqa foydalanuvchiga tegishli (egalik `id` bilan emas, JWT bilan aniqlanadi).
 */
export default function AccountResultPage() {
  const { t } = useTranslation();
  const { assessmentId = '' } = useParams<{ assessmentId: string }>();
  const resultQuery = useMyAssessmentResult(assessmentId);

  usePageTitle(t('pages.result.title'));

  const backLink = (
    <Link to={ROUTES.account.home} className="btn btn-md btn-ghost self-start">
      <ArrowLeft className="size-4" aria-hidden="true" />
      {t('account.result.back')}
    </Link>
  );

  function renderBody() {
    if (resultQuery.isPending) {
      return <StudentResultSkeleton />;
    }

    if (resultQuery.isError) {
      const error = resultQuery.error;

      // `202` — AI tahlili hali navbatda (`docs/07` §5.3).
      if (error instanceof AppError && error.status === 202) {
        return (
          <StudentResultNotice
            spinning
            title={t('pages.result.notReadyTitle')}
            description={t('pages.result.notReadyDescription')}
            action={
              <button
                type="button"
                className="btn btn-md btn-ghost mt-2"
                onClick={() => void resultQuery.refetch()}
              >
                {t('common.retry')}
              </button>
            }
          />
        );
      }

      // `403` — natija ko'rsatish o'chirilgan (global rubilnik, `docs/07` §5.6).
      if (error instanceof AppError && error.status === 403) {
        return (
          <StudentResultNotice
            title={t('pages.result.forbiddenTitle')}
            description={t('pages.result.forbiddenDescription')}
          />
        );
      }

      // `404` — bunday sessiya yo'q yoki begona. Ataylab bitta xabar: "yo'q" va "sizniki
      // emas" ni ajratish mavjudlik oracle'i bo'lardi (`docs/07` §5.3 izohi).
      if (error instanceof AppError && error.status === 404) {
        return (
          <StudentResultNotice
            title={t('account.result.notFoundTitle')}
            description={t('account.result.notFoundDescription')}
          />
        );
      }

      return <ErrorState onRetry={() => void resultQuery.refetch()} />;
    }

    const result = resultQuery.data;

    // Shaxsiyat batareyasisiz dasturda backend `200` bilan BO'SH maydonlar qaytaradi —
    // bo'sh tip kartasi "0" ko'rsatish bilan bir xil xato bo'lardi (`docs/06` §8).
    if (!result.personalityType) {
      return (
        <StudentResultNotice
          title={t('publicAssessment.noBattery.title')}
          description={t('publicAssessment.noBattery.description')}
        />
      );
    }

    return <StudentResultView result={result} />;
  }

  return (
    <div className="wrap flex max-w-3xl flex-col gap-6 py-12 sm:py-16">
      {backLink}
      {renderBody()}
    </div>
  );
}
