import { useTranslation } from 'react-i18next';

/**
 * Ko'p beriladigan savollar — akkordeon.
 *
 * `<details>/<summary>` ATAYLAB tanlangan: ochish-yopish brauzerning o'zida ishlaydi, ya'ni
 * klaviatura (Tab + Enter/Bo'sh joy) va skrinriderlar uchun qo'shimcha `aria-*` kod yozish
 * shart emas va JS o'chiq bo'lsa ham mazmun o'qiladi.
 */

const FAQ_KEYS = ['grade', 'diagnosis', 'access', 'data', 'ai', 'duration', 'change'] as const;

export function FaqSection() {
  const { t } = useTranslation();

  return (
    <section className="py-20 lg:py-28" aria-labelledby="marketing-faq-heading">
      <div className="wrap grid gap-12 lg:grid-cols-[0.8fr_1.2fr] lg:gap-16">
        <div>
          <p className="eyebrow text-ink-soft">{t('marketing.home.faq.eyebrow')}</p>
          <h2
            id="marketing-faq-heading"
            className="balance font-display mt-3 text-4xl font-extrabold sm:text-5xl"
          >
            {t('marketing.home.faq.heading')}
          </h2>
          <p className="lead mt-5">{t('marketing.home.faq.lead')}</p>
        </div>

        <div className="divide-y divide-line overflow-hidden rounded-4xl border border-line bg-paper-card">
          {FAQ_KEYS.map((key) => (
            <details key={key} className="group p-6 sm:p-7">
              <summary className="font-display flex cursor-pointer list-none items-center justify-between gap-4 text-[17px] font-bold text-ink [&::-webkit-details-marker]:hidden">
                {t(`marketing.home.faq.items.${key}.q`)}
                <span
                  className="grid size-8 shrink-0 place-items-center rounded-full border border-line-strong transition-transform duration-300 group-open:rotate-45"
                  aria-hidden="true"
                >
                  <svg viewBox="0 0 16 16" className="size-3.5" fill="none" focusable="false">
                    <path
                      d="M8 3v10M3 8h10"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                    />
                  </svg>
                </span>
              </summary>
              <p className="mt-4 text-[15px] leading-relaxed text-ink-soft">
                {t(`marketing.home.faq.items.${key}.a`)}
              </p>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}
