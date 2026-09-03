import { useTranslation } from 'react-i18next';
import { Lock } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useCatalogQuestionsQuery } from '../api/useCatalogTestDetailQuery';
import type { CatalogQuestionItem } from '../model/types';

export interface SystemScalesInfoSectionProps {
  testId: string;
}

interface SystemScaleSummary {
  code: string;
  nameUz: string | null;
  descriptionUz: string | null;
  questionCount: number;
}

/**
 * Savollarni `scale` bo'yicha guruhlaydi. Tartib — shkalaning BIRINCHI savoli qaysi o'rinda
 * turgani bo'yicha: seed savollari shkalalar bo'ylab aylanib boradi, shu sabab bu tartib
 * `docs/03` dagi ro'yxat tartibi bilan mos tushadi va har safar bir xil chiqadi.
 *
 * Nom va tavsif SAVOL qatoridan olinadi (`scaleNameUz`/`scaleDescriptionUz`) — ya'ni
 * backend'dan. Frontendda kod → nom jadvali YO'Q: aks holda `Custom` uchun bazadan, tizim
 * uchun locale'dan degan ikki manba paydo bo'lardi.
 */
function groupQuestionsByScale(questions: readonly CatalogQuestionItem[]): SystemScaleSummary[] {
  const byCode = new Map<string, SystemScaleSummary>();

  for (const question of [...questions].sort((a, b) => a.order - b.order)) {
    const existing = byCode.get(question.scale);
    if (existing) {
      existing.questionCount += 1;
      continue;
    }

    byCode.set(question.scale, {
      code: question.scale,
      nameUz: question.scaleNameUz ?? null,
      descriptionUz: question.scaleDescriptionUz ?? null,
      questionCount: 1,
    });
  }

  return [...byCode.values()];
}

/**
 * Tizim metodikasi uchun shkalalar — FAQAT O'QISH bloki.
 *
 * Tahrirlanadigan `ScalesSection` (`docs/07` §3.4: "Shkalalar — faqat `Custom`") tizim
 * metodikasida umuman render qilinmaydi, natijada admin `EI`, `SOCA`, `ART` nima ekanini
 * sahifadan hech qayerdan bilib ololmasdi. Bu blok o'sha bo'shliqni to'ldiradi va uni
 * CRUD bo'limi bilan chalkashtirmaslik uchun qulf ikonkasi + aniq tushuntirish bilan
 * ko'rsatiladi; qo'shish/o'chirish tugmalari YO'Q.
 *
 * Talqin oraliqlari ATAYLAB chizilmaydi: ular `SUM` strategiyasiga tegishli
 * (`docs/03` §6.1), tizim metodikalarida umuman yo'q — bo'sh blok soxta ma'lumot bo'lardi.
 */
export function SystemScalesInfoSection({ testId }: SystemScalesInfoSectionProps) {
  const { t } = useTranslation();
  // Savollar bo'limi bilan BIR XIL so'rov kaliti — TanStack Query keshi tufayli qo'shimcha
  // tarmoq chaqiruvi bo'lmaydi.
  const questionsQuery = useCatalogQuestionsQuery(testId);

  const scales = groupQuestionsByScale(questionsQuery.data ?? []);

  return (
    <Card title={t('catalog.detail.systemScalesHeading')}>
      <p className="flex items-start gap-2 rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
        <Lock size={14} className="mt-0.5 shrink-0 text-neutral-400" aria-hidden="true" />
        <span>{t('catalog.detail.systemScalesNotice')}</span>
      </p>

      {questionsQuery.isPending && <Skeleton className="mt-3 h-24 w-full" />}
      {questionsQuery.isError && (
        <div className="mt-3">
          <ErrorState onRetry={() => void questionsQuery.refetch()} />
        </div>
      )}

      {!questionsQuery.isPending && !questionsQuery.isError && scales.length > 0 && (
        <ul className="mt-3 flex flex-col gap-2">
          {scales.map((scale) => (
            <li
              key={scale.code}
              className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-neutral-200 p-3"
            >
              <div>
                <p className="text-sm font-medium text-neutral-900">
                  {scale.nameUz ?? scale.code}
                  {/* Kod BITTA matn tugunida — `({code})` ni uch bo'lakka bo'lish uni
                      matn bo'yicha izlab bo'lmaydigan qiladi (test ham, screen reader ham). */}
                  {scale.nameUz !== null && (
                    <span className="ml-1 text-neutral-500">{`(${scale.code})`}</span>
                  )}
                </p>
                {scale.descriptionUz && (
                  <p className="text-sm text-neutral-600">{scale.descriptionUz}</p>
                )}
              </div>
              <Badge variant="neutral">
                {t('catalog.scalesList.questionCount', { count: scale.questionCount })}
              </Badge>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
