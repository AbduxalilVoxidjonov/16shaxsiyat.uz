import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowRight } from 'lucide-react';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge';
import { formatDate } from '@/shared/lib/formatDate';
import { ROUTES } from '@/shared/config/routes';
import type { MyAssessment } from '@/shared/api/types';
import { historyActionFor } from '../lib/assessmentStatus';

/**
 * Holat → belgi rangi (`docs/05` `AssessmentStatus` enumi). Noma'lum qiymat kelsa
 * `neutral`ga tushadi — ro'yxat yiqilmaydi.
 */
const STATUS_BADGE_VARIANT: Record<string, BadgeVariant> = {
  Draft: 'primary',
  InProgress: 'primary',
  Completed: 'primary',
  Analyzing: 'warning',
  Analyzed: 'success',
  AnalysisFailed: 'danger',
  Abandoned: 'warning',
};

export interface AssessmentHistoryListProps {
  items: readonly MyAssessment[];
  /** Tugallanmagan qatordagi "Davom ettirish" — `POST /api/me/sessions` `{}` (sahifa bajaradi). */
  /** Sessiya tiklanayotgan payt — tugma yuklanish holatida, qayta bosilmaydi. */
}

/**
 * Kabinetdagi test tarixi (`docs/07` §5.2) — holat, sana, dastur nomi va holatga mos amal
 * (`lib/assessmentStatus.ts`):
 *
 * | Holat | Amal |
 * |---|---|
 * | `Draft` / `InProgress` / `Abandoned` | **"Davom ettirish"** — natija havolasi YO'Q |
 * | `Analyzed` + `resultAvailable` | "Natijani ko'rish" → `/kabinet/natijalar/:id` |
 * | `Completed` / `Analyzing` / `Analyzed` (natija yopiq) / `AnalysisFailed` | "Natija hali ochilmagan" |
 * | noma'lum | amal yo'q |
 *
 * Natija tugmasi FAQAT `resultAvailable` bo'lganda ko'rsatiladi: bayroqni frontend o'zi
 * hisoblamaydi (u ikki sozlamaning VA birlashmasi — `docs/07` §5.6), backend aytadi.
 * Ball/indeks/ishonchlilik maydonlari bu ro'yxatda umuman yo'q (`docs/07` §5.2).
 */
export function AssessmentHistoryList({ items }: AssessmentHistoryListProps) {
  const { t } = useTranslation();

  return (
    <ul className="flex flex-col gap-3">
      {items.map((item) => {
        const statusLabel = t(`account.history.status.${item.status}`, {
          defaultValue: item.status,
        });
        const action = historyActionFor(item);

        return (
          <li key={item.id} className="card flex flex-col gap-4 p-5 sm:flex-row sm:items-center">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h3 className="font-display text-base font-bold text-ink">{item.programName}</h3>
                <Badge variant={STATUS_BADGE_VARIANT[item.status] ?? 'neutral'}>
                  {statusLabel}
                </Badge>
              </div>
              <p className="mt-1.5 text-[13px] text-ink-soft">
                {t('account.history.startedAt', { date: formatDate(item.startedAt) })}
                {item.completedAt
                  ? ` · ${t('account.history.completedAt', { date: formatDate(item.completedAt) })}`
                  : ''}
              </p>
            </div>

            {/*
              Tugallanmagan sessiya — bu yerda TUGMA YO'Q, faqat "Davom etmoqda" belgisi.
              "Davom ettirish" sahifa tepasidagi "Tugallanmagan test" kartasida (egasining
              qarori: bitta amal bir joyda). `action === 'resume'` shu sabab hech narsa chizmaydi.
            */}
            {action === 'result' && (
              <Link to={ROUTES.account.result(item.id)} className="btn btn-md btn-primary shrink-0">
                {t('account.history.openResult')}
                <ArrowRight className="size-4" aria-hidden="true" />
              </Link>
            )}

            {action === 'pending' && (
              <p className="shrink-0 text-[13px] text-ink-muted">
                {t('account.history.resultUnavailable')}
              </p>
            )}
          </li>
        );
      })}
    </ul>
  );
}
