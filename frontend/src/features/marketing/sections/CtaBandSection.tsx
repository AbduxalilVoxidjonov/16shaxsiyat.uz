import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Blob, GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';

/**
 * Sahifa oxiridagi to'q fonli CTA banner.
 *
 * To'q fonda matn `text-paper` (#FBF8F3) va `text-firuza-300` bilan beriladi — ikkalasi ham
 * `bg-ink` (#191512) ustida WCAG AA dan ancha yuqori kontrast beradi; `text-paper/70` kabi
 * shaffoflik ATAYLAB ishlatilmagan (kontrast pasayadi).
 */
export function CtaBandSection() {
  const { t } = useTranslation();

  return (
    <section className="wrap pb-4" aria-labelledby="marketing-cta-heading">
      <div className="relative overflow-hidden rounded-5xl bg-ink px-8 py-16 text-center sm:px-16 sm:py-20">
        <PatternBackdrop variant="light" />
        <Blob className="-top-20 -left-20 size-72 bg-firuza-500/25" />
        <Blob className="-right-10 -bottom-24 size-80 bg-binafsha-500/25" />
        <GirihStar
          className="animate-spin-slow pointer-events-none absolute top-1/2 -right-16 size-72 -translate-y-1/2 text-white/10"
          strokeWidth={1}
        />

        <div className="relative mx-auto max-w-2xl">
          <p className="text-[11px] font-bold tracking-[0.2em] text-firuza-300 uppercase">
            {t('marketing.home.cta.eyebrow')}
          </p>
          <h2
            id="marketing-cta-heading"
            className="balance font-display mt-4 text-4xl font-extrabold text-paper sm:text-5xl"
          >
            {t('marketing.home.cta.heading')}
          </h2>
          <p className="mt-5 text-lg leading-relaxed text-paper">{t('marketing.home.cta.text')}</p>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <Link
              to={ROUTES.marketing.contact}
              className="btn btn-lg bg-paper text-ink hover:-translate-y-0.5 hover:bg-white"
            >
              {t('marketing.home.cta.primary')}
            </Link>
            <Link
              to={ROUTES.marketing.methodology}
              className="btn btn-lg border border-white/25 text-paper hover:-translate-y-0.5 hover:bg-white/10"
            >
              {t('marketing.home.cta.secondary')}
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
