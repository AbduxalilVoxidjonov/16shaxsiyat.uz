import { useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Zap } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import type { AnswerScoringMode, RawAnswerDto } from '@/shared/api/assessmentAnswersTypes';
import { useRawAnswersQuery } from './useRawAnswersQuery';

export interface AnswersSectionProps {
  assessmentId: string | null;
  /**
   * Test kodi → katalogdagi nomi (`nameUz`) — ZAXIRA. Asosiy manba javobning o'zidagi
   * `testNameUz` (backend, 2026-09-23); u bo'lmasa shu xarita, u ham bo'lmasa xom kod.
   */
  testNames?: Readonly<Record<string, string>>;
}

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
  /** Katalogdagi test nomi — backend `testNameUz` (2026-09-23); eski javobda yo'q bo'lishi mumkin. */
  testNameUz: string | null;
  scoringMode: AnswerScoringMode;
  answers: RawAnswerDto[];
}

/**
 * Savolma-savol javoblar — `docs/11` A-5 profil sahifasidagi BO'LIM va A-6 sessiya detali
 * sahifasi (P52-A).
 *
 * `widgets/`da turadi (feature EMAS): `features/students` va `features/assessments`
 * ikkalasi ham shu bo'limni ochadi, `docs/10` §2 esa feature'lararo importni taqiqlaydi.
 *
 * <b>Ko'rinish (egasining talabi, 2026-09-23).</b> Bo'lim va test bloklari YIG'ILMAGAN —
 * o'quvchi topshirgan har bir test (sessiya tartibida) o'z katalog nomi sarlavhasi ostida
 * tagma-tag chiqadi, hech narsani bosib ochish shart emas. Ilgari bo'lim boshida turgan
 * ishonchlilik signallari, teskari savol mosligi bloki va "Test bloki"/"Shkala"/"Faqat
 * belgilanganlar" filtrlari olib tashlandi: ular javoblarni sahifaning "ichkarisiga" surib
 * qo'yardi. Ishonchlilik balli backendda hisoblanishda davom etadi va profil sarlavhasi /
 * sessiya detalidagi ishonchlilik kartasida ko'rinadi; qator darajasidagi belgilar (tez
 * javob, bir xil javob bloki, teskari savol farqi) jadvalda qoladi.
 *
 * <b>Nima uchun AI talqini YO'Q.</b> Har bir savolni alohida AI bilan izohlash TAQIQLANGAN
 * (`CLAUDE.md` 6-band): yakka Likert javobidan xulosa chiqarish noto'g'ri xulosaga olib
 * boradi. Shu ma'lumot AI promptiga ham qo'shilmaydi (`CLAUDE.md` 5-band).
 *
 * <b>So'rovnoma (`scoringMode === 'Survey'`) bloklari</b> — Likert ustunlari BU BLOKLARDA
 * ma'nosiz (`docs/18`), shu sabab `AnswersTable` `scoringMode`ga qarab IKKI xil jadval
 * chizadi. Jadval `Table` ning `overflow-x` konteyneri ichida (P30-6): 390px da faqat
 * jadval siljiydi, sahifa emas.
 */
export function AnswersSection({ assessmentId, testNames }: AnswersSectionProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <h3 className="text-base font-semibold text-neutral-900">
        {t('studentProfile.answers.title')}
      </h3>
      <p className="text-xs text-neutral-500">{t('studentProfile.answers.description')}</p>

      <div className="mt-4">
        <AnswersBody assessmentId={assessmentId} testNames={testNames} />
      </div>
    </Card>
  );
}

export interface AnswersBodyProps extends AnswersSectionProps {
  /**
   * Test bloki sarlavhasining darajasi. `AnswersSection` ichida `h4` (karta sarlavhasi `h3`);
   * profildagi bir nechta urinish ro'yxatida urinish sarlavhasi `h4` bo'lgani uchun `h5`
   * (`axe` `heading-order`).
   */
  groupHeadingLevel?: 'h4' | 'h5';
}

/**
 * Bitta sessiyaning savolma-savol javoblari — sarlavhasiz tana. `AnswersSection` (bitta
 * sessiya) va profildagi urinishlar ro'yxati (`features/students` — har bir yig'iladigan
 * urinish ichida) ikkalasi ham shuni chizadi. Faqat MOUNT bo'lganda so'rov yuboradi:
 * yig'ilgan urinish tanasi umuman render qilinmaydi, ya'ni ortiqcha so'rov ham yo'q.
 */
export function AnswersBody({
  assessmentId,
  testNames,
  groupHeadingLevel: GroupHeading = 'h4',
}: AnswersBodyProps) {
  const { t } = useTranslation();
  const headingIdPrefix = useId();

  const answersQuery = useRawAnswersQuery(assessmentId ?? undefined);
  const data = answersQuery.data;

  const groups = useMemo<AnswerGroup[]>(() => {
    if (!data) return [];
    // Backend javoblarni sessiya ichidagi test tartibi, so'ng savol tartibi bo'yicha
    // qaytaradi — `Map` qo'shilish tartibini saqlaydi, ya'ni bloklar ham shu tartibda.
    const byTest = new Map<string, RawAnswerDto[]>();
    for (const answer of data.answers) {
      const bucket = byTest.get(answer.testCode);
      if (bucket) {
        bucket.push(answer);
      } else {
        byTest.set(answer.testCode, [answer]);
      }
    }
    // `scoringMode` bitta test bloki ichida BIR XIL — birinchi javobdan olinadi, guruh bo'sh
    // bo'lmaydi (Map kaliti faqat javob qo'shilganda yaratiladi).
    return [...byTest.entries()].map(([testCode, answers]) => ({
      testCode,
      testNameUz: answers[0]?.testNameUz ?? null,
      scoringMode: answers[0]?.scoringMode ?? 'Scored',
      answers,
    }));
  }, [data]);

  // Ziddiyatli shkalalar — `docs/03` §7.1 band 4. Jadvaldagi "Shkala" ustunida belgilanadi.
  const conflictScales = useMemo(
    () => new Set((data?.scales ?? []).map((scale) => scale.scale)),
    [data],
  );

  return (
    <div className="flex flex-col gap-4">
        {!assessmentId && <EmptyState title={t('studentProfile.answers.empty')} />}

        {assessmentId && answersQuery.isPending && (
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

        {answersQuery.isSuccess && groups.length === 0 && (
          <EmptyState title={t('studentProfile.answers.empty')} />
        )}

        {answersQuery.isSuccess &&
          data &&
          groups.map((group) => (
            <section
              key={group.testCode}
              aria-labelledby={`${headingIdPrefix}-${group.testCode}`}
              className="flex flex-col gap-2"
            >
              <GroupHeading
                id={`${headingIdPrefix}-${group.testCode}`}
                className="text-sm font-semibold text-neutral-900"
              >
                {group.testNameUz ?? testNames?.[group.testCode] ?? group.testCode}{' '}
                <span className="font-normal text-neutral-500">
                  {t('studentProfile.answers.groupCount', { count: group.answers.length })}
                </span>
              </GroupHeading>
              <AnswersTable
                answers={group.answers}
                scoringMode={group.scoringMode}
                conflictScales={conflictScales}
                fastAnswerDurationMs={data.thresholds.fastAnswerDurationMs}
              />
            </section>
          ))}
    </div>
  );
}

/**
 * Bitta test blokining jadvali. `Survey` (ballanmaydigan so'rovnoma) bloklari uchun ATAYLAB
 * ALOHIDA, SODDA jadval — `docs/18` talabiga ko'ra Likert ustunlari (shkala, yo'nalish,
 * samarali qiymat, tez javob, straight-lining) bu yerda ma'nosiz. `Scored` blok markupi
 * (pastda) P52-B DAN OLDINGI holat bilan BAYT-BAYT bir xil qoldirilgan — regressiya.
 */
function AnswersTable({
  answers,
  scoringMode,
  conflictScales,
  fastAnswerDurationMs,
}: {
  answers: readonly RawAnswerDto[];
  scoringMode: AnswerScoringMode;
  conflictScales: ReadonlySet<string>;
  fastAnswerDurationMs: number;
}) {
  const { t } = useTranslation();

  if (scoringMode === 'Survey') {
    return (
      <Table aria-label={t('studentProfile.answers.title')}>
        <TableHeader>
          <TableRow>
            <TableHead>{t('studentProfile.answers.questionColumn')}</TableHead>
            <TableHead>{t('studentProfile.answers.answerColumn')}</TableHead>
            <TableHead>{t('studentProfile.answers.durationColumn')}</TableHead>
            <TableHead>{t('studentProfile.answers.revisionColumn')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {answers.map((answer) => (
            <TableRow key={answer.questionCode}>
              <TableCell>
                <span className="block">{answer.questionText}</span>
                <span className="block text-xs text-neutral-500">{answer.questionCode}</span>
              </TableCell>
              <TableCell>
                <AnswerValue answer={answer} />
              </TableCell>
              <TableCell className="whitespace-nowrap">
                {t('studentProfile.answers.durationMs', { count: answer.durationMs })}
              </TableCell>
              <TableCell>{answer.revisionCount}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    );
  }

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
              <AnswerValue answer={answer} />
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
              <span className="font-semibold text-neutral-900">
                {answer.effectiveValue ?? '—'}
              </span>
              {answer.scaleDirection < 0 && answer.effectiveValue !== null && (
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

/** Uzun matn javobi (`LongText`, 1000 belgigacha) — jadvalni buzmasdan qisqartirib ko'rsatish. */
const LONG_TEXT_PREVIEW_LENGTH = 160;

function TextAnswerValue({ text }: { text: string | null }) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);

  if (text === null || text === '') {
    return <span>—</span>;
  }

  const isLong = text.length > LONG_TEXT_PREVIEW_LENGTH;
  const shown = !isLong || expanded ? text : `${text.slice(0, LONG_TEXT_PREVIEW_LENGTH)}…`;

  return (
    <span className="block max-w-xs">
      <span className="block whitespace-pre-wrap break-words">{shown}</span>
      {isLong && (
        <button
          type="button"
          onClick={() => setExpanded((value) => !value)}
          className="mt-1 text-xs font-medium text-primary-700 underline underline-offset-2 hover:text-primary-800"
        >
          {expanded
            ? t('studentProfile.answers.collapseText')
            : t('studentProfile.answers.showFullText')}
        </button>
      )}
    </span>
  );
}

/**
 * Javob SO'Z bilan — `docs/18` §4.2 shakli bo'yicha to'rt yo'l:
 *
 * 1. `MultiChoice` → `selectedOptionTexts` VERGUL bilan (`selectedValues` xom raqamlari
 *    ASOSIY qiymat sifatida KO'RSATILMAYDI — egasi topgan kamchilik, 2026-09-12).
 * 2. `ShortText`/`LongText`/`Phone` → `textValue` (uzun matn qisqartirilib, "to'liq ko'rish"
 *    bilan).
 * 3. `SingleChoice` (va boshqa variant asosidagi turlar) → mavjud `selectedOptionText`.
 * 4. Qolgan Likert turlari → `Likert5` uchun SO'Z yorlig'i (`docs/03` §1), aks holda xom
 *    qiymat. `rawValue`/`effectiveValue` endi NULLABLE (`Survey` qatorlarda `null`) — `0`
 *    deb KO'RSATILMAYDI, `—` chiqadi.
 */
function AnswerValue({ answer }: { answer: RawAnswerDto }) {
  const { t } = useTranslation();

  if (answer.questionType === 'MultiChoice') {
    const texts = answer.selectedOptionTexts ?? [];
    return <span>{texts.length > 0 ? texts.join(', ') : '—'}</span>;
  }

  if (
    answer.questionType === 'ShortText' ||
    answer.questionType === 'LongText' ||
    answer.questionType === 'Phone'
  ) {
    return <TextAnswerValue text={answer.textValue ?? null} />;
  }

  if (answer.selectedOptionText) {
    return <span>{answer.selectedOptionText}</span>;
  }

  const label =
    answer.questionType === 'Likert5' && answer.rawValue != null && isLikert5Value(answer.rawValue)
      ? t(`studentProfile.answers.likert5.${answer.rawValue}`)
      : null;

  return (
    <span>
      {label ?? (answer.rawValue ?? '—')}
      {label !== null && (
        <span className="block text-xs text-neutral-500">{answer.rawValue}</span>
      )}
    </span>
  );
}
