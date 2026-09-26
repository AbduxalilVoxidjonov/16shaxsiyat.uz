import { useTranslation } from 'react-i18next';
import { Copy, Mail, Phone } from 'lucide-react';
import { formatDate, formatDateTime } from '@/shared/lib/formatDate';
import { formatUzPhone } from '@/shared/lib/formatPhone';
import { useToast } from '@/shared/ui/useToast';
import type { StudentDetailDto } from '../model/profileTypes';

export interface StudentRegistrationInfoProps {
  student: StudentDetailDto;
}

interface InfoItem {
  key: string;
  label: string;
  value: string;
  /** `tel:`/`mailto:` havolasi — telefon va email bosilganda to'g'ridan-to'g'ri qo'ng'iroq/xat. */
  href?: string;
  icon?: 'phone' | 'mail';
  /** Nusxa olish tugmasi — raqamni boshqa joyga (Telegram, CRM) tez ko'chirish uchun. */
  copyValue?: string;
}

/**
 * "Ro'yxatdan o'tish ma'lumotlari" — o'quvchi anketa boshida kiritgan hamma narsa (telefon,
 * ota-ona telefoni, email, tug'ilgan sana, superadmin qo'shgan "o'z maydonlari") profil
 * sarlavhasi ostida bir qarashda ko'rinadi (2026-09-26, egasining talabi). Bo'sh maydonlar
 * chizilmaydi — forma ularni `Optional`/`Hidden` qilgan bo'lishi mumkin.
 */
export function StudentRegistrationInfo({ student }: StudentRegistrationInfoProps) {
  const { t } = useTranslation();
  const toast = useToast();

  const items: InfoItem[] = [];

  if (student.phone) {
    items.push({
      key: 'phone',
      label: t('studentProfile.registration.phone'),
      value: formatUzPhone(student.phone),
      href: `tel:${student.phone}`,
      icon: 'phone',
      copyValue: student.phone,
    });
  }
  if (student.parentPhone) {
    items.push({
      key: 'parentPhone',
      label: t('studentProfile.registration.parentPhone'),
      value: formatUzPhone(student.parentPhone),
      href: `tel:${student.parentPhone}`,
      icon: 'phone',
      copyValue: student.parentPhone,
    });
  }
  if (student.email) {
    items.push({
      key: 'email',
      label: t('studentProfile.registration.email'),
      value: student.email,
      href: `mailto:${student.email}`,
      icon: 'mail',
      copyValue: student.email,
    });
  }
  if (student.birthDate) {
    items.push({
      key: 'birthDate',
      label: t('studentProfile.registration.birthDate'),
      value: formatDate(student.birthDate),
    });
  }
  if (student.gender === 'Male' || student.gender === 'Female') {
    items.push({
      key: 'gender',
      label: t('studentProfile.registration.gender'),
      value: t(`students.enums.gender.${student.gender}`),
    });
  }
  for (const field of student.extraFields) {
    items.push({ key: `extra-${field.code}`, label: field.label, value: field.value });
  }
  items.push({
    key: 'createdAt',
    label: t('studentProfile.registration.registeredAt'),
    value: formatDateTime(student.createdAt),
  });

  async function handleCopy(value: string) {
    try {
      await navigator.clipboard.writeText(value);
      toast.show({ variant: 'success', title: t('studentProfile.registration.copied') });
    } catch {
      toast.show({ variant: 'danger', title: t('studentProfile.registration.copyError') });
    }
  }

  return (
    <section
      aria-labelledby="student-registration-heading"
      className="rounded-2xl border border-line bg-paper-card p-4 shadow-soft"
    >
      <h2
        id="student-registration-heading"
        className="mb-3 font-display text-base font-bold text-ink"
      >
        {t('studentProfile.registration.heading')}
      </h2>
      <dl className="grid grid-cols-1 gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((item) => (
          <div key={item.key} className="min-w-0">
            <dt className="text-xs text-neutral-500">{item.label}</dt>
            <dd className="mt-0.5 flex items-center gap-1.5 text-sm font-medium text-neutral-900">
              {item.icon === 'phone' && (
                <Phone size={14} aria-hidden="true" className="shrink-0 text-neutral-400" />
              )}
              {item.icon === 'mail' && (
                <Mail size={14} aria-hidden="true" className="shrink-0 text-neutral-400" />
              )}
              {item.href ? (
                <a href={item.href} className="break-all text-primary-700 hover:underline">
                  {item.value}
                </a>
              ) : (
                <span className="break-words whitespace-pre-line">{item.value}</span>
              )}
              {item.copyValue && (
                <button
                  type="button"
                  onClick={() => void handleCopy(item.copyValue ?? '')}
                  aria-label={t('studentProfile.registration.copyCta', { label: item.label })}
                  className="rounded p-1 text-neutral-400 hover:bg-neutral-100 hover:text-neutral-700 print:hidden"
                >
                  <Copy size={14} aria-hidden="true" />
                </button>
              )}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
