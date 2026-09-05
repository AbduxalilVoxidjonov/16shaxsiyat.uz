import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { GirihStar } from '@/shared/ui/brand';

/**
 * "Maktabga nima beradi" — olti karta. Ikonkalar sof dekorativ (`aria-hidden`), ma'no
 * har doim yonidagi sarlavha va matnda.
 */

/** Karta kaliti → SVG yo'li. `stroke="currentColor"` ota `<svg>` da beriladi. */
const BENEFITS: readonly { key: string; icon: ReactNode }[] = [
  { key: 'overview', icon: <path d="M4 5h16v6H4zM4 15h7v4H4zM15 15h5v4h-5z" /> },
  { key: 'classes', icon: <path d="M4 19V9l8-4 8 4v10M9 19v-5h6v5" /> },
  {
    key: 'reliability',
    icon: <path d="M12 3 5 6v6c0 4 3 7 7 9 4-2 7-5 7-9V6l-7-3Zm-3 9 2 2 4-4" />,
  },
  { key: 'psychologist', icon: <path d="M4 5h16v11H9l-5 4V5ZM8.5 10h.01M12 10h.01M15.5 10h.01" /> },
  {
    key: 'parents',
    icon: (
      <path d="M8 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM17 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM2 20c0-3 2.7-5 6-5s6 2 6 5M15 15c3 0 7 1.6 7 5" />
    ),
  },
  { key: 'privacy', icon: <path d="M6 10V8a6 6 0 0 1 12 0v2M5 10h14v10H5zM12 14v3" /> },
];

export function BenefitsSection() {
  const { t } = useTranslation();

  return (
    <section
      className="relative overflow-hidden py-20 lg:py-28"
      aria-labelledby="marketing-benefits-heading"
    >
      <div className="wrap">
        <div className="mx-auto max-w-2xl text-center">
          <p className="eyebrow text-ink-soft">{t('marketing.home.benefits.eyebrow')}</p>
          <h2
            id="marketing-benefits-heading"
            className="balance font-display mt-3 text-4xl font-extrabold sm:text-5xl"
          >
            {t('marketing.home.benefits.heading')}
          </h2>
          <p className="lead mt-5">{t('marketing.home.benefits.lead')}</p>
        </div>

        <ul className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {BENEFITS.map((item) => (
            <li key={item.key} className="card card-hover relative overflow-hidden p-7">
              <span className="grid size-12 place-items-center rounded-2xl bg-firuza-50 text-firuza-700">
                <svg
                  viewBox="0 0 24 24"
                  className="size-6"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.7"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden="true"
                  focusable="false"
                >
                  {item.icon}
                </svg>
              </span>
              <h3 className="font-display mt-5 text-lg font-bold">
                {t(`marketing.home.benefits.items.${item.key}.title`)}
              </h3>
              <p className="mt-2 text-[15px] leading-relaxed text-ink-soft">
                {t(`marketing.home.benefits.items.${item.key}.text`)}
              </p>
              <GirihStar
                className="pointer-events-none absolute -right-10 -bottom-10 size-28 text-line/60"
                strokeWidth={2}
              />
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
