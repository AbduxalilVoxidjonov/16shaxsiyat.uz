import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

/**
 * `docs/05-database-schema.md`: "ReliabilityFlag | 1 Reliable, 2 Questionable, 3 Unreliable".
 * Widget'lar `features/*`dan import qilmaydi (CLAUDE.md/`docs/10` 2-bo'lim qoidasi widgetlarga
 * ham tegishli — ular bir nechta feature ishlatadi), shu sabab bu 3 qiymatli enum shu yerda
 * mustaqil e'lon qilinadi; `features/students/model/enums.ts`dagi bir xil nomli tip bilan
 * struktura jihatdan mos (string literal union) — chaqiruvchi tomonda qo'shimcha moslashtirish
 * shart emas.
 */
export type ReliabilityFlag = 'Reliable' | 'Questionable' | 'Unreliable';

export interface ReliabilityBadgeProps {
  flag: ReliabilityFlag;
  /** `ReliabilityScore` (0–100) — berilsa yorliq yonida ko'rsatiladi (docs/11 A-5: "[Ishonchli 82]"). */
  score?: number | null;
  className?: string;
}

const VARIANT_CLASSES: Record<ReliabilityFlag, string> = {
  Reliable: 'bg-success-100 text-success-700',
  Questionable: 'bg-warning-100 text-warning-700',
  Unreliable: 'bg-danger-100 text-danger-700',
};

/**
 * Ishonchlilik bayrog'i — 3 holat, tooltip'da tushuntirish (P26 6-band). Bu holat
 * belgisi (`Reliable`/`Questionable`/`Unreliable`) — diagramma emas, shu sabab rang
 * semantik (docs/11, 1-bo'lim rang jadvali: "Muvaffaqiyat/Ogohlantirish/Xavf"), farqli
 * o'laroq diagramma widget'lari (`AxisBar`, `IndexGauge`, …) hech qachon ball darajasiga
 * qarab rang o'zgartirmaydi.
 */
export function ReliabilityBadge({ flag, score, className }: ReliabilityBadgeProps) {
  const { t } = useTranslation();
  const label = t(`widgets.reliabilityBadge.label.${flag}`);
  const explanation = t(`widgets.reliabilityBadge.explanation.${flag}`);

  return (
    <span
      title={explanation}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
        VARIANT_CLASSES[flag],
        className,
      )}
    >
      <span>{label}</span>
      {score !== null && score !== undefined && <span aria-hidden="true">·</span>}
      {score !== null && score !== undefined && <span>{score.toFixed(0)}</span>}
      <span className="sr-only"> — {explanation}</span>
    </span>
  );
}
