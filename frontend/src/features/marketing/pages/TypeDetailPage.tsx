import type { ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { ROUTES } from '@/shared/config/routes';
import { Divider, ErrorState, GirihStar, PatternBackdrop } from '@/shared/ui';
import type { PublicTypeCatalogItem } from '@/shared/api/types';
import { useTypeCatalogEntry } from '../api/useTypeCatalog';
import { typeTone } from '../lib/typeTone';

/**
 * Bitta shaxsiyat tipining ommaviy sahifasi — `/metodika/:kod` (masalan `/metodika/intj`).
 *
 * Kontentning HAMMASI `GET /api/public/type-catalog` javobidan (`docs/07` 1.10-bo'lim):
 * to'liq tavsif, kuchli tomonlar, o'sish yo'nalishlari va kasb maslahatlari. Bu yerda tip
 * matni yozilmagan va tiplar guruhlarga bo'linmagan (`CLAUDE.md` 6a-qoida).
 *
 * Uch holat ham ishlangan: yuklanish, xato va "bunday kod yo'q" (noma'lum kod → topilmadi
 * ekrani, sahifa yiqilmaydi). Har holatda ekranda AYNAN bitta `<h1>` bo'ladi — tanishtiruv
 * ekranlarining e2e talabi (`e2e/tests/screens.e2e.ts`).
 */
export default function TypeDetailPage() {
  const { t } = useTranslation();
  const { kod } = useParams<{ kod: string }>();
  const { type, previous, next, notFound, isPending, isError, refetch } = useTypeCatalogEntry(kod);

  usePageTitle(type ? `${type.code} — ${type.name}` : t('marketing.methodology.title'));

  if (isPending) {
    return (
      <TypeStateScreen heading={t('marketing.types.detail.loading')}>
        <p role="status" className="lead mt-6">
          {t('marketing.types.detail.loading')}
        </p>
      </TypeStateScreen>
    );
  }

  if (isError) {
    return (
      <TypeStateScreen heading={t('marketing.types.detail.errorTitle')}>
        <ErrorState
          className="mt-8"
          title={t('marketing.types.detail.errorTitle')}
          description={t('marketing.types.detail.errorText')}
          onRetry={() => void refetch()}
        />
      </TypeStateScreen>
    );
  }

  if (notFound || !type) {
    return (
      <TypeStateScreen heading={t('marketing.types.detail.notFoundTitle')}>
        <p className="lead mt-6">{t('marketing.types.detail.notFoundText')}</p>
        <Link to={ROUTES.marketing.methodology} className="btn btn-lg btn-primary mt-8">
          {t('marketing.types.detail.notFoundAction')}
        </Link>
      </TypeStateScreen>
    );
  }

  const tone = typeTone(type.code);

  return (
    <>
      <section className="relative overflow-hidden border-b border-line bg-paper-deep">
        <PatternBackdrop className="opacity-50" />
        <div className="wrap relative py-16 sm:py-20">
          <p className="eyebrow text-ink-soft">{t('marketing.types.detail.eyebrow')}</p>

          <div className="mt-6 flex flex-wrap items-center gap-8">
            <span
              className={`relative grid size-24 shrink-0 place-items-center rounded-4xl ${tone.tint}`}
            >
              <GirihStar
                className={`pointer-events-none absolute inset-0 size-24 ${tone.emblem}`}
                strokeWidth={2}
              />
              <span className={`font-display relative text-lg font-extrabold ${tone.code}`}>
                {type.code}
              </span>
            </span>

            <div>
              <h1 className="balance font-display text-4xl font-extrabold sm:text-5xl">
                {type.name}
              </h1>
              <p className="lead mt-4 max-w-2xl">{type.shortDescription}</p>
            </div>
          </div>
        </div>
      </section>

      <div className="wrap py-16 lg:py-20">
        <section aria-labelledby="type-about-heading" className="card p-8 sm:p-10">
          <h2 id="type-about-heading" className="font-display text-2xl font-extrabold sm:text-3xl">
            {t('marketing.types.detail.aboutHeading')}
          </h2>
          <p className="prose-uz mt-5 max-w-prose text-[16px] leading-relaxed text-ink-soft">
            {type.longDescription}
          </p>
        </section>

        <div className="mt-8 grid gap-5 lg:grid-cols-3">
          <TypeList
            headingId="type-strengths-heading"
            heading={t('marketing.types.detail.strengthsHeading')}
            items={type.strengths}
            markerClassName="bg-zumrad-500"
          />
          <TypeList
            headingId="type-growth-heading"
            heading={t('marketing.types.detail.growthHeading')}
            items={type.growthAreas}
            markerClassName="bg-zarhal-500"
          />
          <TypeList
            headingId="type-career-heading"
            heading={t('marketing.types.detail.careerHeading')}
            items={type.careerHints}
            markerClassName="bg-firuza-500"
          />
        </div>

        <section
          aria-labelledby="type-note-heading"
          className="mt-10 rounded-4xl border border-terakota-200 bg-terakota-50 p-8 sm:p-10"
        >
          <h2 id="type-note-heading" className="font-display text-2xl font-extrabold">
            {t('marketing.types.detail.noteTitle')}
          </h2>
          <p className="mt-4 max-w-3xl text-[15px] leading-relaxed text-ink-soft">
            {t('marketing.types.detail.noteText')}
          </p>
        </section>

        <Divider className="mx-auto my-14 max-w-xs" />

        <nav
          aria-label={t('marketing.types.detail.navHeading')}
          className="grid gap-5 sm:grid-cols-2"
        >
          <TypeNavLink label={t('marketing.types.detail.previous')} type={previous} align="start" />
          <TypeNavLink label={t('marketing.types.detail.next')} type={next} align="end" />
        </nav>

        <p className="mt-10 text-center">
          <Link to={ROUTES.marketing.methodology} className="btn btn-md btn-ghost">
            ← {t('marketing.types.detail.backToMethodology')}
          </Link>
        </p>
      </div>
    </>
  );
}

interface TypeListProps {
  headingId: string;
  heading: string;
  items: readonly string[];
  markerClassName: string;
}

/** Kuchli tomonlar / o'sish yo'nalishlari / kasb maslahatlari — bir xil shakldagi ro'yxat kartasi. */
function TypeList({ headingId, heading, items, markerClassName }: TypeListProps) {
  return (
    <section aria-labelledby={headingId} className="card p-7">
      <h2 id={headingId} className="font-display text-lg font-bold">
        {heading}
      </h2>
      <ul className="mt-5 space-y-3">
        {items.map((item) => (
          <li key={item} className="flex gap-3 text-[15px] leading-relaxed text-ink-soft">
            <span
              className={`mt-2 size-1.5 shrink-0 rounded-full ${markerClassName}`}
              aria-hidden="true"
            />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

interface TypeNavLinkProps {
  label: string;
  type: PublicTypeCatalogItem | undefined;
  align: 'start' | 'end';
}

/** Ro'yxatdagi oldingi/keyingi tipga o'tish. Chekkadagi tiplarda mos tomon ko'rsatilmaydi. */
function TypeNavLink({ label, type, align }: TypeNavLinkProps) {
  if (!type) {
    return <span />;
  }

  return (
    <Link
      to={ROUTES.marketing.type(type.code)}
      className={`card card-hover flex flex-col p-6 no-underline ${align === 'end' ? 'sm:items-end sm:text-right' : ''}`}
    >
      {/* `text-ink-faint` EMAS: kichik, kattalashtirilgan `eyebrow` matni sifatida u
          `paper-card` fonida WCAG AA (4.5:1) dan past qoladi va e2e `axe` tekshiruvi buni
          "serious" deb belgilaydi (2026-09-05 da aynan shunday yiqildi). */}
      <span className="eyebrow text-ink-soft">{label}</span>
      <span className="font-display mt-2 text-lg font-bold text-ink">
        {type.code} · {type.name}
      </span>
    </Link>
  );
}

/**
 * Yuklanish/xato/topilmadi ekranlarining umumiy ramkasi — tanishtiruv sahifalarining hero
 * ko'rinishi bilan bir xil, ekranda AYNAN bitta `<h1>` qoladi.
 */
function TypeStateScreen({ heading, children }: { heading: string; children: ReactNode }) {
  const { t } = useTranslation();

  return (
    <section className="relative overflow-hidden border-b border-line bg-paper-deep">
      <PatternBackdrop className="opacity-50" />
      <div className="wrap relative py-16 text-center sm:py-24">
        <p className="eyebrow text-ink-soft">{t('marketing.types.detail.eyebrow')}</p>
        <h1 className="balance font-display mx-auto mt-3 max-w-3xl text-4xl font-extrabold sm:text-5xl">
          {heading}
        </h1>
        <div className="mx-auto max-w-xl">{children}</div>
      </div>
    </section>
  );
}
