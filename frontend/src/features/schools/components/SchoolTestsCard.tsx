import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ROUTES } from '@/shared/config/routes';
import { useTestOptionsQuery } from '../api/useTestOptionsQuery';

export interface SchoolTestsCardProps {
  /** `SchoolDetailDto.testIds` — shu maktabga ANIQ biriktirilgan testlar (biriktirilgan tartibda). */
  testIds: readonly string[];
}

/**
 * Maktab detalidagi "Biriktirilgan testlar" (2026-09-23, `docs/07` §3.1). DTO faqat ID beradi —
 * nomlar katalog ro'yxatidan (`GET /api/admin/catalog/tests`) olinadi. "Barcha maktablarga"
 * ochiq testlar va eski dastur biriktirmalari bu ro'yxatga kirmaydi (izoh bilan aytiladi).
 */
export function SchoolTestsCard({ testIds }: SchoolTestsCardProps) {
  const { t } = useTranslation();
  const testsQuery = useTestOptionsQuery(testIds.length > 0);
  const byId = new Map((testsQuery.data ?? []).map((test) => [test.id, test]));

  return (
    <Card title={t('schools.detail.tests.heading')} data-testid="school-tests-card">
      {testIds.length === 0 ? (
        <p className="text-sm text-neutral-600">{t('schools.detail.tests.empty')}</p>
      ) : testsQuery.isPending ? (
        <div className="flex flex-wrap gap-2">
          {testIds.map((id) => (
            <Skeleton key={id} className="h-8 w-40" />
          ))}
        </div>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {testIds.map((id) => {
            const test = byId.get(id);
            return (
              <li key={id}>
                <Link
                  to={ROUTES.admin.catalogTestDetail(id)}
                  className="inline-flex items-center gap-1.5 rounded-full bg-primary-50 px-3 py-1 text-sm text-primary-700 hover:bg-primary-100"
                >
                  {test ? test.nameUz : t('schools.detail.tests.unknown')}
                  {test && test.status !== 'Published' && (
                    <span className="text-xs text-zarhal-800">
                      (
                      {test.status === 'Draft'
                        ? t('schools.form.testStatus.draft')
                        : t('schools.form.testStatus.archived')}
                      )
                    </span>
                  )}
                  {test && test.status === 'Published' && !test.isActive && (
                    <span className="text-xs text-zarhal-800">
                      ({t('schools.form.testStatus.inactive')})
                    </span>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      )}
      <p className="mt-3 text-xs text-neutral-500">{t('schools.detail.tests.publicNote')}</p>
    </Card>
  );
}
