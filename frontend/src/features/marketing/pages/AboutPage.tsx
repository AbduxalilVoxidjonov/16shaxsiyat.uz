import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Divider } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { PageHero } from '../components/PageHero';

/**
 * "Biz haqimizda" sahifasi (`/biz-haqimizda`) — P45.
 *
 * Uzun o'qish matni `.prose-uz` konteynerida (paragraf bo'shliqlari va satr balandligi
 * `src/index.css` da bir joyda belgilangan). Matn `dangerouslySetInnerHTML` SIZ, oddiy
 * paragraflar sifatida chiqadi (CLAUDE.md 12-qoida).
 */

const PRINCIPLES = ['language', 'rules', 'privacy', 'noLabels'] as const;

export default function AboutPage() {
  const { t } = useTranslation();
  usePageTitle(t('marketing.about.title'));

  return (
    <>
      <PageHero
        eyebrow={t('marketing.about.hero.eyebrow')}
        heading={t('marketing.about.hero.heading')}
        lead={t('marketing.about.hero.lead')}
      />

      <div className="wrap-narrow py-16 lg:py-20">
        <div className="prose-uz">
          <p>{t('marketing.about.body.p1')}</p>
          <p>{t('marketing.about.body.p2')}</p>
          <p>{t('marketing.about.body.p3')}</p>
        </div>

        <Divider className="my-12" />

        <h2 className="font-display text-3xl font-extrabold">
          {t('marketing.about.methodHeading')}
        </h2>
        <div className="prose-uz mt-6">
          <p>{t('marketing.about.methodBody.p1')}</p>
          <p>{t('marketing.about.methodBody.p2')}</p>
        </div>

        <ul className="mt-12 grid gap-5 sm:grid-cols-2">
          {PRINCIPLES.map((key) => (
            <li key={key} className="card p-7">
              <h3 className="font-display text-lg font-bold">
                {t(`marketing.about.principles.${key}.title`)}
              </h3>
              <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                {t(`marketing.about.principles.${key}.text`)}
              </p>
            </li>
          ))}
        </ul>

        <div className="mt-14 rounded-4xl border border-line bg-paper-deep p-8 text-center">
          <h2 className="font-display text-2xl font-extrabold">
            {t('marketing.about.cta.heading')}
          </h2>
          <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
            {t('marketing.about.cta.text')}
          </p>
          <Link to={ROUTES.marketing.contact} className="btn btn-md btn-dark mt-6">
            {t('marketing.about.cta.action')}
          </Link>
        </div>
      </div>
    </>
  );
}
