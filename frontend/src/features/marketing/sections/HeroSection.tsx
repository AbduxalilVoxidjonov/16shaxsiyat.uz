import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Blob, GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';

/**
 * Bosh sahifaning birinchi ekrani (P45) — chapda sarlavha va CTA, o'ngda namunaviy profil
 * kartasi, pastida raqamlar paneli.
 *
 * Karta ichidagi ko'rsatkichlar DIZAYN ma'lumoti: ular hech qanday real o'quvchidan
 * olinmagan va shu sabab kartada "namuna" yozuvi hamda ostida ochiq izoh turadi (CLAUDE.md
 * MAXSUS DIQQAT: platforma va'da bermaydi, chalg'itmaydi).
 */

/** Namunaviy profil ustunlari: kalit → ko'rsatkich foizi va rang klasslari. */
const SAMPLE_BARS = [
  { key: 'social', percent: 68, bar: 'bg-firuza-500', label: 'text-firuza-700' },
  { key: 'openness', percent: 74, bar: 'bg-zarhal-500', label: 'text-zarhal-700' },
  { key: 'cooperation', percent: 61, bar: 'bg-zumrad-500', label: 'text-zumrad-700' },
  { key: 'discipline', percent: 57, bar: 'bg-lojuvard-500', label: 'text-lojuvard-700' },
] as const;

const TRUST_KEYS = ['link', 'rules', 'noDiagnosis'] as const;

const STAT_KEYS = ['blocks', 'link', 'reliability', 'privacy'] as const;

/** Ro'yxat oldidagi kichik belgi — dekorativ, ma'no yonidagi matnda. */
function CheckIcon() {
  return (
    <svg
      viewBox="0 0 16 16"
      className="mt-0.5 size-3.5 shrink-0 text-zumrad-600"
      fill="none"
      aria-hidden="true"
      focusable="false"
    >
      <path
        d="m3 8.5 3.2 3.2L13 5"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function HeroSection() {
  const { t } = useTranslation();

  return (
    <section className="relative overflow-hidden pt-10 sm:pt-16 lg:pt-24">
      <PatternBackdrop className="-z-10 opacity-40" />
      <Blob className="-top-24 -left-32 -z-10 size-[420px] bg-firuza-200/50" />
      <Blob className="top-32 -right-24 -z-10 size-[380px] bg-binafsha-200/40" />

      <div className="wrap grid items-center gap-14 pb-16 lg:grid-cols-[1.05fr_1fr] lg:gap-10 lg:pb-24">
        <div className="animate-fade-up">
          <p className="chip border border-line-strong bg-paper-card text-ink-soft">
            <span className="size-1.5 rounded-full bg-zumrad-600" aria-hidden="true" />
            {t('marketing.home.hero.badge')}
          </p>

          <h1 className="balance font-display mt-6 text-[42px] leading-[1.05] font-extrabold sm:text-6xl lg:text-[64px]">
            {t('marketing.home.hero.headingLead')}
            <br />
            <span className="relative inline-block">
              <span className="relative z-10">{t('marketing.home.hero.headingHighlight')}</span>
              <span
                className="absolute inset-x-0 bottom-1.5 -z-0 h-4 rounded-sm bg-zarhal-200/80 sm:bottom-2 sm:h-5"
                aria-hidden="true"
              />
            </span>
          </h1>

          <p className="lead mt-6 max-w-xl">{t('marketing.home.hero.lead')}</p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <Link to={ROUTES.marketing.contact} className="btn btn-lg btn-primary">
              {t('marketing.home.hero.primaryCta')}
            </Link>
            <Link to={ROUTES.marketing.methodology} className="btn btn-lg btn-ghost">
              {t('marketing.home.hero.secondaryCta')}
            </Link>
          </div>

          <ul
            aria-label={t('marketing.home.hero.trustListLabel')}
            className="mt-8 flex flex-wrap gap-x-6 gap-y-2 text-[13px] font-medium text-ink-soft"
          >
            {TRUST_KEYS.map((key) => (
              <li key={key} className="flex items-start gap-1.5">
                <CheckIcon />
                {t(`marketing.home.hero.trust.${key}`)}
              </li>
            ))}
          </ul>
        </div>

        <div className="animate-fade-in relative">
          <div
            className="pointer-events-none absolute inset-0 grid place-items-center"
            aria-hidden="true"
          >
            <GirihStar
              className="animate-spin-slow h-[130%] w-[130%] text-line-strong/50"
              strokeWidth={0.5}
            />
          </div>

          <div className="card relative mx-auto max-w-md overflow-hidden p-7 shadow-lift">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="eyebrow text-ink-soft">{t('marketing.home.hero.sample.eyebrow')}</p>
                <p className="font-display mt-1 text-2xl font-extrabold">
                  {t('marketing.home.hero.sample.student')}
                </p>
              </div>
              <span className="chip bg-zumrad-50 text-zumrad-700">
                {t('marketing.home.hero.sample.reliability')}
              </span>
            </div>

            {/*
              `dl` ichida FAQAT `dt`/`dd` (yoki ularni bevosita o'rovchi BITTA `div`)
              turishi mumkin. Ilgari ustun `dl > div > div > dt` bo'lib ketgan edi va
              `axe` ni ikki jiddiy qoida bilan yiqitardi (`definition-list`, `dlitem`) —
              skrinrider uchun ro'yxat butunlay buzilgan hisoblanadi. Shu sabab qator
              endi grid: `dt` chapda, foizli `dd` o'ngda, ustunning o'zi esa ikkinchi
              `dd` ichida (bitta `dt` dan keyin bir nechta `dd` — HTML da to'g'ri).
            */}
            <dl className="mt-7 space-y-4">
              {SAMPLE_BARS.map((item, index) => (
                <div
                  key={item.key}
                  className="grid grid-cols-[1fr_auto] items-baseline gap-x-3"
                >
                  <dt className={`text-[13px] font-bold ${item.label}`}>
                    {t(`marketing.home.hero.sample.bars.${item.key}`)}
                  </dt>
                  <dd className="text-[13px] font-bold text-ink-soft">{item.percent}%</dd>
                  <dd
                    className="col-span-2 mt-1.5 h-2 overflow-hidden rounded-full bg-paper-deep"
                    aria-hidden="true"
                  >
                    <div
                      className={`animate-grow h-full origin-left rounded-full ${item.bar}`}
                      style={{
                        width: `${item.percent}%`,
                        animationDelay: `${180 + index * 120}ms`,
                      }}
                    />
                  </dd>
                </div>
              ))}
            </dl>

            <p className="mt-7 rounded-3xl bg-paper-deep p-4 text-[13px] leading-relaxed text-ink-soft">
              {t('marketing.home.hero.sample.note')}
            </p>
          </div>
        </div>
      </div>

      <div className="wrap">
        <dl
          aria-label={t('marketing.home.hero.statsLabel')}
          className="grid grid-cols-2 gap-px overflow-hidden rounded-4xl border border-line bg-line sm:grid-cols-4"
        >
          {STAT_KEYS.map((key) => (
            <div key={key} className="bg-paper-card px-6 py-7 text-center">
              <dt className="font-display text-3xl font-extrabold text-firuza-700">
                {t(`marketing.home.hero.stats.${key}.value`)}
              </dt>
              <dd className="mt-1 text-[13px] font-medium text-ink-soft">
                {t(`marketing.home.hero.stats.${key}.label`)}
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
