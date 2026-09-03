import { useState, type ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Pencil, RefreshCw, Users } from 'lucide-react';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Badge } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { formatDate } from '@/shared/lib/formatDate';
import { ROUTES } from '@/shared/config/routes';
import { useSchoolDetailQuery } from '../api/useSchoolDetailQuery';
import { SchoolLinkCell } from '../components/SchoolLinkCell';
import { SchoolLinkHealthBadge } from '../components/SchoolLinkHealthBadge';
import { SchoolQrModal, type SchoolQrModalData } from '../components/SchoolQrModal';
import { SchoolFormDialog } from '../components/SchoolFormDialog';
import { RegenerateLinkDialog } from '../components/RegenerateLinkDialog';
import { SchoolStatsCards } from '../components/SchoolStatsCards';

/** Ma'lumot bo'lmagan matnli maydon uchun `—` (nol bilan chalkashmaydi — nol faqat sonlarda). */
function orDash(value: string | null | undefined): string {
  return value && value.trim() !== '' ? value : '—';
}

function InfoRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs font-medium text-neutral-500">{label}</dt>
      <dd className="text-sm text-neutral-900">{children}</dd>
    </div>
  );
}

/**
 * Maktab ichki sahifasi (`/admin/schools/:id`) — egasining talabi: "maktabning ichiga
 * kirilganda, o'sha maktabda nechta odam test topshirgani ko'rinishi kerak".
 *
 * Sahifada maktab ma'lumotlari (nom, raqam, viloyat/tuman, aloqa, havola/QR, holat) va
 * ishtirok statistikasi (`SchoolStatsCards`) ko'rsatiladi. O'quvchilar jadvali BU YERDA
 * TAKRORLANMAYDI — mavjud `features/students` ro'yxatiga `?schoolId=` filtri bilan havola
 * beriladi (`docs/11` A-4: filtr paneli maktabni URL'dan o'qiydi).
 */
export default function SchoolDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const detailQuery = useSchoolDetailQuery(id ?? null);

  const [editOpen, setEditOpen] = useState(false);
  const [qrOpen, setQrOpen] = useState(false);
  const [regenerateOpen, setRegenerateOpen] = useState(false);
  /**
   * Havola yangilangandan keyin QR modal DARHOL ochiladi — bu paytda `detailQuery` hali
   * qayta yuklanmagan bo'lishi mumkin, shu sabab QR/havola `regenerate-link` JAVOBIDAN
   * olinadi (aks holda admin ESKI, endi ishlamaydigan QR ni chop etib yuborishi mumkin edi).
   * Modal yopilganda tozalanadi — keyingi safar yangilangan `detailQuery` ma'lumoti ishlatiladi.
   */
  const [freshLink, setFreshLink] = useState<Pick<SchoolQrModalData, 'publicUrl' | 'qrCodeBase64'> | null>(null);

  usePageTitle(detailQuery.data?.name ?? t('pages.schoolDetail.title'));

  const backLink = (
    <Link
      to={ROUTES.admin.schools}
      className="flex w-fit items-center gap-1.5 text-sm text-neutral-500 hover:text-neutral-900"
    >
      <ArrowLeft size={16} aria-hidden="true" />
      {t('schools.detail.backToList')}
    </Link>
  );

  if (detailQuery.isPending) {
    return (
      <div className="flex flex-col gap-4">
        {backLink}
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <div className="flex flex-col gap-4">
        {backLink}
        <ErrorState
          title={t('schools.detail.notFoundTitle')}
          description={t('schools.detail.notFoundDescription')}
          onRetry={() => void detailQuery.refetch()}
        />
      </div>
    );
  }

  const school = detailQuery.data;

  return (
    <div className="flex flex-col gap-4">
      {backLink}

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-neutral-900">{school.name}</h1>
          <p className="text-sm text-neutral-500">
            {school.region}, {school.district}
          </p>
          <div className="mt-2">
            <Badge variant={school.isActive ? 'success' : 'neutral'}>
              {school.isActive
                ? t('schools.statusBadge.active')
                : t('schools.statusBadge.inactive')}
            </Badge>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
            <Pencil size={14} aria-hidden="true" />
            {t('schools.actions.edit')}
          </Button>
          <Button variant="outline" size="sm" onClick={() => setRegenerateOpen(true)}>
            <RefreshCw size={14} aria-hidden="true" />
            {t('schools.actions.regenerateLink')}
          </Button>
        </div>
      </div>

      <SchoolStatsCards stats={school.stats} />

      <Card title={t('schools.detail.info.heading')}>
        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <InfoRow label={t('schools.detail.info.schoolNumber')}>
            {orDash(school.schoolNumber)}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.contactPerson')}>
            {orDash(school.contactPerson)}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.contactPhone')}>
            {orDash(school.contactPhone)}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.dailyLimit')}>
            {school.dailyRegistrationLimit}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.accessCode')}>
            {orDash(school.accessCode)}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.createdAt')}>
            {formatDate(school.createdAt)}
          </InfoRow>
          <InfoRow label={t('schools.detail.info.link')}>
            <SchoolLinkCell
              publicUrl={school.publicUrl}
              linkHealth={school.linkHealth}
              onShowQr={() => setQrOpen(true)}
            />
          </InfoRow>
          <InfoRow label={t('schools.linkHealth.detailHeading')}>
            <SchoolLinkHealthBadge linkHealth={school.linkHealth} showReason />
          </InfoRow>
          <InfoRow label={t('schools.detail.info.notes')}>{orDash(school.notes)}</InfoRow>
        </dl>
      </Card>

      <Card title={t('schools.detail.students.heading')}>
        <p className="mb-3 text-sm text-neutral-600">
          {t('schools.detail.students.description', { count: school.stats.studentCount })}
        </p>
        <Link
          to={`${ROUTES.admin.students}?schoolId=${school.id}`}
          className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 px-3 py-1.5 text-sm font-medium text-neutral-900 hover:bg-neutral-50"
        >
          <Users size={14} aria-hidden="true" />
          {t('schools.detail.students.cta')}
        </Link>
      </Card>

      <SchoolFormDialog open={editOpen} schoolId={school.id} onClose={() => setEditOpen(false)} />

      {regenerateOpen && (
        <RegenerateLinkDialog
          open
          schoolId={school.id}
          schoolName={school.name}
          onClose={() => setRegenerateOpen(false)}
          onSuccess={(result) => {
            setFreshLink({ publicUrl: result.publicUrl, qrCodeBase64: result.qrCodeBase64 });
            setQrOpen(true);
          }}
        />
      )}

      <SchoolQrModal
        open={qrOpen}
        onClose={() => {
          setQrOpen(false);
          setFreshLink(null);
        }}
        data={{
          schoolName: school.name,
          slug: school.slug,
          publicUrl: freshLink?.publicUrl ?? school.publicUrl,
          qrCodeBase64: freshLink?.qrCodeBase64 ?? school.qrCodeBase64,
        }}
      />
    </div>
  );
}
