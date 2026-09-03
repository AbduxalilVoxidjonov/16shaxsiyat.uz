import { describe, expect, it } from 'vitest';
import { AppError } from '@/shared/api/AppError';
import { parsePublishIssues, type PublishIssue } from './publishIssues';

/** `POST .../publish` ning haqiqiy 400 javobi (`docs/07` 3.4-bo'lim namunasi). */
function publishError(issues: unknown): AppError {
  return AppError.fromProblemDetails(
    {
      title: 'Anketa nashr qilishga tayyor emas.',
      status: 400,
      code: 'TEST_NOT_PUBLISHABLE',
      issues,
    },
    400,
  );
}

describe('parsePublishIssues', () => {
  it("ProblemDetails `issues[]` kengaytmasini o'qiydi", () => {
    const error = publishError([
      {
        code: 'SCALE_TOO_FEW_QUESTIONS',
        scale: 'SUPPORT',
        questionCode: null,
        message: 'Kamida 4 savol kerak, hozir 2',
      },
      {
        code: 'QUESTION_WITHOUT_SCALE',
        scale: null,
        questionCode: 'ST-Q07',
        message: "'ST-Q07' savoli uchun shkala tanlanmagan.",
      },
      // `publishError` ATAYLAB `unknown` qabul qiladi (pastdagi buzuq shakl testlari uchun),
      // shu sabab TO'G'RI holat bu yerda aniq tip bilan bog'lanadi.
    ] satisfies PublishIssue[]);

    expect(parsePublishIssues(error)).toEqual([
      {
        code: 'SCALE_TOO_FEW_QUESTIONS',
        scale: 'SUPPORT',
        questionCode: null,
        message: 'Kamida 4 savol kerak, hozir 2',
      },
      {
        code: 'QUESTION_WITHOUT_SCALE',
        scale: null,
        questionCode: 'ST-Q07',
        message: "'ST-Q07' savoli uchun shkala tanlanmagan.",
      },
    ]);
  });

  it("kengaytma yo'q / kutilmagan shakl → bo'sh ro'yxat (chaqiruvchi umumiy xabarga qaytadi)", () => {
    expect(parsePublishIssues(publishError(undefined))).toEqual([]);
    expect(parsePublishIssues(publishError('nimadir'))).toEqual([]);
    expect(parsePublishIssues(publishError([null, 42, {}]))).toEqual([]);
    expect(parsePublishIssues(new Error('boshqa xato'))).toEqual([]);
    expect(parsePublishIssues(undefined)).toEqual([]);
  });
});
