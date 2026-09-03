import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, XAxis, YAxis } from 'recharts';
import { usePrefersReducedMotion } from '@/shared/hooks/usePrefersReducedMotion';
import { VisuallyHidden } from '@/shared/ui/VisuallyHidden';

/**
 * RIASEC 6 tipi — **Holland harflari** `R I A S E C` (docs/03, 4.1-bo'lim: "matnda qisqalik
 * uchun R-I-A-S-E-C harflari"; docs/07, 3.2-bo'lim `RIASEC.types` kalitlari). Bu bazadagi
 * `scale` kodlari (`R, I, ART, SOC, ENT, CONV`) EMAS — API `scale` kodlarini hech qachon
 * chiqarmaydi (CLAUDE.md 9-band), ularni `StudentProfileMapping` harfga o'giradi.
 */
export type RiasecTypeCode = 'R' | 'I' | 'A' | 'S' | 'E' | 'C';

export interface RiasecChartProps {
  types: Record<RiasecTypeCode, number>;
  /** Holland kodi (eng yuqori 3 tip) — docs/03, 4.2-bo'lim, masalan `"IRA"`. */
  resultCode: string;
  /** `max(typePct) − min(typePct)` — docs/03, 4.2-bo'lim. */
  differentiation: number;
  className?: string;
}

/** Holland olti burchagi tartibi (docs/03, 4.2-bo'lim). */
const TYPE_ORDER: RiasecTypeCode[] = ['R', 'I', 'A', 'S', 'E', 'C'];

/**
 * Harf → i18n kaliti. Tarjima kalitlari (`widgets.riasecChart.type.*`) `scale` kodlari bilan
 * nomlangan, API esa harf bilan keladi — moslama shu yerda, bitta joyda.
 */
const TYPE_LABEL_KEY: Record<RiasecTypeCode, string> = {
  R: 'R',
  I: 'I',
  A: 'ART',
  S: 'SOC',
  E: 'ENT',
  C: 'CONV',
};

/** Differensiatsiya < 20 — docs/03, 4.2-bo'lim: "qiziqishlar hali aniq shakllanmagan". */
const LOW_DIFFERENTIATION_THRESHOLD = 20;

/**
 * Kasb qiziqishlari (RIASEC) 6 ustunli bar — docs/11 A-5, P26 3-band. Top-3 (Holland kodi
 * harflari) asosiy tusda, qolganlari yumshoqroq neytral tusda ajratiladi — bu FARQLASH,
 * ballni "yaxshi/yomon" deb baholash emas (docs/11, 1-bo'lim).
 */
export function RiasecChart({ types, resultCode, differentiation, className }: RiasecChartProps) {
  const { t } = useTranslation();
  const titleId = useId();
  const reducedMotion = usePrefersReducedMotion();
  const topLetters = new Set(resultCode.split(''));

  const chartData = TYPE_ORDER.map((code) => ({
    code,
    letter: code,
    label: t(`widgets.riasecChart.type.${TYPE_LABEL_KEY[code]}`),
    value: types[code],
    isTop: topLetters.has(code),
  }));

  const ariaLabel = t('widgets.riasecChart.ariaLabel', {
    values: chartData.map((row) => `${row.label} ${row.value.toFixed(1)}%`).join(', '),
    resultCode,
  });

  return (
    <div className={className}>
      <h4 id={titleId} className="sr-only">
        {t('widgets.riasecChart.title')}
      </h4>
      <div role="img" aria-label={ariaLabel} className="h-56 w-full min-w-0">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--color-neutral-200)" />
            <XAxis
              dataKey="letter"
              tick={{ fill: 'var(--color-neutral-600)', fontSize: 12 }}
              axisLine={{ stroke: 'var(--color-neutral-300)' }}
              tickLine={false}
            />
            <YAxis
              domain={[0, 100]}
              tick={{ fill: 'var(--color-neutral-400)', fontSize: 10 }}
              axisLine={false}
              tickLine={false}
              width={28}
            />
            <Bar dataKey="value" radius={[4, 4, 0, 0]} isAnimationActive={!reducedMotion}>
              {chartData.map((row) => (
                <Cell
                  key={row.code}
                  fill={row.isTop ? 'var(--color-primary-500)' : 'var(--color-neutral-300)'}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      <dl
        data-testid="riasec-chart-summary"
        className="mt-3 grid grid-cols-3 gap-x-4 gap-y-2 text-sm sm:grid-cols-6"
      >
        {chartData.map((row) => (
          <div key={row.code} className="flex flex-col">
            <dt className={row.isTop ? 'font-semibold text-neutral-900' : 'text-neutral-500'}>
              {row.label}
            </dt>
            <dd className={row.isTop ? 'font-semibold text-neutral-900' : 'text-neutral-700'}>
              {row.value.toFixed(1)}%
            </dd>
          </div>
        ))}
      </dl>

      <p data-testid="riasec-chart-code" className="mt-3 text-sm text-neutral-700">
        <span className="font-semibold">{t('widgets.riasecChart.hollandCodeLabel')}</span>{' '}
        {resultCode}
        {' — '}
        {differentiation < LOW_DIFFERENTIATION_THRESHOLD
          ? t('widgets.riasecChart.lowDifferentiation')
          : t('widgets.riasecChart.differentiationValue', { value: differentiation.toFixed(1) })}
      </p>

      <VisuallyHidden>
        <table data-testid="riasec-chart-table">
          <caption>{ariaLabel}</caption>
          <thead>
            <tr>
              <th scope="col">{t('widgets.riasecChart.tableTypeColumn')}</th>
              <th scope="col">{t('widgets.riasecChart.tableValueColumn')}</th>
            </tr>
          </thead>
          <tbody>
            {chartData.map((row) => (
              <tr key={row.code}>
                <td>{row.label}</td>
                <td>{row.value.toFixed(1)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </VisuallyHidden>
    </div>
  );
}
