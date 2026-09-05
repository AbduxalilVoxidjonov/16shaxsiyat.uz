import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Divider, GirihStar } from '@/shared/ui/brand';
import { ROUTES } from '@/shared/config/routes';
import { PageHero } from '../components/PageHero';
import { TypeCatalogSection } from '../sections/TypeCatalogSection';

/**
 * Metodika sahifasi (`/metodika`) — P45.
 *
 * Maqsad: platforma nimani o'lchashini, ball berish qanday ishlashini, ishonchlilik qanday
 * tekshirilishini va AI qayerda ishlatilishini OCHIQ aytish. Ohang ataylab quruq va
 * va'dasiz: bu yerda "aniqlaymiz/bashorat qilamiz" degan da'vo yo'q, cheklovlar esa
 * sahifaning oxirida alohida blok bilan yozilgan (CLAUDE.md 6-qoida).
 */

/** To'rt blok: kalit → rang ohangi va tartib raqami uchun klasslar. */
const BLOCKS = [
  { key: 'style', tint: 'bg-firuza-50', text: 'text-firuza-700', supporting: false },
  { key: 'fiveFactor', tint: 'bg-binafsha-50', text: 'text-binafsha-700', supporting: false },
  { key: 'interests', tint: 'bg-lojuvard-50', text: 'text-lojuvard-700', supporting: false },
  { key: 'activity', tint: 'bg-zarhal-50', text: 'text-zarhal-700', supporting: true },
] as const;

const RELIABILITY_CHECKS = ['consistency', 'pattern', 'pace'] as const;

/** Ishonchlilik belgilari — `docs/11` dagi holat ranglari bilan bir xil ohangda. */
const RELIABILITY_STATUSES = [
  { key: 'reliable', tone: 'bg-zumrad-50 text-zumrad-700 border-zumrad-200' },
  { key: 'questionable', tone: 'bg-zarhal-50 text-zarhal-700 border-zarhal-200' },
  { key: 'unreliable', tone: 'bg-terakota-50 text-terakota-700 border-terakota-200' },
] as const;

const AI_ITEMS = ['scoring', 'privacy', 'noDiagnosis', 'human'] as const;

const NOTES = ['percent', 'border', 'repeat'] as const;

export default function MethodologyPage() {
  const { t } = useTranslation();
  usePageTitle(t('marketing.methodology.title'));

  return (
    <>
      <PageHero
        centered
        eyebrow={t('marketing.methodology.hero.eyebrow')}
        heading={t('marketing.methodology.hero.heading')}
        lead={t('marketing.methodology.hero.lead')}
      />

      <div className="wrap py-16 lg:py-20">
        <ol className="space-y-8">
          {BLOCKS.map((block, index) => (
            <li key={block.key} className="card relative overflow-hidden p-8 sm:p-10">
              <GirihStar
                className={`pointer-events-none absolute -top-16 -right-16 size-56 opacity-[0.06] ${block.text}`}
                strokeWidth={2}
              />

              <div className="relative flex flex-wrap items-center gap-4">
                <span
                  className={`font-display grid size-12 place-items-center rounded-2xl text-lg font-extrabold ${block.tint} ${block.text}`}
                  aria-hidden="true"
                >
                  {index + 1}
                </span>
                <div>
                  <h2 className="font-display text-2xl font-extrabold sm:text-3xl">
                    {t(`marketing.methodology.blocks.items.${block.key}.title`)}
                  </h2>
                  <p className="text-[14px] text-ink-soft">
                    {t(`marketing.methodology.blocks.items.${block.key}.question`)}
                  </p>
                </div>
                {block.supporting && (
                  <span className="chip ml-auto border border-line-strong bg-paper-deep text-ink-soft">
                    {t('marketing.methodology.blocks.supportingChip')}
                  </span>
                )}
              </div>

              <div className="relative mt-8 grid gap-6 sm:grid-cols-2">
                <div className={`rounded-4xl border border-line p-6 ${block.tint}`}>
                  <h3 className={`font-display text-lg font-bold ${block.text}`}>
                    {t('marketing.methodology.blocks.measuresLabel')}
                  </h3>
                  <p className="mt-4 text-[15px] leading-relaxed text-ink-soft">
                    {t(`marketing.methodology.blocks.items.${block.key}.measures`)}
                  </p>
                </div>
                <div className="rounded-4xl border border-line bg-paper-deep p-6">
                  <h3 className="font-display text-lg font-bold text-ink">
                    {t('marketing.methodology.blocks.scoringLabel')}
                  </h3>
                  <p className="mt-4 text-[15px] leading-relaxed text-ink-soft">
                    {t(`marketing.methodology.blocks.items.${block.key}.scoring`)}
                  </p>
                </div>
              </div>
            </li>
          ))}
        </ol>

        <Divider className="mx-auto my-16 max-w-xs" />

        {/* Uslub bloki natijasi shu 16 tipdan biriga to'g'ri keladi — shu sabab bo'lim
            bloklar ta'rifidan KEYIN turadi (`prompts`/loyiha egasining talabi: ommaviy
            sahifada 16 tipning har biri haqida ma'lumot bo'lishi kerak). */}
        <TypeCatalogSection />

        <Divider className="mx-auto my-16 max-w-xs" />

        <section aria-labelledby="methodology-reliability-heading">
          <div className="mx-auto max-w-2xl text-center">
            <p className="eyebrow text-ink-soft">
              {t('marketing.methodology.reliability.eyebrow')}
            </p>
            <h2
              id="methodology-reliability-heading"
              className="balance font-display mt-3 text-3xl font-extrabold sm:text-4xl"
            >
              {t('marketing.methodology.reliability.heading')}
            </h2>
            <p className="lead mt-5">{t('marketing.methodology.reliability.lead')}</p>
          </div>

          <ul className="mt-12 grid gap-5 md:grid-cols-3">
            {RELIABILITY_CHECKS.map((key) => (
              <li key={key} className="card p-7">
                <h3 className="font-display text-lg font-bold">
                  {t(`marketing.methodology.reliability.checks.${key}.title`)}
                </h3>
                <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                  {t(`marketing.methodology.reliability.checks.${key}.text`)}
                </p>
              </li>
            ))}
          </ul>

          <h3 className="font-display mt-12 text-center text-xl font-bold">
            {t('marketing.methodology.reliability.statusesHeading')}
          </h3>
          <dl className="mt-6 grid gap-4 md:grid-cols-3">
            {RELIABILITY_STATUSES.map((status) => (
              <div key={status.key} className={`rounded-4xl border p-6 ${status.tone}`}>
                <dt className="font-display text-lg font-bold">
                  {t(`marketing.methodology.reliability.statuses.${status.key}.label`)}
                </dt>
                <dd className="mt-2 text-[15px] leading-relaxed text-ink-soft">
                  {t(`marketing.methodology.reliability.statuses.${status.key}.text`)}
                </dd>
              </div>
            ))}
          </dl>
        </section>

        <Divider className="mx-auto my-16 max-w-xs" />

        <section aria-labelledby="methodology-ai-heading">
          <div className="mx-auto max-w-2xl text-center">
            <p className="eyebrow text-ink-soft">{t('marketing.methodology.ai.eyebrow')}</p>
            <h2
              id="methodology-ai-heading"
              className="balance font-display mt-3 text-3xl font-extrabold sm:text-4xl"
            >
              {t('marketing.methodology.ai.heading')}
            </h2>
            <p className="lead mt-5">{t('marketing.methodology.ai.lead')}</p>
          </div>

          <ul className="mt-12 grid gap-5 sm:grid-cols-2">
            {AI_ITEMS.map((key) => (
              <li key={key} className="card p-7">
                <h3 className="font-display text-lg font-bold">
                  {t(`marketing.methodology.ai.items.${key}.title`)}
                </h3>
                <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                  {t(`marketing.methodology.ai.items.${key}.text`)}
                </p>
              </li>
            ))}
          </ul>
        </section>

        <ul className="mt-16 grid gap-5 md:grid-cols-3">
          {NOTES.map((key) => (
            <li key={key} className="card p-7">
              <h3 className="font-display text-lg font-bold">
                {t(`marketing.methodology.notes.${key}.title`)}
              </h3>
              <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                {t(`marketing.methodology.notes.${key}.text`)}
              </p>
            </li>
          ))}
        </ul>

        <div className="mt-14 rounded-4xl border border-line bg-paper-deep p-10 text-center">
          <h2 className="font-display text-3xl font-extrabold">
            {t('marketing.methodology.cta.heading')}
          </h2>
          <p className="lead mx-auto mt-4 max-w-xl">{t('marketing.methodology.cta.text')}</p>
          <Link to={ROUTES.marketing.contact} className="btn btn-lg btn-primary mt-8">
            {t('marketing.methodology.cta.action')}
          </Link>
        </div>
      </div>
    </>
  );
}
