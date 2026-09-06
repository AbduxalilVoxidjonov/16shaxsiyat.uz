import { AlertTriangle, CheckCircle2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { ROUTES } from '@/shared/config/routes';
import {
  availabilityReasonKey,
  findBlockedPrograms,
  isPublicSpaceBlocked,
  type PublicSpaceAvailabilityDto,
  type PublicSpaceProgramDto,
} from '../model/types';

export interface PublicSpaceAvailabilityAlertProps {
  availability: PublicSpaceAvailabilityDto;
  programs: readonly PublicSpaceProgramDto[];
}

/**
 * "Hozir test boshlanadimi" signali — sahifaning ENG YUQORISIDA.
 *
 * 2026-09-03 jonli hodisasi: yagona dastur o'chirilgan edi, butun oqim jimgina o'lib qoldi,
 * panelda esa hech qanday belgi yo'q edi. Shu sabab bu yerda holat ANIQ ko'rsatiladi va
 * to'sqinlik qilayotgan dasturga TO'G'RIDAN-TO'G'RI havola beriladi.
 *
 * Dastur bu yerdan YOQILMAYDI — faollashtirish egasining qarori (vazifa ko'rsatmasi);
 * panel faqat holatni ko'rsatadi va kerakli sahifaga olib boradi.
 */
export function PublicSpaceAvailabilityAlert({
  availability,
  programs,
}: PublicSpaceAvailabilityAlertProps) {
  const { t } = useTranslation();

  if (!isPublicSpaceBlocked(availability)) {
    return (
      <p
        className="flex items-center gap-2 rounded-2xl border border-zumrad-200 bg-zumrad-50 px-4 py-3 text-sm text-zumrad-800"
        data-testid="public-space-availability-ok"
      >
        <CheckCircle2 size={16} className="shrink-0" aria-hidden="true" />
        <span>
          <strong className="font-semibold">{t('publicSpace.availability.okTitle')}</strong>{' '}
          {t('publicSpace.availability.okDescription')}
        </span>
      </p>
    );
  }

  const blockedPrograms = findBlockedPrograms(programs);

  return (
    <div
      role="alert"
      data-testid="public-space-availability-alert"
      className="flex flex-col gap-2 rounded-2xl border border-terakota-200 bg-terakota-50 px-4 py-3 text-sm text-terakota-800"
    >
      <p className="flex items-center gap-2">
        <AlertTriangle size={16} className="shrink-0" aria-hidden="true" />
        <strong className="font-semibold">{t('publicSpace.availability.brokenTitle')}</strong>
      </p>

      <p>{t(availabilityReasonKey(availability.status))}</p>

      {blockedPrograms.map((program) => (
        <p key={program.id} className="flex flex-wrap items-center gap-2">
          <span>
            {t('publicSpace.availability.inactiveProgramNotice', { name: program.nameUz })}
          </span>
          <Link
            to={ROUTES.admin.programDetail(program.id)}
            className="font-semibold underline underline-offset-2"
          >
            {t('publicSpace.availability.goToProgram')}
          </Link>
        </p>
      ))}

      {blockedPrograms.length === 0 && (
        <p>
          <Link
            to={ROUTES.admin.programs}
            className="font-semibold underline underline-offset-2"
          >
            {t('publicSpace.availability.goToPrograms')}
          </Link>
        </p>
      )}
    </div>
  );
}
