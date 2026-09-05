import { useTranslation } from 'react-i18next';
import { Divider } from '@/shared/ui/brand';

/**
 * "Qanday ishlaydi" — havoladan hisobotgacha bo'lgan to'rt qadam (docs/01, docs/11 E-1…E-5
 * oqimining qisqacha ommaviy tavsifi).
 *
 * `<ol>` ATAYLAB: qadamlar tartibi ma'noli, skrinrider ham "1 dan 4 gacha" deb o'qiydi.
 */

/** Qadam kalitlari va ularning rang ohanglari (nomlar `marketing.home.steps.items.*` da). */
const STEPS = [
  { key: 'link', tone: 'border-firuza-200 bg-firuza-50 text-firuza-700' },
  { key: 'form', tone: 'border-lojuvard-200 bg-lojuvard-50 text-lojuvard-700' },
  { key: 'tests', tone: 'border-zumrad-200 bg-zumrad-50 text-zumrad-700' },
  { key: 'analysis', tone: 'border-zarhal-200 bg-zarhal-50 text-zarhal-700' },
] as const;

export function HowItWorksSection() {
  const { t } = useTranslation();

  return (
    <section className="py-20 lg:py-28" aria-labelledby="marketing-steps-heading">
      <div className="wrap">
        <div className="mx-auto max-w-2xl text-center">
          <p className="eyebrow text-ink-soft">{t('marketing.home.steps.eyebrow')}</p>
          <h2
            id="marketing-steps-heading"
            className="balance font-display mt-3 text-4xl font-extrabold sm:text-5xl"
          >
            {t('marketing.home.steps.heading')}
          </h2>
          <p className="lead mt-5">{t('marketing.home.steps.lead')}</p>
        </div>

        <Divider className="mx-auto mt-12 max-w-xs" />

        <ol className="mt-12 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((step, index) => {
            const number = String(index + 1).padStart(2, '0');
            return (
              <li key={step.key} className="card card-hover relative overflow-hidden p-8">
                <span className={`chip border ${step.tone}`}>
                  {t('marketing.home.steps.stepLabel', { number })}
                </span>
                <h3 className="font-display mt-5 text-xl font-bold">
                  {t(`marketing.home.steps.items.${step.key}.title`)}
                </h3>
                <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                  {t(`marketing.home.steps.items.${step.key}.text`)}
                </p>
                <span
                  className="font-display pointer-events-none absolute -right-3 -bottom-6 text-[110px] leading-none font-extrabold text-line/70"
                  aria-hidden="true"
                >
                  {number}
                </span>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
}
