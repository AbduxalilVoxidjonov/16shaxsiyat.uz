import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import type { BigFiveResult, Mbti16Result, RiasecResult } from '../model/profileTypes';

export interface StudentSummaryCardsProps {
  mbti16: Mbti16Result | null | undefined;
  bigFive: BigFiveResult | null | undefined;
  riasec: RiasecResult | null | undefined;
  activityIndex: number | null | undefined;
  activityLevelText: string | null | undefined;
}

/**
 * Holland kodining birinchi harfidan ustuvor RIASEC tipi nomini olish uchun — docs/11 A-5
 * maketida "IRA" ostida "Tadqiqotch(i)" kabi tip nomi ko'rsatilgan. `widgets/RiasecChart.tsx`
 * bilan bir xil i18n kalitlaridan (`widgets.riasecChart.type.*`) foydalanadi, lekin
 * harf → i18n-kalit moslamasi bu yerda mustaqil (widgets `features/*`dan import
 * qilinmaydi va aksincha — feature widget'ning ICHKI xaritasini import qilmaydi, docs/10 2-bo'lim).
 *
 * `resultCode` HARFLARDAN iborat (`"IRA"`, docs/07 3.2), tarjima kalitlari esa `scale`
 * kodlari bilan nomlangan — shu sabab moslama kerak.
 */
const SHORT_LETTER_TO_TYPE_CODE: Record<string, string> = {
  R: 'R',
  I: 'I',
  A: 'ART',
  S: 'SOC',
  E: 'ENT',
  C: 'CONV',
};

function SummaryCard({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  hint: string | null;
}) {
  return (
    <Card className="flex flex-col gap-1">
      <span className="text-xs font-medium text-neutral-500">{label}</span>
      <span className="text-2xl font-bold text-neutral-900">{value}</span>
      {hint && <span className="text-xs text-neutral-500">{hint}</span>}
    </Card>
  );
}

/**
 * Yig'ma kartalar (4 ta) — docs/11 A-5, P25 2-band: shaxsiyat tipi, Yetuklik indeksi,
 * Aktivlik indeksi, Holland kodi. Har biri katta raqam + qisqa izoh. Har bir manba
 * mustaqil ravishda `null`/`undefined` bo'lishi mumkin (masalan faqat bitta test bloki
 * tugallangan) — CLAUDE.md "Bo'sh ma'lumotga chidamlilik" shu yerda kartani "—" bilan
 * ko'rsatib yiqilmasdan davom etadi.
 */
export function StudentSummaryCards({
  mbti16,
  bigFive,
  riasec,
  activityIndex,
  activityLevelText,
}: StudentSummaryCardsProps) {
  const { t } = useTranslation();
  const dominantTypeCode = riasec ? SHORT_LETTER_TO_TYPE_CODE[riasec.resultCode[0] ?? ''] : undefined;

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {/* Egasining talabi (2026-09-03): tipning TO'LIQ NOMI asosiy, 4 harfli kod ikkinchi
          darajali (`ART`/`Artistik` bilan bir xil naqsh) — yolg'iz `INTJ` tushunarsiz.
          `typeName` bo'sh bo'lsa (`TypeCatalog`da yozuv yo'q) faqat kod ko'rsatiladi:
          soxta nom O'YLAB TOPILMAYDI, `docs/03` ning o'zi ham shunday qilishni talab qiladi. */}
      <SummaryCard
        label={t('studentProfile.cards.personalityType')}
        value={mbti16?.typeName || mbti16?.resultCode || '—'}
        hint={
          mbti16?.resultCode
            ? mbti16.typeName
              ? mbti16.resultCode
              : null
            : t('studentProfile.cards.noData')
        }
      />
      <SummaryCard
        label={t('studentProfile.cards.maturityIndex')}
        value={bigFive?.maturityIndex != null ? bigFive.maturityIndex.toFixed(1) : '—'}
        hint={bigFive?.maturityLevel ?? t('studentProfile.cards.noData')}
      />
      <SummaryCard
        label={t('studentProfile.cards.activityIndex')}
        value={activityIndex !== null && activityIndex !== undefined ? activityIndex.toFixed(1) : '—'}
        hint={activityLevelText ?? t('studentProfile.cards.noData')}
      />
      <SummaryCard
        label={t('studentProfile.cards.hollandCode')}
        value={riasec?.resultCode ?? '—'}
        hint={
          dominantTypeCode
            ? t(`widgets.riasecChart.type.${dominantTypeCode}`)
            : t('studentProfile.cards.noData')
        }
      />
    </div>
  );
}
