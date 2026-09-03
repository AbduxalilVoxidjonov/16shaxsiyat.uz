import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Select } from '@/shared/ui/Select';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { useRawAnswersQuery } from '../api/useRawAnswersQuery';

export interface RawAnswersDialogProps {
  open: boolean;
  onClose: () => void;
  assessmentId: string | null;
}

const TEST_CODES = ['MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY'] as const;

/** Xom javoblar dialogi — docs/07, 3.3-bo'lim, P25 7-band. */
export function RawAnswersDialog({ open, onClose, assessmentId }: RawAnswersDialogProps) {
  const { t } = useTranslation();
  const [testCode, setTestCode] = useState<(typeof TEST_CODES)[number]>('MBTI16');

  const answersQuery = useRawAnswersQuery(assessmentId ?? undefined, open ? testCode : undefined);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('studentProfile.rawAnswers.title')}
      description={t('studentProfile.rawAnswers.description')}
      className="max-w-2xl"
    >
      <div className="flex flex-col gap-4">
        <Select
          label={t('studentProfile.rawAnswers.testLabel')}
          value={testCode}
          onChange={(event) => setTestCode(event.target.value as (typeof TEST_CODES)[number])}
          options={TEST_CODES.map((code) => ({
            value: code,
            label: t(`studentProfile.rawAnswers.testNames.${code}`),
          }))}
        />

        {answersQuery.isPending && (
          <div className="flex flex-col gap-2">
            <Skeleton className="h-8 w-full" />
            <Skeleton className="h-8 w-full" />
            <Skeleton className="h-8 w-full" />
          </div>
        )}

        {answersQuery.isError && (
          <ErrorState
            description={t('studentProfile.rawAnswers.loadError')}
            onRetry={() => void answersQuery.refetch()}
          />
        )}

        {answersQuery.isSuccess && answersQuery.data.length === 0 && (
          <EmptyState title={t('studentProfile.rawAnswers.empty')} />
        )}

        {answersQuery.isSuccess && answersQuery.data.length > 0 && (
          <div className="max-h-96 overflow-y-auto">
            <Table aria-label={t('studentProfile.rawAnswers.title')}>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('studentProfile.rawAnswers.questionColumn')}</TableHead>
                  <TableHead>{t('studentProfile.rawAnswers.valueColumn')}</TableHead>
                  <TableHead>{t('studentProfile.rawAnswers.durationColumn')}</TableHead>
                  <TableHead>{t('studentProfile.rawAnswers.revisionColumn')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {answersQuery.data.map((answer) => (
                  <TableRow key={answer.questionCode}>
                    <TableCell>{answer.questionText}</TableCell>
                    <TableCell>{answer.rawValue}</TableCell>
                    <TableCell>
                      {t('studentProfile.rawAnswers.durationMs', { count: answer.durationMs })}
                    </TableCell>
                    <TableCell>{answer.revisionCount}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </div>
    </Dialog>
  );
}
