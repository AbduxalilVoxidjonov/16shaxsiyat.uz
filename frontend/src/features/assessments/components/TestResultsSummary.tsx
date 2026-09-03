import { useTranslation } from 'react-i18next';
import { Badge } from '@/shared/ui/Badge';
import { Card } from '@/shared/ui/Card';
import { EmptyState } from '@/shared/ui/EmptyState';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/Table';
import type { TestSummaryRow } from '../model/testSummary';

export interface TestResultsSummaryProps {
  rows: TestSummaryRow[];
}

/** Bitta qatorning natija ustuni — holatga qarab ball, "ballanmaydi" yoki "ma'lumot yo'q". */
function ResultCell({ row }: { row: TestSummaryRow }) {
  const { t } = useTranslation();

  if (row.state === 'notScored') {
    // `Survey` anketasi: ball UMUMAN yozilmaydi. `0` ko'rsatish yolg'on bo'lardi.
    return <span className="text-neutral-500">{t('assessmentDetail.tests.notScored')}</span>;
  }

  if (row.state === 'noData') {
    return <span className="text-neutral-400">{t('assessmentDetail.tests.noData')}</span>;
  }

  // Nom asosiy, kod ikkinchi darajali (egasining 2026-09-03 talabi: "qisqartirib yozilgan
  // 16 ta shaxsiyatni to'liq nomi bilan chiqar"). Ilgari `"INTJ · Loyihachi"` bo'lib, kod
  // birinchi va ikkalasi bir xil vaznda edi — 4 harfli kod o'zi hech narsa anglatmaydi.
  // Kod baribir qoldiriladi: u eksport, filtr va tip katalogida ishlatiladi.
  // `ART`/`Artistik` uchun katalogda qo'llangan naqshning aynan o'zi.
  const primaryLabel = row.resultName ?? row.resultCode;
  const secondaryCode = row.resultName !== null ? row.resultCode : null;

  return (
    <span className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
      {primaryLabel !== null && (
        <span className="font-medium text-neutral-900">{primaryLabel}</span>
      )}
      {secondaryCode !== null && <span className="text-xs text-neutral-400">{secondaryCode}</span>}
      {row.indexKind !== null &&
        (row.index !== null ? (
          <span className="text-neutral-700">
            {t(`assessmentDetail.tests.index.${row.indexKind}`, { value: row.index.toFixed(1) })}
          </span>
        ) : (
          // Ball hisoblangan, lekin yig'ma indeks yo'q (masalan BIG5 bor, ACTIVITY yo'q —
          // `MaturityIndex` faqat ikkalasi birga bo'lganda ma'noga ega).
          <span className="text-neutral-400">{t('assessmentDetail.tests.indexUnknown')}</span>
        ))}
      {primaryLabel === null && row.indexKind === null && (
        <span className="text-neutral-400">{t('assessmentDetail.tests.noData')}</span>
      )}
    </span>
  );
}

function StateBadge({ state }: { state: TestSummaryRow['state'] }) {
  const { t } = useTranslation();
  const variant = state === 'scored' ? 'success' : state === 'notScored' ? 'primary' : 'neutral';
  return <Badge variant={variant}>{t(`assessmentDetail.tests.state.${state}`)}</Badge>;
}

/**
 * Har bir test bo'yicha qisqa xulosa — `docs/11` A-6. `Survey` rejimidagi anketalar
 * BALLANMAYDI va shu holda aniq shunday yoziladi (`docs/06` qarorlar jurnali, 2026-09-02).
 */
export function TestResultsSummary({ rows }: TestResultsSummaryProps) {
  const { t } = useTranslation();

  return (
    <Card title={t('assessmentDetail.tests.heading')}>
      {rows.length === 0 ? (
        <EmptyState
          title={t('assessmentDetail.tests.emptyTitle')}
          description={t('assessmentDetail.tests.emptyDescription')}
        />
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('assessmentDetail.tests.columnTest')}</TableHead>
              <TableHead>{t('assessmentDetail.tests.columnState')}</TableHead>
              <TableHead>{t('assessmentDetail.tests.columnResult')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.testCode}>
                <TableCell>
                  <span className="font-medium">
                    {row.name ??
                      t(`assessmentDetail.tests.names.${row.testCode}`, {
                        defaultValue: row.testCode,
                      })}
                  </span>
                  <span className="ml-2 text-xs text-neutral-500">{row.testCode}</span>
                </TableCell>
                <TableCell>
                  <StateBadge state={row.state} />
                </TableCell>
                <TableCell>
                  <ResultCell row={row} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  );
}
