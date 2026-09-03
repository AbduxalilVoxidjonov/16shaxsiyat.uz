import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ChevronDown, Zap } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { Select } from '@/shared/ui/Select';
import { Checkbox } from '@/shared/ui/Checkbox';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import { useRawAnswersQuery } from '../api/useRawAnswersQuery';
import type {
  AnswerScaleSignalDto,
  AnswerThresholdsDto,
  RawAnswerDto,
} from '../model/profileTypes';

export interface AnswersSectionProps {
  assessmentId: string | null;
}

const ALL = '';

/**
 * `Likert5` javob qiymatining SO'Z bilan yorlig'i — `docs/03` §1 jadvali. Xom `5` psixologga
 * hech narsa aytmaydi; ayniqsa teskari savolda u aldaydi (shu sabab yonida "shkalaga qanday
 * tushgani" ustuni ham bor). Faqat `Likert5` uchun: boshqa turlarda (`Likert7`, `Binary`,
 * `ForcedChoice`) hujjatda yorliq jadvali YO'Q, shu sabab xom qiymat ko'rsatiladi — soxta
 * yorliq o'ylab topilmaydi.
 */
const LIKERT5_VALUES = [1, 2, 3, 4, 5] as const;

function isLikert5Value(value: number): value is (typeof LIKERT5_VALUES)[number] {
  return LIKERT5_VALUES.some((candidate) => candidate === value);
}

/** Bitta test bloki — jadval shu guruhlar ichida chiziladi (190 savol bitta ro'yxatda o'qilmaydi). */
interface AnswerGroup {
  testCode: string;
  answers: RawAnswerDto[];
}

/**
 * Savolma-savol javoblar va ularning tahlili — `docs/11` A-5 profil sahifasidagi BO'LIM
 * (oyna emas: egasi buni profilda ko'rishni so'radi, 2026-09-03; oyna ichida turgani uchun
 * u umuman topilmagan edi).
 *
 * <b>Nima uchun AI talqini YO'Q.</b> Har bir savolni alohida AI bilan izohlash TAQIQLANGAN
 * (`CLAUDE.md` 6-band): bitta Likert savolining ishonchliligi deyarli nolga teng — har
 * shkalaga 10–15 savol qo'yilishining butun sababi shu. Yakka javobdan xulosa chiqarish
 * aynan noto'g'ri xulosaga olib boradi. Bu yerdagi ANIQ HISOB (teskari tuzatish, tez javob,
 * straight-lining) AI taxminidan foydaliroq va himoya qilinadigan. Shu ma'lumot AI promptiga
 * ham qo'shilmaydi (`CLAUDE.md` 5-band).
 *
 * <b>Ishlash (190 savol).</b> Bo'lim boshida YOPIQ — so'rov ham yuborilmaydi. Ochilgach
 * bitta so'rov butun sessiyani oladi; test bloklari alohida yig'iladi va HAR BLOK ham
 * mustaqil ochiladi/yopiladi — yopiq blokning qatorlari umuman render qilinmaydi. Jadval
 * `Table` ning `overflow-x` konteyneri ichida (P30-6): 390px da faqat jadval siljiydi,
 * sahifa emas.
 */
export function AnswersSection({ assessmentId }: AnswersSectionProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [testFilter, setTestFilter] = useState<string>(ALL);
  const [scaleFilter, setScaleFilter] = useState<string>(ALL);
  const [onlyFlagged, setOnlyFlagged] = useState(false);
  const [expandedTests, setExpandedTests] = useState<readonly string[]>([]);

  const answersQuery = useRawAnswersQuery(assessmentId ?? undefined, open);
  const data = answersQuery.data;

  const scaleOptions = useMemo(() => {
    if (!data) return [];
    const seen = new Map<string, string>();
    for (const answer of data.answers) {
      if (!seen.has(answer.scale)) {
        seen.set(answer.scale, answer.scaleNameUz ?? answer.scale);
      }
    }
    return [...seen.entries()].map(([code, label]) => ({ code, label }));
  }, [data]);

  const testOptions = useMemo(() => {
    if (!data) return [];
    return [...new Set(data.answers.map((answer) => answer.testCode))];
  }, [data]);

  const groups = useMemo<AnswerGroup[]>(() => {
    if (!data) return [];
    const byTest = new Map<string, RawAnswerDto[]>();
    for (const answer of data.answers) {
      if (testFilter !== ALL && answer.testCode !== testFilter) continue;
      if (scaleFilter !== ALL && answer.scale !== scaleFilter) continue;
      if (onlyFlagged && !answer.isFastAnswer && answer.straightLiningBlockIndex == null) continue;
      const bucket = byTest.get(answer.testCode);
      if (bucket) {
        bucket.push(answer);
      } else {
        byTest.set(answer.testCode, [answer]);
      }
    }
    return [...byTest.entries()].map(([testCode, answers]) => ({ testCode, answers }));
  }, [data, testFilter, scaleFilter, onlyFlagged]);

  // Ziddiyatli shkalalar — `docs/03` §7.1 band 4. Jadvaldagi "Shkala" ustunida shu
  // guruhlar belgilanadi, ya'ni signal AYNAN o'sha shkala qatorlarida ko'rinadi.
  const conflictScales = useMemo(
    () => new Set((data?.scales ?? []).map((scale) => scale.scale)),
    [data],
  );

  function toggleTest(testCode: string) {
    setExpandedTests((current) =>
      current.includes(testCode)
        ? current.filter((code) => code !== testCode)
        : [...current, testCode],
    );
  }

  return (
    <Card>
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        disabled={!assessmentId}
        className="flex w-full items-center justify-between gap-2 text-left disabled:cursor-not-allowed disabled:opacity-50"
      >
        <span>
          <span className="block text-base font-semibold text-neutral-900">
            {t('studentProfile.answers.title')}
          </span>
          <span className="block text-xs text-neutral-500">
            {t('studentProfile.answers.description')}
          </span>
        </span>
        <ChevronDown
          size={18}
          aria-hidden="true"
          className={open ? 'shrink-0 rotate-180 text-neutral-500' : 'shrink-0 text-neutral-500'}
        />
      </button>

      {open && (
        <div className="mt-4 flex flex-col gap-4">
          {answersQuery.isPending && (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-8 w-full" />
              <Skeleton className="h-8 w-full" />
              <Skeleton className="h-8 w-full" />
            </div>
          )}

          {answersQuery.isError && (
            <ErrorState
              description={t('studentProfile.answers.loadError')}
              onRetry={() => void answersQuery.refetch()}
            />
          )}

          {answersQuery.isSuccess && data && data.answers.length === 0 && (
            <EmptyState title={t('studentProfile.answers.empty')} />
          )}

          {answersQuery.isSuccess && data && data.answers.length > 0 && (
            <>
              <SessionSignals
                answeredCount={data.session.answeredCount}
                fastAnswerCount={data.session.fastAnswerCount}
                straightLiningBlockCount={data.session.straightLiningBlockCount}
                allSameAnswer={data.session.allSameAnswer}
                shortSession={data.session.shortSession}
                reliabilityScore={data.session.reliabilityScore ?? null}
                thresholds={data.thresholds}
              />

              {data.scales.length > 0 && <ScaleSignals scales={data.scales} />}

              <div className="flex flex-wrap items-end gap-3">
                <Select
                  label={t('studentProfile.answers.testLabel')}
                  value={testFilter}
                  onChange={(event) => setTestFilter(event.target.value)}
                  options={[
                    { value: ALL, label: t('studentProfile.answers.allTests') },
                    ...testOptions.map((code) => ({ value: code, label: code })),
                  ]}
                />
                <Select
                  label={t('studentProfile.answers.scaleLabel')}
                  value={scaleFilter}
                  onChange={(event) => setScaleFilter(event.target.value)}
                  options={[
                    { value: ALL, label: t('studentProfile.answers.allScales') },
                    ...scaleOptions.map((scale) => ({ value: scale.code, label: scale.label })),
                  ]}
                />
                <Checkbox
                  label={t('studentProfile.answers.onlyFlagged')}
                  checked={onlyFlagged}
                  onChange={(event) => setOnlyFlagged(event.target.checked)}
                />
              </div>

              {groups.length === 0 && <EmptyState title={t('studentProfile.answers.noMatches')} />}

              {groups.map((group) => {
                const expanded = expandedTests.includes(group.testCode);
                return (
                  <div key={group.testCode} className="rounded-xl border border-neutral-200">
                    <button
                      type="button"
                      onClick={() => toggleTest(group.testCode)}
                      aria-expanded={expanded}
                      className="flex w-full items-center justify-between gap-2 px-4 py-3 text-left"
                    >
                      <span className="text-sm font-medium text-neutral-900">
                        {group.testCode}{' '}
                        <span className="font-normal text-neutral-500">
                          {t('studentProfile.answers.groupCount', { count: group.answers.length })}
                        </span>
                      </span>
                      <ChevronDown
                        size={16}
                        aria-hidden="true"
                        className={expanded ? 'rotate-180 text-neutral-500' : 'text-neutral-500'}
                      />
                    </button>

                    {/* Yopiq blok qatorlari UMUMAN render qilinmaydi — 190 savolli sessiyada
                        sahifa og'irlashmasligining asosiy sababi shu. */}
                    {expanded && (
                      <AnswersTable
                        answers={group.answers}
                        conflictScales={conflictScales}
                        fastAnswerDurationMs={data.thresholds.fastAnswerDurationMs}
                      />
                    )}
                  </div>
                );
              })}
            </>
          )}
        </div>
      )}
    </Card>
  );
}

/**
 * Sessiya darajasidagi signallar — "nega ishonchlilik 62" savoliga javob (`docs/03` §7).
 * Chegaralar (`900 ms`, `12`, `6 daqiqa`) matnga QO'LDA yozilmaydi: hammasi `thresholds`
 * dan, ya'ni `ScoringConstants` dan keladi.
 */
function SessionSignals({
  answeredCount,
  fastAnswerCount,
  straightLiningBlockCount,
  allSameAnswer,
  shortSession,
  reliabilityScore,
  thresholds,
}: {
  answeredCount: number;
  fastAnswerCount: number;
  straightLiningBlockCount: number;
  allSameAnswer: boolean;
  shortSession: boolean;
  reliabilityScore: number | null;
  thresholds: AnswerThresholdsDto;
}) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-2 rounded-xl bg-neutral-50 p-3">
      <p className="text-xs font-medium text-neutral-500">
        {t('studentProfile.answers.signals.heading', {
          score: reliabilityScore == null ? '—' : reliabilityScore.toFixed(0),
        })}
      </p>
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="neutral">
          {t('studentProfile.answers.signals.answered', { count: answeredCount })}
        </Badge>
        <Badge variant={fastAnswerCount > 0 ? 'warning' : 'neutral'}>
          {t('studentProfile.answers.signals.fast', {
            count: fastAnswerCount,
            ms: thresholds.fastAnswerDurationMs,
          })}
        </Badge>
        <Badge variant={straightLiningBlockCount > 0 ? 'warning' : 'neutral'}>
          {t('studentProfile.answers.signals.straightLining', {
            count: straightLiningBlockCount,
            run: thresholds.straightLiningMinRunLength,
          })}
        </Badge>
        {allSameAnswer && (
          <Badge variant="danger">{t('studentProfile.answers.signals.allSame')}</Badge>
        )}
        {shortSession && (
          <Badge variant="warning">
            {t('studentProfile.answers.signals.shortSession', {
              minutes: thresholds.shortSessionMinutes,
            })}
          </Badge>
        )}
      </div>
    </div>
  );
}

/** Teskari savol ziddiyati shkala bo'yicha — `docs/03` §7.1 band 4 (`d_shkala`). */
function ScaleSignals({ scales }: { scales: readonly AnswerScaleSignalDto[] }) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-2 rounded-xl border border-neutral-200 p-3">
      <p className="text-xs font-medium text-neutral-500">
        {t('studentProfile.answers.reverseConflict.heading')}
      </p>
      <ul className="flex flex-col gap-1">
        {scales.map((scale) => (
          <li key={scale.scale} className="text-sm text-neutral-700">
            <span className="font-medium text-neutral-900">{scale.scaleNameUz ?? scale.scale}</span>{' '}
            <span className="text-xs text-neutral-500">({scale.scale})</span> —{' '}
            {t('studentProfile.answers.reverseConflict.row', {
              forward: scale.forwardAvgPct.toFixed(0),
              reverse: scale.reverseAvgPct.toFixed(0),
              mismatch: scale.mismatchPct.toFixed(0),
            })}
          </li>
        ))}
      </ul>
    </div>
  );
}

/** Bitta test blokining jadvali. */
function AnswersTable({
  answers,
  conflictScales,
  fastAnswerDurationMs,
}: {
  answers: readonly RawAnswerDto[];
  conflictScales: ReadonlySet<string>;
  fastAnswerDurationMs: number;
}) {
  const { t } = useTranslation();

  return (
    <Table aria-label={t('studentProfile.answers.title')}>
      <TableHeader>
        <TableRow>
          <TableHead>{t('studentProfile.answers.questionColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.answerColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.scaleColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.directionColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.effectiveColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.durationColumn')}</TableHead>
          <TableHead>{t('studentProfile.answers.revisionColumn')}</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {answers.map((answer) => (
          <TableRow
            key={answer.questionCode}
            className={answer.straightLiningBlockIndex == null ? undefined : 'bg-warning-50'}
          >
            <TableCell>
              <span className="block">{answer.questionText}</span>
              <span className="block text-xs text-neutral-500">{answer.questionCode}</span>
            </TableCell>
            <TableCell>
              <AnswerLabel answer={answer} />
            </TableCell>
            <TableCell>
              <span className="block">{answer.scaleNameUz ?? answer.scale}</span>
              <span className="block text-xs text-neutral-500">
                {answer.scale}
                {conflictScales.has(answer.scale) && (
                  <span className="ml-1 text-warning-700">
                    {t('studentProfile.answers.reverseConflict.marker')}
                  </span>
                )}
              </span>
            </TableCell>
            <TableCell>
              {answer.scaleDirection < 0 ? (
                <Badge variant="warning">{t('studentProfile.answers.direction.reverse')}</Badge>
              ) : (
                <span className="text-neutral-500">
                  {t('studentProfile.answers.direction.forward')}
                </span>
              )}
            </TableCell>
            <TableCell>
              <span className="font-semibold text-neutral-900">{answer.effectiveValue}</span>
              {answer.scaleDirection < 0 && (
                <span className="block text-xs text-neutral-500">
                  {t('studentProfile.answers.effectiveHint', { raw: answer.rawValue })}
                </span>
              )}
            </TableCell>
            <TableCell>
              <span className="whitespace-nowrap">
                {t('studentProfile.answers.durationMs', { count: answer.durationMs })}
              </span>
              {answer.isFastAnswer && (
                <span className="mt-1 flex items-center gap-1 whitespace-nowrap text-xs text-warning-700">
                  <Zap size={12} aria-hidden="true" />
                  {t('studentProfile.answers.fastMarker', { ms: fastAnswerDurationMs })}
                </span>
              )}
              {answer.straightLiningBlockIndex != null && (
                <span className="mt-1 flex items-center gap-1 whitespace-nowrap text-xs text-warning-700">
                  <AlertTriangle size={12} aria-hidden="true" />
                  {t('studentProfile.answers.straightLiningMarker', {
                    index: answer.straightLiningBlockIndex,
                  })}
                </span>
              )}
            </TableCell>
            <TableCell>{answer.revisionCount}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

/**
 * Javob SO'Z bilan. `SingleChoice` da variant matni, `Likert5` da `docs/03` §1 yorlig'i;
 * qolgan turlarda xom qiymat (hujjatda yorliq jadvali yo'q — soxta yorliq berilmaydi).
 */
function AnswerLabel({ answer }: { answer: RawAnswerDto }) {
  const { t } = useTranslation();

  if (answer.selectedOptionText) {
    return <span>{answer.selectedOptionText}</span>;
  }

  const label =
    answer.questionType === 'Likert5' && isLikert5Value(answer.rawValue)
      ? t(`studentProfile.answers.likert5.${answer.rawValue}`)
      : null;

  return (
    <span>
      {label ?? answer.rawValue}
      {label !== null && (
        <span className="block text-xs text-neutral-500">{answer.rawValue}</span>
      )}
    </span>
  );
}
