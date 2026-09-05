import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArchTop } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';

/**
 * Bosh sahifadagi qisqa metodika bloki — chapda yopishqoq (sticky) matn, o'ngda to'rt blok
 * kartasi. To'liq tavsif `/metodika` sahifasida (`MethodologyPage`).
 */

/** Blok kaliti → tartib raqami ohangi. */
const BLOCKS = [
  { key: 'style', tone: 'bg-firuza-50 text-firuza-700' },
  { key: 'fiveFactor', tone: 'bg-binafsha-50 text-binafsha-700' },
  { key: 'interests', tone: 'bg-lojuvard-50 text-lojuvard-700' },
  { key: 'activity', tone: 'bg-zarhal-50 text-zarhal-700' },
] as const;

export function MethodBlocksSection() {
  const { t } = useTranslation();

  return (
    <section
      className="relative overflow-hidden border-y border-line bg-paper-deep py-20 lg:py-28"
      aria-labelledby="marketing-method-heading"
    >
      <div className="wrap grid gap-12 lg:grid-cols-[0.9fr_1.1fr] lg:gap-16">
        <div className="lg:sticky lg:top-28 lg:self-start">
          <ArchTop className="h-10 w-20 text-line-strong" />
          <p className="eyebrow mt-6 text-ink-soft">{t('marketing.home.method.eyebrow')}</p>
          <h2
            id="marketing-method-heading"
            className="balance font-display mt-3 text-4xl font-extrabold sm:text-5xl"
          >
            {t('marketing.home.method.heading')}
          </h2>
          <p className="lead mt-5">{t('marketing.home.method.lead')}</p>
          <Link to={ROUTES.marketing.methodology} className="btn btn-md btn-dark mt-8">
            {t('marketing.home.method.cta')}
          </Link>
        </div>

        <ol className="space-y-4">
          {BLOCKS.map((block, index) => (
            <li key={block.key} className="card p-6 sm:p-7">
              <div className="flex items-start gap-4">
                <span
                  className={`font-display grid size-11 shrink-0 place-items-center rounded-2xl text-base font-extrabold ${block.tone}`}
                  aria-hidden="true"
                >
                  {index + 1}
                </span>
                <div>
                  <h3 className="font-display text-xl font-bold">
                    {t(`marketing.home.method.items.${block.key}.title`)}
                  </h3>
                  <p className="mt-2 text-[15px] leading-relaxed text-ink-soft">
                    {t(`marketing.home.method.items.${block.key}.text`)}
                  </p>
                </div>
              </div>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
