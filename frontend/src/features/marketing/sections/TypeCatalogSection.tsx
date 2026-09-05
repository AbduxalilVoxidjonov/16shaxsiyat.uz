import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTES } from '@/shared/config/routes';
import { EmptyState, ErrorState, GirihStar } from '@/shared/ui';
import { useTypeCatalog } from '../api/useTypeCatalog';
import { typeTone } from '../lib/typeTone';

/** Skeleton kartalar soni — katalogdagi yozuvlar soni bilan bir xil (16), sahifa "sakramasligi" uchun. */
const SKELETON_COUNT = 16;

/**
 * `/metodika` sahifasidagi "16 ta shaxsiyat tipi" bo'limi — har bir tip uchun bitta karta
 * (kod + nom + qisqa tavsif), karta o'zi `/metodika/:kod` sahifasiga havola.
 *
 * Kontent `GET /api/public/type-catalog` dan keladi (`docs/07` 1.10-bo'lim) — bu yerda
 * hech qanday tip matni yozilmagan. Ranglar FAQAT kodning birinchi harfiga qarab beriladi
 * (`lib/typeTone.ts`): tiplar guruhlarga bo'linmaydi va guruh nomi ishlatilmaydi
 * (`CLAUDE.md` 6a-qoida).
 *
 * Yuklanish/xato/bo'sh holatlarining uchalasi ham ishlangan (docs/10, 7-bo'lim).
 */
export function TypeCatalogSection() {
  const { t } = useTranslation();
  const { data, isPending, isError, refetch } = useTypeCatalog();
  const types = data?.types ?? [];

  return (
    <section aria-labelledby="methodology-types-heading">
      <div className="mx-auto max-w-2xl text-center">
        <p className="eyebrow text-ink-soft">{t('marketing.types.section.eyebrow')}</p>
        <h2
          id="methodology-types-heading"
          className="balance font-display mt-3 text-3xl font-extrabold sm:text-4xl"
        >
          {t('marketing.types.section.heading')}
        </h2>
        <p className="lead mt-5">{t('marketing.types.section.lead')}</p>
      </div>

      {isPending && (
        <>
          <p role="status" className="mt-12 text-center text-[15px] text-ink-soft">
            {t('marketing.types.section.loading')}
          </p>
          <ul
            aria-hidden="true"
            className="mt-6 grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
          >
            {Array.from({ length: SKELETON_COUNT }, (_, index) => (
              <li key={index} className="card h-44 animate-pulse bg-paper-deep p-7" />
            ))}
          </ul>
        </>
      )}

      {isError && (
        <ErrorState
          className="mt-12"
          title={t('marketing.types.section.errorTitle')}
          description={t('marketing.types.section.errorText')}
          onRetry={() => void refetch()}
        />
      )}

      {!isPending && !isError && types.length === 0 && (
        <EmptyState
          className="mt-12"
          title={t('marketing.types.section.emptyTitle')}
          description={t('marketing.types.section.emptyText')}
        />
      )}

      {types.length > 0 && (
        <ul className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {types.map((type) => {
            const tone = typeTone(type.code);
            return (
              <li key={type.code}>
                <Link
                  to={ROUTES.marketing.type(type.code)}
                  className="card card-hover flex h-full flex-col p-7 no-underline"
                >
                  <span
                    className={`relative grid size-14 place-items-center ${tone.tint} rounded-2xl`}
                  >
                    <GirihStar
                      className={`pointer-events-none absolute inset-0 size-14 ${tone.emblem}`}
                      strokeWidth={2}
                    />
                    <span
                      className={`font-display relative text-[13px] font-extrabold ${tone.code}`}
                    >
                      {type.code}
                    </span>
                  </span>

                  <h3 className="font-display mt-5 text-xl font-bold text-ink">{type.name}</h3>
                  <p className="mt-3 text-[15px] leading-relaxed text-ink-soft">
                    {type.shortDescription}
                  </p>
                  <span
                    className={`mt-5 text-[14px] font-semibold ${tone.code}`}
                    aria-hidden="true"
                  >
                    {t('marketing.types.section.cardHint')} →
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
