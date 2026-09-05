import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import { Skeleton } from '@/shared/ui/Skeleton';
import { Divider, GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import type { StudentResultResponse } from '@/shared/api/types';

/**
 * O'quvchi natijasining KO'RINISHI — ikki sahifa uchun umumiy (`widgets/`, `docs/10` §2:
 * "bir necha feature ishlatadigan katta bloklar"):
 *
 * - `features/public-assessment/pages/StudentResultPage` — maktab oqimidagi joriy sessiya
 *   natijasi (`GET /api/public/sessions/result`, `docs/07` §1.9);
 * - `features/public-account/pages/AccountResultPage` — kabinetdagi ARXIV natija
 *   (`GET /api/me/assessments/{id}/result`, `docs/07` §5.3).
 *
 * Ikkala endpoint ham **aynan bir xil** javob shaklini qaytaradi (`docs/07` §5.3: "§1.9
 * bilan aynan bir xil"), shu sabab ko'rinish nusxa ko'chirilmaydi. `features/*`
 * bir-birini import qilmaydi, shuning uchun umumiy blok shu yerda.
 *
 * **Ko'rsatilmaydi:** aktivlik ballari, `NeedsAttention`, xom ballar, to'liq AI hisobot
 * (CLAUDE.md 9-qoida) — `StudentResultResponse` shartnomasi ham bu maydonlarni umuman
 * o'z ichiga olmaydi.
 *
 * Dizayn eslatmasi: backend o'lcham/ball/diagramma qaytarmagani uchun bu ekranda hech
 * qanday progress-bar yoki foiz KO'RSATILMAYDI — mavjud bo'lmagan ma'lumotni "chizib
 * qo'yish" soxta xulosa bo'lardi (`docs/06` §8).
 */

/** Yuklanish holati — natija kartasining o'lchamlarini takrorlaydi (sakrash bo'lmasin). */
export function StudentResultSkeleton() {
  return (
    <div className="flex flex-col gap-5" aria-hidden="true">
      <Skeleton className="h-56 w-full rounded-5xl bg-line/70" />
      <Skeleton className="h-28 w-full rounded-4xl bg-line/70" />
      <Skeleton className="h-28 w-full rounded-4xl bg-line/70" />
    </div>
  );
}

export interface StudentResultNoticeProps {
  title: string;
  description: string;
  /** Girih nishoni sekin aylansa — "kutish" ma'nosi; aylanmasa — yakuniy, tinch holat. */
  spinning?: boolean;
  action?: ReactNode;
}

/**
 * Natija ekranining "kartasiz" holatlari — tayyorlanmoqda / ko'rsatilmaydi / bu dasturda
 * yo'q. Uchchalasi ham xato EMAS (odatiy oqim), shu sabab qizil "xato" uslubi ataylab
 * ishlatilmagan: bir xil vazmin qog'oz karta, girih nishoni va tushuntirish matni.
 */
export function StudentResultNotice({
  title,
  description,
  spinning = false,
  action,
}: StudentResultNoticeProps) {
  return (
    <section className="relative overflow-hidden rounded-5xl border border-line bg-paper-card px-6 py-14 text-center shadow-soft">
      <PatternBackdrop className="opacity-25" />
      <div className="animate-fade-up relative flex flex-col items-center gap-4">
        <span className="relative grid size-24 place-items-center">
          <GirihStar
            className={
              spinning
                ? 'animate-spin-slow absolute inset-0 text-firuza-200'
                : 'absolute inset-0 text-line-strong'
            }
            strokeWidth={1.5}
          />
          <GirihStar
            className={
              spinning
                ? 'absolute inset-[24%] text-firuza-400'
                : 'absolute inset-[24%] text-ink-faint'
            }
            strokeWidth={2}
            withCircle={false}
          />
        </span>
        <h1 className="font-display balance text-xl font-extrabold tracking-tight text-ink sm:text-2xl">
          {title}
        </h1>
        <p className="max-w-sm text-[15px] leading-relaxed text-ink-soft">{description}</p>
        {action}
      </div>
    </section>
  );
}

export interface StudentResultViewProps {
  result: StudentResultResponse;
}

/** Tayyor natija: tip emblemasi, kuchli tomonlar, yo'nalishlar va majburiy eslatma. */
export function StudentResultView({ result }: StudentResultViewProps) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-6">
      {/*
        Emblema rangi ATAYLAB har doim brend firuzasi: kodni guruh/oila rangiga bog'lash
        begona tasniflash tizimini takrorlash bo'lardi (CLAUDE.md 6a — huquqiy qoida).
        Kod matni oq, `firuza-500 → firuza-800` gradient ustida va `text-3xl` (yirik, qalin)
        — eng och burchakda ham WCAG AA (yirik matn uchun 3:1) chegarasidan yuqori.
      */}
      <section className="relative overflow-hidden rounded-5xl border border-line bg-firuza-50/70 px-6 py-10 text-center shadow-soft sm:px-8 sm:py-12">
        <PatternBackdrop className="opacity-30" />
        <GirihStar
          className="animate-spin-slow pointer-events-none absolute -top-20 -right-20 size-64 text-firuza-200/60"
          strokeWidth={1.5}
        />

        <div className="animate-fade-up relative flex flex-col items-center gap-5">
          <span className="relative grid size-28 shrink-0 place-items-center rounded-[26%] bg-linear-to-br from-firuza-500 to-firuza-800 shadow-glow sm:size-32">
            <GirihStar className="absolute inset-[10%] text-white/30" strokeWidth={1.5} />
            <GirihStar
              className="absolute inset-[26%] text-white/20"
              strokeWidth={1.5}
              withCircle={false}
            />
            <span className="font-display relative text-3xl font-extrabold tracking-[0.14em] text-white">
              {result.personalityType}
            </span>
          </span>

          <div>
            <p className="eyebrow">{t('pages.result.title')}</p>
            <h1 className="font-display balance mt-2 text-3xl font-extrabold tracking-tight text-ink sm:text-4xl">
              {result.typeName}
            </h1>
          </div>

          {result.shortDescription && (
            <p className="max-w-prose text-[15px] leading-relaxed text-ink-soft sm:text-base">
              {result.shortDescription}
            </p>
          )}
        </div>
      </section>

      {(result.topStrengths.length > 0 || result.careerFields.length > 0) && <Divider />}

      {result.topStrengths.length > 0 && (
        <section className="card p-6 sm:p-7">
          <h2 className="font-display text-lg font-extrabold tracking-tight text-ink">
            {t('pages.result.strengthsHeading')}
          </h2>
          <ul className="mt-4 flex flex-col gap-3">
            {result.topStrengths.map((strength) => (
              <li key={strength} className="flex gap-3 text-[15px] leading-relaxed text-ink-soft">
                <span
                  aria-hidden="true"
                  className="mt-2 size-1.5 shrink-0 rounded-full bg-firuza-500"
                />
                {strength}
              </li>
            ))}
          </ul>
        </section>
      )}

      {result.careerFields.length > 0 && (
        <section className="card p-6 sm:p-7">
          <h2 className="font-display text-lg font-extrabold tracking-tight text-ink">
            {t('pages.result.careerFieldsHeading')}
          </h2>
          <ul className="mt-4 flex flex-wrap gap-2">
            {result.careerFields.map((field) => (
              <li key={field} className="chip bg-lojuvard-50 text-lojuvard-700">
                {field}
              </li>
            ))}
          </ul>
        </section>
      )}

      {/*
        CLAUDE.md 6-qoidasi: "AI tashxis qo'ymaydi" izohi natija sahifasida KO'RINARLI joyda
        bo'lishi shart — shu sabab u kichik kulrang izoh emas, alohida rangli panel.
      */}
      <aside className="flex items-start gap-3 rounded-4xl border border-zarhal-200 bg-zarhal-50 px-5 py-4">
        <Info className="mt-0.5 size-5 shrink-0 text-zarhal-700" aria-hidden="true" />
        <p className="text-sm leading-relaxed text-ink-soft">{result.note}</p>
      </aside>
    </div>
  );
}
