import { AlertTriangle, CheckCircle2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui/Badge';
import { isSchoolLinkBroken, linkHealthReasonKey, type SchoolLinkHealthDto } from '../model/types';

export interface SchoolLinkHealthBadgeProps {
  linkHealth: SchoolLinkHealthDto;
  /** `true` — sabab matni belgi yonida ko'rsatiladi (ichki sahifa). Ro'yxatda faqat `title`. */
  showReason?: boolean;
}

/**
 * "Havola ishlaydimi" belgisi — 2026-09-03 jonli hodisasi: yagona dastur o'chirilgan edi,
 * BARCHA maktab havolasi jimgina o'lik bo'lib qoldi, panelda esa hech qanday belgi yo'q edi.
 *
 * Mezon backendda (`linkHealth`, `docs/07` 3.1) — ommaviy handler bilan AYNAN bir xil manbadan
 * hisoblanadi, frontend uni qayta ixtiro QILMAYDI (aks holda ikkisi ajralib ketardi).
 *
 * Uch holat:
 * - `Ok` — belgi ko'rsatilmaydi (yolg'on ogohlantirish bermaslik ham muhim);
 * - `ProgramsWithoutTests` — sariq: havola ochiladi, lekin testi yo'q;
 * - qolgani — qizil: o'quvchi `409 NO_PROGRAM_AVAILABLE` oladi.
 */
export function SchoolLinkHealthBadge({
  linkHealth,
  showReason = false,
}: SchoolLinkHealthBadgeProps) {
  const { t } = useTranslation();

  if (!isSchoolLinkBroken(linkHealth)) {
    return (
      <span className="inline-flex items-center gap-1 text-xs text-neutral-500">
        <CheckCircle2 size={14} className="text-success-600" aria-hidden="true" />
        {t('schools.linkHealth.okBadge')}
      </span>
    );
  }

  const isWarning = linkHealth.status === 'ProgramsWithoutTests';
  const reason = t(linkHealthReasonKey(linkHealth.status));

  return (
    <span className={showReason ? 'flex flex-col gap-1' : 'inline-flex'}>
      <Badge variant={isWarning ? 'warning' : 'danger'} title={reason}>
        <AlertTriangle size={12} className="mr-1" aria-hidden="true" />
        {isWarning ? t('schools.linkHealth.warningBadge') : t('schools.linkHealth.brokenBadge')}
      </Badge>
      {showReason && <span className="text-xs text-neutral-600">{reason}</span>}
      {!showReason && <span className="sr-only">{reason}</span>}
    </span>
  );
}
