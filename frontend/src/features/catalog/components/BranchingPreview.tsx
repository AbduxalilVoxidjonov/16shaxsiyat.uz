import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { Badge } from '@/shared/ui/Badge';
import { EmptyState } from '@/shared/ui/EmptyState';
import type { CatalogQuestionItem, CatalogSection } from '../model/types';
import { describeVisibilityRule, toVisibilityEditorQuestion } from '../model/visibilityEditorHelpers';

export interface BranchingPreviewProps {
  sections: CatalogSection[];
  questions: CatalogQuestionItem[];
}

/**
 * Oqim ko'rinishi — `docs/18` §6.3: "qaysi javob qaysi bo'limga olib borishini ko'rsatuvchi
 * sodda ro'yxat" (diagramma kutubxonasi YO'Q, ataylab). Faqat O'QISH: bu yerda hech narsa
 * tahrirlanmaydi, ma'lumot `SectionsSection`/`QuestionsSection` bilan bir xil so'rovlardan.
 */
export function BranchingPreview({ sections, questions }: BranchingPreviewProps) {
  const { t } = useTranslation();

  const sortedSections = [...sections].sort((a, b) => a.displayOrder - b.displayOrder);
  const visibilityQuestions = questions.map(toVisibilityEditorQuestion);
  const questionCountBySection = new Map<string, number>();
  for (const question of questions) {
    if (!question.sectionId) continue;
    questionCountBySection.set(
      question.sectionId,
      (questionCountBySection.get(question.sectionId) ?? 0) + 1,
    );
  }

  return (
    <Card title={t('catalog.branchingPreview.heading')}>
      <p className="mb-3 text-sm text-neutral-500">{t('catalog.branchingPreview.description')}</p>

      {sortedSections.length === 0 ? (
        <EmptyState description={t('catalog.branchingPreview.emptyHint')} />
      ) : (
        <ol className="flex flex-col gap-2">
          {sortedSections.map((section, index) => (
            <li
              key={section.id}
              className="flex flex-wrap items-center gap-2 rounded-lg border border-neutral-200 p-3"
            >
              <Badge variant="neutral">{index + 1}</Badge>
              <span className="font-medium text-neutral-900">
                {section.titleUz} ({section.code})
              </span>
              <Badge variant="neutral">
                {t('catalog.sections.questionCount', {
                  count: questionCountBySection.get(section.id) ?? 0,
                })}
              </Badge>
              <span className="text-sm text-neutral-600">
                {section.visibility
                  ? describeVisibilityRule(section.visibility, visibilityQuestions)
                  : t('catalog.branchingPreview.alwaysVisible')}
              </span>
            </li>
          ))}
        </ol>
      )}
    </Card>
  );
}
