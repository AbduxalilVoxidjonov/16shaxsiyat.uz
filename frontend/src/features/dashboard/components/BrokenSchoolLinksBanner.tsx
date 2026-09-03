import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { AlertTriangle } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { ROUTES } from '@/shared/config/routes';
import { useSchoolsLinkHealthQuery } from '@/features/schools/api/useSchoolsLinkHealthQuery';
import { linkHealthReasonKey } from '@/features/schools/model/types';

/**
 * "N ta maktab havolasi ishlamaydi" banneri — 2026-09-03 jonli hodisasi.
 *
 * Admin yagona dasturni o'chirdi, hech qanday ogohlantirish ko'rmadi, va BARCHA maktab
 * havolasi jimgina o'lik bo'lib qoldi (buni faqat havolani brauzerda ochib bilish mumkin
 * edi). Boshqaruv paneli — admin har kuni ochadigan sahifa, shu sabab signal shu yerda.
 *
 * Ma'lumot `GET /api/admin/schools/link-health` dan; mezon ommaviy handler bilan BITTA
 * manbadan (`docs/07` 1.1 va 3.1). So'rov muvaffaqiyatsiz bo'lsa banner UMUMAN chiqmaydi —
 * "ma'lumot yo'q" ni "hammasi joyida" deb ko'rsatmaslik uchun ham, yolg'on ogohlantirish
 * bermaslik uchun ham (`docs/06` qarorlar jurnali: "ma'lumot yo'q ≠ nol").
 */
export function BrokenSchoolLinksBanner() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const linkHealthQuery = useSchoolsLinkHealthQuery();

  const data = linkHealthQuery.data;
  if (!data || data.brokenSchoolCount === 0) {
    return null;
  }

  const hiddenCount = data.brokenSchoolCount - data.schools.length;

  return (
    <section
      role="alert"
      className="flex flex-col gap-2 rounded-xl border border-danger-200 bg-danger-50 p-4"
    >
      <div className="flex items-start gap-2">
        <AlertTriangle size={18} className="mt-0.5 shrink-0 text-danger-600" aria-hidden="true" />
        <div className="flex flex-col gap-1">
          <p className="text-sm font-semibold text-danger-800">
            {t('dashboard.linkHealth.title', { count: data.brokenSchoolCount })}
          </p>
          <p className="text-sm text-danger-700">{t('dashboard.linkHealth.description')}</p>
        </div>
      </div>

      <ul className="ml-7 flex list-disc flex-col gap-0.5 text-sm text-danger-700">
        {data.schools.map((school) => (
          <li key={school.id}>
            <span className="font-medium">{school.name}</span>
            {' — '}
            {t(linkHealthReasonKey(school.linkHealth.status))}
          </li>
        ))}
        {hiddenCount > 0 && <li>{t('dashboard.linkHealth.more', { count: hiddenCount })}</li>}
      </ul>

      <div>
        <Button size="sm" variant="outline" onClick={() => navigate(ROUTES.admin.schools)}>
          {t('dashboard.linkHealth.cta')}
        </Button>
      </div>
    </section>
  );
}
